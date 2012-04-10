using System;
using System.Net;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace NCouch
{
	
	public abstract class Transport
	{
		public string ContentType;
		public string ETag;
		public string GetUnquotedETag()
		{
			if (!String.IsNullOrEmpty(ETag) && ETag.Length > 2)
				return ETag.Substring(1, ETag.Length-2);
			return String.Empty;
		}
		public byte[] Body;
		public string Text
		{
			get
			{
				return Body == null ? null : Encoding.UTF8.GetString(Body);
			}
			set
			{
				Body = value == null ? null : Encoding.UTF8.GetBytes(value);
			}
		}
		
		public long Size
		{
			get
			{
				return Body == null ? 0 : Body.Length;
			}
		}
		
		public void SetObject(object o)
		{
			Text = (new JavaScriptSerializer()).Serialize(o);
		}
		
		public object GetObject()
		{
			return (new JavaScriptSerializer()).DeserializeObject(Text);
		}
		
		protected static ResponseCache Cache = new ResponseCache(Config.GetLong("ncouch.cache_size", 1048576 * 100));
	}
	
	public class Request : Transport
	{
		public string Uri;
		public string Verb
		{
			get { return m_Verb; }
			set { m_Verb = value.ToUpper(); }
		} string m_Verb;
		public string Login;
		public string Password;
		
		public Dictionary<string, object> Query
		{
			get {return m_Query;}
		} Dictionary<string, object> m_Query = new Dictionary<string, object>();
		
		public void setQueryObject(object query)
		{
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			string query_string = serializer.Serialize(query);
			Dictionary<string, object> test_query = 
				serializer.DeserializeObject(query_string) as Dictionary<string, object>;
			if (test_query != null)
				m_Query = test_query;
			else
				throw new ArgumentException("setQueryObject can't deserialize to dictionary: " + query_string);
		}
		
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
					foreach(KeyValuePair<string,object> kvp in Query)
					{
						url.Append(kvp.Key);
						url.Append("=");
						url.Append(HttpUtility.UrlEncode(serializer.Serialize(kvp.Value)));
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
			request.ContentType = ContentType;			
			request.KeepAlive = true;
			if (!String.IsNullOrEmpty(Login))
			{
	            string authValue = "Basic ";
    	        string userNAndPassword = Login + ":" + Password;
            	string b64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(userNAndPassword));
            	request.Headers.Add("Authorization", authValue + b64);
			}
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
		
		public bool TrySend(out Response response)
		{
			try
			{
				response = Send();
				return true;
			}
			catch(ResponseException ex)
			{
				response = ex.Response;
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
			if (Verb == "GET")
			{
				from_cache = Cache.Get(URL);
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
					throw;
				else
				{
					if (response.StatusCode == HttpStatusCode.NotModified)
					{
						if (from_cache != null)
							return from_cache;
						else
						{
							from_cache = Cache.Get(URL);
							if (from_cache != null && from_cache.ETag == ETag)
								return from_cache;
						}
					}
					else
					{
						Cache.Remove(URL);
					}
					throw new ResponseException(new Response(response));
				}
			}
			Response res = new Response(response);
			if ((Verb == "GET") && !String.IsNullOrEmpty(res.ETag))
			{
				Cache.Add(URL, res);
			}
			else if (Verb == "DELETE")
			{
				Cache.Remove(URL);
			}
			return res;		
		}
	}
	
	public class Response : Transport
	{
		public HttpStatusCode Status;
		
		public bool IsCached 
		{
			get 
			{
				return CacheIndex != 0;
			}
		}
		
		internal long CacheIndex = 0;
		
		internal Response(HttpWebResponse response)
		{
			using (response)
			{
				using (Stream response_stream = response.GetResponseStream())
				{
					if (response_stream.CanRead)
					{
						byte[] buffer = new byte[16654];
						int read;
						using (MemoryStream s = new MemoryStream())
						{
							while((read = response_stream.Read(buffer, 0, buffer.Length)) > 0)
							{
								s.Write(buffer, 0, read);
							}
							Body = new byte[s.Length];
							s.Seek(0, SeekOrigin.Begin);							
							s.Read(Body, 0, Body.Length);
						}
					}
				}											
				Status = response.StatusCode;
				ContentType = response.GetResponseHeader("Content-Type");
				ETag = response.GetResponseHeader("ETag");		
			}
		}
	}

}

