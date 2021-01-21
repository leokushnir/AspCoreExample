using System;
using System.Collections.Generic;
using System.Text;

namespace Tlv.Framework.Logging
{
	internal class LoggingHttpUtils:Logging.LoggingBaseUtils
	{
		public override string UserName
		{
			get
			{
				string name = null;
				try
				{
					name = System.Web.HttpContext.Current.User.Identity.Name;
				}
				catch (Exception ex)
				{
					ProjectErrLogger.WriteLoggerLog(string.Format("Fail on: System.Web.HttpContext.Current.User.Identity.Name.{0}At LoggingHttpUtils.UserName.", Environment.NewLine));
					name = "<Fail to get UserName>";
				}
				if (string.IsNullOrEmpty(name))
				{
					try
					{
						if (!System.Web.HttpContext.Current.User.Identity.IsAuthenticated)
							return "<anonymous>";
						else
							return "N/A";

					}
					catch (Exception ex)
					{
						ProjectErrLogger.WriteLoggerLog(string.Format("Fail on: System.Web.HttpContext.Current.User.Identity.IsAuthenticated.{0}At LoggingHttpUtils.UserName.", Environment.NewLine));
						return "<Fail to determine if user is anonymous";
					}
				}
				else
					return name;

			}
		}
		public override string PageName
		{
			get
			{
				string fullPagePath = null;
				try
				{
					fullPagePath = System.Web.HttpContext.Current.Request.CurrentExecutionFilePath;
				}
				catch (Exception ex)
				{
					ProjectErrLogger.WriteLoggerLog(string.Format("Fail on: System.Web.HttpContext.Current.Request.CurrentExecutionFilePath.{0}At LoggingHttpUtils.PageName.", Environment.NewLine));
					return "<Fail to determine PageName>";
				}


				int startIndex = fullPagePath.LastIndexOf("/") + 1;//to remove "/"

				if (startIndex >= 0)
				{
					int pageNameLen = fullPagePath.Length - startIndex;
					return fullPagePath.Substring(startIndex, pageNameLen);
				}
				else
					return fullPagePath;

			}		
		}

		public override string AppName
		{
			get
			{
				string nameApp = base.AppName;
				if (nameApp == null)
					try
					{
						nameApp = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
					}
					catch (Exception ex)
					{
						ProjectErrLogger.WriteLoggerLog(string.Format("Fail on: AppDomain.CurrentDomain.SetupInformation.ApplicationBase.{0}At LoggingHttpUtils.AppName.", Environment.NewLine));
						return "<Fail to get AppName>";
					}
				return nameApp;
			}
		}

		public override Dictionary<string, AppLogMetaData> LoggersCollection
		{
			get
			{
				if (CacheObj[LoggersManagerCacheKey] == null)
					CreateLoggersCollection();
				return CacheObj[LoggersManagerCacheKey] as Dictionary<string, AppLogMetaData>;
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
		static private void CreateLoggersCollection()
		{
			if (CacheObj[LoggersManagerCacheKey] == null)
                CacheObj[LoggersManagerCacheKey] = new Dictionary<string, AppLogMetaData>();
        }

		private static System.Web.Caching.Cache CacheObj
		{
			get
			{
				try
				{
					return System.Web.Hosting.HostingEnvironment.Cache;
				}
				catch (Exception ex)
				{
					ProjectErrLogger.WriteLoggerLog(string.Format("Fail to run: System.Web.Hosting.HostingEnvironment.Cache.{0}At: LoggingHttpUtils.CacheObj.", Environment.NewLine),ex);
					throw ex;
				}
			}
		}
	}
}
