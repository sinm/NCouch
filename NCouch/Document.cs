using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace NCouch
{
	//TODO: g_ move to IDictionary in castings + Base class with Parse alike for Dictionary based classes?
	//TODO: tie on db?
	[DebuggerDisplay ("{_id}@{_rev}")]
	public sealed class Document : Dictionary<string, object>, IData
	{		
		public Document() : base()
		{
			InlineAttachments = false;
		}
		
		public DB DB {get; internal set;}
		
		[ScriptIgnore]
		public Document Data
		{
			get
			{
				return this;
			}
			set
			{
				throw new InvalidOperationException("Can't call set_Data on Document instance! Use FromHash<T> as a factory.");
			}
		}
		
		private Document (Dictionary<string, object> content) : base(content)
		{
			InlineAttachments = containsInlineAttachment();
		}
		
		public bool InlineAttachments {get; private set;}
		
		bool containsInlineAttachment()
		{
			if (this["_attachments"] != null)
				foreach(KeyValuePair<string, object> kvp in this["_attachments"])
					return ((IDictionary)kvp.Value).Contains("data");
			return false;
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
		
		public Request Prepare(string verb)
		{
			return DB.Prepare(verb, DB.EscapePath(Id));
		}
		
		#region CRUD
		public string Update(string updateHandler)
		{
			Request request = DB.Prepare("PUT", updateHandler + "/" + DB.EscapePath(Id));
			request.SetObject(this);
			return request.Send().Text;
		}
		
		/// <summary>
		/// Refresh this instance.
		/// </summary>
		/// <returns>
		/// false if deleted
		/// </returns>
		public bool Refresh()
		{
			try
			{
				var request = Prepare("GET");
				if (InlineAttachments)
					request.Query["attachments"] = true;
				var response = request.Send().GetObject() as Dictionary<string, object>;
				Clear();
				foreach(KeyValuePair<string, object> kvp in response) {
					this[kvp.Key] = kvp.Value;
				}
				return true;
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return false;
				else
					throw;
			}
		}
		
		public void Update()
		{
			var request = Prepare("PUT");
			request.SetObject(this);
			Rev = request.Send().Parse("rev") as string;				
		}
		
		public bool Delete()
		{
			var request = Prepare("DELETE");
			request.SetQueryObject(new {rev = Rev});
			try
			{
				Rev = request.Send().Parse("rev") as string;	
				return true;
			}
			catch(ResponseException re)
			{
				if (re.Response.Status != HttpStatusCode.NotFound)
					throw;
				return false;
			}
			
		}
		#endregion
		
		public Attachment GetAttachment(string name)
		{
			var result = new Attachment{
				DocumentId = Id,
				DocumentRev = Rev,
				Name = name
			};
			if (this["_attachments"] != null)
			{
				var att = 
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
			InlineAttachments = true;
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
		
		public static T FromHash<T>(Dictionary<string, object> hash, DB db) where T : class, IData, new()
		{
			var obj = new Document(hash);
			obj.DB = db;
			if (typeof(T) == typeof(Document))
				return obj as T;
			else {
				T t_obj = new T();
				t_obj.Data = obj;
				return t_obj;
			};
		}
		
		public static Document FromHash(Dictionary<string, object> hash, DB db)
		{
			return FromHash<Document>(hash, db);
		}
	}
}

