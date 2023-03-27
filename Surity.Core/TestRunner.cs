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

			var testClassesWithOnly = testClasses.Where(t => t.GetCustomAttribute<TestClass>().only);

			if (testClassesWithOnly.Any())
			{
				testClasses = testClassesWithOnly.ToArray();
			}

			DebugLog($"Found {testClasses.Length} test classes");

			foreach (var testClass in testClasses)
			{
				var executions = GetExecutionGroups(testClass).ToList();

				DebugLog($"Test class {testClass.FullName} has {executions.Count} execution groups");

				if (executions.Count == 0)
				{
					continue;
				}

				DebugLog($"Running {executions.Count - 2} tests in {testClass.FullName}");

				var instance = Activator.CreateInstance(testClass);

				for (int i = 0; i < executions.Count; i++)
				{
					var exec = executions[i];

					string id = string.Join(" -> ", exec.Steps.Select(step => $"[{step.StepType.Name}] {step.Name}"));
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

			var testSteps = FindSteps<Test>(type);

			if (testSteps.Length == 0)
			{
				return executions;
			}

			var beforeEachSteps = FindSteps<BeforeEach>(type);
			var afterEachSteps = FindSteps<AfterEach>(type);
			var beforeAllSteps = FindSteps<BeforeAll>(type);
			var afterAllSteps = FindSteps<AfterAll>(type);

			executions.Add(new TestExecutionGroup("Before All", beforeAllSteps));

			foreach (var testStep in testSteps)
			{
				var steps = new List<TestStepInfo>();
				steps.AddRange(beforeEachSteps);
				steps.Add(testStep);
				steps.AddRange(afterEachSteps);
				executions.Add(new TestExecutionGroup(testStep.Name, steps));
			}

			executions.Add(new TestExecutionGroup("After All", afterAllSteps));

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

		private static TestStepInfo[] FindSteps<T>(Type testClassType) where T : Attribute
		{
			return testClassType
				.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(m => m.GetCustomAttributes<T>().Any())
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

			return types.Where(t => t.GetCustomAttribute<TestClass>()?.skip == false).ToArray();
		}

		private static void DebugLog(string message)
		{
			UnityEngine.Debug.Log($"[Surity] {message}");
		}
	}
}
