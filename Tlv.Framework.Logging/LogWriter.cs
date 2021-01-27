using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Tlv.Framework.ClassFactory;

namespace Tlv.Framework.Logging
{
    public sealed class LogWriter
    {
        private static readonly Lazy<LogWriter> lazy = new Lazy<LogWriter>(() => new LogWriter());

        public static LogWriter Instance { get { return lazy.Value; } }

        private LogWriter()
        {
        }

        private const string GeneralSource = "tlv_log";

        /// <summary>
        ///     The logger we will use to log transactions to help with debugging.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        ///     The my log message.
        /// </summary>
        private static readonly StructuredMessage StructuredMessage = new StructuredMessage();

        private AppLogMetaData _appMd;
        private LoggingBaseUtils _helpUtil;

        public void Init(AppLogMetaData appLogMetaData, LoggingBaseUtils helpUtil)
        {
            _helpUtil = helpUtil;
            _appMd = appLogMetaData;
            Setup();
        }

        /// <summary>
        ///     Log4Net configuration
        /// </summary>
        private void Setup()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout
            {
                ConversionPattern =
                    "{\"@timestamp\":\"%date{ISO8601}\",\"thread\":\"[%thread]\",\"log.level\":\"%-5level\",\"EntityName\":\"%property{EntityName}\",\"message\":%message}%newline"
            };
            patternLayout.ActivateOptions();

            var roller = new RollingFileAppender
            {
                AppendToFile = true,
                File = new FileInfo(_appMd.FileName).DirectoryName + $@"\{_appMd.LoggerName}.json",
                DatePattern = "dd_MM_yyyy",
                Layout = patternLayout,
                MaxSizeRollBackups = 2,
                MaximumFileSize = "1MB",
                RollingStyle = RollingFileAppender.RollingMode.Date,
                StaticLogFileName = false,
                PreserveLogFileNameExtension = true,
            };
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            var memory = new MemoryAppender();
            memory.ActivateOptions();
            hierarchy.Root.AddAppender(memory);

            hierarchy.Root.Level = log4net.Core.Level.Info;
            hierarchy.Configured = true;
        }

        public void Write2Log(string msg, Exception ex, Level l, Target target, Logger.ScreenShowHandler screenHandler)
        {
            string messageToWrite = "";
            if ((target & Target.File) == Target.File)
            {
                switch (l)
                {
                    case Level.Debug:
                    case Level.Info:
                        messageToWrite = MessageFormat1(msg, ex, l);
                        WriteToFile(messageToWrite, l);
                        break;

                    case Level.Warning:

                    case Level.Error:

                    case Level.Fatal:
                        Logger.Fatal(StructuredMessage.GetMessage(Guid.NewGuid().ToString(), _helpUtil.PageName, msg,
                            ex?.Message, ex?.StackTrace, ex, _appMd));

                        //Logger.Error(logEvent.Create(new LoggingEvent()
                        //    {
                        //);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(l), l, null);
                }
                return;
                ;
                //WriteToFile(messageToWrite, l);
            }
            messageToWrite = MessageFormat1(msg, ex, l);
            var write2EventViewer = (target & Target.Ev) == Target.Ev;
            if (write2EventViewer) WriteToEventViewer(messageToWrite, l);

            if ((target & Target.Db) == Target.Db && !WriteToDb(msg, ex, l) && !write2EventViewer) WriteToEventViewer(messageToWrite, l);

            if (
                screenHandler != null && (target & Target.Screen) == Target.Screen)
                WriteToScreen(messageToWrite, l, screenHandler);
        }

        private void WriteToScreen(string msg, Level l, Logger.ScreenShowHandler screenHandler)
        {
            try
            {
                screenHandler(msg, l);
            }
            catch (Exception ex)
            {
                WriteGeneralErrorToEventLog(msg);
                WriteGeneralErrorToEventLog(GetFormatedMessage(msg, ex));
            }
        }

        private bool CanWriteToTarget(Target target, Level level)
        {
            switch (level)
            {
                case Level.Error:
                case Level.Fatal:
                    return (target & _appMd.ErrorTarget) == target;

                case Level.Debug:
                    return (target & _appMd.DebugTarget) == target;

                case Level.Info:
                    return (target & _appMd.InfoTarget) == target;

                case Level.Warning:
                    return (target & _appMd.WarningTarget) == target;

                default:
                    return false;
            }
        }

        private void WriteToEventViewer(string msg, Level l)
        {
            if (CanWriteToTarget(Target.Ev, l) == false)
                return;
            try
            {
                EventLog.WriteEntry(
                    _appMd.EventViewerSourceName,
                    msg,
                    GetEvLevel(l)
                );
            }
            catch (Exception ex)
            {
                WriteGeneralErrorToEventLog(
                    "AppName : " + _appMd.LoggerName + Environment.NewLine + ex
                );
                WriteGeneralErrorToEventLog(msg);
            }
        }

