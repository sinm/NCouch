using System;
using System.Collections.Generic;

namespace NCouch
{
	/// <summary>
	/// View.
	/// You may want to use Query object whenever query parameter is required
	/// </summary>
	public class View
	{
		public View (DB db, string name)
		{
			DB = db;
			Name = name;
			Path = DB.Path + name;
		}
		
		public Server Server
		{
			get { return DB.Server; }
		}
		public readonly DB DB;
		public readonly string Name;
		public readonly string Path;
		
		public Request Prepare(string verb)
		{
			return Server.Prepare(verb, Path);		
		}
		
		public static List<Row<Document>> operator / (View view, object query)
		{
			return view.Execute(query);
		}
		
		List<Row<Document>> Execute(object query)
		{
			return Execute<Document>(query);
		}
		
		List<Row<T>> Execute<T>(object query) where T : class, IData, new()
		{
			if (query as Query != null)
				query = ((Query)query).ToDictionary();
			Request request = Prepare("GET");
			request.SetQueryObject(query);
			Response response = request.Send();
			object[] array = (object[])response.Parse("rows");
			List<Row<T>> result = new List<Row<T>>();
			foreach(Dictionary<string,object> dict in array)
			{
				result.Add(new Row<T>(dict, DB));
			}
			return result;
		}
		
		public List<T> ListDocuments<T>(object query) where T : class, IData, new()
		{
			if (query as Query != null)
				query = ((Query)query).ToDictionary();
			Request request = Prepare("GET");
			request.SetQueryObject(query);
			request.Query["include_docs"] = true;
			Response response = request.Send();
			object[] array = (object[])response.Parse("rows");
			List<T> result = new List<T>();
			foreach(Dictionary<string,object> dict in array)
			{
				result.Add(Document.FromHash<T>(dict["doc"] as Dictionary<string,object>, DB));
			}
			return result;			
		}
	}
}

