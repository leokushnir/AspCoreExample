using System;
using System.Collections.Generic;
using System.Text;

namespace Tlv.Framework.Logging
{
    public class AppLogMetaData
    {
        public string LoggerName;
        public string FileName;
        public string EventViewerSourceName;
        public Target DebugTarget;
        public Target InfoTarget;
        public Target WarningTarget;
        public Target ErrorTarget;
        public bool StackTraceForMessageIsRelevant;

        public AppLogMetaData(string loggerName,
            string fileName,
            string eventViewerSourceName,
            Target debugTarget,
            Target infoTarget,
            Target warningTarget,
            Target errorTarget,
            bool stackTraceForMessageIsRelevant)
        {
            this.LoggerName = loggerName;
            this.FileName = fileName;
            this.EventViewerSourceName = eventViewerSourceName;
            this.DebugTarget = debugTarget;
            this.InfoTarget = infoTarget;
            this.WarningTarget = warningTarget;
            this.ErrorTarget = errorTarget;
            this.StackTraceForMessageIsRelevant = stackTraceForMessageIsRelevant;
        }
    }
}