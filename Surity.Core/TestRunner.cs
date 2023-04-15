using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
			IEnumerable<(Type, List<TestExecutionGroup>)> executionsByTestClass;
			var testClassInstances = new Dictionary<Type, object>();

			try
			{
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
					testClassInstances[testClass] = Activator.CreateInstance(testClass);
					var executions = GetExecutionGroups(testClass, testClassInstances[testClass]).ToList();
					testClassExecutionGroupPairs.AddRange(executions.Select(exec => (testClass, exec)));
				}

				if (testClassExecutionGroupPairs.Any(p => p.Item2.Only))
				{
					var forcedExecutions = new[] { BeforeAll, AfterAll };
					testClassExecutionGroupPairs = testClassExecutionGroupPairs.Where(p => p.Item2.Only || forcedExecutions.Contains(p.Item2.Name)).ToList();
				}

				testClassExecutionGroupPairs = testClassExecutionGroupPairs.Where(p => !p.Item2.Skip).ToList();

				executionsByTestClass = testClassExecutionGroupPairs.GroupBy(
					p => p.Item1,
					p => p.Item2,
					(testClass, executions) => (testClass, executions.ToList())
				);
			}
			catch (Exception e)
			{
				client.SendFinishMessage(e.Message);
				yield break;
			}

			foreach (var (testClass, executions) in executionsByTestClass)
			{
				DebugLog($"Test class {testClass.FullName} has {executions.Count} execution groups");
				DebugLog($"Running {executions.Count - 2} tests in {testClass.FullName}");

				for (int i = 0; i < executions.Count; i++)
				{
					var exec = executions[i];

					string id = string.Join(" -> ", exec.Steps.Select(step => $"[{step.StepType.Name.Replace("Attribute", "")}] {step.Name}"));
					DebugLog($"Running {exec.Name}: {id}");

					// Skip BeforeAll and AfterAll executions
					if (i > 0 && i < executions.Count - 1)
					{
						client.SendTestInfo(exec.Name, testClass.Name);
					}

					yield return Run(testClassInstances[testClass], exec);

					if (i > 0 && i < executions.Count - 1)
					{
						client.SendTestResult(new TestResult(exec.Name, testClass.Name, exec.Result));
					}
				}
			}

			client.SendFinishMessage();
		}

		private static IEnumerable<TestExecutionGroup> GetExecutionGroups(Type type, object instance)
		{
			var executions = new List<TestExecutionGroup>();
			var testSteps = new List<TestStepInfo>();

			foreach (var testGenerator in FindOrderedSteps<TestGeneratorAttribute>(type))
			{
				if (testGenerator.MethodInfo.ReturnType != typeof(IEnumerable<TestInfo>))
				{
					throw new Exception($"Test generator {testGenerator.Name} must return IEnumerable<TestStepInfo>");
				}

				var generatedSteps = (IEnumerable<TestInfo>) testGenerator.MethodInfo.Invoke(instance, new object[] { });
				testSteps.AddRange(generatedSteps.Select(generatedStep => new TestStepInfo(generatedStep, testGenerator)));
			}

			testSteps.AddRange(FindOrderedSteps<TestAttribute>(type));

			if (testSteps.Count == 0)
			{
				return executions;
			}

			testSteps.Sort((a, b) => a.Order.CompareTo(b.Order));

			var beforeEachSteps = FindOrderedSteps<BeforeEachAttribute>(type).OrderBy(step => step.Order);
			var afterEachSteps = FindOrderedSteps<AfterEachAttribute>(type).OrderBy(step => step.Order);
			var beforeAllSteps = FindOrderedSteps<BeforeAllAttribute>(type).OrderBy(step => step.Order);
			var afterAllSteps = FindOrderedSteps<AfterAllAttribute>(type).OrderBy(step => step.Order);

			executions.Add(new TestExecutionGroup(BeforeAll, beforeAllSteps));

			foreach (var step in testSteps)
			{
				var baseStep = step.Generator ?? step;
				var testAttribute = baseStep.MethodInfo.GetCustomAttribute<TestAttribute>();
				var testGeneratorAttribute = baseStep.MethodInfo.GetCustomAttribute<TestGeneratorAttribute>();
				bool only = testAttribute?.Only ?? testGeneratorAttribute?.Only ?? false;
				bool skip = testAttribute?.Skip ?? testGeneratorAttribute?.Skip ?? false;

				var steps = new List<TestStepInfo>();
				steps.AddRange(beforeEachSteps);
				steps.Add(step);
				steps.AddRange(afterEachSteps);
				executions.Add(new TestExecutionGroup(step.Name, steps) { Only = only, Skip = skip });
			}

			executions.Add(new TestExecutionGroup(AfterAll, afterAllSteps));

			return executions;
		}

		private static IEnumerator Run(object instance, TestExecutionGroup execution)
		{
			foreach (var step in execution.Steps)
			{
				var methodTarget = step.MethodTarget ?? instance;

				if (step.MethodInfo.ReturnType == typeof(IEnumerator))
				{
					var enumerators = new Stack<IEnumerator>();

					try
					{
						var enumerator = (IEnumerator) step.MethodInfo.Invoke(methodTarget, new object[] { });
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
						step.MethodInfo.Invoke(methodTarget, new object[] { });
					}
					catch (Exception ex)
					{
						execution.Result = ex is TargetInvocationException invocationException
							? ExecutionResult.Fail(invocationException.GetBaseException())
							: ExecutionResult.Fail(ex);
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
					.ToList();

				currentType = currentType.BaseType;
				currentDepth++;
			}

			return methodsByDepth.Keys
				.Select(depth => (depth, methodsByDepth[depth]))
				.SelectMany(pair => pair.Item2.Select(m =>
					new TestStepInfo(
						name: m.Name,
						stepType: typeof(T),
						testClassType: testClassType,
						methodInfo: m,
						order: new TestStepInfo.StepOrder(pair.depth, m.GetCustomAttribute<T>().Order)
					)
				))
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
