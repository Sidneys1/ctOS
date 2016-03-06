using System.Collections.Generic;

namespace ctOS
{
	public static class StringExtensions
	{
		public static string FormatString(string format, params object[] vars)
		{
			var lists = new List<int>[vars.Length];
			var lastIndex = 0;

			for (var i = 0; i < vars.Length; i++)
			{
				lists[i] = new List<int>();
				int index;
				var current = "{" + i + "}";
				do
				{
					index = format.CtIndexOf(current, lastIndex + 1);
					if (index != -1)
						lists[i].Add(index);
					lastIndex = index;
				} while (index != -1);
				lastIndex = 0;
			}

			for (var i = lists.Length - 1; i >= 0; i--)
			{
				var current = "{" + i + "}";
				for (var j = lists[i].Count - 1; i >= 0; i--)
				{
					var removeAt = lists[i][j];
					format = format.Remove(removeAt, current.Length);
					format = format.Insert(removeAt, vars[i].ToString());
				}
			}

			return format;
		}

		public static int CtIndexOf(this string str, string find, int startIndex = 0)
		{
			for (var i = startIndex; i < (str.Length - find.Length) + 1; i++)
			{
				var isfound = true;
				for (var j = 0; j < find.Length; j++)
				{
					if (str[i + j] == find[j]) continue;
					isfound = false;
					break;
				}
				if (isfound)
					return i;
			}
			return -1;
		}
	}
}
