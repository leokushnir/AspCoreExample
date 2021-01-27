using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;
using Tlv.Framework.Logging.TrackerNS;

namespace Tlv.Framework.Logging
{
    public static class LogEntryTemp
    {
        public static int EventID { get; set; }
        public static string ClientUser { get; set; }
        public static string ClientIP { get; set; }
        public static bool EventIDSpecified { get; set; }
        public static string Component { get; set; }
    }

    public class Logger
    {
        public delegate void ScreenShowHandler(string message, Level l);

        private static Logging.LoggingBaseUtils _helpUtil;

        //static private LogEntryTemp _logEntry;
        private const int EmptyEventID = -777;

        //private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // private static log4net.ILog log = log4net.LogManager.GetLogger(@"D:\logs\Config\log4net.config");
        // private static ILog log = LogManager.GetLogger("LOGGER");

        private static void Init()
        {
            LogEntryTemp.EventID = EmptyEventID;
        }

        static Logger()
        {
            _helpUtil = LoggerManager.HelpUtil;
            Init();
            //LogEntryTemp.EventID = EmptyEventID;
        }

        public static void Write_Tracer(string msg, string tracerConfigFilePath, string logName, Level priority)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(typeof(Logger));
            string configParam = ConfigurationManager.AppSettings["TracerConfigFilePath"];

            if (!string.IsNullOrEmpty(configParam) && string.IsNullOrEmpty(tracerConfigFilePath))
            {
                tracerConfigFilePath = configParam;
            }

            if (string.IsNullOrEmpty(tracerConfigFilePath))
            {
                tracerConfigFilePath = @"D:\TlvApp\config\log4net.config";
            }

