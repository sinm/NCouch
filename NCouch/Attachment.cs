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
		public string ContentType;
		public long Length;
		public int RevPos;
		public bool Stub = true;
		public byte[] Data;
	}
}

