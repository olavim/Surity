using System;
using System.Diagnostics;

namespace Surity
{
	[Serializable]
	public class ParameterDetails
	{
		public string Prefix { get; set; }
		public TypeDetails Type { get; set; }
		public string Name { get; set; }

		public ParameterDetails(ResolvedParameter param)
		{
			this.Prefix = param.Prefix;
			this.Type = new TypeDetails(param.ResolvedType);
			this.Name = param.Name ?? string.Empty;
		}

		private ParameterDetails() { }
	}
}