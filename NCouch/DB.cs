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
			Path = name + "/";
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
		
		public bool _bulk_docs(IList<Document> docs)
		{
			//TODO: throw ConflictError instead of returning bool
			IList<Document> conflicts;
			return _bulk_docs(docs, out conflicts);
		}
			
		public bool _bulk_docs(IList<Document> docs, out IList<Document> conflicts)
		{
			bool result = true;
			conflicts = new List<Document>();
			Request request = Prepare("POST", "_bulk_docs");
			request.SetObject(new {docs = docs});
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
					docs[index]._id = (string)report["id"];
					docs[index]._rev = (string)report["rev"];
				}
			}
			return result;
		}
		
		//TODO: changes
		//TODO: replication
		//TODO: copy
		//TODO: db info
		
		public Response Update(string update_handler, string object_id, object args)
		{
			bool has_id = String.IsNullOrEmpty(object_id);
			Request request = Prepare(has_id? "PUT" : "POST", update_handler + (has_id? "/" + object_id : ""));
			request.JsonQuery = false;
			request.SetQueryObject(args);
			return request.Send();
		}
		
		#region CRUD
		public void Create(Document doc)
		{
			Request request;
			if (doc._id == null)
				request = Prepare("POST", String.Empty);
			else
				request = Prepare("PUT", doc._id);
			request.SetObject(doc);
			Dictionary<string, object> report = (Dictionary<string, object>)request.Send().GetObject();
			doc._rev = (string)report["rev"];
			if (doc._id == null)
				doc._id = (string)report["id"];
		}
		
		public Document Read(string id)
		{
			try
			{
				return new Document(Prepare("GET", id).Send().GetObject() as Dictionary<string, object>);				
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return null;
				else
					throw;
			}
		}
		
		public void Update(Document doc)
		{
			Request request = Prepare("PUT", doc._id);
			request.SetObject(doc);
			doc._rev = request.Send().Parse("rev") as string;				
		}
		
		public bool Delete(Document doc)
		{
			Request request = Prepare("DELETE", doc._id);
			request.SetQueryObject(new {rev=doc._rev});
			try
			{
				doc._rev = request.Send().Parse("rev") as string;	
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
	}
}

