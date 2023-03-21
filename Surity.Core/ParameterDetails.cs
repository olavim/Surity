using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Surity
{
	[Serializable]
	public class ParameterDetails
	{
		public string Prefix { get; protected set; }
		public TypeDetails Type { get; protected set; }
		public string Name { get; protected set; }

		public ParameterDetails(ResolvedParameter param)
		{
			this.Prefix = param.Prefix;
			this.Type = new TypeDetails(param.ResolvedType);
			this.Name = param.Name ?? string.Empty;
		}

		[JsonConstructor]
		private ParameterDetails() { }
	}
}