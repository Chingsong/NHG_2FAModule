using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.SessionState;
using Microsoft.SharePoint;
using SP_2FAModule_Form.Model;

namespace SP_2FAModule_Form.Common
{
    public class CommonApplicationService
    {
        private const string SecurityKey = "C0mPleXSecRetPh@5e_5oaOgnbwMf7[9!C";

        public static string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString != null ? ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString : String.Empty;

        public static readonly Dictionary<string, string> errorCode = new Dictionary<string, string>()
        {
            {"01010", "SMS MT request successfully submitted"},
            {"01011","Invalid request format."},
            {"01012","Unauthorized access. Either the API ID does not exist or the API password is invalid, the account has been suspended, or the request is being originated from a non-authorized IP address."},
            {"01013","Transient system error, please retry after 60 seconds."},
            {"01014","Unable to route to mobile operator for the attempted mobile number."},
            {"01015","The credit balance for the API account is not sufficient."},
            {"01018","The mobile number attempted is blacklisted."},
        };

        public struct ActiveDirectoryDetail
        {
            public string AD_Path { get; set; }
            public string AD_UserName { get; set; }
            public string AD_Password { get; set; }
            public string AD_Query { get; set; }
            public string AD_Domain { get; set; }

            public void GetDetails()
            {
                AD_Path = GetConfigurationValue("LDAP_Path");
                AD_Query = GetConfigurationValue("LDAP_Query");
                AD_Domain = GetConfigurationValue("DomainName");
                AD_UserName = GetConfigurationValue("LDAP_Username");
                AD_Password = Decryptor(GetConfigurationValue("LDAP_Key"));
            }
        }

