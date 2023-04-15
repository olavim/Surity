using System;
using System.Reflection;

namespace Surity
{
	internal class TestStepInfo
	{
		public class StepOrder : IComparable<StepOrder>
		{
			public int DeclarationDepth { get; }
			public int DeclarationOrder { get; }

			public StepOrder(int declarationDepth, int declarationOrder)
			{
				this.DeclarationDepth = declarationDepth;
				this.DeclarationOrder = declarationOrder;
			}

			public int CompareTo(StepOrder other)
			{
				if (this.DeclarationDepth != other.DeclarationDepth)
				{
					return other.DeclarationDepth.CompareTo(this.DeclarationDepth);
				}

				return this.DeclarationOrder.CompareTo(other.DeclarationOrder);
			}
		}

		public string Name { get; }
		public Type StepType { get; }
		public Type Type { get; }
		public MethodInfo MethodInfo { get; }
		public object MethodTarget { get; }
		public StepOrder Order { get; }
		public TestStepInfo Generator { get; }

		internal TestStepInfo(string name, Type stepType, Type testClassType, MethodInfo methodInfo, StepOrder order)
		{
			this.Name = name;
			this.StepType = stepType;
			this.Type = testClassType;
			this.MethodInfo = methodInfo;
			this.MethodTarget = null;
			this.Order = order;
		}

		internal TestStepInfo(TestInfo generatedInfo, TestStepInfo generator)
		{
			this.Name = generatedInfo.Name;
			this.StepType = typeof(TestAttribute);
			this.Type = generator.Type;
			this.MethodInfo = generatedInfo.MethodInfo;
			this.MethodTarget = generatedInfo.MethodTarget;
			this.Order = generator.Order;
			this.Generator = generator;
		}
	}
}
