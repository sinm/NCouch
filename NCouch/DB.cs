using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace NCouch
{
	
	public class DB
	{
		internal DB (Server server, string name) 
		{
			Server = server;
			Name = name;
			Path = EscapePath(name) + "/";
		}
		
		public readonly Server Server;
		public readonly string Name;
		public readonly string Path;
		
		Dictionary<string, View> m_Views = new Dictionary<string, View>();
				
		public static View operator / (DB db, string view_name)
		{
			View view;
			if (!db.m_Views.TryGetValue(view_name, out view))
			{
				db.m_Views[view_name] = view = new View(db, view_name);
			}
			return view;
		}
		
		public Request Prepare(string verb, string path)
		{
			return Server.Prepare(verb, Path + path);		
		}
			
		public bool _bulk_docs<T>(IList<T> docs, out List<T> conflicts) where T : IData
		{
			bool result = true;
			conflicts = new List<T>();
			Request request = Prepare("POST", "_bulk_docs");
			var data = new List<Document>(docs.Count);
			foreach(T doc in docs)
				data.Add(doc.Data);
			request.SetObject(new {docs = data});
			int index = -1;
			foreach(Dictionary<string, object> report in (object[])request.Send().GetObject())
			{
				index++;
				if (report.ContainsKey("error"))
				{
					conflicts.Add(docs[index]);
					result = false;
				}
				else
				{
					docs[index].Data.Id = (string)report["id"];
					docs[index].Data.Rev = (string)report["rev"];
				}
			}
			return result;
		}
		
		//TODO: replication
		//TODO: copy
		//TODO: locals?

		public string Update(string update_handler)
		{
			return Update (update_handler, null, null);
		}
		
		public string Update(string update_handler, object args)
		{
			return Update (update_handler, null, args);
		}
		
		public string Update(string update_handler, string object_id, object args)
		{
			var has_id = !String.IsNullOrEmpty(object_id);
			var request = Prepare(has_id? "PUT" : "POST", update_handler + (has_id? "/" + EscapePath(object_id) : ""));
			request.JsonQuery = false;
			if (args != null)
				request.SetQueryObject(args);
			return request.Send().Text;
		}

		public byte[] ReadAttachment(Attachment attachment)
		{
			var request = Prepare("GET", EscapePath(attachment.DocumentId) + "/" + EscapePath(attachment.Name));
			try
			{
				var response = request.Send();
				attachment.ContentType = response.ContentType;
				attachment.Length = response.Body.Length;
				attachment.DocumentRev = response.GetUnquotedETag();
				//Returning copy securing cache
				var result = new byte[response.Body.Length];
				Buffer.BlockCopy(response.Body, 0, result, 0, result.Length);
				return result;
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return null;
				throw;
			}
		}
		
		public bool DeleteAttachment(Attachment attachment)
		{
			Request request = Prepare("DELETE", EscapePath(attachment.DocumentId) + "/" + EscapePath(attachment.Name));
			request.Query["rev"] = attachment.DocumentRev;
			try
			{
				Response response = request.Send();
				Dictionary<string, object> report = response.GetObject() as Dictionary<string, object>;
				attachment.DocumentId = (string)report["id"];
				attachment.DocumentRev = (string)report["rev"];		
				return true;
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return false;
				throw;
			}
		}
		
		public void SaveAttachment(Attachment attachment)
		{
			var request = Prepare("PUT", EscapePath(attachment.DocumentId) + "/" + EscapePath(attachment.Name));
			request.ContentType = attachment.ContentType;
			request.Query["rev"] = attachment.DocumentRev;
			request.Body = attachment.Data;
			var response = request.Send();
			var report = response.GetObject() as Dictionary<string, object>;
			attachment.Length = attachment.Data.Length;
			attachment.DocumentId = (string)report["id"];
			attachment.DocumentRev = (string)report["rev"];
		}
		
		#region CRUD
		public void Create(IData doc)
		{
			Request request;
			if (doc.Data.Id == null)
				request = Prepare("POST", String.Empty);
			else
				request = Prepare("PUT", EscapePath(doc.Data.Id));
			request.SetObject(doc.Data);
			Dictionary<string, object> report = (Dictionary<string, object>)request.Send().GetObject();
			doc.Data.Rev = (string)report["rev"];
			if (doc.Data.Id == null)
				doc.Data.Id = (string)report["id"];
			doc.Data.DB = this;
		}
		
		public Document Read(string id)
		{
			return Read(id, false);
		}
		
		public Document Read(string id, bool attachments)
		{
			return Read<Document>(id, attachments);
		}

		public T Read<T>(string id) where T : class, IData, new()
		{
			return Read<T>(id, false);
		}
		
		public T Read<T>(string id, bool attachments) where T : class, IData, new()
		{
			try
			{
				var request = Prepare("GET", EscapePath(id));
				if (attachments)
					request.Query["attachments"] = true;
				var response = request.Send().GetObject() as Dictionary<string, object>;
				var doc = Document.FromHash<T>(response, this);
				doc.Data.DB = this;
				return doc;
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return null;
				else
					throw;
			}
		}		
		#endregion
		
		public static string EscapePath(string id)
		{
			return id.StartsWith("_design/")? id : id.Replace("/", "%2f");
		}
		
		public void Changes(Feed feed, ChangesDelegate del)
		{
			new Thread(delegate () {
				try
				{
					ChangeLog log = null;
					Exception e;
					do {
						try
						{
							log = Changes(feed);
							feed.since = log.last_seq;
							e = null;
						}
						catch(Exception ex)
						{
							e = ex;
							log = null;
						}
					} while( del(log, e) );
				}
				catch(ThreadAbortException) {}
			}).Start();
		}
		
		public ChangeLog Changes(Feed feed)
		{
			Request request = Prepare("GET", "_changes");
			request.JsonQuery = false;
			request.Query = feed.ToDictionary();
			return new ChangeLog(request.Send().GetObject() as Dictionary<string, object>, this);
		}
		
		public DBInfo GetInfo()
		{
			try
			{
				return new DBInfo(Prepare("GET",String.Empty).Send().GetObject() 
			                  as Dictionary<string, object>);
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return null;
				throw;
			}
		}
		
		public bool Exists()
		{
			return Prepare("HEAD", String.Empty).TrySend().Status == HttpStatusCode.OK;
		}
	}
}

