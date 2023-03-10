using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Surity
{
	public class TestRunner
	{
		public static bool IsTestClient => System.Environment.GetCommandLineArgs().Any(arg => arg == "-runSurityTests");

		public static IEnumerator RunTestsAndExit()
		{
			using (var client = new AdapterClient())
			{
				var runner = new TestRunner(client);
				UnityEngine.Debug.Log("RunTestsAndExit start");
				yield return runner.DiscoverAndRun();
				UnityEngine.Debug.Log("RunTestsAndExit end");
			}
		}

		private readonly AdapterClient client;

		public TestRunner(AdapterClient client)
		{
			this.client = client;
		}

		public IEnumerator DiscoverAndRun()
		{
			this.DebugLog("Test discovery started");

			var testClasses = this.GetTestClasses();

			var testClassesWithOnly = testClasses.Where(t => t.GetCustomAttribute<TestClass>().only);

			if (testClassesWithOnly.Any())
			{
				testClasses = testClassesWithOnly.ToArray();
			}

			this.DebugLog($"Found {testClasses.Length} test classes");

			foreach (var testClass in testClasses)
			{
				var executions = this.GetExecutionGroups(testClass).ToList();

				this.DebugLog($"Test class {testClass.FullName} has {executions.Count} execution groups");

				if (executions.Count == 0)
				{
					continue;
				}

				this.DebugLog($"Running {executions.Count - 2} tests in {testClass.FullName}");

				var instance = Activator.CreateInstance(testClass);

				for (int i = 0; i < executions.Count; i++)
				{
					var exec = executions[i];

					string id = string.Join(" -> ", exec.Steps.Select(step => $"[{step.StepType.Name}] {step.Name}"));
					this.DebugLog($"Running {exec.Name}: {id}");

					yield return this.Run(instance, exec);

					// Skip BeforeAll and AfterAll executions
					if (i > 0 && i < executions.Count - 1)
					{
						var result = new TestResult()
						{
							testCategory = testClass.Name,
							testName = exec.Name,
							pass = exec.Result.pass,
							message = exec.Result.message
						};
						this.client.SendTestResult(result);
					}
				}
			}
		}

		public IEnumerable<TestExecutionGroup> GetExecutionGroups(Type type)
		{
			var executions = new List<TestExecutionGroup>();

			var testSteps = this.FindSteps<Test>(type);

			if (testSteps.Length == 0)
			{
				return executions;
			}

			var beforeEachSteps = this.FindSteps<BeforeEach>(type);
			var afterEachSteps = this.FindSteps<AfterEach>(type);
			var beforeAllSteps = this.FindSteps<BeforeAll>(type);
			var afterAllSteps = this.FindSteps<AfterAll>(type);

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

		public IEnumerator Run(object instance, TestExecutionGroup execution)
		{
			foreach (var step in execution.Steps)
			{
				if (step.MethodInfo.ReturnType == typeof(IEnumerator))
				{
					var enumerator = (IEnumerator) step.MethodInfo.Invoke(instance, new object[] { });
					bool moveNext = true;

					while (moveNext)
					{
						try
						{
							moveNext = enumerator.MoveNext();
						}
						catch (Exception e)
						{
							this.DebugLog(e.ToString());
							execution.Result = ExecutionResult.Fail(e.ToString());
							yield break;
						}

						if (moveNext)
						{
							yield return enumerator.Current;
						}
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
						execution.Result = ExecutionResult.Fail(e.GetBaseException().ToString());
						yield break;
					}
				}
			}

			execution.Result = ExecutionResult.Pass();
		}

		private TestStepInfo[] FindSteps<T>(Type testClassType) where T : Attribute
		{
			return testClassType
				.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(m => m.GetCustomAttributes<T>().Any())
				.Select(m => new TestStepInfo(typeof(T), m.Name, testClassType, m))
				.ToArray();
		}

		private Type[] GetTestClasses()
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

		private void DebugLog(string message)
		{
			UnityEngine.Debug.Log($"[Surity] {message}");
		}
	}
}