        private void WriteToFile(string msg, Level l)
        {
            if (CanWriteToTarget(Target.File, l) == false)
                return;
            var lineSeparator = "====================";
            var s = string.Format("{2}{1}{0}{1}{1}", msg, Environment.NewLine, lineSeparator);
            try
            {
                WriteToFileSync(_appMd.FileName, s);
            }
            catch (Exception e)
            {
                WriteGeneralErrorToEventLog(msg);
                WriteGeneralErrorToEventLog(e.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void WriteToFileSync(string fileName, string msg)
        {
            CheckDirForFile(fileName);
            File.AppendAllText(fileName, msg);
        }

        private static void CheckDirForFile(string fileName)
        {
            var dir = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir ?? string.Empty);
        }

        private bool WriteToDb(string msg, Exception ex, Level l)
        {
            if (CanWriteToTarget(Target.Db, l) == false)
                return true;
            var success = false;
            string exCategory = "N/A", exMessage = "", exStackTrace = "", exInner = "";
            var SubAppName = "";
            if (msg == null) msg = "";
            if (ex != null)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                    exMessage = ex.Message;
                if (!string.IsNullOrEmpty(ex.StackTrace))
                    exStackTrace = ex.StackTrace;
                exInner = InnerException2Str(ex);
                exCategory = ex.GetType().ToString();
            }

            try
            {
                var dal = ServiceFactory.GetTlvDal();
                dal.ExecNonQryPrms("tlvLog", "InsertToLog",
                    new object[]
                    {
                        _helpUtil.UserName,
                        _helpUtil.AppName,
                        SubAppName,
                        _helpUtil.PageName,
                        (int) l,
                        exCategory,
                        msg,
                        exMessage,
                        exStackTrace,
                        exInner,
                        DateTime.Now
                    }
                );
                success = true;
            }
            catch (Exception e)
            {
                var subject = "Reason: Fail to write the log to the database." + Environment.NewLine;
                WriteGeneralErrorToEventLog(subject + e);
            }

            return success;
        }

        private void WriteGeneralErrorToEventLog(string msg)
        {
            try
            {
                EventLog.WriteEntry(
                    GeneralSource,
                    msg,
                    EventLogEntryType.Error
                );
            }
            catch (Exception)
            {
                //Nothing to Do
            }
        }

        private string MessageFormat1(string msg, Exception ex, Level level)
        {
            var formatedMsg = GetFormatedMessage(msg, ex);
            var sb = new StringBuilder(1024);
            sb.AppendFormat("Time: {0}{1}Guid: {2}{1}Level: {3}{1}", DateToStringAbsolute(DateTime.Now),
                Environment.NewLine, Guid.NewGuid().ToString(), level.ToString());
            sb.Append(formatedMsg);
            return sb.ToString();
        }

        private string GetFormatedMessage(string msg, Exception ex)
        {
            var stb = new StringBuilder(1024);
            stb.AppendFormat("AppName: {0}", _appMd.LoggerName ?? "N/A");
            var entity = _helpUtil.PageName;
            stb.AppendFormat("{0}Entity: {1}", Environment.NewLine, entity ?? "N/A");
            var username = _helpUtil.UserName;
            stb.Append($"{Environment.NewLine}UserName: {username ?? "N/A"}");
            stb.AppendFormat("{0}Principal Name: {1}", Environment.NewLine, GetSecurityPrincipalName());
            if (ex != null) stb.AppendFormat("{0}Category: {1}", Environment.NewLine, ex.GetType());
            if (!string.IsNullOrEmpty(msg))
                stb.AppendFormat("{0}Message: {1}", Environment.NewLine, msg);
            if (ex != null)
            {
                stb.AppendFormat("{0}Exp Message: {1}", Environment.NewLine,
                    ex.Message ?? "<null>");
                if (ex.InnerException != null)
                    stb.AppendFormat("{0}InnerException: {1}", Environment.NewLine, InnerException2Str(ex));
                if (ex.StackTrace != null)
                    stb.AppendFormat("{0}StackTrace: {1}", Environment.NewLine, ex.StackTrace);
            }
            else if (_appMd.StackTraceForMessageIsRelevant)
            {
                stb.Append(Environment.NewLine + "Trace Information:" + GetFileStackTrace());
            }

            return stb.ToString();
        }

        private string GetFileStackTrace()
        {
            var stb = new StringBuilder();
            //Build a stack trace from one frame, skipping the current
            //frame and using the next frame.

            // The first 5 are the logger frames.
            // If this number changes then the number parameter must be changed too.
            var st = new StackTrace(5, true);
            //StackTrace st = new StackTrace(true);
            for (
                var i = 0;
                i < st.FrameCount;
                i++)
            {
                // Build a stack trace from one frame, skipping the  6  frames of logger functions
                var sf = st.GetFrame(i);
                stb.Append(Environment.NewLine + sf.GetMethod());
                var fileName = sf.GetFileName();
                if (string.IsNullOrEmpty(fileName) == false)
                    stb.AppendFormat(" at: {0}:Line {1}", sf.GetFileName(), sf.GetFileLineNumber());
            }

            return stb.ToString();
        }

        private EventLogEntryType GetEvLevel(Level l)
        {
            switch (l)
            {
                case Level.Warning:
                    return EventLogEntryType.Warning;

                case Level.Error:
                    return EventLogEntryType.Error;

                case Level.Fatal:
                    return EventLogEntryType.Error;

                default:
                    return EventLogEntryType.Information;
            }
        }

        private string DateToStringAbsolute(DateTime d)
        {
            return d.ToString("yyyy/MM/dd HH:mm:ss.fff");
        }

        private string GetSecurityPrincipalName()
        {
            try
            {
                return WindowsIdentity.GetCurrent().Name;
            }
            catch (Exception)
            {
                return "<Fail to evaluate>";
            }
        }

        private string InnerException2Str(Exception ex)
        {
            var innerEx = "";
            if (ex.InnerException != null) innerEx = ex.InnerException.ToString();

            return innerEx;
        }
    }
}