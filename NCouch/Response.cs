using System;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace NCouch
{
	[DebuggerDisplay ("{Status} [Cached? {IsCached}]")]
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

