using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Diagnostics;
using System.IO;

namespace NCouch
{
	//TODO: g_ move to IDictionary in castings + Base class with Parse alike for Dictionary based classes?
	//TODO: tie on db?
	[DebuggerDisplay ("{_id}@{_rev}")]
	public class Document : Dictionary<string, object>, IData
	{		
		public Document() : base()
		{
		}
		
		[ScriptIgnore]
		public Document Data
		{
			get
			{
				return this;
			}
			set
			{
				Clear();
				if (value == null)
					return;
				foreach(KeyValuePair<string, object> kvp in value)
				{
					this[kvp.Key] = kvp.Value;
				}
			}
		}
		
		public Document (string id) : base()
		{
			this["_id"] = id;
		}
		
		public Document (Dictionary<string, object> content) : base(content)
		{
		}
		
		[ScriptIgnore]
		public string Id
		{
			get { return this["_id"] as string; } 
			set { this["_id"] = value; }
		}
		
		[ScriptIgnore]
		public string Rev 
		{
			get { return this["_rev"] as string; } 
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
		
		public static Document FromFile(string path)
		{
			return Deserialize(File.ReadAllText(path));
		}
		
		public Attachment GetAttachment(string name)
		{
			Attachment result = new Attachment();
			result.DocumentId = Id;
			result.DocumentRev = Rev;
			result.Name = name;
			if (this["_attachments"] != null)
			{
				Dictionary<string, object> att = 
					((Dictionary<string, object>)this["_attachments"])[name] as Dictionary<string, object>;
				if (att != null)
				{
					result.ContentType = att["content_type"] as string;
					long length;
					if(long.TryParse(att["length"].ToString(), out length))
						result.Length = length;
					int revpos;
					if(int.TryParse(att["revpos"].ToString(), out revpos))
						result.RevPos = revpos;
					if(att.ContainsKey("data"))
					{
						result.Stub = false;
						result.Data = Convert.FromBase64String(att["data"] as string);
					}				
					return result;
				}
			}
			return null;
		}
		
		public void SetInlineAttachments(IEnumerable<Attachment> attachments)
		{
			var atts = new Dictionary<string, object>();
			this["_attachments"] = atts;
			foreach(var attachment in attachments)
			{
				atts[attachment.Name] = new {
					content_type = attachment.ContentType, 
					data = Convert.ToBase64String(attachment.Data)
				};
			}
		}
		
		public Attachment NewAttachment(string name, string content_type)
		{
			return new Attachment {Name = name, DocumentId = Id, DocumentRev = Rev, ContentType = content_type};
		}
		
		public List<Attachment> GetAttachments()
		{
			List<Attachment> atts = new List<Attachment>();
			if (ContainsKey("_attachments"))
			{
				foreach(string name in ((Dictionary<string, object>)this["_attachments"]).Keys)
				{
					atts.Add(GetAttachment(name));
				}
			}
			return atts;
		}
	}
}

