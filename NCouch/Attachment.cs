using System;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace NCouch
{
	public class Attachment
	{
		public string DocumentId;
		public string DocumentRev;
		public string Name;
		public string content_type;
		public long length;
		public int revpos;
		public bool stub = true;
		public byte[] data;
	}
}

