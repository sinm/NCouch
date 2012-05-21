using System;
using System.Collections;
using System.Collections.Generic;

namespace NCouch
{
	/// <summary>
	/// Changes delegate.
	/// </summary>
	/// <param name="ex">
	/// Exception that had been thrown while getting log. Log is null if ex present.
	/// </param>
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
	
	public class ChangeLog
	{
		public readonly long last_seq;		
		public readonly List<Change> results = new List<Change>();
		public readonly DB DB;
		
		public ChangeLog(Dictionary<string, object> dict, DB db)
		{
			DB = db;
			var r = dict["results"] as object[];
			foreach(Dictionary<string, object> o in r)
			{
				results.Add(new Change(o, DB));
			}
			last_seq = long.Parse(dict["last_seq"].ToString());
		}
	}
	
	public class Change
	{
		public readonly long seq;
		public readonly string id;
		public readonly string rev;		
		public readonly bool deleted;
		public readonly Document doc;		
		public readonly DB DB;
		
		public Change(Dictionary<string, object> dict, DB db)
		{
			DB = db;
			seq = long.Parse(dict["seq"].ToString());
			id = (string)dict["id"];
			var d = ((object[])dict["changes"])[0] as IDictionary;
			rev = d.Contains("rev") ? (string)d["rev"] : String.Empty;
			deleted = dict.ContainsKey("deleted") ? (bool)dict["deleted"] : false;
			doc = dict.ContainsKey("doc") ? 
				Document.FromHash(dict["doc"] as Dictionary<string, object>, DB) : null;
		}
	}
}

