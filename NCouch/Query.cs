using System;
using System.Collections.Generic;

namespace NCouch
{
	//http://wiki.apache.org/couchdb/HTTP_view_API#Access.2BAC8-Query
	public class Query
	{
		public static readonly object NULL = new object();
	
		public object 		key 			= NULL;
		public List<object>	keys 			= new List<object>();
		public object 		startkey 		= NULL;
		public string 		startkey_docid 	= null;
		public object 		endkey 			= NULL;
		public string 		endkey_docid 	= null;
		public int 			limit 			= 0;
		public string 		stale 			= null;
		public bool 		@descending 	= false;
		public int 			skip 			= 0;
		public bool 		@group 			= false;
		public int 			group_level 	= 0;
		public bool? 		reduce 			= null;
		public bool 		include_docs 	= false;	
		public bool 		inclusive_end 	= true;
		public bool 		update_seq 		= false;
		
		public Dictionary<string, object> ToDictionary()
		{
			Dictionary<string, object> result = new Dictionary<string, object>();
			
			if (key != NULL)
				result["key"] = key;
			if (keys != null && keys.Count > 0)
				result["keys"] = keys;
			if (startkey != NULL)
				result["startkey"] = startkey;
			if (startkey_docid != null)
				result["startkey_docid"] = startkey_docid;
			if (endkey != NULL)
				result["endkey"] = endkey;
			if (endkey_docid != null)
				result["endkey_docid"] = endkey_docid;
			if (limit > 0)
				result["limit"] = limit;
			if (stale != null)
				result["stale"] = stale;			
			if (@descending != false)
				result["descending"] = true;		
			if (skip > 0)
				result["skip"] = skip;
			if (@group != false)
				result["group"] = true;
			if (group_level > 0)
				result["group_level"] = group_level;
			if (reduce.HasValue)
				result["reduce"] = reduce;
			if (include_docs != false)
				result["include_docs"] = true;
			if (inclusive_end != true)
				result["inclusive_end"] = false;
			if (update_seq != false)
				result["update_seq"] = true;
			
			return result;
		}
	}
}

