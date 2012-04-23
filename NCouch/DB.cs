using System;
using System.Collections.Generic;
using System.Net;

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
		
		//TODO: changes
		//TODO: replication
		//TODO: copy
		//TODO: db info
		//TODO: locals
		
		public Response Update(string update_handler, string object_id, object args)
		{
			bool has_id = !String.IsNullOrEmpty(object_id);
			Request request = Prepare(has_id? "PUT" : "POST", update_handler + (has_id? "/" + EscapePath(object_id) : ""));
			request.JsonQuery = false;
			request.SetQueryObject(args);
			return request.Send();
		}
		
		public Response Update(string update_handler, IData doc)
		{
			Request request = Prepare("PUT", update_handler + "/" + EscapePath(doc.Data.Id));
			request.SetObject(doc.Data);
			return request.Send();
		}
			
		public void Refresh(IData doc)
		{
			string path = EscapePath(doc.Data.Id);
			Document new_data = Read(path);
			if (new_data == null)
			{
				doc.Data.Clear();
			}
			else
			{
				doc.Data = new_data;
			}
		}
		
		public byte[] ReadAttachment(Attachment attachment)
		{
			Request request = Prepare("GET", EscapePath(attachment.DocumentId) + "/" + EscapePath(attachment.Name));
			try
			{
				Response response = request.Send();
				attachment.ContentType = response.ContentType;
				attachment.Length = response.Body.Length;
				attachment.DocumentRev = response.GetUnquotedETag();
				//Returning copy securing cache
				byte[] result = new byte[response.Body.Length];
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
		
		public void SaveAttachment(Attachment attachment, byte[] body)
		{
			Request request = Prepare("PUT", EscapePath(attachment.DocumentId) + "/" + EscapePath(attachment.Name));
			request.ContentType = attachment.ContentType;
			request.Query["rev"] = attachment.DocumentRev;
			request.Body = body;
			Response response = request.Send();
			Dictionary<string, object> report = response.GetObject() as Dictionary<string, object>;
			attachment.Length = body.Length;
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
		}
		
		public Document Read(string id)
		{
			return Read<Document>(id);
		}
		
		public T Read<T>(string id) where T : IData, new()
		{
			try
			{
				T obj = new T();
				obj.Data = new Document(
					Prepare("GET", EscapePath(id))
					.Send().GetObject() as Dictionary<string, object>);	
				return obj;
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return default(T);
				else
					throw;
			}
		}
		
		public void Update(IData doc)
		{
			Request request = Prepare("PUT", EscapePath(doc.Data.Id));
			request.SetObject(doc.Data);
			doc.Data.Rev = request.Send().Parse("rev") as string;				
		}
		
		public bool Delete(IData doc)
		{
			Request request = Prepare("DELETE", EscapePath(doc.Data.Id));
			request.SetQueryObject(new {rev=doc.Data.Rev});
			try
			{
				doc.Data.Rev = request.Send().Parse("rev") as string;	
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
		
		public static string EscapePath(string id)
		{
			return id.StartsWith("_design/")? id : id.Replace("/", "%2f");
		}
	}
}

