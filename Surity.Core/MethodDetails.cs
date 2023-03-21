using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;

namespace Surity
{
	[Serializable]
	public class MethodDetails
	{
		public string Name { get; protected set; }
		public TypeDetails DeclaringType { get; protected set; }
		public TypeDetails ReturnType { get; protected set; }
		public ParameterDetails[] Parameters { get; protected set; }
		public bool IsAsync { get; protected set; }
		public bool IsTestMethod { get; protected set; }

		public MethodDetails(ResolvedMethod method)
		{
			this.Name = method.Name + (method.GenericArguments ?? string.Empty);
			this.DeclaringType = new TypeDetails(method.DeclaringType);
			this.ReturnType = new TypeDetails(method.ReturnParameter.ResolvedType);
			this.Parameters = method.MethodBase.GetParameters().Length > 0
				? method.Parameters.Select(p => new ParameterDetails(p)).ToArray()
				: null;
			this.IsAsync = method.IsAsync;
		}

		[JsonConstructor]
		private MethodDetails() { }
	}
}