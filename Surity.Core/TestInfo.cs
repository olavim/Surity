using System;
using System.Collections;
using System.Reflection;

namespace Surity
{
	public class TestInfo
	{
		public string Name { get; }
		internal MethodInfo MethodInfo { get; }
		internal object MethodTarget { get; }

		public TestInfo(string name, Action func)
		{
			this.Name = name;
			this.MethodInfo = func.Method;
			this.MethodTarget = func.Target;
		}

		public TestInfo(string name, Func<IEnumerator> func)
		{
			this.Name = name;
			this.MethodInfo = func.Method;
			this.MethodTarget = func.Target;
		}
	}
}
