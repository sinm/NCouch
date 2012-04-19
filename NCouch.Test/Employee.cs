using System;
using System.IO;
using NCouch;
using System.Text;

namespace NCouch.Test
{
	public class Employee
	{		
		public Employee (string id) : this(new Document(id))
		{
		}
		
		public Employee (Document doc)
		{
			Data = doc;
		}
		
		public readonly Document Data;
		
		public string Id
		{
			get {return Data._id;}
			set {Data._id = value;}
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

