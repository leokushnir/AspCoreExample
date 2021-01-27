using System;
using System.Collections.Generic;
using System.Text;

///expected keys :
/// Logging.WriteStackTraceForMessage
/// Logging.DefaultFileName
/// Logging.LoggingFolder
/// Logging.EventViewerSourceName
///
/// tlvAppName
///
/// Logging.DebugTarget (  Logging.DebugTarget="Db=0,File=0,EventViewer=0,Screen=0" )
/// Logging.InfoTarget
/// Logging.WarningTarget
/// Logging.ErrorTarget

namespace Tlv.Framework.Logging
{
    public abstract class LoggingBaseUtils
    {
        protected const string LoggersManagerCacheKey = "tlv_LogMngCache_1.0"; //"tlv_LoggersManagerCacheKey";
        public abstract string UserName { get; }
        public abstract string PageName { get; }
        public abstract Dictionary<string, AppLogMetaData> LoggersCollection { get; }

        public virtual string AppName
        {
            get
            {
                string name;
                name = GetFromConfig("tlvAppName");
                return name;
            }
        }

        public virtual string GetLoggingFolder()
        {
            string str = GetFromConfig("Logging.LoggingFolder");
            if (string.IsNullOrEmpty(str))
            {
            }
            else
            {
                if (str.LastIndexOf("\\") != str.Length - 1)
                {
                    str += "\\";
                }
            }
            return str;
        }

        public virtual string GetDefaultFileName()
        {
            return GetFromConfig("Logging.DefaultFileName");
        }

        public virtual bool GetStackTraceForMessageIsRelevant()
        {
            String value = GetFromConfig("Logging.WriteStackTraceForMessage");
            if (string.IsNullOrEmpty(value))
                return false;
            return (value.ToLower().Trim() == "true");
        }

        public virtual string GetEventViewerSourceName()
        {
            return GetFromConfig("Logging.EventViewerSourceName");
        }

        public Target GetTargetForDebugLevel()
        {
            return GetTargetForLoggingLevel("Logging.DebugTarget");
        }

        public Target GetTargetForErrorLevel()
        {
            return GetTargetForLoggingLevel("Logging.ErrorTarget");
        }

        public Target GetTargetForInfoLevel()
        {
            return GetTargetForLoggingLevel("Logging.InfoTarget");
        }

        public Target GetTargetForWarningLevel()
        {
            return GetTargetForLoggingLevel("Logging.WarningTarget");
        }

        private Target GetTargetForLoggingLevel(string config_key)
        {
            Target target = Target.None;
            string str = GetFromConfig(config_key);
            if (!string.IsNullOrEmpty(str))
            {
                str = str.Trim().ToLower();

                if (str.Contains("db=1"))
                    target |= Target.Db;
                if (str.Contains("file=1"))
                    target |= Target.File;
                if (str.Contains("eventviewer=1"))
                    target |= Target.Ev;
                if (str.Contains("screen=1"))
                    target |= Target.Screen;
            }
            return target;
        }

        public virtual string GetFromConfig(string key)
        {
            string strval = null;
            try
            {
                strval = System.Configuration.ConfigurationManager.AppSettings[key];
            }
            catch (Exception e)
            {
            }

            return strval;
        }
    }
}