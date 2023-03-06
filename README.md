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

Run tests with the standalone `Surity.exe` executable or by installing the dotnet tool:

```
$ dotnet tool install Surity.CLI
$ dotnet surity \<path-to-game-exe\>
```

The program runs the game and listens for test results.

If you use the [BepInEx](https://github.com/BepInEx/BepInEx) modding framework, you can add the `Surity.BepInEx` NuGet package as a dependency to your test project. The `Surity.BepInEx` package is a BepInEx plugin which runs all loaded Surity tests if the game was started with `Surity.exe`.

Refer to [Surity.BepInEx.cs](Surity.BepInEx/Surity.BepInEx.cs) if you need or want to invoke the test runner manually.