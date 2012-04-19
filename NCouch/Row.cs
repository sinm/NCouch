using System;
using System.Collections.Generic;

namespace NCouch
{
	public class Row
	{
		public Row (Dictionary<string, object> source)
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
				doc = test == null ? null : new Document(test as Dictionary<string, object>);
			}
		}
		
		public object key;
		public object value;
		public string id;
		public Document doc;
	}
}

