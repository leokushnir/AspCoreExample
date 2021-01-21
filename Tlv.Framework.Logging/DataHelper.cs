using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Data.SqlClient;

namespace Tlv.Framework.Logging
{
	public class DataHelper 
	{
        SqlConnection conn;
        SqlCommand cmdInsertLog;


		public DataHelper()
		{
			InitializeComponent();
		}
        private void InitializeComponent()
        {
            this.conn = new System.Data.SqlClient.SqlConnection();
            this.cmdInsertLog = new System.Data.SqlClient.SqlCommand();
            // 
            // conn
            // 
            this.conn.ConnectionString = "User ID=tlv_log;PWD=tlv_log;Data Source=sql_dev01;DataBase=db099";
            this.conn.FireInfoMessageEventOnUserErrors = false;
            // 
            // cmdInsertLog
            // 
            this.cmdInsertLog.CommandText = 
@"INSERT INTO tlv_logging
        (username, app_name, sub_app, entity_name, alert_level, alert_type, alert_message, exception_message, stack_trace, inner_exception, alert_time)
VALUES  (@username,@app_name,@sub_app,@entity_name,@alert_level,@alert_type,@alert_message,@exception_message,@stack_trace,@inner_exception,@alert_time)
";
                
                
            this.cmdInsertLog.Connection = this.conn;
            this.cmdInsertLog.Parameters.AddRange(new System.Data.SqlClient.SqlParameter[] {
            new System.Data.SqlClient.SqlParameter("@username", System.Data.SqlDbType.VarChar, 50, "username"),
            new System.Data.SqlClient.SqlParameter("@app_name", System.Data.SqlDbType.VarChar, 100, "app_name"),
            new System.Data.SqlClient.SqlParameter("@sub_app", System.Data.SqlDbType.VarChar, 100, "sub_app"),
            new System.Data.SqlClient.SqlParameter("@entity_name", System.Data.SqlDbType.VarChar, 200, "entity_name"),
            new System.Data.SqlClient.SqlParameter("@alert_level", System.Data.SqlDbType.Int, 4, "alert_level"),
            new System.Data.SqlClient.SqlParameter("@alert_type", System.Data.SqlDbType.Int, 4, "alert_type"),
            new System.Data.SqlClient.SqlParameter("@alert_message", System.Data.SqlDbType.VarChar, 500, "alert_message"),
            new System.Data.SqlClient.SqlParameter("@exception_message", System.Data.SqlDbType.VarChar, 500, "exception_message"),
            new System.Data.SqlClient.SqlParameter("@stack_trace", System.Data.SqlDbType.VarChar, 1000, "stack_trace"),
            new System.Data.SqlClient.SqlParameter("@inner_exception", System.Data.SqlDbType.VarChar, 500, "inner_exception"),
            new System.Data.SqlClient.SqlParameter("@alert_time", System.Data.SqlDbType.DateTime, 8, "alert_time")});

        }

        public void LogMessage(LogEntry logEntry)
        {
            cmdInsertLog.Parameters["@Username"].Value = logEntry.Username;
            cmdInsertLog.Parameters["@alert_time"].Value = logEntry.AlertTime;
            cmdInsertLog.Parameters["@app_name"].Value = logEntry.AppName;
            cmdInsertLog.Parameters["@sub_app"].Value = logEntry.SubAppName;
            cmdInsertLog.Parameters["@alert_level"].Value = logEntry.alertLevel;
            cmdInsertLog.Parameters["@alert_type"].Value = logEntry.alertType ;
            cmdInsertLog.Parameters["@alert_message"].Value = logEntry.AlertMessage ;
            cmdInsertLog.Parameters["@exception_message"].Value = logEntry.ExceptionMessage ;
            cmdInsertLog.Parameters["@stack_trace"].Value = logEntry.StackTrace ;
            cmdInsertLog.Parameters["@inner_exception"].Value = logEntry.InnerException ;
            cmdInsertLog.Parameters["@entity_name"].Value = logEntry.EntityName ;

            conn.Open();
            cmdInsertLog.ExecuteNonQuery();
            conn.Close();

        }
	}
}
