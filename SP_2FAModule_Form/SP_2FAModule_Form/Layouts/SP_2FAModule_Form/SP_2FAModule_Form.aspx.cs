using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using SP_2FAModule_Form.Common;
using SP_2FAModule_Form.Interface;
using SP_2FAModule_Form.Model;
using static SP_2FAModule_Form.Common.CommonApplicationService;
using Timer = System.Timers.Timer;


namespace SP_2FAModule_Form.Layouts.SP_2FAModule_Form
{
    public partial class SP_2FAModule_Form : LayoutsPageBase
    {
        public static string apiId = CommonApplicationService.GetConfigurationValue("CommzGateId");
        public static string apiKey = CommonApplicationService.Decryptor(CommonApplicationService.GetConfigurationValue("CommzGateKey"));
        public static string phoneNumber = string.Empty;
        public static string msg = CommonApplicationService.GetConfigurationValue("OtpMessage");
        public static string otpExpiry = CommonApplicationService.GetConfigurationValue("OtpExpiry");
        public static string otpLength = CommonApplicationService.GetConfigurationValue("OtpLength");
        public static string otpMaxValidate = CommonApplicationService.GetConfigurationValue("OtpMaxValidate");
        public static int countdown = int.Parse(otpExpiry) + 1;
        public static bool IsResend = false;
        public static bool IsUserExisted = false;
        public static string requestedUrl = String.Empty;
        public static string validateResult = String.Empty;
        public static string resultMsg = String.Empty;
        public static string resultCode = String.Empty;
        public static bool IsRequestSuccess = false;
        public static bool IsAuthenticated = false;

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    requestedUrl = HttpContext.Current.Request.Url.AbsoluteUri.ToString().Split('=')[1];
                    UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, "RequestedUrl : " + requestedUrl);

                    Session["IsAuthenticated"] = IsAuthenticated.ToString();
                    Session["NumberOfAttempts"] = otpMaxValidate;
                    Session["Countdown"] = countdown;
                    //Timer.Enabled = true;

                    //Get Current Login User
                    SPUser currentUser = SPContext.Current.Web.CurrentUser;
                    Lbl_Welcome.Text = "Welcome " + currentUser.Name;
                    string currentUserLogin = String.Empty;

                    if (currentUser.LoginName.IndexOf("|") >= 0)
                    {
                        currentUserLogin = currentUser.LoginName.Split('|')[1].ToString().ToLower().Trim();
                    }
                    else
                    {
                        currentUserLogin = currentUser.LoginName.ToString().ToLower().Trim();
                    }

                    List<UserEntityDB> userList_Authentication = Common.CommonApplicationService.SelectUserProfileFromAuthenticationTable(currentUserLogin);
                    List<UserEntityDB> userList_Audit = Common.CommonApplicationService.SelectUserProfileFromAuditTable(currentUserLogin);

                    //Check if exists in database
                    if (userList_Authentication != null)
                    {
                        //Update session to inactive if exists
                        CommonApplicationService.UpdateUserProfileSessionAuthenticationTable(userList_Authentication, 0);
                        IsUserExisted = true;
                        Cb_Session.Checked = true;
                        Cb_Session.Enabled = false;
                    }
                    else
                    {
                        TblRow_SessionCheckbox.Style.Add("display", "none");
                    }

                    if (userList_Audit != null)
                    {
                        CommonApplicationService.UpdateUserProfileSessionAuditTable(userList_Audit, 0);
                    }

                    //Get UserInformation from AD
                    ActiveDirectoryDetail activeDirectoryDetail = new ActiveDirectoryDetail();
                    activeDirectoryDetail.GetDetails();
                    ActiveDirectoryConnection ADConnection = new ActiveDirectoryConnection(activeDirectoryDetail.AD_UserName, activeDirectoryDetail.AD_Password);
                    List<UserEntityAD> user = ADConnection.GetUser(activeDirectoryDetail.AD_Path, activeDirectoryDetail.AD_Query);
                    var usr = user.Where(u => u.UserName.ToLower().Trim() == currentUserLogin.Split('\\')[1].ToLower().Trim()).FirstOrDefault();