        public static List<UserEntityDB> SelectUserProfileFromAuthenticationTable(string userId)
        {
            List<UserEntityDB> userEntityDbList = new List<UserEntityDB>();
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, System.Security.Principal.WindowsIdentity.GetCurrent().Name + " for database connection.");
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
                            UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, "User " + userId + " not found in Authentication database");
                            mySqlConn.Close();
                        }
                        else
                        {
                            while (myReader.Read())
                            {
                                UserEntityDB user = new UserEntityDB();
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
                                userEntityDbList.Add(user);
                            }
                            mySqlConn.Close();
                        }
                    }
                }
            });
            return userEntityDbList.Count == 0 ? null : userEntityDbList;
        }

        public static List<UserEntityDB> SelectUserProfileFromAuditTable(string userId)
        {
            List<UserEntityDB> userEntityDbList = new List<UserEntityDB>();
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, System.Security.Principal.WindowsIdentity.GetCurrent().Name + " for database connection.");
                using (SqlConnection mySqlConn = new SqlConnection(connectionString))
                {
                    SqlCommand mySqlCommand = new SqlCommand("Audit_SelectRecord", mySqlConn);
                    mySqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    mySqlCommand.Parameters.Add("@userIdInput", SqlDbType.NVarChar, 50).Value = userId;
                    mySqlConn.Open();

                    using (SqlDataReader myReader = mySqlCommand.ExecuteReader())
                    {
                        if (!myReader.HasRows)
                        {
                            UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, "User " + userId + " not found in Audit database");
                            mySqlConn.Close();
                        }
                        else
                        {
                            while (myReader.Read())
                            {
                                UserEntityDB user = new UserEntityDB();
                                user.authenticateId = myReader["AuthenticateAuditId"].ToString();
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
                                user.resultMessage = myReader["ResultMessage"].ToString();
                                user.resultCode = myReader["ResultCode"].ToString();
                                user.messageId = myReader["MessageId"].ToString();
                                user.isActive = int.Parse(myReader["IsActive"].ToString());
                                userEntityDbList.Add(user);
                            }
                            mySqlConn.Close();
                        }
                    }
                }
            });
            return userEntityDbList.Count == 0 ? null : userEntityDbList;
        }

        public static void UpdateUserProfileSessionAuthenticationTable(List<UserEntityDB> userProfiles, int IsActive)
        {
            userProfiles.ForEach(usr =>
            {
                usr = UpdateNullValue(usr);
            });

            DateTime dt = DateTime.Now;
            string dateInput = dt.ToString("dd/MM/yyyy");
            string timeInput = dt.ToString("HH:mm:ss"); ;
            string concateWindowsSessionId = String.Empty;
            string concateTwoFaSessionId = String.Empty;

            if (IsActive == 0)
            {
                userProfiles.ForEach(usr =>
                {
                    concateWindowsSessionId += usr.windowsSessionId + ",";
                    concateTwoFaSessionId += usr.twoFaSessionId + ",";
                });
            }
            else
            {
                concateWindowsSessionId = userProfiles[0].windowsSessionId;
                concateTwoFaSessionId = userProfiles[0].twoFaSessionId;
            }

            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                using (SqlConnection mySqlConn = new SqlConnection(connectionString))
                {
                    SqlCommand mySqlCommand = new SqlCommand("Authentication_UpdateRecordSession", mySqlConn);
                    mySqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    mySqlCommand.Parameters.Add("@userIdInput", SqlDbType.VarChar, 50).Value = userProfiles[0].userId;
                    mySqlCommand.Parameters.Add("@windowsSessionIdInput", SqlDbType.VarChar, -1).Value = concateWindowsSessionId;
                    mySqlCommand.Parameters.Add("@twoFaSessionIdInput", SqlDbType.VarChar, -1).Value = concateTwoFaSessionId;
                    mySqlCommand.Parameters.Add("@dateInput", SqlDbType.VarChar, 50).Value = dateInput;
                    mySqlCommand.Parameters.Add("@timeInput", SqlDbType.VarChar, 50).Value = timeInput;
                    mySqlCommand.Parameters.Add("@webUrlInput", SqlDbType.NVarChar, 400).Value = userProfiles[0].webUrl;
                    mySqlCommand.Parameters.Add("@browserTypeInput", SqlDbType.NVarChar, 50).Value = userProfiles[0].browserType;
                    mySqlCommand.Parameters.Add("@statusInput", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                    mySqlCommand.Parameters.Add("@isActiveInput", SqlDbType.VarChar, 50).Value = IsActive.ToString();
                    mySqlConn.Open();
                    mySqlCommand.ExecuteNonQuery();
                    mySqlConn.Close();
                }
            });
        }

        public static void UpdateUserProfileSessionAuditTable(List<UserEntityDB> userProfiles, int IsActive)
        {
            userProfiles.ForEach(usr => { usr = UpdateNullValue(usr); });

            DateTime dt = DateTime.Now;
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                using (SqlConnection mySqlConn = new SqlConnection(connectionString))
                {
                    SqlCommand mySqlCommand = new SqlCommand("Audit_UpdateRecordSession", mySqlConn);
                    mySqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    mySqlCommand.Parameters.Add("@userIdInput", SqlDbType.VarChar, 50).Value = userProfiles[0].userId;
                    //mySqlCommand.Parameters.Add("@windowsSessionIdInput", SqlDbType.VarChar, -1).Value = concateWindowsSessionId;
                    //mySqlCommand.Parameters.Add("@twoFaSessionIdInput", SqlDbType.VarChar, -1).Value = concateTwoFaSessionId;
                    mySqlCommand.Parameters.Add("@expiryDateInput", SqlDbType.VarChar, 50).Value = dt.ToString("dd/MM/yyyy");
                    mySqlCommand.Parameters.Add("@expiryTimeInput", SqlDbType.VarChar, 50).Value = dt.ToString("HH:mm:ss");
                    mySqlCommand.Parameters.Add("@isActiveInput ", SqlDbType.VarChar, 50).Value = IsActive.ToString();
                    mySqlConn.Open();
                    mySqlCommand.ExecuteNonQuery();
                    mySqlConn.Close();
                }
            });
        }

        public static void InsertUserToAuthenticationTable(UserEntityDB user, string validateResult)
        {
            user = UpdateNullValue(user);

            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                using (SqlConnection mySqlConn = new SqlConnection(connectionString))
                {
                    SqlCommand mySqlCommand = new SqlCommand("Authentication_InsertRecord", mySqlConn);
                    mySqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    mySqlCommand.Parameters.Add("@userIdInput", SqlDbType.NVarChar, 50).Value = user.userId;
                    mySqlCommand.Parameters.Add("@userNameInput", SqlDbType.NVarChar, 50).Value = user.userName;
                    mySqlCommand.Parameters.Add("@windowsSessionIdInput", SqlDbType.NVarChar, 88).Value = user.windowsSessionId;
                    mySqlCommand.Parameters.Add("@twoFaSessionIdInput", SqlDbType.NVarChar, 88).Value = user.twoFaSessionId;
                    mySqlCommand.Parameters.Add("@startTimeInput", SqlDbType.NVarChar, 50).Value = user.startTime;
                    mySqlCommand.Parameters.Add("@expiryTimeInput", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                    mySqlCommand.Parameters.Add("@startDateInput", SqlDbType.NVarChar, 50).Value = user.startDate;
                    mySqlCommand.Parameters.Add("@expiryDateInput", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                    mySqlCommand.Parameters.Add("@webUrlInput", SqlDbType.NVarChar, 400).Value = user.webUrl;
                    mySqlCommand.Parameters.Add("@browserTypeInput", SqlDbType.NVarChar, 50).Value = user.browserType;
                    mySqlCommand.Parameters.Add("@statusInput", SqlDbType.NVarChar, 50).Value = validateResult.ToString();
                    mySqlCommand.Parameters.Add("@machineInfoInput", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                    mySqlCommand.Parameters.Add("@machineIpInput", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                    mySqlCommand.Parameters.Add("@isActiveInput", SqlDbType.Int).Value = user.isActive;
                    mySqlConn.Open();
                    mySqlCommand.ExecuteNonQuery();
                    mySqlConn.Close();
                }
            });
        }

        public static void InsertUserToAuditTable(UserEntityDB user)
        {
            user = UpdateNullValue(user);

            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                using (SqlConnection mySqlConn = new SqlConnection(connectionString))
                {
                    SqlCommand mySqlCommand = new SqlCommand("Audit_InsertRecord", mySqlConn);
                    mySqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    mySqlCommand.Parameters.Add("@userIdInput", SqlDbType.NVarChar, 50).Value = user.userId;
                    mySqlCommand.Parameters.Add("@userNameInput", SqlDbType.NVarChar, 50).Value = user.userName;
                    mySqlCommand.Parameters.Add("@windowsSessionIdInput", SqlDbType.NVarChar, 88).Value = user.windowsSessionId;
                    mySqlCommand.Parameters.Add("@twoFaSessionIdInput", SqlDbType.NVarChar, 88).Value = user.twoFaSessionId;
                    mySqlCommand.Parameters.Add("@startTimeInput", SqlDbType.NVarChar, 50).Value = user.startTime;
                    mySqlCommand.Parameters.Add("@expiryTimeInput", SqlDbType.NVarChar, 50).Value = user.expiryTime;
                    mySqlCommand.Parameters.Add("@startDateInput", SqlDbType.NVarChar, 50).Value = user.startDate;
                    mySqlCommand.Parameters.Add("@expiryDateInput", SqlDbType.NVarChar, 50).Value = user.expiryDate;
                    mySqlCommand.Parameters.Add("@webUrlInput", SqlDbType.NVarChar, 400).Value = user.webUrl;
                    mySqlCommand.Parameters.Add("@browserTypeInput", SqlDbType.NVarChar, 50).Value = user.browserType;
                    mySqlCommand.Parameters.Add("@statusInput", SqlDbType.NVarChar, 50).Value = user.status;
                    mySqlCommand.Parameters.Add("@machineInfoInput", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                    mySqlCommand.Parameters.Add("@machineIpInput", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                    mySqlCommand.Parameters.Add("@resultMessageInput", SqlDbType.NVarChar, 50).Value = user.resultMessage;
                    mySqlCommand.Parameters.Add("@resultCodeInput", SqlDbType.NVarChar, 50).Value = user.resultCode;
                    mySqlCommand.Parameters.Add("@messageIdInput", SqlDbType.NVarChar, 50).Value = DBNull.Value;
                    mySqlCommand.Parameters.Add("@isActiveInput", SqlDbType.Int).Value = user.isActive;
                    mySqlConn.Open();
                    mySqlCommand.ExecuteNonQuery();
                    mySqlConn.Close();
                }
            });
        }

        public static string Decryptor(string cipherText)
        {
            byte[] toEncryptArray = Convert.FromBase64String(cipherText);
            MD5CryptoServiceProvider objMD5CryptoService = new MD5CryptoServiceProvider();
            byte[] securityKeyArray = objMD5CryptoService.ComputeHash(UTF8Encoding.UTF8.GetBytes(SecurityKey));
            objMD5CryptoService.Clear();

            var objTripleDESCryptoService = new TripleDESCryptoServiceProvider();
            objTripleDESCryptoService.Key = securityKeyArray;
            objTripleDESCryptoService.Mode = CipherMode.ECB;
            objTripleDESCryptoService.Padding = PaddingMode.PKCS7;

            var objCrytpoTransform = objTripleDESCryptoService.CreateDecryptor();
            byte[] resultArray = objCrytpoTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            objTripleDESCryptoService.Clear();

            return UTF8Encoding.UTF8.GetString(resultArray);
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

        public static string GetPropertyValue(SearchResult sr, String PropertyName)
        {
            if (sr.Properties[PropertyName] != null && sr.Properties[PropertyName].Count > 0 && sr.Properties[PropertyName][0] != null)
            {
                return sr.Properties[PropertyName][0].ToString();
            }
            return "";
        }

        public static string GetSIDPropertyValue(SearchResult sr)
        {
            if (sr.Properties["objectsid"].Count == 0)
            {
                return "";
            }
            byte[] sidInBytes = (byte[])sr.Properties["objectSid"][0];
            SecurityIdentifier sid = new SecurityIdentifier(sidInBytes, 0);
            return sid.ToString();
        }

        public static bool IsActive(SearchResult sr)
        {
            if (sr.Properties["UserAccountControl"].Count == 0)
            {
                return true;
            }
            int flags = (int)sr.Properties["userAccountControl"][0];
            return !Convert.ToBoolean(flags & 2);
        }

        public static string sendCgSms(string ID, string Password, string mobile, string msg)
        {
            string response = "";
            try
            {
                //Construct Send Params.
                string gateWay = "https://www.commzgate.net/gateway/SendMessage?";

                //Setup Params "ID=129720002&Password=stotHaYo5RuZ&Mobile=6591828454&Type=A&Message=CommzGate%20Test%20Message&"
                string paramData = "";
                paramData += "ID=" + ID + "&";
                paramData += "Password=" + Password + "&";
                paramData += "Mobile=" + mobile + "&";
                paramData += "Type=" + "A" + "&";
                paramData += "Message=" + System.Uri.EscapeDataString(msg) + "&";

                //Ensure Ascii format
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] ASCIIparamData = encoding.GetBytes(paramData);

                //Setting Request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(gateWay);
                request.Method = "POST";
                request.Accept = "text/plain";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = ASCIIparamData.Length;

                //Sending Request.
                Stream streamReq = request.GetRequestStream();
                streamReq.Write(ASCIIparamData, 0, ASCIIparamData.Length);

                //Get Response
                HttpWebResponse HttpWResp = (HttpWebResponse)request.GetResponse();
                Stream streamResponse = HttpWResp.GetResponseStream();

                //Read the Response.. and open a dialog
                StreamReader reader = new StreamReader(streamResponse);
                response = reader.ReadToEnd();

                //Consult CommzGate API guide for response details.
                return response;
            }
            catch (Exception ex)
            {
                UlsLoggingService.LogError(UlsLoggingService.AuthenticationForm_Error, ex.Message);
                return "" + ex;
            }
        }

        public static string sendCgOtp(string ID, string Password, string mobile, string msg, string otpExpiry, string otpLength, string otpMaxValidate)
        {
            string response = "";
            try
            {
                //Construct Send Params.
                string gateWay = "https://www.commzgate.net/gateway/SendMessage?";

                //Setup Params
                string paramData = "";
                paramData += "ID=" + ID + "&";
                paramData += "Password=" + Password + "&";
                paramData += "Mobile=" + mobile + "&";
                paramData += "Type=" + "A" + "&";
                paramData += "OTP=" + "advanced" + "&";
                paramData += "OtpExpiry=" + otpExpiry + "&";
                paramData += "OtpLength=" + otpLength + "&";
                paramData += "OtpMaxValidate=" + otpMaxValidate + "&";
                paramData += "Message=" + System.Uri.EscapeDataString(msg) + "&";

                //Ensure Ascii format
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] ASCIIparamData = encoding.GetBytes(paramData);

                //Setting Request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(gateWay);
                request.Method = "POST";

                request.Accept = "text/plain";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = ASCIIparamData.Length;

                //Sending Request.
                Stream streamReq = request.GetRequestStream();
                streamReq.Write(ASCIIparamData, 0, ASCIIparamData.Length);

                //Get Response
                HttpWebResponse HttpWResp = (HttpWebResponse)request.GetResponse();
                Stream streamResponse = HttpWResp.GetResponseStream();

                //Read the Response.. and open a dialog
                StreamReader reader = new StreamReader(streamResponse);
                response = reader.ReadToEnd();

                //Consult CommzGate API guide for response details.
                return response;
            }
            catch (Exception ex)
            {
                UlsLoggingService.LogError(UlsLoggingService.AuthenticationForm_Error, ex.Message);
                return "" + ex;
            }
        }

        public static string validateCgOtp(string ID, string Password, string mobile, string otp)
        {
            string response = "";
            try
            {
                //Construct Send Params.
                string gateWay = "https://www.commzgate.net/OTP/Validate?";

                //Setup Params
                string paramData = "";
                paramData += "ID=" + ID + "&";
                paramData += "Password=" + Password + "&";
                paramData += "Mobile=" + mobile + "&";
                paramData += "OtpValidate=" + otp;

                //Setting Request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(gateWay + paramData);
                request.Method = "GET";

                request.Accept = "text/plain";
                request.ContentType = "application/x-www-form-urlencoded";

                //Get Response
                HttpWebResponse HttpWResp = (HttpWebResponse)request.GetResponse();
                Stream streamResponse = HttpWResp.GetResponseStream();

                //Read the Response.. and open a dialog
                StreamReader reader = new StreamReader(streamResponse);
                response = reader.ReadToEnd();

                //Consult CommzGate API guide for response details.
                return response;
            }
            catch (Exception ex)
            {
                UlsLoggingService.LogError(UlsLoggingService.AuthenticationForm_Error, ex.Message);
                return "" + ex;
            }
        }

        public static void InitializeSession(SPUser spUser, HttpContext context, string validateResult)
        {
            if (validateResult == "ACCEPT")
            {
                SessionIDManager manager = new SessionIDManager();
                string TwoFaSessionID = manager.CreateSessionID(context);
                context.Session.Add("UserId", spUser.LoginName.ToLower().Split('|')[1]);
                context.Session.Add("WindowsSessionId", context.Session.SessionID);
                context.Session.Add("TwoFaSessionId", TwoFaSessionID);
                context.Session.Add("BrowserType", context.Request.Browser.Browser);
            }
            else
            {
                context.Session.Add("UserId", spUser.LoginName.ToLower().Split('|')[1]);
                context.Session.Add("WindowsSessionId", context.Session.SessionID);
                context.Session.Add("TwoFaSessionId", "");
                context.Session.Add("BrowserType", context.Request.Browser.Browser);
            }
        }

        public static UserEntityDB CreateUserProfile(SPUser spUser, HttpContext context, string validateResult, string resultCode, string resultMsg, string requestedWebUrl, int isActive)
        {
            DateTime dt = DateTime.Now;
            UserEntityDB user = new UserEntityDB();
            user.userId = spUser.LoginName.ToLower().Split('|')[1];
            user.userName = spUser.Name;
            user.windowsSessionId = context.Session["WindowsSessionId"].ToString();
            user.twoFaSessionId = context.Session["TwoFaSessionId"].ToString();
            user.startTime = dt.ToString("HH:mm:ss");
            user.startDate = dt.ToString("dd/MM/yyyy");
            user.browserType = context.Session["BrowserType"].ToString(); ;
            user.webUrl = requestedWebUrl;
            user.status = validateResult;
            user.resultMessage = resultMsg;
            user.resultCode = resultCode;
            user.messageId = null;
            user.isActive = isActive;

            if (validateResult == "REJECT")
            {
                user.expiryDate = user.startDate;
                user.expiryTime = user.startTime;
            }
            else
            {
                user.expiryDate = null;
                user.expiryTime = null;
            }
            return user;
        }

        public static UserEntityDB UpdateNullValue(UserEntityDB user)
        {
            Type type = user.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            PropertyInfo[] properties = type.GetProperties(flags);

            foreach (PropertyInfo property in properties)
            {

                if (property.GetValue(user, null) == null || property.GetValue(user, null) == "")
                {
                    property.SetValue(user, "");
                }
            }
            return user;
        }
    }
}
