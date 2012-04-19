using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Diagnostics;

namespace NCouch
{
	[DebuggerDisplay ("{_id}@{_rev}")]
	public class Document : Dictionary<string, object>
	{		
		/*public Document ()
		{

		}*/
		
		public Document (string id) : base()
		{
			this["_id"] = id;
		}
		
		public Document (Dictionary<string, object> content) : base(content)
		{
		}
		
		[ScriptIgnore]
		public string _id
		{
			get 
			{
				/*object id;
				if(TryGetValue("_id", out id))
				{
					return (string)id;
				}
				return null;*/
				return this["_id"] as string;
			} 
			set { this["_id"] = value; }
		}
		
		[ScriptIgnore]
		public string _rev {
			get 
			{
				/*object rev;
				if(TryGetValue("_rev", out rev))
				{
					return (string)rev;
				}
				return null;*/
				return this["_rev"] as string;
			} 
			set { this["_rev"] = value; }
		}
		
		public new object this[string key]
		{
			get
			{
				object v;
				TryGetValue(key, out v);
				return v;
			}
			set
			{
				base[key] = value;
			}
		}
		
		public static Document Deserialize(string json)
		{
			return new Document((new JavaScriptSerializer()).DeserializeObject(json) as Dictionary<string, object>);
		}
		
		public void Refresh(DB db)
		{
			Document new_doc = db.Read(_id);
			Clear();
			if (new_doc == null)
				return;
			foreach(KeyValuePair<string, object> kvp in new_doc)
			{
				this[kvp.Key] = kvp.Value;
			}
		}
	}
}

