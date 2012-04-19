using System;
using System.Collections.Generic;
using System.Net;

namespace NCouch
{
	public class Server
	{		
		public Server (string uri) : this(uri, null)
		{
		}
		
		public Server (string uri, Auth auth)
		{
			Uri = uri;
			if (!uri.EndsWith("/"))
				Uri += "/";
			Auth = auth;
		}
		
		public readonly string Uri;
		public readonly Auth Auth;
		
		Dictionary<string, DB> m_DBs = new Dictionary<string, DB>();
		
		public void Create(DB db)
		{
			db.Prepare("PUT", String.Empty).Send();
		}
		
		public bool Delete(DB db)
		{
			try
			{
				db.Prepare("DELETE", String.Empty).Send();
				return true;
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return false;
				throw;
			}
		}
		
		public DB Read(string db_name)
		{
			var db = this/db_name;
			try
			{
				db.Prepare("HEAD", String.Empty).Send();
				return db;
			}
			catch(ResponseException re)
			{
				if (re.Response.Status == HttpStatusCode.NotFound)
					return null;
				throw;
			}
		}
		
		public List<string> _all_dbs()
		{
			List<string> result = new List<string>();
			foreach(string db in (object[])Prepare("GET", "_all_dbs").Send().GetObject())
			{
				result.Add(db);
			}
			return result;
		}
		
		public List<string> _uuids(int count)
		{
			List<string> result = new List<string>();
			Request request = Prepare("GET", "_uuids");
			request.Query["count"] = count;
			foreach(string uuid in (IEnumerable<object>)request.Send().Parse("uuids"))
			{
				result.Add(uuid);
			}
			return result;
		}	
		
		public static DB operator / (Server server, string db_name)
		{
			DB db;
			if (!server.m_DBs.TryGetValue(db_name, out db))
			{
				server.m_DBs[db_name] = db = new DB(server, db_name);
			}
			return db;
		}
		
		public Request Prepare(string verb, string path)
		{
			return new Request {
				Verb = verb,
				Uri = Uri + path,
				Auth = Auth
			};			
		}
	}
}

