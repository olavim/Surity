using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Surity
{
	internal class TypePattern
	{
		private enum Operator
		{
			Matches,
			Not,
			And,
			Or
		}

		private readonly string pattern;
		private readonly TypePattern left;
		private readonly TypePattern right;
		private readonly Operator op;

		public TypePattern(string pattern)
		{
			this.pattern = pattern.Trim();

			if (string.IsNullOrEmpty(this.pattern))
			{
				throw new ArgumentException("Invalid pattern");
			}

			var tokens = Tokenize(pattern);

			if (tokens.Length == 1)
			{
				if (tokens[0].StartsWith("!"))
				{
					this.op = Operator.Not;
					this.left = new TypePattern(tokens[0][1..]);
				}
				else if (tokens[0].StartsWith("("))
				{
					this.op = Operator.Matches;
					this.left = new TypePattern(tokens[0][1..^1]);
				}
				else
				{
					this.op = Operator.Matches;
				}
			}

			if (tokens.Length > 1)
			{
				this.left = new TypePattern(tokens[0]);
				this.right = new TypePattern(tokens[2]);

				if (tokens[1] == "&")
				{
					this.op = Operator.And;
				}
				else if (tokens[1] == "|")
				{
					this.op = Operator.Or;
				}
				else
				{
					throw new ArgumentException("Invalid pattern");
				}
			}
		}

		public bool Matches(string str)
		{
			if (this.op == Operator.Matches && this.left == null)
			{
				return str.Like(this.pattern);
			}

			if (this.op == Operator.Matches && this.left != null)
			{
				return this.left.Matches(str);
			}

			if (this.op == Operator.Not)
			{
				return !this.left.Matches(str);
			}

			if (this.op == Operator.And)
			{
				return this.left.Matches(str) && this.right.Matches(str);
			}

			if (this.op == Operator.Or)
			{
				return this.left.Matches(str) || this.right.Matches(str);
			}

			return false;
		}

		public override string ToString()
		{
			var builder = new StringBuilder();

			if (this.op == Operator.Matches && this.left == null)
			{
				builder.Append(this.pattern);
			}

			if (this.op == Operator.Matches && this.left != null)
			{
				builder.Append(this.left.ToString());
			}

			if (this.op == Operator.And)
			{
				builder.Append("(");
				builder.Append(this.left.ToString());
				builder.Append(" & ");
				builder.Append(this.right.ToString());
				builder.Append(")");
			}

			if (this.op == Operator.Or)
			{
				builder.Append("(");
				builder.Append(this.left.ToString());
				builder.Append(" | ");
				builder.Append(this.right.ToString());
				builder.Append(")");
			}

			if (this.op == Operator.Not)
			{
				builder.Append("!").Append(this.left.ToString());
			}

			return builder.ToString();
		}

		private static string[] Tokenize(string str)
		{
			var opMatches = new Regex(@"\||&").Matches(str);

			if (opMatches.Count == 0)
			{
				return new[] { str };
			}

			if (opMatches[0].Index == 0)
			{
				throw new ArgumentException("Invalid pattern");
			}

			var tokens = new List<string>();
			int matchIndex = 0;
			int start = 0;
			int end = 0;

			for (; end < str.Length; end++)
			{
				while (matchIndex < opMatches.Count && opMatches[matchIndex].Index < end)
				{
					matchIndex++;
				}

				if (matchIndex < opMatches.Count && end == opMatches[matchIndex].Index)
				{
					tokens.Add(str[start..end].Trim());
					tokens.Add(opMatches[matchIndex].Value);
					start = opMatches[matchIndex].Index + opMatches[matchIndex].Length;
					end = start;
					matchIndex++;
				}

				if (str[end] == '(')
				{
					int stack = 1;
					while (stack > 0 && end++ < str.Length - 1)
					{
						if (str[end] == '(')
						{
							stack++;
						}

						if (str[end] == ')')
						{
							stack--;
						}
					}

					if (str[end] != ')')
					{
						throw new ArgumentException("Invalid pattern");
					}
				}
			}

			if (start < end)
			{
				tokens.Add(str[start..].Trim());
			}

			return tokens.ToArray();
		}
	}
}