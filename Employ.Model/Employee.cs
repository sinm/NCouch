using System;
using System.IO;
using NCouch;
using System.Text;

namespace Employ.Model
{
	public class Employee : IData
	{				
		public Document Data {get; set;}
		
		public object Thumb;
		
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
		
		public string Department
		{
			get {return Data["department"] as string;}
			set {Data["department"] = value;}
		}
		
		public string Title
		{
			get {return Data["title"] as string;}
			set {Data["title"] = value;}
		}
		
		public string Name
		{
			get 
			{
				return String.Format("{0} {1}.", FirstName, LastName.Substring(0,1));
			}
		}
	}
}

