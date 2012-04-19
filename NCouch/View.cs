using System;
using System.Collections.Generic;

namespace NCouch
{
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
		
		public static List<Row> operator / (View view, object query)
		{
			return view.Execute(query);
		}
		
		List<Row> Execute(object query)
		{
			if (query as Query != null)
				query = ((Query)query).ToDictionary();
			Request request = Prepare("GET");
			request.SetQueryObject(query);
			Response response = request.Send();
			object[] array = (object[])response.Parse("rows");
			List<Row> result = new List<Row>();
			foreach(Dictionary<string,object> dict in array)
			{
				result.Add(new Row(dict));
			}
			return result;
		}		
	}
}

