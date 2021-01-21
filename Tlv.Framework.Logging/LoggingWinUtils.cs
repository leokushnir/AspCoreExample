using System;
using System.Collections.Generic;
using System.Text;

namespace Tlv.Framework.Logging
{
	internal class LoggingWinUtils:Logging.LoggingBaseUtils
	{
		static System.Collections.Generic.Dictionary<string,Logging.AppLogMetaData> loggerDict;
		static LoggingWinUtils()
		{
			loggerDict = new Dictionary<string, AppLogMetaData>();
		}
		public override string UserName
		{
			get 
			{
                string domain = System.Environment.UserDomainName;
                if(!string.IsNullOrEmpty(domain))
                    return domain + "\\" + System.Environment.UserName;
                else
                    return System.Environment.UserName;
			}
		}
		public override string  PageName
		{
			get
			{
				string name;
				name = System.AppDomain.CurrentDomain.FriendlyName;
				return name;
			}
		}

		public override string AppName
		{
			get 
			{
				string nameApp = base.AppName;
				if(nameApp==null)
					nameApp = AppDomain.CurrentDomain.FriendlyName;
				return nameApp;
			}
		}

        public override string GetLoggingFolder()
        {
            string str = base.GetLoggingFolder();
            if (str == null)
                str = AppDomain.CurrentDomain.BaseDirectory;
            return str;           
        }

		public override Dictionary<string, AppLogMetaData> LoggersCollection
		{
			get
			{
				return loggerDict;
			}
		}
	}
}
