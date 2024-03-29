[![Nuget](https://img.shields.io/nuget/v/Surity.Core?label=Surity.Core)](https://www.nuget.org/packages/Surity.Core)
[![Nuget](https://img.shields.io/nuget/v/Surity.CLI?label=Surity.CLI)](https://www.nuget.org/packages/Surity.CLI)
[![Nuget](https://img.shields.io/nuget/v/Surity.BepInEx?label=Surity.BepInEx)](https://www.nuget.org/packages/Surity.BepInEx)

# Surity

Unit-testing framework for Unity mods.

## Writing tests

```csharp
using Surity;
using System.Collections.Generic;

// Only test classes with Only = true are run.
// Test classes with Skip = true are skipped.
[TestClass(Only = false, Skip = false)]
public class MyTests
{
	// BeforeAll, AfterAll, BeforeEach and AfterEach can be inherited
	[BeforeAll]
	public void SetUp()
	{
		// Ran before all tests in this class
	}

	[AfterAll]
	public void TearDown()
	{
		// Ran after all tests in this class
	}

	[BeforeEach]
	public void TestSetUp()
	{
		// Ran before each test in this class
	}

	[AfterEach]
	public void TestTearDown()
	{
		// Ran after each test in this class
	}

	// Across all test classes, only tests with Only = true are run.
	// Tests with Skip = true are skipped.
	[Test(Only = false, Skip = false)]
	public void TestSomething()
	{
		// Throw an error to fail. Use your favourite assertion library
	}

	// Tests and setup methods (BeforeEach, AfterEach, etc.) can also return IEnumerator
	[Test]
	public IEnumerator TestSomething()
	{
		yield break;
	}

	// Tests can be generated programmatically. Just return the generated tests.
	// `Only` and `Skip` apply to generated tests.
	[TestGenerator(Only = false, Skip = false)]
	public IEnumerable<TestInfo> GenerateTests()
	{
		void Func1(int num) {
			// Works like normal tests
		}

		IEnumerator Func2(int num) {
			// Generated tests can also return IEnumerator, as you would expect
		}

		yield return new TestInfo("Name of test1", () => Func1(1));
		yield return new TestInfo("Name of test2", () => Func2(2));
	}
}
```

## Running tests

Tests must be run inside the game if they depend on the Unity runtime.

If you use the [BepInEx](https://github.com/BepInEx/BepInEx) modding framework, you can add the [`Surity.BepInEx`](https://www.nuget.org/packages/Surity.BepInEx) NuGet package to your test project. The package contains a BepInEx plugin which makes sure all loaded Surity tests are ran only once. The plugin only runs tests if the game was started with the Surity CLI program.

Refer to [`Surity.BepInEx.cs`](Surity.BepInEx/Surity.BepInEx.cs) on how to invoke the test runner manually. Add the [`Surity.Core`](https://www.nuget.org/packages/Surity.Core) NuGet package to your test project if you don't need `Surity.BepInEx`.

### Using Surity CLI

Run tests with the standalone [Surity.exe](https://github.com/olavim/Surity/releases/latest) executable or by installing the dotnet tool:

```
$ dotnet tool install Surity.CLI
$ dotnet surity <path-to-game-exe> [options] [-- arguments]
```

The program runs the game in [batchmode](https://docs.unity3d.com/Manual/PlayerCommandLineArguments.html) and listens for test results.

Any arguments after `--` are passed to the game. For example in

```
$ dotnet surity <path-to-game-exe> -- -nolog
```

the `-nolog` argument is passed to the game executable.