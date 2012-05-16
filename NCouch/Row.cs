using System;
using System.Collections.Generic;

namespace NCouch
{
	public class Row<T> where T: class, IData, new()
	{
		public Row(Dictionary<string, object> source)
		{
			object test;
			if (source.TryGetValue("key", out test))
			{
				key = test;
			}
			if (source.TryGetValue("value", out test))
			{
				value = test;
			}
			if (source.TryGetValue("id", out test))
			{
				id = test == null ? null : test.ToString();
			}
			if (source.TryGetValue("doc", out test))
			{
				if (test != null)
				{
					doc = Document.FromHash<T>(test as Dictionary<string, object>);
				}
			}
		}
		
		public object key;
		public object value;
		public string id;
		public T doc;
	}
}

