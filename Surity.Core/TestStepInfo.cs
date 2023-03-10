using System;
using System.Reflection;

namespace Surity
{
	public class TestStepInfo
	{
		public string Name { get; }
		public Type StepType { get; }
		public Type Type { get; }
		public MethodInfo MethodInfo { get; }

		public TestStepInfo(Type stepType, string name, Type testClassType, MethodInfo methodInfo)
		{
			this.StepType = stepType;
			this.Name = name;
			this.Type = testClassType;
			this.MethodInfo = methodInfo;
		}
	}
}
