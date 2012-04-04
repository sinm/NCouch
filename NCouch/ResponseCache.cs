using System;
using System.Collections.Generic;

namespace NCouch
{
	public class ResponseCache
	{
		Dictionary<string, Response> m_Cache = new Dictionary<string, Response>();
		SortedDictionary<long, string> m_Index = new SortedDictionary<long, string>();
		long m_MaxSize;
		long m_LastIndex = 0;
		long m_Size = 0;
		object m_SyncRoot = new object();
		
		public ResponseCache (long max_size)
		{	
			m_MaxSize = max_size;
		}
		
		public void Clear()
		{
			lock(m_SyncRoot)
			{
				m_Cache.Clear();
				m_Index.Clear();
			}
		}
		
		public Response Remove(string uri)
		{
			lock(m_SyncRoot)
			{
				Response response = null;
				if (m_Cache.TryGetValue(uri, out response))
				{
					m_Cache.Remove(uri);
					m_Index.Remove(response.CacheIndex);
					response.CacheIndex = 0;	
					m_Size -= response.Size;
					if (m_Size < 0) m_Size = 0;
				}
				return response;
			}
		}
		
		public Response Get(string uri)
		{
			lock(m_SyncRoot)
			{
				Response response = null;
				if(m_Cache.TryGetValue(uri, out response))
				{
					m_Index.Remove(response.CacheIndex);
					m_LastIndex += 1;
					response.CacheIndex = m_LastIndex;
					m_Index.Add(m_LastIndex, uri);
				}
				return response;
			}
		}
		
		public bool Add(string uri, Response response)
		{
			if (response.Size > m_MaxSize)
				return false;
			lock(m_SyncRoot)
			{				
				Remove(uri);
				if (m_Size + response.Size > m_MaxSize)
				{
					long to_remove_size = 0;
					List<string> to_remove = new List<string>();
					foreach(KeyValuePair<long, string> kvp in m_Index)
					{
						to_remove.Add(kvp.Value);
						to_remove_size += m_Cache[kvp.Value].Size;
						if (m_Size + response.Size - to_remove_size <= m_MaxSize)
							break;
					}
					foreach(string to_remove_uri in to_remove)
						Remove(to_remove_uri);
				}
				m_LastIndex += 1;
				response.CacheIndex = m_LastIndex;
				m_Cache.Add(uri, response);
				m_Index.Add(response.CacheIndex, uri);
				return true;
			}
		}		
	}
}

