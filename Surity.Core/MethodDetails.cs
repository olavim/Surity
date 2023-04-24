using System;
using System.Diagnostics;
using System.Linq;

namespace Surity
{
	[Serializable]
	public class MethodDetails
	{
		public string Name { get; set; }
		public TypeDetails DeclaringType { get; set; }
		public TypeDetails ReturnType { get; set; }
		public ParameterDetails[] Parameters { get; set; }
		public bool IsAsync { get; set; }
		public bool IsTestMethod { get; set; }

		public MethodDetails(ResolvedMethod method)
		{
			this.Name = method.Name + (method.GenericArguments ?? string.Empty);
			this.DeclaringType = new TypeDetails(method.DeclaringType);
			this.ReturnType = method.ReturnParameter == null ? null : new TypeDetails(method.ReturnParameter.ResolvedType);
			this.Parameters = method.MethodBase?.GetParameters().Length > 0
				? method.Parameters.Select(p => new ParameterDetails(p)).ToArray()
				: null;
			this.IsAsync = method.IsAsync;
		}

		private MethodDetails() { }
	}
}