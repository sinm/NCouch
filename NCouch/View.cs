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
		
		public List<Row<Document>> ListRows(object query)
		{
			return ListRows<Document>(query);
		}
		
		public List<Document> ListDocuments(object query)
		{
			return ListDocuments<Document>(query);
		}
		
		object[] getRows(object query, bool require_docs)
		{
			if (query as Query != null)
				query = ((Query)query).ToDictionary();
			var request = Prepare("GET");
			request.SetQueryObject(query);
			if (require_docs)
				request.Query["include_docs"] = true;
			return (object[])request.Send().Parse("rows");			
		}
		
		/// <summary>
		/// Queries view, returning rows (look at Row class).
		/// </summary>
		/// <returns>
		/// List of rows. Row::value depends on raw json deserialization and 
		/// might be object[], Dictionary<string, object>, null, bool, decimal or string.
		/// </returns>
		/// <param name='query'>
		/// Query instance or an object of anonymous type.
		/// </param>
		/// <typeparam name='T'>
		/// Document type or any other custom type implementing IData. 
		/// You can extract docs of specified type using Row::doc
		/// </typeparam>
		List<Row<T>> ListRows<T>(object query) where T : class, IData, new()
		{
			List<Row<T>> result = new List<Row<T>>();
			foreach(Dictionary<string,object> dict in getRows(query, false))
			{
				result.Add(new Row<T>(dict, DB));
			}
			return result;
		}
		
		/// <summary>
		/// Queries view, returning documents.
		/// </summary>
		/// <returns>
		/// List of documents, one per row returned
		/// </returns>
		/// <param name='query'>
		/// Query instance or an object of anonymous type. 
		/// include_docs argument of query is set to true internally
		/// </param>
		/// <typeparam name='T'>
		/// Document type or any other custom type implementing IData
		/// </typeparam>
		public List<T> ListDocuments<T>(object query) where T : class, IData, new()
		{
			List<T> result = new List<T>();
			foreach(Dictionary<string,object> dict in getRows(query, true))
			{
				result.Add(Document.FromHash<T>(dict["doc"] as Dictionary<string,object>, DB));
			}
			return result;			
		}
	}
}

