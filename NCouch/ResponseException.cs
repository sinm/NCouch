using System;
using System.Net;
using System.Diagnostics;

namespace NCouch
{
	public class ResponseException : Exception
	{
		public Response Response;
		/*
		 * {"error":"unknown_error","reason":"badarg"}
		 */
		public ResponseException (Response response) : base()
		{
			Response = response;
		}
	}
	
	/*
	class ServerException : ResponseException
	{
	}
	
	class UnexpectedException : ResponseException
	{
	}
	*/
}

