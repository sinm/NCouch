using System;

namespace NCouch
{
	//TODO: support for the "type" attribute... TypeResolver, type att name, etc, CouchTypeAttribute
	public interface IData
	{
		Document Data{get;set;}
	}
}

