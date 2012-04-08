using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;

namespace NCouch
{
    public static class Config
    {
        public static string DefaultConfigSection = "appSettings";
		
		public static long GetLong(string param, long default_value)
        {
            return GetLong(DefaultConfigSection, param, default_value);
        }
        public static long GetLong(string section, string param, long default_value)
        {
            return getAny<long>(section, param, default_value,
                delegate(string s) { return long.Parse(s); });
        }	
		
        public static int GetInt(string param, int default_value)
        {
            return GetInt(DefaultConfigSection, param, default_value);
        }
        public static int GetInt(string section, string param, int default_value)
        {
            return getAny<int>(section, param, default_value,
                delegate(string s) { return Int32.Parse(s); });
        }

        public static string GetString(string param, string default_value)
        {
            return GetString(DefaultConfigSection, param, default_value);
        }
        public static string GetString(string section, string param, string default_value)
        {
            return getAny<string>(section, param, default_value,
                delegate(string s) { return s; });
        }

        public static bool GetBool(string param, bool default_value)
        {
            return GetBool(DefaultConfigSection, param, default_value);
        }
        public static bool GetBool(string section, string param, bool default_value)
        {
            return getAny<bool>(section, param, default_value,
                delegate(string s) { return Boolean.Parse(s); });
        }

        static Dictionary<string, object> m_ConfigValues = new Dictionary<string, object>();
		
        static T getAny<T>(string section, string param, T default_value, Func<string, T> func)
        {
            object res;
            string section_param = section + "::" + param;
            lock (m_ConfigValues)
            {
                if (!m_ConfigValues.TryGetValue(section_param, out res))
                {
                    try
                    {
                        string s = ((NameValueCollection)ConfigurationManager.GetSection(section))[param];
                        if (s == null)
                            res = default_value;
                        else
                            res = func(s);
                    }
                    catch
                    {
                        res = default_value;
                    }
                    m_ConfigValues.Add(section_param, res);
                }
            }
            return (T)res;
        }
    }
}
