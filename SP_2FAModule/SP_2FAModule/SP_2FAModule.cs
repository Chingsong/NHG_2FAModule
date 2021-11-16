using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using Microsoft.SharePoint;
using SP_2FAModule.Common;

namespace SP_2FAModule
{
    public class SP_2FAModule : IHttpModule
    {
        public void Dispose() { }

        public void Init(HttpApplication context)
        {
            context.PostAcquireRequestState += new EventHandler(Application_PostAcquireRequestState);
            context.PostMapRequestHandler += new EventHandler(Application_PostMapRequestHandler);
        }

        public void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            try
            {
                HttpApplication application = (HttpApplication)sender;
                MyHttpHandler resourceHttpHandler = HttpContext.Current.Handler as MyHttpHandler;
                UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "HttpContext.Current.Session 1: " + HttpContext.Current.Session);

                if (resourceHttpHandler != null)
                {
                    // set the original handler back
                    HttpContext.Current.Handler = resourceHttpHandler.OriginalHandler;
                }

                if (application.Context.Request.Url.AbsoluteUri.Contains("/SP_2FAModule_Form/SP_2FAModule_Form.aspx") || application.Context.Request.Url.AbsoluteUri.Contains("closeconnection.aspx?loginasanotheruser=true") || application.Context.Request.Url.AbsoluteUri.Contains("accessdenied.aspx"))
                {
                    return;
                }
                else if (HttpContext.Current.Session != null && HttpContext.Current.Session["IsAuthenticated"] != null)
                {
                    UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "HttpContext.Current.Session[\"IsAuthenticated\"] : " + HttpContext.Current.Session["IsAuthenticated"]);
                    if (String.Equals(HttpContext.Current.Session["IsAuthenticated"].ToString(), "True"))
                        return;
                }
                else if (application.Context.Request.Url.AbsoluteUri.Contains(".aspx") && application.Context.Request.IsAuthenticated)
                {
                    string accountToSkip = Common.Utility.GetConfigurationValue("AccountToSkip").ToLower().Trim();
                    SPUser currentUser = SPContext.Current.Web.CurrentUser;
                    string currentUserLogin = String.Empty;

                    if (currentUser.LoginName.IndexOf("|") >= 0)
                    {
                        currentUserLogin = currentUser.LoginName.Split('|')[1].ToString().ToLower().Trim();
                    }
                    else 
                    {
                        currentUserLogin = currentUser.LoginName.ToString().ToLower().Trim();
                    }
                    
                    //to skip service account whitelisted in web.config
                    if (!String.IsNullOrEmpty(accountToSkip))
                    {
                        if (accountToSkip.Contains(currentUserLogin))
                        {
                            UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Skipping whitelisted account. currentUserLogin : " + currentUserLogin);
                            return;
                        }
                    }

                    List<UserProfile> userList = Common.Utility.GetRecordFromAuthenticationTable(currentUserLogin);
                    List<UserProfile> userRecordToUpdate = new List<UserProfile>();
                    string authenticationFormUrl = application.Context.Request.Url.Scheme + "://" + application.Request.Url.Authority + application.Request.ApplicationPath + "_layouts/15/SP_2FAModule_Form/SP_2FAModule_Form.aspx?requestedUrl=" + HttpContext.Current.Request.Url.AbsoluteUri;

                    //either no user record in database OR user has no active session in database
                    if (userList.Count == 0)
                    {
                        UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "No Active Session found for user from database. currentUserLogin : " + currentUserLogin);
                        UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Redirecting to 2FA Authentication Form. currentUserLogin: " + currentUserLogin);
                        HttpContext.Current.Response.Redirect(authenticationFormUrl, false);
                        return;
                    }
                    //user active session exists in database
                    else
                    {
                        UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Active Session found for user from database. currentUserLogin : " + currentUserLogin);
                        if (HttpContext.Current.Session["UserId"] != null && HttpContext.Current.Session["WindowsSessionId"] != null &&
                            HttpContext.Current.Session["TwoFaSessionId"] != null && HttpContext.Current.Session["BrowserType"] != null)
                        {
                            string session_UserId = HttpContext.Current.Session["UserId"].ToString();
                            string session_WindowsSessionId = HttpContext.Current.Session["WindowsSessionId"].ToString();
                            string session_TwoFaSessionId = HttpContext.Current.Session["TwoFaSessionId"].ToString();
                            string session_BrowserType = HttpContext.Current.Session["BrowserType"].ToString();
                            bool IsActiveSessionExist = false;

                            userList.ForEach(usr =>
                            {
                                if ((session_UserId == usr.userId) && (session_WindowsSessionId == usr.windowsSessionId) &&
                                (session_TwoFaSessionId == usr.twoFaSessionId) && (session_BrowserType == usr.browserType))
                                {
                                    IsActiveSessionExist = true;
                                }
                                else
                                {
                                    userRecordToUpdate.Add(usr);
                                }
                            });

                            if (userRecordToUpdate.Count != 0)
                            {
                                UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Database session is different with current browser session (Current Session NOT Empty)");
                                UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Redirecting to 2FA Authentication Form.  currentUserLogin : " + currentUserLogin);
                                HttpContext.Current.Response.Redirect(authenticationFormUrl, false);
                            }

                            if (IsActiveSessionExist)
                            {
                                UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Database Session is same with current browser session. currentUserLogin : " + currentUserLogin);
                                return;
                            }
                            else
                            {
                                UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Redirecting to 2FA Authentication Form : " + session_BrowserType + ". currentUserLogin : " + currentUserLogin);
                                HttpContext.Current.Response.Redirect(authenticationFormUrl, false);
                            }
                        }
                        else
                        {
                            UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Database Session is different with current browser session (Current Session Empty)");
                            UlsLoggingService.LogInfo(UlsLoggingService.SP_2FAModule_Info, "Redirecting to 2FA Authentication Form : " + HttpContext.Current.Request.Browser.Browser + " " + currentUserLogin);
                            HttpContext.Current.Response.Redirect(authenticationFormUrl, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UlsLoggingService.LogError(UlsLoggingService.SP_2FAModule_Error, ex.Message);
            }
        }

        public void Application_PostMapRequestHandler(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            if (application.Context.Handler is IReadOnlySessionState || application.Context.Handler is IRequiresSessionState)
            {
                // no need to replace the current handler
                return;
            }

            // swap the current handler
            application.Context.Handler = new MyHttpHandler(application.Context.Handler);
        }
    }
}
