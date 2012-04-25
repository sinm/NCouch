using System;
using System.Collections.Generic;

namespace NCouch
{
	public class DBInfo
	{
		/*
		 	db_name
				Name of the database (string)
			doc_count
				Number of documents (including design documents) in the database (int)
			update_seq
				Current number of updates to the database (int)
			purge_seq
				Number of purge operations (int)
			compact_running
				Indicates, if a compaction is running (boolean)
			disk_size
				Current size in Bytes of the database (Note: Size of views indexes on disk are not included)
			instance_start_time
				Timestamp of CouchDBs start time (int in ms)
			disk_format_version
				Current version of the internal database format on disk (int)
		 */
		public readonly string db_name;
		public readonly int doc_count;
		public readonly int update_seq;
		public readonly int purge_seq;
		public readonly bool compact_running;
		public readonly long disk_size;
		public readonly long instance_start_time;
		public readonly int disk_format_version;
		public DBInfo (Dictionary<string, object> dict)
		{
			db_name = (string)dict["db_name"];
			doc_count = int.Parse(dict["doc_count"].ToString());
			update_seq = int.Parse(dict["update_seq"].ToString());
			purge_seq = int.Parse(dict["purge_seq"].ToString());
			compact_running = (bool)dict["compact_running"];
			disk_size = long.Parse(dict["disk_size"].ToString());
			instance_start_time = long.Parse(dict["instance_start_time"].ToString());
			disk_format_version = int.Parse(dict["disk_format_version"].ToString());
		}
	}
}

