using System;
using NUnit.Framework;
using NCouch;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using Employ.Model;

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
			Assert.IsNull(e1_1.Data.Rev);
			
			DB1.Create(e1_1);
			string old_rev = e1_1.Data.Rev;
			Assert.IsNotNull(old_rev);			
			
			e1_1.FirstName = new_name;			
			DB1.Update(e1_1);
			Assert.IsNotNull(e1_1.Data.Rev);
			Assert.AreNotEqual(old_rev, e1_1.Data.Rev);
			
			Employee e1_2 = DB1.Read<Employee>(id);
			Assert.AreEqual(e1_1.Data.Rev, e1_2.Data.Rev);
			Assert.AreEqual(e1_1.FirstName, e1_2.FirstName);
			
			e1_1.FirstName = String.Empty;
			DB1.Update(e1_1);
			Assert.AreNotEqual(e1_1.FirstName, e1_2.FirstName);
			DB1.Refresh(e1_2);
			Assert.AreEqual(e1_1.FirstName, e1_2.FirstName);
			
			DB1.Delete(e1_1);
			DB1.Refresh(e1_2);
			Assert.IsNull(e1_2.Id);
		}
		
		[Test]
		public void Query_View()
		{
			List<Employee> conflicts;
			Assert.IsTrue(bulk_insert(out conflicts));
			var rows1 = DB1/"_all_docs"/new Query {include_docs=true, key="employee-12"};
			var rows2 = DB1/"_all_docs"/new {keys=new object[]{"employee-12"}};
			Assert.AreEqual(rows1[0].doc.Id, rows2[0].key);
		}
		
		[Test]
		public void Bulk()
		{
			List<Employee> conflicts;
			Assert.IsTrue(bulk_insert(out conflicts));
			Assert.IsTrue(DB1.Delete(DB1.Read("employee-1")));
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
				att.Data = SampleData.readPicture(e.Id);
				DB1.SaveAttachment(att);
				Assert.Greater(att.Length, 0);
				Assert.AreNotEqual(rev, att.DocumentRev);
			}	
			var emp = DB1.Read<Employee>("employee-1");
			var a = emp.Data.GetAttachment("picture");
			byte[] attachment = SampleData.readPicture("employee-1");
			a.Data = attachment;
			DB1.SaveAttachment(a);
			try
			{
				var a2 = emp.Data.GetAttachment("picture");
				a2.Data = attachment;
				DB1.SaveAttachment(a2);
				Assert.IsTrue(false);
			}
			catch (ResponseException re)
			{
				Assert.AreEqual((int)re.Response.Status, 409);
			}
			DB1.SaveAttachment(a);
			DB1.Refresh(emp);
			a = emp.Data.GetAttachment("picture");
			a.Data = attachment;
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
			DB1.Refresh(e);
			Assert.AreEqual((string)e.Data["foo"], "bar");
		}
	}
}

