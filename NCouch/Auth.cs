using System;
using System.Net;
using System.Text;

namespace NCouch
{
	public abstract class Auth
	{
		public abstract void BeforeRequest(HttpWebRequest request);
		public abstract void AfterResponse(HttpWebResponse response);
	}
	
	public class BasicAuth : Auth
	{
		public BasicAuth (string login, string password)
		{
			Value = "Basic " + 
				Convert.ToBase64String(Encoding.ASCII.GetBytes(login + ":" + password));
		}
		
		public readonly string Value;
		
		public override void BeforeRequest(HttpWebRequest request)
		{ 
        	request.Headers.Add("Authorization", Value);
		}
		
		public override void AfterResponse(HttpWebResponse response)
		{
			//NOP
		}
	}
	
	//TODO: OAuth & cookie
}

