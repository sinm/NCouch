using System;
using System.IO;
using NCouch;
using System.Text;

namespace NCouch.Test
{
	public class Employee : IData
	{		
		/*public Employee (string id) : this(new Document(id))
		{
		}
		
		public Employee (Document doc)
		{
			Data = doc;
		}*/
		
		public Document Data {get; set;}
		
		public string Id
		{
			get {return Data.Id;}
			set {Data.Id = value;}
		}
		
		public string ManagerId
		{
			get {return Data["managerId"] as string;}
			set {Data["managerId"] = value;}
		}
		
		public string FirstName
		{
			get {return Data["firstName"] as string;}
			set {Data["firstName"] = value;}
		}
		
		public string LastName
		{
			get {return Data["lastName"] as string;}
			set {Data["lastName"] = value;}
		}
		
	}
}

