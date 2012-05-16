using System;
using System.Collections;
using System.Collections.Generic;

namespace NCouch
{
	public delegate bool ChangesDelegate(DB db, ChangeLog log, Exception ex);
	
	public enum FeedMode {normal, longpoll}
	
	public class Feed
	{
		public FeedMode feed = FeedMode.normal;
		public long? since = null;
		public int? timeout = null;
		public bool include_docs = false;
		public string filter = null;
		public int? limit = null;
	}
	
	public class ChangeLog : Dictionary<string, object>
	{
		public ChangeLog() : base() {}
		
		public ChangeLog(Dictionary<string, object> dict) : base(dict)
		{
			object[] r = dict["results"] as object[];
			foreach(Dictionary<string, object> o in r)
			{
				results.Add(new Change(o));
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
				IDictionary d = ((object[])this["changes"])[0] as IDictionary;
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
				return ContainsKey("doc") ? Document.FromHash(this["doc"] as Dictionary<string, object>) : null;
			}
		}
		
		public Change(Dictionary<string, object> dict) : base(dict)
		{
		}
	}
}

