using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Surity
{
	[Serializable]
	public class TypeDetails
	{
		public string Namespace { get; protected set; }
		public string Name { get; protected set; }
		public string FullName { get; protected set; }
		public TypeDetails[] GenericArguments { get; protected set; }

		public TypeDetails(Type type)
		{
			this.Namespace = type.Namespace;
			this.Name = TypeNameHelper.GetTypeDisplayName(type, false, true);
			this.FullName = TypeNameHelper.GetTypeDisplayName(type, true, true);

			if (type.IsGenericType)
			{
				this.Name = this.Name.Substring(0, this.Name.LastIndexOf('<'));
				this.FullName = this.FullName.Substring(0, this.FullName.LastIndexOf('<'));
			}

			this.GenericArguments = type.GenericTypeArguments.Select(t => new TypeDetails(t)).ToArray();
		}

		[JsonConstructor]
		private TypeDetails() { }

		public string GetDisplayName(bool fullName = false)
		{
			var builder = new StringBuilder(fullName ? this.FullName : this.Name);

			if (this.GenericArguments.Length > 0)
			{
				builder.Append('<');
				var typeNames = this.GenericArguments.Select(t => t.GetDisplayName(fullName));
				builder.Append(string.Join(", ", typeNames));
				builder.Append('>');
			}

			return builder.ToString();
		}
	}
}