using System.Collections.Generic;
using System.Linq;

namespace Surity
{
	internal class TestExecutionGroup
	{
		public string Name { get; }
		public IEnumerable<TestStepInfo> Steps { get; }
		public ExecutionResult Result { get; set; }
		public IEnumerable<TestExecutionGroup> GeneratedExecutions { get; set; }

		public bool Only { get; set; }
		public bool Skip { get; set; }

		public TestExecutionGroup(string name, IEnumerable<TestStepInfo> steps)
		{
			this.Name = name;
			this.Steps = steps.ToArray();
		}
	}
}
