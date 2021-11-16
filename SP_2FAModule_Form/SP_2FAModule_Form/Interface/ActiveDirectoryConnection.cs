using System;
using System.Collections.Generic;
using System.DirectoryServices;
using SP_2FAModule_Form.Common;
using SP_2FAModule_Form.Model;

namespace SP_2FAModule_Form.Interface
{
    class ActiveDirectoryConnection : IActiveDirectoryConnection
    {
        private readonly string _ldapUserName = "";
        private readonly string _ldapPassword = "";
        public ActiveDirectoryConnection()
        {

        }

        public ActiveDirectoryConnection(string ldapUserName, string ldapPassword)
        {
            _ldapUserName = ldapUserName;
            _ldapPassword = ldapPassword;
        }

        private DirectoryEntry _InitializeDirectoryEntry(string path = null, string username = null, string password = null)
        {
            //using System.DirectoryServices => DirectoryEntry
            if (path != null && username != null && password != null)
            {
                return new DirectoryEntry(path, username, password);
            }
            if (path != null)
            {
                return new DirectoryEntry(path);
            }
            return new DirectoryEntry();
        }

        private SearchResultCollection _QueryFromActiveDirectory(string ldap, string username = null, string password = null, string filter = "")
        {
            try
            {
                DirectoryEntry directoryEntry = _InitializeDirectoryEntry(ldap, username, password);
                DirectorySearcher directorySearcher = null;
                directorySearcher = string.IsNullOrEmpty(filter) ? new DirectorySearcher(directoryEntry) : new DirectorySearcher(directoryEntry, filter);
                string[] requiredProperties = new string[7] { "department", "sAMAccountName", "displayName", "mail", "objectSid", "userAccountControl", "telephoneNumber" };
                directorySearcher.PropertiesToLoad.AddRange(requiredProperties);
                return directorySearcher.FindAll();
            }
            catch (Exception ex)
            {
                UlsLoggingService.LogError(UlsLoggingService.AuthenticationForm_Error, "_QueryFromActiveDirectory : " + ex.Message);
                return null;
            }
        }

        //private bool _IsNotProcessed(string account)
        //{
        //    account = account.ToLower();
        //    return !account.Contains("from") && account.Contains("from.tp");
        //}

        public List<UserEntityAD> GetUser(string path, string query = "")
        {
            List<UserEntityAD> users = new List<UserEntityAD>();
            string ldap = path;
            SearchResultCollection results = _QueryFromActiveDirectory(ldap, _ldapUserName, _ldapPassword, query);
            foreach (SearchResult result in results)
            {
                try
                {
                    //if (result.Properties["sAMAccountName"].Count != 0 && !_IsNotProcessed(result.Properties["DisplayName"][0].ToString()))
                    if (result.Properties["sAMAccountName"].Count != 0)
                    {
                        string sAMAccountName = CommonApplicationService.GetPropertyValue(result, "sAMAccountName");
                        string department = CommonApplicationService.GetPropertyValue(result, "department");
                        string displayName = CommonApplicationService.GetPropertyValue(result, "displayName");
                        string email = CommonApplicationService.GetPropertyValue(result, "mail");
                        string sid = CommonApplicationService.GetSIDPropertyValue(result);
                        string telephoneNumber = CommonApplicationService.GetPropertyValue(result, "telephoneNumber");
                        bool isActive = CommonApplicationService.IsActive(result);

                        UserEntityAD user = new UserEntityAD
                        {
                            UserName = sAMAccountName,
                            DisplayName = displayName,
                            Email = email,
                            Department = department,
                            SID = sid,
                            IsActive = isActive,
                            PhoneNumber = telephoneNumber
                        };
                        users.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    UlsLoggingService.LogError(UlsLoggingService.AuthenticationForm_Error, "GetUser : " + ex.Message);
                }
            }
            return users;
        }
    }
}