            try
            {
                FileInfo fi = new FileInfo(tracerConfigFilePath);

                log4net.GlobalContext.Properties["LogName"] = logName;
                if (HttpRuntime.AppDomainAppId != null)
                {
                    //is web app
                    //log4net.GlobalContext.Properties["UserName"] = System.Web.HttpContext.Current.User.Identity.Name;

                    HttpContext context = HttpContext.Current;
                    string username = string.Empty;

                    if (context != null && context.User != null && context.User.Identity.IsAuthenticated)
                    {
                        //MDC.Set("user", HttpContext.Current.User.Identity.Name);

                        username = " HTTPUser: " + HttpContext.Current.User.Identity.Name;
                    }

                    string appPoolusername = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    log4net.MDC.Set("UserName", appPoolusername + username);
                }
                else
                {
                    //is windows app
                    //log4net.GlobalContext.Properties["UserName"] = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    log4net.MDC.Set("UserName", System.Security.Principal.WindowsIdentity.GetCurrent().Name);
                }
                log4net.Config.XmlConfigurator.Configure(fi);

                switch (priority)
                {
                    case Level.Debug:
                        log.Debug(msg);
                        break;

                    case Level.Info:
                        log.Info(msg);
                        break;

                    case Level.Warning:
                        log.Warn(msg);
                        break;

                    case Level.Error:
                        log.Error(msg);
                        break;

                    case Level.Fatal:
                        log.Fatal(msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                ProjectErrLogger.WriteLoggerLog("Fail on: Logger logger = Write_Tracer();", ex);
            }
        }

        public static void Write(object msg)
        {
            if (msg is Exception)
            {
                Write(null, msg as Exception, Level.Debug, Target.File);
            }
            //Write(null, msg as Exception, Level.Debug, Target.Ev);
            else
            {
                Write(msg.ToString(), null, Level.Debug, Target.File);
            }
            //Write(msg.ToString(), null, Level.Debug, Target.Ev);
        }

        /*static public void Write(string msg, Exception ex) { Init(); Write(msg, ex, Level.Debug, Target.Ev); }
        static public void Write(string msg, Exception ex, Level l) { Init(); Write(msg, ex, l, Target.Ev); } */

        public static void Write(string msg, Exception ex)
        {
            Write(msg, ex, Level.Debug, Target.File);
        }

        public static void Write(string msg, Exception ex, Level l)
        {
            Write(msg, ex, l, Target.File);
        }

        /// <summary>
        /// Writes to the EventLog with the Integration Deps. Component params.
        /// As well as writing to the Tashtiot logger.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        /// <param name="l"></param>
        /// <param name="eventID"></param>
        /// <param name="clientUser"></param>
        /// <param name="clientIP"></param>
        public static void Write(string msg, Exception ex, Level l, int eventID, string clientUser, string clientIP)
        {
            string componentName = System.Configuration.ConfigurationManager.AppSettings["tlvAppName"];

            LogEntryTemp.EventID = eventID;
            LogEntryTemp.EventIDSpecified = true;
            LogEntryTemp.Component = componentName;
            LogEntryTemp.ClientUser = clientUser;
            LogEntryTemp.ClientIP = clientIP;

            Write(msg, ex, l, Target.File);
        }

        private static LogWriter _logWriter = null;

        public static void Write(string msg, Exception ex, Level l, Target target)
        {
            try
            {
                AppLogMetaData loggerMd = GetActualLoggerMetaData();
                if (null != loggerMd)
                {
                    try
                    {
                        if (_logWriter == null)
                        {
                            _logWriter = LogWriter.Instance;
                            _logWriter.Init(loggerMd, _helpUtil);
                        }
                    }
                    catch (Exception exp0)
                    {
                        ProjectErrLogger.WriteLoggerLog("Fail on: logWriter = new LogWriter(loggerMd, _helpUtil);", exp0, ex);
                        return;
                    }

                    //OepTrack(msg, ex, l, target, logWriter);
                    try
                    {
                        _logWriter.Write2Log(msg, ex, l, target, null);
                    }
                    catch (Exception exp1)
                    {
                        ProjectErrLogger.WriteLoggerLog("Fail on: logWriter.Write2Log(msg, ex, l, target, null);", exp1, ex);
                    }
                }
            }
            catch (Exception exp)
            {
                ProjectErrLogger.WriteLoggerLog("Fail on: Logger logger = GetActualLogger();", exp, ex);
            }
        }

        /// <summary>
        /// Writes to the EventLog with the Integration Dept. with the OepTracker component.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        /// <param name="l"></param>
        /// <param name="target"></param>
        /// <param name="logWriter"></param>
        private static void OepTrack(string msg, Exception ex, Level l, Target target, LogWriter logWriter)
        {
            try
            {
                string url = System.Configuration.ConfigurationManager.AppSettings["OEPTrackedAddress"];
                if (String.IsNullOrEmpty(url))
                {
                    return;
                }

                ManualResetEvent ev = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(s =>
                {
                    try
                    {
                        OEPTracker trackerService = new OEPTracker();
                        //string url = System.Configuration.ConfigurationManager.AppSettings["OEPTrackedAddress"];
                        //if (String.IsNullOrEmpty(url))
                        //    url = @"http://bzppr/OepTracker/OEPTracker.svc";
                        string OEPTrack_User = System.Configuration.ConfigurationManager.AppSettings["OEPTrack_User"];
                        if (String.IsNullOrEmpty(OEPTrack_User))
                        {
                            OEPTrack_User = @"000_tlv_logger";
                        }

                        string OEPTrack_Pwd = System.Configuration.ConfigurationManager.AppSettings["OEPTrack_Pwd"];
                        if (String.IsNullOrEmpty(OEPTrack_Pwd))
                        {
                            OEPTrack_Pwd = @"Logger@tlv1";
                        }

                        string OEPTrack_Domain = System.Configuration.ConfigurationManager.AppSettings["OEPTrack_Domain"];
                        if (String.IsNullOrEmpty(OEPTrack_Domain))
                        {
                            OEPTrack_Domain = @"dom001";
                        }

                        trackerService.Url = url;

                        // TODO :
                        //string componentName == _appName;
                        //if( componentName.Contains("TlvWeb") )
                        //    // get IIS app name

                        string componentName = System.Configuration.ConfigurationManager.AppSettings["tlvAppName"];
                        if (String.IsNullOrEmpty(componentName))
                        {
                            componentName = "tlvWeb";
                        }

                        //Run Web Service with a domain User
                        trackerService.UseDefaultCredentials = true;
                        System.Net.CredentialCache myCredentials = new System.Net.CredentialCache();
                        NetworkCredential netCred = new NetworkCredential(OEPTrack_User, OEPTrack_Pwd, OEPTrack_Domain);
                        myCredentials.Add(new Uri(trackerService.Url), "Negotiate", netCred);
                        trackerService.Credentials = myCredentials;

                        LogEntry logEntry;
                        if (LogEntryTemp.EventID == EmptyEventID)
                        {
                            logEntry = new LogEntry()
                            {
                                EventID = (Int16.MaxValue << 1) - 1, // Max Unsigned int16
                                EventIDSpecified = true,
                                Component = componentName,
                                ClientUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                                ClientIP = Dns.GetHostName()
                            };
                        }
                        else
                        {
                            logEntry = new LogEntry()
                            {
                                EventID = LogEntryTemp.EventID,
                                EventIDSpecified = LogEntryTemp.EventIDSpecified,
                                Component = LogEntryTemp.Component,
                                ClientUser = LogEntryTemp.ClientUser,
                                ClientIP = LogEntryTemp.ClientIP
                            };
                        }
                        logEntry.Message = msg;
                        if (ex != null)
                        {
                            logEntry.Message += "\n" + ex.ToString();
                        }

                        //if (ex != null)
                        //    trackerService.LogError(logEntry);
                        //else
                        //{
                        if (l == Level.Info || l == Level.Debug)
                        {
                            trackerService.LogInfo(logEntry);
                        }
                        else if (l == Level.Warning)
                        {
                            trackerService.LogWarning(logEntry);
                        }
                        else // Fatal and Error level
                        {
                            trackerService.LogError(logEntry);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    finally
                    {
                        Init();
                        ev.Set();
                    }
                });

                Console.WriteLine("Main thread finished");

                logWriter.Write2Log(msg, ex, l, target, null);

                ev.WaitOne();
            }
            catch (Exception exp1)
            {
                ProjectErrLogger.WriteLoggerLog("Fail on: logWriter.Write2Log(msg, ex, l, target, null);", exp1, ex);
            }
        }

        public static void Write(string msg, Exception ex, Level l, Target target, ScreenShowHandler screen_handler)
        {
            try
            {
                LogWriter logWriter = null;
                AppLogMetaData loggerMd = GetActualLoggerMetaData();
                if (null != loggerMd)
                {
                    try
                    {
                        if (logWriter == null)
                        {
                            logWriter = LogWriter.Instance;
                            logWriter.Init(loggerMd, _helpUtil);
                        }
                    }
                    catch (Exception exp0)
                    {
                        ProjectErrLogger.WriteLoggerLog("Fail on: logWriter = new LogWriter(loggerMd, _helpUtil);", exp0, ex);
                        return;
                    }
                    try
                    {
                        logWriter.Write2Log(msg, ex, l, target, screen_handler);
                    }
                    catch (Exception exp1)
                    {
                        ProjectErrLogger.WriteLoggerLog("Fail on: logWriter.Write2Log(msg, ex, l, target, screen_handler);", exp1, ex);
                    }
                }
            }
            catch (Exception exp)
            {
                ProjectErrLogger.WriteLoggerLog("Fail on: Logger logger = GetActualLogger();", exp, ex);
            }
        }

        private static AppLogMetaData GetActualLoggerMetaData()
        {
            try
            {
                if (null == _helpUtil)
                {
                    ProjectErrLogger.WriteLoggerLog("_helpUtil of Logger.cs is null. function name: 'GetActualLogger'");
                    return null;
                }
                else
                {
                    try
                    {
                        return LoggerManager.GetLogger(_helpUtil.AppName);
                    }
                    catch (Exception exp)
                    {
                        ProjectErrLogger.WriteLoggerLog(string.Format("Fail on: return LoggerManager.GetLogger(_helpUtil.AppName);{0}Actual call: return LoggerManager.GetLogger('{1}');", Environment.NewLine, _helpUtil.AppName), exp);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                ProjectErrLogger.WriteLoggerLog("Fail on: if (null == _helpUtil)", ex);
                return null;
            }
        }
    }
}