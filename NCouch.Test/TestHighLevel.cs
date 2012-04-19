using System;
using NUnit.Framework;
using NCouch;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;

namespace NCouch.Test
{
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
			Employee e1_1 = readEmployee(id);
			
			Assert.AreEqual(e1_1.Id, id);
			Assert.AreNotEqual(e1_1.FirstName, new_name);
			Assert.IsNull(e1_1.Data._rev);
			
			DB1.Create(e1_1.Data);
			string old_rev = e1_1.Data._rev;
			Assert.IsNotNull(old_rev);			
			
			e1_1.FirstName = new_name;			
			DB1.Update(e1_1.Data);
			Assert.IsNotNull(e1_1.Data._rev);
			Assert.AreNotEqual(old_rev, e1_1.Data._rev);
			
			Employee e1_2 = new Employee(DB1.Read(id));
			Assert.AreEqual(e1_1.Data._rev, e1_2.Data._rev);
			Assert.AreEqual(e1_1.FirstName, e1_2.FirstName);
			
			e1_1.FirstName = String.Empty;
			DB1.Update(e1_1.Data);
			Assert.AreNotEqual(e1_1.FirstName, e1_2.FirstName);
			e1_2.Data.Refresh(DB1);
			Assert.AreEqual(e1_1.FirstName, e1_2.FirstName);
			
			DB1.Delete(e1_1.Data);
			e1_2.Data.Refresh(DB1);
			Assert.IsNull(e1_2.Data._id);
		}
		
		[Test]
		public void Query_View()
		{
			IList<Document> conflicts;
			Assert.IsTrue(bulk_insert(out conflicts));
			var rows1 = DB1/"_all_docs"/new Query {include_docs=true, key="employee-12"};
			var rows2 = DB1/"_all_docs"/new {keys=new object[]{"employee-12"}};
			Assert.AreEqual(rows1[0].doc._id, rows2[0].key);
		}
		
		[Test]
		public void Bulk()
		{
			IList<Document> conflicts;
			Assert.IsTrue(bulk_insert(out conflicts));
			Assert.IsTrue(DB1.Delete(DB1.Read("employee-1")));
			Assert.IsFalse(bulk_insert(out conflicts));
			Assert.AreEqual(conflicts.Count, 11);
		}
		
		bool bulk_insert(out IList<Document> conflicts)
		{
			Employee e;
			var docs = new List<Document>();
			for(int i=1; i<= 12; i++)
			{
				e = readEmployee("employee-"+i.ToString());
				docs.Add(e.Data);
			}

			return DB1._bulk_docs(docs, out conflicts);
		}
		
		Employee readEmployee(string id)
		{
			return new Employee(
				Document.Deserialize(
					File.ReadAllText(
						Environment.CurrentDirectory + "/Employees/" + id + ".json")));
		}
	}
}

