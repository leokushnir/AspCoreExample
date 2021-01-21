using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net.Core;
using log4net.ElasticSearch;
using log4net.ElasticSearch.Infrastructure;
using log4net.ElasticSearch.Models;
using log4net.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Tlv.Framework.Logging
{
    /// <summary>
    /// Primary object which will get serialized into a json object to pass to ES. Deviating from CamelCase
    /// class members so that we can stick with the build in serializer and not take a dependency on another lib. ES
    /// exepects fields to start with lowercase letters.
    /// </summary>
    public class logEvent
    {
        public logEvent()
        {
            properties = new Dictionary<string, string>();
        }

        public string timeStamp { get; set; }

        public string message { get; set; }

        public object messageObject { get; set; }

        public object exception { get; set; }

        public string loggerName { get; set; }

        public string domain { get; set; }

        public string identity { get; set; }

        public string level { get; set; }

        public string className { get; set; }

        public string fileName { get; set; }

        public string lineNumber { get; set; }

        public string fullInfo { get; set; }

        public string methodName { get; set; }

        public string fix { get; set; }

        public IDictionary<string, string> properties { get; set; }

        public string userName { get; set; }

        public string threadName { get; set; }

        public string hostName { get; set; }

        //public static IEnumerable<logEvent> CreateMany(IEnumerable<LoggingEvent> loggingEvents)
        //{
        //    return loggingEvents.Select(@event => Create(@event)).ToArray();
        //}

        public static logEvent Create(LoggingEvent loggingEventData, Exception ex, string message, DateTime timeStamp, string fileName, string methodName, string fix)
        {
            var logEvent = new logEvent
            {
                loggerName = loggingEventData.LoggerName,
                domain = loggingEventData.Domain,
                identity = loggingEventData.Identity,
                threadName = loggingEventData.ThreadName,
                userName = loggingEventData.UserName,
                timeStamp = timeStamp.ToUniversalTime().ToString("O"),
                exception = JsonSerializableException.Create(ex),
                message = message,
                fix = fix,
                hostName = Environment.MachineName,
                level = loggingEventData.Level.DisplayName,
                fileName = fileName,
                methodName = methodName,

            };
            List<FieldNameOverride> fieldNameOverrides = new List<FieldNameOverride>();
            List<FieldValueReplica> fieldValueReplicas = new List<FieldValueReplica>();
            var overrides = fieldNameOverrides.ToDictionary(x => x.Original, x => x.Replacement);

            var resolver = new CustomDataContractResolver
            {
                FieldNameChanges = overrides,
                FieldValueReplica = fieldValueReplicas,
            };
            loggingEventData.ToJson(resolver);
            return logEvent;
        }

        public static logEvent Create(LoggingEvent loggingEvent)
        {
            var logEvent = new logEvent
            {
                loggerName = loggingEvent.LoggerName,
                domain = loggingEvent.Domain,
                identity = loggingEvent.Identity,
                threadName = loggingEvent.ThreadName,
                userName = loggingEvent.UserName,
                timeStamp = loggingEvent.TimeStamp.ToUniversalTime().ToString("O"),
                exception = loggingEvent.ExceptionObject == null ? new object() : JsonSerializableException.Create(loggingEvent.ExceptionObject),
                message = loggingEvent.RenderedMessage,
                fix = loggingEvent.Fix.ToString(),
                hostName = Environment.MachineName,
                level = loggingEvent.Level == null ? null : loggingEvent.Level.DisplayName
            };

            // Added special handling of the MessageObject since it may be an exception.
            // Exception Types require specialized serialization to prevent serialization exceptions.
            if (loggingEvent.MessageObject != null && loggingEvent.MessageObject.GetType() != typeof(string))
            {
                if (loggingEvent.MessageObject is Exception)
                {
                    logEvent.messageObject = JsonSerializableException.Create((Exception)loggingEvent.MessageObject);
                }
                else
                {
                    logEvent.messageObject = loggingEvent.MessageObject;
                }
            }
            else
            {
                logEvent.messageObject = new object();
            }

            if (loggingEvent.LocationInformation != null)
            {
                logEvent.className = loggingEvent.LocationInformation.ClassName;
                logEvent.fileName = loggingEvent.LocationInformation.FileName;
                logEvent.lineNumber = loggingEvent.LocationInformation.LineNumber;
                logEvent.fullInfo = loggingEvent.LocationInformation.FullInfo;
                logEvent.methodName = loggingEvent.LocationInformation.MethodName;
            }

            AddProperties(loggingEvent, logEvent);

            return logEvent;
        }

        private static void AddProperties(LoggingEvent loggingEvent, logEvent logEvent)
        {
            loggingEvent.Properties().Union(AppenderPropertiesFor(loggingEvent)).
                         Do(pair => logEvent.properties.Add(pair));
        }

        private static IEnumerable<KeyValuePair<string, string>> AppenderPropertiesFor(LoggingEvent loggingEvent)
        {
            yield return Pair.For("@timestamp", loggingEvent.TimeStamp.ToUniversalTime().ToString("O"));
        }
    }
    public class FieldNameOverride : IOptionHandler
    {
        public string Original { get; set; }

        public string Replacement { get; set; }

        public void ActivateOptions()
        {
        }
    }
    /// <summary>
    /// Portable data structure used by <see cref="T:log4net.Core.LoggingEvent" />
    /// </summary>
    /// <remarks>
    /// <para>
    /// Portable data structure used by <see cref="T:log4net.Core.LoggingEvent" />
    /// </para>
    /// </remarks>
    /// <author>Nicko Cadell</author>
    public struct LoggingEventData
    {
        /// <summary>The logger name.</summary>
        /// <remarks>
        /// <para>
        /// The logger name.
        /// </para>
        /// </remarks>
        public string LoggerName;

        /// <summary>Level of logging event.</summary>
        /// <remarks>
        /// <para>
        /// Level of logging event. Level cannot be Serializable
        /// because it is a flyweight.  Due to its special serialization it
        /// cannot be declared final either.
        /// </para>
        /// </remarks>
        public global::log4net.Core.Level Level;

        /// <summary>The application supplied message.</summary>
        /// <remarks>
        /// <para>
        /// The application supplied message of logging event.
        /// </para>
        /// </remarks>
        public string Message;

        /// <summary>The name of thread</summary>
        /// <remarks>
        /// <para>
        /// The name of thread in which this logging event was generated
        /// </para>
        /// </remarks>
        public string ThreadName;

        /// <summary>Gets or sets the local time the event was logged</summary>
        /// <remarks>
        /// <para>
        /// Prefer using the <see cref="P:log4net.Core.LoggingEventData.TimeStampUtc" /> setter, since local time can be ambiguous.
        /// </para>
        /// </remarks>
        [Obsolete("Prefer using TimeStampUtc, since local time can be ambiguous in time zones with daylight savings time.")]
        public DateTime TimeStamp;

        private DateTime _timeStampUtc;

        /// <summary>Location information for the caller.</summary>
        /// <remarks>
        /// <para>
        /// Location information for the caller.
        /// </para>
        /// </remarks>
        public LocationInfo LocationInfo;

        /// <summary>String representation of the user</summary>
        /// <remarks>
        /// <para>
        /// String representation of the user's windows name,
        /// like DOMAIN\username
        /// </para>
        /// </remarks>
        public string UserName;

        /// <summary>String representation of the identity.</summary>
        /// <remarks>
        /// <para>
        /// String representation of the current thread's principal identity.
        /// </para>
        /// </remarks>
        public string Identity;

        /// <summary>The string representation of the exception</summary>
        /// <remarks>
        /// <para>
        /// The string representation of the exception
        /// </para>
        /// </remarks>
        public string ExceptionString;

        /// <summary>String representation of the AppDomain.</summary>
        /// <remarks>
        /// <para>
        /// String representation of the AppDomain.
        /// </para>
        /// </remarks>
        public string Domain;

        /// <summary>Additional event specific properties</summary>
        /// <remarks>
        /// <para>
        /// A logger or an appender may attach additional
        /// properties to specific events. These properties
        /// have a string key and an object value.
        /// </para>
        /// </remarks>
        public PropertiesDictionary Properties;

        /// <summary>Gets or sets the UTC time the event was logged</summary>
        /// <remarks>
        /// <para>
        /// The TimeStamp is stored in the UTC time zone.
        /// </para>
        /// </remarks>
        public DateTime TimeStampUtc
        {
            get => this.TimeStamp != new DateTime() && this._timeStampUtc == new DateTime() ? this.TimeStamp.ToUniversalTime() : this._timeStampUtc;
            set
            {
                this._timeStampUtc = value;
                this.TimeStamp = this._timeStampUtc.ToLocalTime();
            }
        }
    }
    public class CustomDataContractResolver : DefaultContractResolver
    {
        public Dictionary<string, string> FieldNameChanges { get; set; }
        public List<FieldValueReplica> FieldValueReplica { get; set; }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.DeclaringType != typeof(logEvent)) return property;

            if (FieldNameChanges.Count > 0 && FieldNameChanges.TryGetValue(property.PropertyName, out var newValue))
                property.PropertyName = newValue;

            return property;
        }
    }
    public class FieldValueReplica : IOptionHandler
    {
        public string Original { get; set; }

        public string Replica { get; set; }

        public void ActivateOptions()
        {
        }
    }
}