using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;

namespace NCouch
{
	[DebuggerDisplay ("{Response.Status} {reason} ({error})")]
	public class ResponseException : Exception
	{
		public readonly Response Response;
		public readonly string error = String.Empty;
		public readonly string reason = String.Empty;
		
		public ResponseException (Response response) : base()
		{
			Response = response;
			try
			{
				Dictionary<string, object> error_object = 
					response.GetObject() as Dictionary<string, object>;
				error = (string)error_object["error"];				
				reason = (string)error_object["reason"];
			}
			catch 
			{
				//NOOP
			}
		}
		
		public override string ToString ()
		{
			return reason;
		}
	}
}

