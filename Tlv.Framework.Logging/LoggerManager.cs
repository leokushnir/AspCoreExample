using System;
using System.Data;
using System.Configuration;
using System.Web;

using System.Collections.Generic;
using System.Web.Caching;
using System.Runtime.CompilerServices;

namespace Tlv.Framework.Logging
{
    /// <summary>
    /// Summary description for Logger
    /// </summary>
    internal static class LoggerManager
    {
        private static Logging.LoggingBaseUtils _helpUtil;

        static LoggerManager()
        {
            SetUtil();
        }

        private static void SetUtil()
        {
            _helpUtil = GetActualUtil();
        }

        private static Logging.LoggingBaseUtils GetActualUtil()
        {
            try
            {
                if (System.Web.Hosting.HostingEnvironment.IsHosted)
                    return new Logging.LoggingHttpUtils();
                else
                    return new Logging.LoggingWinUtils();
            }
            catch (Exception ex)
            {
                return new Logging.LoggingWinUtils();
            }
        }

        public static Logging.LoggingBaseUtils HelpUtil
        {
            get
            {
                return _helpUtil;
            }
        }

        public static AppLogMetaData GetLogger(string name)
        {
            bool utilExists;
            bool itemExists;
            Dictionary<string, AppLogMetaData> logCollecttion;

            try
            {
                utilExists = null != _helpUtil;
            }
            catch (Exception ex)
            {
                ProjectErrLogger.WriteLoggerLog("Fail on: utilExists = null != _helpUtil;", ex);
                return null;
            }

            try
            {
                logCollecttion = _helpUtil.LoggersCollection;
            }
            catch (Exception ex)
            {
                ProjectErrLogger.WriteLoggerLog("Fail on: logCollecttion = _helpUtil.LoggersCollection;", ex);
                try
                {
                    AppLogMetaData appLogMd = GetLogMetadata(name);
                    return appLogMd;
                }
                catch (Exception xx)
                {
                    ProjectErrLogger.WriteLoggerLog(string.Format("Fail to create instance of AppLogMetaData. Action: AppLogMetaData appLogMd = GetLogMetadata({0}", name), xx);
                    return null;
                }
            }
            try
            {
                itemExists = logCollecttion.ContainsKey(name);
            }
            catch (Exception ex)
            {
                ProjectErrLogger.WriteLoggerLog(string.Format("Fail on: itemExists = logCollecttion.ContainsKey('{0}');", name), ex);
                return null;
            }
            if (itemExists)
                try
                {
                    return logCollecttion[name];
                }
                catch (Exception ex)
                {
                    ProjectErrLogger.WriteLoggerLog("Fail on: return logCollecttion[name];", ex);
                    return null;
                }
            else
            {
                try
                {
                    AppLogMetaData appLogMd = CreateLogger(name);
                    return appLogMd;
                }
                catch (Exception ex)
                {
                    ProjectErrLogger.WriteLoggerLog(string.Format("Fail on: CreateLogger('{0}');", name), ex);
                    return null;
                }
                //try
                //{
                //    return _helpUtil.LoggersCollection[name];
                //}
                //catch (Exception ex)
                //{
                //    ProjectErrLogger.WriteLoggerLog(string.Format("Fail on: return _helpUtil.LoggersCollection['{0}']", name), ex);
                //    return null;
                //}
            }
        }

        private static AppLogMetaData GetLogMetadata(string name)
        {
            string filename_last_part = DateTime.Now.ToString("yyyy_MM_dd") + ".log";
            string logging_folder = _helpUtil.GetLoggingFolder();
            string default_filename = _helpUtil.GetDefaultFileName();
            string LogFileName;
            if (string.IsNullOrEmpty(name) == true)
                LogFileName = logging_folder + default_filename + filename_last_part;
            else
                LogFileName = logging_folder + name + "\\" + name + filename_last_part;
            string EventViewerSourceName = _helpUtil.GetEventViewerSourceName();

            AppLogMetaData appLogMd = new AppLogMetaData(name,
                LogFileName,
                EventViewerSourceName,
                _helpUtil.GetTargetForDebugLevel(),
                _helpUtil.GetTargetForInfoLevel(),
                _helpUtil.GetTargetForWarningLevel(),
                _helpUtil.GetTargetForErrorLevel(),
                _helpUtil.GetStackTraceForMessageIsRelevant());
            return appLogMd;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static AppLogMetaData CreateLogger(string name)
        {
            AppLogMetaData appLogMd;
            if (_helpUtil.LoggersCollection.TryGetValue(name, out appLogMd))
                return appLogMd;
            appLogMd = GetLogMetadata(name);
            _helpUtil.LoggersCollection[name] = appLogMd;
            return appLogMd;
        }
    }
}