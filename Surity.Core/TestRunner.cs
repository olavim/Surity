using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Surity
{
	public static class TestRunner
	{
		private const string BeforeAll = "$BeforeAll";
		private const string AfterAll = "$AfterAll";

		public static bool IsTestClient => Environment.GetCommandLineArgs().Any(arg => arg == "-runSurityTests");

		public static IEnumerator RunTestsAndExit()
		{
			using (var client = new AdapterClient())
			{
				yield return RunTestsAndExit(client);
			}
		}

		public static IEnumerator RunTestsAndExit(AdapterClient client)
		{
			yield return DiscoverAndRun(client);
		}

		private static IEnumerator DiscoverAndRun(AdapterClient client)
		{
			DebugLog("Test discovery started");

			var testClasses = GetTestClasses();

			var testClassesWithOnly = testClasses.Where(t => t.GetCustomAttribute<TestClassAttribute>().Only);

			if (testClassesWithOnly.Any())
			{
				testClasses = testClassesWithOnly.ToArray();
			}

			DebugLog($"Found {testClasses.Length} test classes");

			var testClassExecutionGroupPairs = new List<(Type, TestExecutionGroup)>();

			foreach (var testClass in testClasses)
			{
				var executions = GetExecutionGroups(testClass).ToList();
				testClassExecutionGroupPairs.AddRange(executions.Select(exec => (testClass, exec)));
			}

			if (testClassExecutionGroupPairs.Any(p => p.Item2.Only))
			{
				var forcedExecutions = new[] { BeforeAll, AfterAll };
				testClassExecutionGroupPairs = testClassExecutionGroupPairs.Where(p => p.Item2.Only || forcedExecutions.Contains(p.Item2.Name)).ToList();
			}

			testClassExecutionGroupPairs = testClassExecutionGroupPairs.Where(p => !p.Item2.Skip).ToList();

			var executionsByTestClass = testClassExecutionGroupPairs.GroupBy(
				p => p.Item1,
				p => p.Item2,
				(testClass, executions) => (testClass, executions.ToList())
			);

			foreach (var (testClass, executions) in executionsByTestClass)
			{
				DebugLog($"Test class {testClass.FullName} has {executions.Count} execution groups");
				DebugLog($"Running {executions.Count - 2} tests in {testClass.FullName}");

				var instance = Activator.CreateInstance(testClass);

				for (int i = 0; i < executions.Count; i++)
				{
					var exec = executions[i];

					string id = string.Join(" -> ", exec.Steps.Select(step => $"[{step.StepType.Name.Replace("Attribute", "")}] {step.Name}"));
					DebugLog($"Running {exec.Name}: {id}");

					var testInfo = new TestInfo(testClass.Name, exec.Name);

					// Skip BeforeAll and AfterAll executions
					if (i > 0 && i < executions.Count - 1)
					{
						client.SendTestInfo(testInfo);
					}

					yield return Run(instance, exec);

					if (i > 0 && i < executions.Count - 1)
					{
						client.SendTestResult(new TestResult(testInfo, exec.Result));
					}
				}
			}

			client.SendFinishMessage();
		}

		private static IEnumerable<TestExecutionGroup> GetExecutionGroups(Type type)
		{
			var executions = new List<TestExecutionGroup>();

			var testSteps = FindOrderedSteps<TestAttribute>(type);

			if (testSteps.Length == 0)
			{
				return executions;
			}

			var beforeEachSteps = FindOrderedSteps<BeforeEachAttribute>(type);
			var afterEachSteps = FindOrderedSteps<AfterEachAttribute>(type);
			var beforeAllSteps = FindOrderedSteps<BeforeAllAttribute>(type);
			var afterAllSteps = FindOrderedSteps<AfterAllAttribute>(type);

			executions.Add(new TestExecutionGroup(BeforeAll, beforeAllSteps));

			foreach (var testStep in testSteps)
			{
				var testAttribute = testStep.MethodInfo.GetCustomAttribute<TestAttribute>();
				var steps = new List<TestStepInfo>();
				steps.AddRange(beforeEachSteps);
				steps.Add(testStep);
				steps.AddRange(afterEachSteps);
				executions.Add(new TestExecutionGroup(testStep.Name, steps) { Only = testAttribute.Only, Skip = testAttribute.Skip });
			}

			executions.Add(new TestExecutionGroup(AfterAll, afterAllSteps));

			return executions;
		}

		private static IEnumerator Run(object instance, TestExecutionGroup execution)
		{
			foreach (var step in execution.Steps)
			{
				if (step.MethodInfo.ReturnType == typeof(IEnumerator))
				{
					var enumerators = new Stack<IEnumerator>();

					try
					{
						var enumerator = (IEnumerator) step.MethodInfo.Invoke(instance, new object[] { });
						enumerators.Push(enumerator);
					}
					catch (TargetInvocationException e)
					{
						execution.Result = ExecutionResult.Fail(e.GetBaseException());
						yield break;
					}

					while (enumerators.Count > 0)
					{
						try
						{
							if (!enumerators.Peek().MoveNext())
							{
								enumerators.Pop();
								continue;
							}
						}
						catch (Exception ex)
						{
							execution.Result = ExecutionResult.Fail(ex);
							yield break;
						}

						if (enumerators.Peek().Current is IEnumerator innerEnumerator)
						{
							enumerators.Push(innerEnumerator);
						}

						yield return null;
					}
				}
				else
				{
					try
					{
						step.MethodInfo.Invoke(instance, new object[] { });
					}
					catch (TargetInvocationException e)
					{
						execution.Result = ExecutionResult.Fail(e.GetBaseException());
						yield break;
					}
				}
			}

			execution.Result = ExecutionResult.Pass();
		}

		private static TestStepInfo[] FindOrderedSteps<T>(Type testClassType) where T : Attribute, IOrdered
		{
			var methodsByDepth = new Dictionary<int, List<MethodInfo>>();

			int currentDepth = 0;
			var currentType = testClassType;
			while (currentType != null)
			{
				methodsByDepth[currentDepth] = currentType
					.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
					.Where(m => m.GetCustomAttribute<T>() != null)
					.OrderBy(m => m.GetCustomAttribute<T>().Order)
					.ToList();

				currentType = currentType.BaseType;
				currentDepth++;
			}

			return methodsByDepth.Keys
				.OrderByDescending(k => k)
				.SelectMany(k => methodsByDepth[k])
				.Select(m => new TestStepInfo(typeof(T), m.Name, testClassType, m))
				.ToArray();
		}

		private static Type[] GetTestClasses()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var types = new List<Type>();

			foreach (var assembly in assemblies)
			{
				try
				{
					types.AddRange(assembly.GetTypes());
				}
				catch (Exception ex)
				{
					if (ex is ReflectionTypeLoadException reflectionEx)
					{
						foreach (var loaderEx in reflectionEx.LoaderExceptions)
						{
							UnityEngine.Debug.LogException(loaderEx);
						}

						types.AddRange(reflectionEx.Types.Where(t => t != null));
					}
					else
					{
						UnityEngine.Debug.LogException(ex);
					}
				}
			}

			return types.Where(t => t.GetCustomAttribute<TestClassAttribute>()?.Skip == false).ToArray();
		}

		private static void DebugLog(string message)
		{
			UnityEngine.Debug.Log($"[Surity] {message}");
		}
	}
}