                    if (usr != null)
                    {
                        UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, "AD User : " + usr.UserName);
                        if (!String.IsNullOrEmpty(usr.PhoneNumber))
                        {
                            //SendOTP
                            phoneNumber = usr.PhoneNumber;
                            Session["telephoneNumber"] = phoneNumber;
                            string response = CommonApplicationService.sendCgOtp(apiId, apiKey, phoneNumber, msg, otpExpiry, otpLength, otpMaxValidate);
                            if (!String.IsNullOrEmpty(response))
                            {
                                resultCode = response.Split(',')[0];
                                if (CommonApplicationService.errorCode.ContainsKey(resultCode))
                                {
                                    resultMsg = CommonApplicationService.errorCode[resultCode].ToString();
                                }

                                if (resultCode == "01010")
                                {
                                    IsRequestSuccess = true;
                                    Timer.Enabled = true;
                                }
                            }
                        }
                        else
                        {
                            //Redirect to error page
                            //user phone number not found
                            FormDiv.Disabled = true;
                            UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, "Phone Number not found. currentUserLogin : " + currentUserLogin);
                            
                        }
                    }
                    else
                    {
                        UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, "User not found in Active Directory. currentUserLogin : " + currentUserLogin);
                    }
                }
            }
            catch (Exception ex)
            {
                UlsLoggingService.LogError(UlsLoggingService.AuthenticationForm_Error, ex.Message);
            }
        }

        public void Timer_Tick(Object source, EventArgs e)
        {
            if (Session["Countdown"] == null)
            {
                Session["Countdown"] = countdown;
            }
            var new_currentTimeSeconds = int.Parse(Session["Countdown"].ToString());

            new_currentTimeSeconds--;

            if (new_currentTimeSeconds <= 0)
            {
                Timer.Enabled = false;
                Lbl_ErrorMsg_InvalidOtp.Text = "OT Expired. Please click resend to obtain a new OTP.";
                TblCell_ErrorMsg_InvalidOtp.Style.Add("display", "table-cell");
            }

            DisplayTextSeconds.Text = String.Format("{0:00}", new_currentTimeSeconds);
            Session["Countdown"] = new_currentTimeSeconds;
        }

        protected void Btn_Submit_Click(object sender, EventArgs e)
        {
            string otpValue = Txt_Otp.Text;
            int numberOfAttempts = int.Parse(Session["NumberOfAttempts"].ToString()) - 1;
            Session["NumberOfAttempts"] = numberOfAttempts;
            SPUser currentUser = SPContext.Current.Web.CurrentUser;

            if (int.Parse(Session["NumberOfAttempts"].ToString()) < 0)
            {
                TblCell_ErrorMsg_InvalidOtp.Style.Add("display", "none");
                Lbl_ErrorMsg_MaxAttempt.Text = "You have reached your maximum number of attempts. Please click resend to obtain a new OTP.";
                TblCell_ErrorMsg_MaxAttempt.Style.Add("display", "table-cell");
            }

            validateResult = CommonApplicationService.validateCgOtp(apiId, apiKey, phoneNumber, otpValue);
            UlsLoggingService.LogInfo(UlsLoggingService.AuthenticationForm_Info, "Submit OTP : " + otpValue + " " + validateResult);

            if (validateResult == "REJECT")
            {
                CommonApplicationService.InitializeSession(currentUser, HttpContext.Current, validateResult);
                UserEntityDB user = CommonApplicationService.CreateUserProfile(currentUser, HttpContext.Current, validateResult, resultCode, resultMsg, requestedUrl, 0);
                CommonApplicationService.InsertUserToAuditTable(user);

                if (numberOfAttempts >= 0)
                {
                    Lbl_ErrorMsg_InvalidOtp.Text = "Invalid OTP. Please try again.";
                    Txt_Otp.Text = "";
                    TblCell_ErrorMsg_InvalidOtp.Style.Add("display", "table-cell");
                }
            }
            else
            {

                TblCell_ErrorMsg_InvalidOtp.Style.Add("display", "none");

                CommonApplicationService.InitializeSession(currentUser, HttpContext.Current, validateResult);
                UserEntityDB user = CommonApplicationService.CreateUserProfile(currentUser, HttpContext.Current, validateResult, resultCode, resultMsg, requestedUrl, 1);
                List<UserEntityDB> userList = new List<UserEntityDB>(new UserEntityDB[] { user });
                if (IsUserExisted)
                {
                    CommonApplicationService.UpdateUserProfileSessionAuthenticationTable(userList, 1);
                }
                else
                {

                    CommonApplicationService.InsertUserToAuthenticationTable(user, validateResult);
                }
                //Update previous record expiry date and time first
                CommonApplicationService.UpdateUserProfileSessionAuditTable(userList, 0);
                CommonApplicationService.InsertUserToAuditTable(user);
                HttpContext.Current.Response.Redirect(requestedUrl, false);
                IsAuthenticated = true;
                Session["IsAuthenticated"] = IsAuthenticated.ToString();
            }
        }

        protected void Btn_Resend_Click(object sender, EventArgs e)
        {
            Txt_Otp.Text = "";
            Session["NumberOfAttempts"] = otpMaxValidate;
            TblCell_ErrorMsg_InvalidOtp.Style.Add("display", "none");
            TblCell_ErrorMsg_MaxAttempt.Style.Add("display", "none");

            System.Web.HttpContext.Current.Session["Countdown"] = int.Parse(otpExpiry) + 1;
            string resend = CommonApplicationService.sendCgOtp(apiId, apiKey, phoneNumber, msg, otpExpiry, otpLength, otpMaxValidate);
            if (!String.IsNullOrEmpty(resend))
            {
                resultCode = resend.Split(',')[0];
                if (CommonApplicationService.errorCode.ContainsKey(resultCode))
                {
                    resultMsg = CommonApplicationService.errorCode[resultCode].ToString();
                }

                if (resultCode == "01010")
                {
                    IsRequestSuccess = true;
                    Timer.Enabled = true;
                }
            }
        }
    }
}
