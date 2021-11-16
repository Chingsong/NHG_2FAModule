using Microsoft.SharePoint.Administration;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using SP_2FAModule_ClearSessionTimerJob.Common;
using Microsoft.SharePoint;

namespace SP_2FAModule_ClearSession_TimerJob
{
    public class SP_2FAModule_ClearSession_TimerJob : SPJobDefinition
    {
        public SP_2FAModule_ClearSession_TimerJob() : base() { }

        public SP_2FAModule_ClearSession_TimerJob(string jobName, SPService service) : base(jobName, service, null, SPJobLockType.None)
        {
            this.Title = "SP 2FAModule Clear Session Timer Job";
        }

        public SP_2FAModule_ClearSession_TimerJob(string jobName, SPWebApplication webapp) : base(jobName, webapp, null, SPJobLockType.ContentDatabase)
        {
            this.Title = "SP 2FAModule Clear Session Timer Job";
        }

        public override void Execute(Guid targetInstanceId)
        {
            UlsLoggingService.LogInfo(UlsLoggingService.ClearSessionTimerJob_Info, "Initializing Clear Session Timer Job");
            ClearSession();
        }

        public static void ClearSession()
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;

                    using (SqlConnection mySqlConn = new SqlConnection(connectionString))
                    {
                        mySqlConn.Open();

                        DateTime dt = DateTime.Now;
                        SqlCommand mySqlCommand_Authentication = new SqlCommand("Authentication_ClearSession", mySqlConn);
                        mySqlCommand_Authentication.CommandType = System.Data.CommandType.StoredProcedure;
                        mySqlCommand_Authentication.Parameters.Add("@expiryTimeInput", SqlDbType.VarChar, 50).Value = dt.ToString("HH:mm:ss");
                        mySqlCommand_Authentication.Parameters.Add("@expiryDateInput", SqlDbType.VarChar, 50).Value = dt.ToString("dd/MM/yyyy");
                        mySqlCommand_Authentication.ExecuteNonQuery();

                        SqlCommand mySqlCommand_Audit = new SqlCommand("Audit_ClearSession", mySqlConn);
                        mySqlCommand_Audit.CommandType = System.Data.CommandType.StoredProcedure;
                        mySqlCommand_Audit.Parameters.Add("@expiryTimeInput", SqlDbType.VarChar, 50).Value = dt.ToString("HH:mm:ss");
                        mySqlCommand_Audit.Parameters.Add("@expiryDateInput", SqlDbType.VarChar, 50).Value = dt.ToString("dd/MM/yyyy");
                        mySqlCommand_Audit.ExecuteNonQuery();

                        mySqlConn.Close();
                        UlsLoggingService.LogInfo(UlsLoggingService.ClearSessionTimerJob_Info, "Completed Clear Session Timer Job.");
                    }
                });
            }
            catch (Exception ex)
            {
                UlsLoggingService.LogError(UlsLoggingService.ClearSessionTimerJob_Error, ex.Message);
            }
        }
    }
}