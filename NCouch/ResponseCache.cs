using System;
using System.Collections.Generic;
using System.Timers;

namespace NCouch
{
	public class ResponseCache
	{
		Dictionary<string, Response> m_Cache = new Dictionary<string, Response>();
		Dictionary<string, DateTime> m_CacheTTL = new Dictionary<string, DateTime>();
		TimeSpan m_TTL;
		Timer m_Timer;
		object m_SyncRoot = new object();
		
		public ResponseCache (TimeSpan ttl)
		{
			m_TTL = ttl;
			m_Timer = new Timer(m_TTL.TotalMilliseconds);
			m_Timer.Elapsed += timer_elapsed;
			m_Timer.AutoReset = false;
			m_Timer.Start();
		}
		
		public void Clear()
		{
			lock(m_SyncRoot)
			{
				m_CacheTTL.Clear();
				m_Cache.Clear();
			}
		}
		
		public void Remove(string uri)
		{
			lock(m_SyncRoot)
			{
				Response response;
				if (m_Cache.TryGetValue(uri, out response))
				{
					m_CacheTTL.Remove(uri);
					m_Cache.Remove(uri);
					response.IsCached = false;
				}
			}
		}
		
		public Response Get(string uri)
		{
			lock(m_SyncRoot)
			{
				Response response = null;
				if (m_Cache.TryGetValue(uri, out response))
				{
					m_CacheTTL[uri] = DateTime.Now;
				}
				return response;
			}
		}
		
		public void Add(string uri, Response response)
		{
			lock(m_SyncRoot)
			{
				m_CacheTTL[uri] = DateTime.Now;
				m_Cache[uri] = response;
				response.IsCached = true;
			}
		}
		
		void timer_elapsed(object sender, ElapsedEventArgs e)
		{
			m_Timer.Stop();
			lock(m_SyncRoot)
			{
				List<string> to_remove = new List<string>();
				DateTime now = DateTime.Now;
				foreach(KeyValuePair<string,DateTime> kvp in m_CacheTTL)
				{
					if ((now - kvp.Value) > m_TTL)
					{
						to_remove.Add(kvp.Key);
					}
				}
				foreach(string uri in to_remove)
				{
					Remove(uri);
				}
			}
			m_Timer.Start();
		}
		
	}
}

