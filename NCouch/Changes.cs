using System;
using System.Collections;
using System.Collections.Generic;

namespace NCouch
{
	public delegate bool ChangesDelegate(ChangeLog log, Exception ex);
	
	public enum FeedMode {normal, longpoll}
	
	public class Feed
	{
		public FeedMode feed = FeedMode.normal;
		public long? since = null;
		public int? timeout = null;
		public bool include_docs = false;
		public string filter = null;
		public int? limit = null;
		
		public Dictionary<string, object> ToDictionary()
		{
			var result = new Dictionary<string, object>();
						result["feed"] = feed.ToString();
			if (timeout.HasValue && feed != FeedMode.normal)
				result["timeout"] = timeout <= 0 ? 60000 : timeout;
			if (since.HasValue)
				result["since"] = since < 0 ? 0 : since;
			if (include_docs)
				result["include_docs"] = "true";
			if (!String.IsNullOrEmpty(filter))
				result["filter"] = filter;
			if (limit.HasValue)
				result["limit"] = limit < 0 ? 0 : limit;
			return result;
		}
	}
	
	public class ChangeLog : Dictionary<string, object>
	{
		public readonly DB DB;
		
		public ChangeLog(Dictionary<string, object> dict, DB db) : base(dict)
		{
			DB = db;
			var r = dict["results"] as object[];
			foreach(Dictionary<string, object> o in r)
			{
				results.Add(new Change(o, DB));
			}
		}
		
		public long last_seq
		{
			get
			{
				return long.Parse(this["last_seq"].ToString());
			}
		}
		
		public List<Change> results = new List<Change>();
	}
	
	public class Change : Dictionary<string, object>
	{
		public long seq
		{
			get
			{
				return long.Parse(this["seq"].ToString());
			}
		}
		
		public string id
		{
			get
			{
				return (string)this["id"];
			}
		}
		
		public string rev
		{
			get
			{
				var d = ((object[])this["changes"])[0] as IDictionary;
				return d.Contains("rev") ? (string)d["rev"] : String.Empty;
			}
		}
		
		public bool deleted
		{
			get
			{
				return (bool)this["deleted"];
			}
		}
		
		public IData doc
		{
			get
			{
				return ContainsKey("doc") ? 
					Document.FromHash(this["doc"] as Dictionary<string, object>, DB) : null;
			}
		}
		
		DB DB;
		
		public Change(Dictionary<string, object> dict, DB db) : base(dict)
		{
			DB = db;
		}
	}
}

