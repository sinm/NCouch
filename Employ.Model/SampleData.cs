using System;
using NCouch;
using System.IO;
using System.Collections.Generic;

namespace Employ.Model
{
	public static class SampleData
	{
		public const string SAMPLE_DIR = @"SampleData";
		
		public static Employee readEmployee(string id)
		{
			Employee e = new Employee();
			e.Data = readDocument(id);
			return e;
		}
		
		public static Document readDocument(string id)
		{
			return Document.FromFile(Environment.CurrentDirectory + "/" + SAMPLE_DIR + "/" + id + ".json");
		}
		
		public static byte[] readPicture(string id)
		{
			return File.ReadAllBytes(Environment.CurrentDirectory + "/" + SAMPLE_DIR + "/" + id + ".jpg");
		}
	}
}

