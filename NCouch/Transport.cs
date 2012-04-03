using System;
using System.Net;
using System.Text;
using System.IO;
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
		
		public void SetObject(object o)
		{
			Text = (new JavaScriptSerializer()).Serialize(o);
		}
		
		public T GetObject<T>()
		{
			return (new JavaScriptSerializer()).Deserialize<T>(Text);
		}
		
		public Dictionary<string, object> getTree()
		{
			return GetObject<Dictionary<string, object>>();
		}
		
		
		protected static ResponseCache Cache = new ResponseCache(TimeSpan.FromSeconds(100));
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
		
		HttpWebRequest composeRequest()
		{
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Uri);
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
		
		public Response Send()
		{
			if (Verb == "GET" && String.IsNullOrEmpty(ETag))
			{
				Response from_cache = Cache.Get(Uri);
				if (from_cache != null)
				{
					ETag = from_cache.ETag;
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
						Response from_cache = Cache.Get(Uri);
						if (from_cache != null)
						{
							return from_cache;
						}
					}
					throw new ResponseException(new Response(response));
				}
			}
			Response res = new Response(response);
			if ((Verb == "GET") && !String.IsNullOrEmpty(res.ETag))
			{
				Cache.Add(Uri, res);
			}
			return res;		
		}
	}
	
	public class Response : Transport
	{
		public HttpStatusCode Status;
		
		public bool IsCached {
			get {return m_IsCached;} 
			internal set {m_IsCached = value;}
		} bool m_IsCached;
		
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

