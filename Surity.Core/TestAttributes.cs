using System;
using System.Runtime.CompilerServices;

namespace Surity
{
	public interface IOrdered
	{
		int Order { get; }
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class TestClassAttribute : Attribute
	{
		public bool Skip { get; set; }
		public bool Only { get; set; }
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class TestAttribute : Attribute, IOrdered
	{
		public bool Skip { get; set; }
		public bool Only { get; set; }
		public int Order { get; }

		public TestAttribute([CallerLineNumber] int order = 0)
		{
			this.Order = order;
		}
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class BeforeEachAttribute : Attribute, IOrdered
	{
		public int Order { get; }

		public BeforeEachAttribute([CallerLineNumber] int order = 0)
		{
			this.Order = order;
		}
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class AfterEachAttribute : Attribute, IOrdered
	{
		public int Order { get; }

		public AfterEachAttribute([CallerLineNumber] int order = 0)
		{
			this.Order = order;
		}
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class BeforeAllAttribute : Attribute, IOrdered
	{
		public int Order { get; }

		public BeforeAllAttribute([CallerLineNumber] int order = 0)
		{
			this.Order = order;
		}
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class AfterAllAttribute : Attribute, IOrdered
	{
		public int Order { get; }

		public AfterAllAttribute([CallerLineNumber] int order = 0)
		{
			this.Order = order;
		}
	}
}
