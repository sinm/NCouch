using System;
using NCouch;
using System.Collections.Generic;

namespace Employ.Model
{
	public class Database
	{
		DB db;
		
		public const string PICTURE_NAME = "picture";
		
		public Database (string url, string db_name)
		{
			db = new Server(url, null)/db_name;
		}
		
		public List<Employee> GetAll()
		{
			return (db/"_all_docs").ListDocuments<Employee>(new {include_docs=true, startkey="employee-0", endkey="employee-9999999"});
		}
		
		public void Touch()
		{
			if (TryOnline() && !db.Exists())
				db.Server.Create(db);
		}
		
		public bool TryOnline()
		{
			try
			{
				db.Exists();
				return true;
			}
			catch
			{
				return false;
			}
		}
		
		public bool recreateWithSampleData()
		{
			//recreate db
			db.Server.Delete(db);
			db.Server.Create(db);
			
			//push design
			db.Create(SampleData.readDocument("_design"));
			
			//prepare docs
			var docs = new List<Employee>();
			Employee e;
			string id;
			for(int i=1; i<= 12; i++)
			{
				id = "employee-" + i.ToString();
				e = SampleData.readEmployee(id);
				docs.Add(e);
				e.Data["_attachments"] = new {
					picture = new {
						content_type = "image/jpg", 
						data = Convert.ToBase64String(SampleData.readPicture(id))
					}
				};				
			}
			
			
			
			//bulk insert
			List<Employee> conflicts;
			var result = db._bulk_docs<Employee>(docs, out conflicts);		
			return result;
		}
		
		public byte[] GetPicture(Employee emp)
		{
			var att = emp.Data.GetAttachment(PICTURE_NAME);
			return att == null ? null : db.ReadAttachment(att);
		}
		
		public void DumpResponses()
		{
			ResponseCache.Instance.Dump();
		}
		
		public void BlindTest()
		{
			db.Update("_design/design/_update/in_place", "employee-1", new {param="foo", value="bar", attachments="true"});	
		}
	}
}

