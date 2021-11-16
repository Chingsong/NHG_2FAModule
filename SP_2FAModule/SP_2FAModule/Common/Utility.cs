using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SharePoint;

namespace SP_2FAModule.Common
{
    public class Utility
    {
        private const string SecurityKey = "C0mPleXSecRetPh@5e_5oaOgnbwMf7[9!C";
        public static string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString != null ? ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString : String.Empty;

        public static List<UserProfile> GetRecordFromAuthenticationTable(string userId)
        {
            List<UserProfile> userProfileList = new List<UserProfile>();
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, System.Security.Principal.WindowsIdentity.GetCurrent().Name + " for database connection.");
                using (SqlConnection mySqlConn = new SqlConnection(connectionString))
                {
                    SqlCommand mySqlCommand = new SqlCommand("Authentication_SelectRecord", mySqlConn);
                    mySqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    mySqlCommand.Parameters.Add("@userIdInput", SqlDbType.NVarChar, 50).Value = userId;
                    mySqlConn.Open();

                    using (SqlDataReader myReader = mySqlCommand.ExecuteReader())
                    {
                        if (!myReader.HasRows)
                        {
                            mySqlConn.Close();
                        }
                        else
                        {
                            while (myReader.Read())
                            {
                                UserProfile user = new UserProfile();
                                user.authenticateId = myReader["AuthenticateId"].ToString();
                                user.userId = myReader["UserId"].ToString();
                                user.userName = myReader["UserName"].ToString();
                                user.windowsSessionId = myReader["WindowsSessionId"].ToString();
                                user.twoFaSessionId = myReader["TwoFaSessionId"].ToString();
                                user.startTime = myReader["StartTime"].ToString();
                                user.expiryTime = myReader["ExpiryTime"].ToString();
                                user.startDate = myReader["StartDate"].ToString();
                                user.expiryDate = myReader["ExpiryDate"].ToString();
                                user.webUrl = myReader["WebUrl"].ToString();
                                user.browserType = myReader["BrowserType"].ToString();
                                user.status = myReader["Status"].ToString();
                                user.machineInfo = myReader["MachineInfo"].ToString();
                                user.machineIp = myReader["MachineIp"].ToString();
                                user.isActive = int.Parse(myReader["IsActive"].ToString());
                                userProfileList.Add(user);
                            }
                            mySqlConn.Close();
                        }
                    }
                }
            });
            return userProfileList;
        }

        public static string GetConfigurationValue(string keyName)
        {
            string configValue = ConfigurationManager.AppSettings[keyName];
            if (configValue != null)
            {
                return configValue;
            }
            return string.Empty;
        }
    }
}