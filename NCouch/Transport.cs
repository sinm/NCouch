using System;
using System.Text;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace NCouch
{
	public abstract class Transport
	{
		public string ContentType;
		public string ETag;
		public string GetUnquotedETag()
		{
			if (!String.IsNullOrEmpty(ETag) && ETag.Length > 2)
				return ETag.Substring(1, ETag.Length-2);
			return String.Empty;
		}
		
		public byte[] Body;
		
		[ScriptIgnore]
		public string Text
		{
			get
			{
				return Body == null ? null : Encoding.UTF8.GetString(Body);
			}
			set
			{
				Body = value == null ? null : Encoding.UTF8.GetBytes(value);
			}
		}
		
		[ScriptIgnore]
		public long Size
		{
			get
			{
				return Body == null ? 0 : Body.Length;
			}
		}
		
		public void SetObject(object o)
		{
			Text = (new JavaScriptSerializer()).Serialize(o);
		}
		
		public object GetObject()
		{
			return (new JavaScriptSerializer()).DeserializeObject(Text);
		}
		
		/*public bool TryGetObject(out object obj)
		{
			try
			{
				obj = GetObject();
				return obj != null;
			}
			catch
			{
				obj = null;
				return false;
			}
		}*/
		
		public object Parse(params object[] path)
		{
			object obj = GetObject();			
			uint index;
			foreach(object cmp in path)
			{
				if (UInt32.TryParse(cmp.ToString(), out index))
				{
					obj = (obj as object[])[index];					
				}
				else
				{
					obj = (obj as Dictionary<string, object>)[cmp.ToString()];
				}
			}
			return obj;
		}
		
		public bool TryParse(out object result, params object[] path)
		{
			try
			{
				result = Parse(path);
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}
	}
}

