using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsGateways
{
    partial class Program
    {
        public static bool UseLocalEnviroment { get; private set; }

        public static bool IsTraceEnabled { get { return Program.GetArgumentAsBool("trace"); } }

        public static bool IsSimulated { get { return Program.GetArgumentAsBool("simulate"); } }

        public static bool GetArgumentAsBool(string name, bool defaultValue = true, bool nullValue = false)
        {
            bool result = defaultValue;
            var value = Program.GetArgumentValue(name);
            if (!string.IsNullOrEmpty(value))
            {
                bool.TryParse(value, out result);
            }
            else if (value == null) return nullValue;
            return result;
        }

        public static int GetArgumentAsInt(string name, int defaultValue = 0)
        {
            int result = defaultValue;
            var value = Program.GetArgumentValue(name);
            if (!string.IsNullOrEmpty(value))
            {
                int.TryParse(value, out result);
            }

            return result;
        }

        public static string GetArgumentValue(string name)
        {
            if (!name.StartsWith("/")) name = "/" + name.ToLower();
            foreach (var s in Environment.GetCommandLineArgs())
            {
                var tokens = s.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (string.Compare(name, tokens[0], true) == 0)
                {
                    if (tokens.Length > 1)
                    {
                        return tokens[1];
                    }
                    else return string.Empty;
                }

            }

            return null;
        }

        public static string GetAppSettingValue(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }


        public static bool GetAppSettingAsBoolean(string name, bool defaultValue = false)
        {
            bool result = defaultValue;
            var value = GetAppSettingValue(name);
            if (!string.IsNullOrEmpty(value))
            {
                bool.TryParse(value, out result);
            }

            return result;
        }
    }
}
