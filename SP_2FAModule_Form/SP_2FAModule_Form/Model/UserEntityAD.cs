using System.Configuration;

namespace SP_2FAModule_Form.Model
{
    public class UserEntityAD
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string LoginName => ConfigurationManager.AppSettings["DomainName"] + "\\" + UserName;
        public string Department { get; set; }
        public string SID { get; set; }
        public bool IsActive { get; set; }
        public string SPUserName { get; set; } = "";
        public string PhoneNumber { get; set; }

        public override string ToString()
        {
            return $"DisplayName: {DisplayName}, UserName: {UserName}, Department: {Department}, SID: {SID}, IsActive: {IsActive}, SPUser: {SPUserName}, PhoneNumber: {PhoneNumber}";
        }
    }
}
