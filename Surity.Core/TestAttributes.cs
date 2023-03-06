using System;

namespace Surity
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class TestClass : Attribute
	{
		public readonly bool skip;
		public readonly bool only;

		public TestClass() : this(false, false) { }

		public TestClass(bool skip = false, bool only = false)
		{
			this.skip = skip;
			this.only = only;
		}
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class Test : Attribute { }

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class BeforeEach : Attribute { }

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class AfterEach : Attribute { }

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class BeforeAll : Attribute { }

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class AfterAll : Attribute { }
}
