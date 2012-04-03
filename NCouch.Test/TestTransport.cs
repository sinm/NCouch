using System;
using NUnit.Framework;
using NCouch;
using System.Net;

namespace NCouch.Test
{
	[TestFixture]
	public class TestTransport
	{
		const string SERVER_URL = "http://127.0.0.1:3004/db";
		const string LOGIN = "root@localhost";
		const string PASSWORD = "1q2w3e4r";
		const string DB_NAME = "ncouch-test";
		const string DOC_ID = "ncouch-5";
		
		Request request;
		Response response;
		
		[SetUp]
		public void request_setup()
		{
			request = new Request {
				Uri = SERVER_URL + "/" + DB_NAME + "/",
				Verb = "GET",
				ContentType = "application/json",
				Login = LOGIN,
				Password = PASSWORD
			};
			response = null;
		}
		
		[TestFixtureSetUp]
		public void db_setup()
		{
			try
			{
				db_teardown();
			}
			catch(ResponseException ex)
			{
				if (ex.Response.Status != System.Net.HttpStatusCode.NotFound)
					throw;
			}
			request_setup();
			request.Verb = "PUT";
			request.Send();
		}
		
		[TestFixtureTearDown]
		public void db_teardown()
		{
			request_setup();
			request.Verb = "DELETE";
			request.Send();
		}
				
		[Test]
		public void HTTP_Basic_Authentication()
		{
			response = request.Send();
			Assert.AreEqual(response.Status, HttpStatusCode.OK);
			
			request_setup();
			request.Login = "nouser";
			response = request.TrySend();
			Assert.AreEqual(response.Status, HttpStatusCode.Unauthorized);
		}
		
		[Test]
		public void ETag_Cache_Invalidation()
		{
			request.Uri += DOC_ID;
			request.Verb = "PUT";
			request.Text = "{\"content\":\"content-3\"}";
			response = request.Send();
			Assert.AreEqual(response.Status, HttpStatusCode.Created);
			
			request_setup();
			request.Uri += DOC_ID;
			response = request.Send();
			Assert.IsTrue(response.IsCached);
			Response response2 = response;
			
			request_setup();
			request.Uri += DOC_ID;
			response = request.Send();
			Assert.AreSame(response, response2);
			string etag = response.ETag;
			
			request_setup();
			request.Uri += DOC_ID;
			request.Verb = "DELETE";
			request.ETag = etag;
			response = request.Send();
			Assert.AreEqual(response.Status, HttpStatusCode.OK);
			
			request_setup();
			request.Uri += DOC_ID;
			response = request.TrySend();
			Assert.AreEqual(response.Status, HttpStatusCode.NotFound);
			Assert.IsFalse(response.IsCached);
			
		}
	}
}

