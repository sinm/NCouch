using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace NCouch
{
	[DebuggerDisplay ("{Verb} {URL}")]
	public class Request : Transport
	{
		static bool g_CacheTryOnFail = Config.GetBool("ncouch.cache_try_on_fail", false);
		
		public string Uri;
		public string Verb
		{
			get { return m_Verb; }
			set { m_Verb = value.ToUpper(); }
		} string m_Verb;
		
		public string Destination;
		
		public Auth Auth;
		
		public Dictionary<string, object> Query = new Dictionary<string, object>();

		public bool UseCache = true;
		
		public void SetQueryObject(object query)
		{
			if (query == null)
			{
				Query = new Dictionary<string, object>();
				return;
			}
			Dictionary<string, object> test_query = query as Dictionary<string, object>;
			if (test_query != null)
			{
				Query = test_query;
				return;
			}
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			string query_string = serializer.Serialize(query);
			test_query = 
				serializer.DeserializeObject(query_string) as Dictionary<string, object>;
			Query = test_query ?? new Dictionary<string, object>();
		}
		
		public bool JsonQuery = true;
		
		public string URL
		{
			get
			{
				if (String.IsNullOrEmpty(Uri))
					return Uri;
				StringBuilder url = new StringBuilder(Uri);
				if (Query.Count > 0)
				{
					url.Append("?");
					JavaScriptSerializer serializer = new JavaScriptSerializer();
					int index = 0;
					string val;
					foreach(KeyValuePair<string,object> kvp in Query)
					{
						url.Append(kvp.Key);
						url.Append("=");
						if (JsonQuery)
						{
							if (kvp.Value is string && !kvp.Key.StartsWith("key") && !kvp.Key.EndsWith("key"))
								val = (string)kvp.Value;
							else
								val = serializer.Serialize(kvp.Value);
						}
						else
						{
							val = kvp.Value == null ? "<null>" : kvp.Value.ToString();
						}
						url.Append(HttpUtility.UrlEncode(val));
						if (index < Query.Count - 1)
						{
							url.Append("&");
						}
						index++;
					}
				}
				return url.ToString();
			}
		}
		
		HttpWebRequest composeRequest()
		{
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);
			request.Method = Verb; 
			request.ContentType = ContentType ?? "application/json";			
			request.KeepAlive = true;
			
			if (Auth != null)
				Auth.BeforeRequest(request);
			
			if (!String.IsNullOrEmpty(ETag))
			{
				if (Verb == "GET")
				{
					request.Headers.Add("If-None-Match", ETag);
				}
				else if (Verb == "DELETE")
				{
					request.Headers.Add("If-Match", ETag);
				}
			}
			if (!string.IsNullOrEmpty(Destination))
			{
				request.Headers.Add("Destination", Destination);
			}
			if (Verb != "GET" && Verb != "DELETE" && Verb != "HEAD" && Body != null)
			{
				request.ContentLength = Body.Length;
				using (Stream request_stream = request.GetRequestStream())
				{
					request_stream.Write(Body, 0, Body.Length);
				}
			}
			return request;
		}
		
		public Response TrySend()
		{
			try
			{
				return Send();
			}
			catch(ResponseException ex)
			{
				return ex.Response;
			}			
		}
		
		public bool TrySend(out Response response)
		{
			try
			{
				response = TrySend();
				return true;
			}
			catch
			{
				response = null;
				return false;
			}
		}
		
		public Response Send()
		{
			Response from_cache = null;
			if (UseCache && Verb == "GET")
			{
				from_cache = ResponseCache.Instance.Get(URL);
				if (from_cache != null)
				{
					if (String.IsNullOrEmpty(ETag))
						ETag = from_cache.ETag;
					else if (ETag != from_cache.ETag)
						from_cache = null;
				}				
			}
			HttpWebRequest request = composeRequest();
			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch(WebException ex)
			{				
				response = ex.Response as HttpWebResponse;
				if (response == null) 
				{
					//TODO: so we can cache 404? Need so?
					if (UseCache && g_CacheTryOnFail && from_cache != null)
						return from_cache;
					else
						throw;
				}
				else
				{
					if (response.StatusCode == HttpStatusCode.NotModified)
					{
						if (from_cache != null)
							return from_cache;
						else
						{
							from_cache = ResponseCache.Instance.Get(URL);
							if (from_cache != null && from_cache.ETag == ETag)
								return from_cache;
						}
					}
					else
					{
						ResponseCache.Instance.Remove(URL);
					}
					throw new ResponseException(new Response(response));
				}
			}
			Response res = new Response(response);
			if (UseCache)
			{
				if ((Verb == "GET") && !String.IsNullOrEmpty(res.ETag))
				{
					ResponseCache.Instance.Add(URL, res);
				}
				else if (Verb == "DELETE")
				{
					ResponseCache.Instance.Remove(URL);
				}
			}
			return res;		
		}
	}
}

