# Surity

Unit-testing framework for Unity mods.

## Writing tests

```csharp
using Surity;

[TestClass]
public class MyTests
{
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

	[Test]
	public void TestSomething()
	{
		// Throw an error to fail. Use your favourite assertion library.
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
$ dotnet surity <path-to-game-exe> [arguments]
```

The program runs the game in [batchmode](https://docs.unity3d.com/Manual/PlayerCommandLineArguments.html) and listens for test results.

Any arguments besides the path to the game executable are passed to the game.
