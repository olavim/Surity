using System.Collections.Generic;
using System.Linq;

namespace Surity
{
	public class TestExecutionGroup
	{
		public string Name { get; }
		public IEnumerable<TestStepInfo> Steps { get; }
		public ExecutionResult Result { get; set; }

		internal bool Only { get; set; }
		internal bool Skip { get; set; }

		public TestExecutionGroup(string name, IEnumerable<TestStepInfo> steps)
		{
			this.Name = name;
			this.Steps = steps.ToArray();
		}
	}
}
