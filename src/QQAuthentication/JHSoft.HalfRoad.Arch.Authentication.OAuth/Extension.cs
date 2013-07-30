using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace JHSoft.HalfRoad.Arch.Authentication.OAuth
{
	internal static class Extension
	{
		internal static void AppendQueryArgs(this UriBuilder builder, IEnumerable<KeyValuePair<string, string>> args)
		{
			if (args != null && args.Count<KeyValuePair<string, string>>() > 0)
			{
				StringBuilder sb = new StringBuilder(50 + args.Count<KeyValuePair<string, string>>() * 10);
				if (!string.IsNullOrEmpty(builder.Query))
				{
					sb.Append(builder.Query.Substring(1));
					sb.Append('&');
				}
				foreach (KeyValuePair<string, string> item in args)
				{
					sb.Append(string.Format("{0}={1}&", item.Key, item.Value));
				}
				sb.Length--;
				builder.Query = sb.ToString();
			}
		}
	}
}
