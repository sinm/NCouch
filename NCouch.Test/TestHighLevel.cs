using System;
using NUnit.Framework;
using NCouch;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using Employ.Model;
using System.Threading;

namespace NCouch.Test
{
	//TODO: more tests
	[TestFixture]
	public class TestHighLevel
	{
		const string SERVER_URL = "http://127.0.0.1:5984";
		const string DB1_NAME = "ncouch-test-1";
		const string DB2_NAME = "ncouch-test-2";
		
		Server Couch;
		DB DB1;
		DB DB2;
		Auth AUTH = null;
		
		public TestHighLevel()
		{
			Couch = new Server(SERVER_URL, AUTH);
			DB1 = Couch/DB1_NAME;
			DB2 = Couch/DB2_NAME;
		}
		

		
		[SetUp]
		public void db_setup()
		{
			db_teardown();
			Couch.Create(DB1);
			Couch.Create(DB2);
		}
		
		[TearDown]
		public void db_teardown()
		{
			Couch.Delete(DB1);
			Couch.Delete(DB2);
		}
				
		[Test]
		public void CRUD()
		{			
			string id = "employee-1";
			string new_name = "Foo";
			Employee e1_1 = SampleData.readEmployee(id);
			
			Assert.AreEqual(e1_1.Id, id);
			Assert.AreNotEqual(e1_1.FirstName, new_name);
			Assert.IsNull(e1_1.Data._rev);
			
			DB1.Create(e1_1);
			string old_rev = e1_1.Data._rev;
			Assert.IsNotNull(old_rev);			
			
			e1_1.FirstName = new_name;			
			e1_1.Data.Update();
			Assert.IsNotNull(e1_1.Data._rev);
			Assert.AreNotEqual(old_rev, e1_1.Data._rev);
			
			Employee e1_2 = DB1.Read<Employee>(id);
			Assert.AreEqual(e1_1.Data._rev, e1_2.Data._rev);
			Assert.AreEqual(e1_1.FirstName, e1_2.FirstName);
			
			e1_1.FirstName = String.Empty;
			e1_1.Data.Update();

			Assert.AreNotEqual(e1_1.FirstName, e1_2.FirstName);
			e1_2.Data.Refresh();
			Assert.AreEqual(e1_1.FirstName, e1_2.FirstName);
			
			e1_2.Data.Delete();
			Assert.IsFalse(e1_2.Data.Refresh());
		}
		
		[Test]
		public void Query_View()
		{
			List<Employee> conflicts;
			Assert.IsTrue(bulk_insert(out conflicts));
			var rows1 = (DB1/"_all_docs").ListRows(new Query {include_docs=true, key="employee-12"});
			var rows2 = (DB1/"_all_docs").ListRows(new {keys=new object[]{"employee-12"}});
			Assert.AreEqual(rows1[0].doc._id, rows2[0].key);
		}
		
		[Test]
		public void Bulk()
		{
			List<Employee> conflicts;
			Assert.IsTrue(bulk_insert(out conflicts));
			var emp = DB1.Read("employee-1");
			Assert.IsTrue(emp.Delete());
			Assert.IsFalse(bulk_insert(out conflicts));
			Assert.AreEqual(conflicts.Count, 11);
		}
		
		bool bulk_insert(out List<Employee> conflicts)
		{
			var docs = new List<Employee>();
			for(int i=1; i<= 12; i++)
			{
				docs.Add(SampleData.readEmployee("employee-"+i.ToString()));
			}
			return DB1._bulk_docs<Employee>(docs, out conflicts);
		}
		
		[Test]
		public void Attachments()
		{
			List<Employee> conflicts;
			Assert.IsTrue(bulk_insert(out conflicts));
			foreach(Employee e in (DB1/"_all_docs").ListDocuments<Employee>(null))
			{	
				var att = e.Data.NewAttachment("picture", "image/jpg");
				var rev = att.DocumentRev;
				att.data = SampleData.readPicture(e.Id);
				DB1.SaveAttachment(att);
				Assert.Greater(att.length, 0);
				Assert.AreNotEqual(rev, att.DocumentRev);
			}	
			var emp = DB1.Read<Employee>("employee-1");
			var a = emp.Data.GetAttachment("picture");
			byte[] attachment = SampleData.readPicture("employee-1");
			a.data = attachment;
			DB1.SaveAttachment(a);
			try
			{
				var a2 = emp.Data.GetAttachment("picture");
				a2.data = attachment;
				DB1.SaveAttachment(a2);
				Assert.IsTrue(false);
			}
			catch (ResponseException re)
			{
				Assert.AreEqual((int)re.Response.Status, 409);
			}
			DB1.SaveAttachment(a);
			emp.Data.Refresh();
			a = emp.Data.GetAttachment("picture");
			a.data = attachment;
			DB1.SaveAttachment(a);
			byte[] db_attachment = DB1.ReadAttachment(a);
			Assert.AreNotSame(db_attachment, DB1.ReadAttachment(a));
			for(int i=0; i<attachment.Length;i++)
			{
				Assert.AreEqual(attachment[i],db_attachment[i]);
			}
			Assert.IsTrue(DB1.DeleteAttachment(a));
			Assert.IsNull(DB1.ReadAttachment(a));
		}
		
		[Test]
		public void Updates()
		{
			var e = SampleData.readEmployee("employee-1");
			DB1.Create(e);
			DB1.Create(SampleData.readDocument("_design"));
			DB1.Update("_design/design/_update/in_place", e.Id, new {param="foo", value="bar"});
			e.Data.Refresh();
			Assert.AreEqual((string)e.Data["foo"], "bar");
		}
		
		[Test]
		public void Changes()
		{
			var e = SampleData.readEmployee(listen_id);
			DB1.Create(e);
			listen_count = 0;
			var info = DB1.GetInfo();
			DB1.Changes(new Feed{feed = FeedMode.longpoll, since = info.update_seq}, listen);
			e.LastName += "x";
			e.Data.Update();
			e.LastName += "x";
			e.Data.Update();
			e.LastName += "x";
			e.Data.Update();
			
			for(int i=0; i<200;i++)
				Thread.Sleep(10);
			Assert.AreEqual(listen_count, 3);
		}
		
		int listen_count = 0;
		int listen_max_count = 3;
		string listen_id = "employee-1";
		
		bool listen(ChangeLog log, Exception ex) 
		{
			if (ex != null)
				return false;
			foreach(Change change in log.results)
			{
				if (change.id == listen_id)
					listen_count++;
			}
			return listen_count < listen_max_count;
		}
	}
}

