using System.Collections.Generic;
using Microsoft.SharePoint.Administration;

namespace SP_2FAModule.Common
{
    public class UlsLoggingService : SPDiagnosticsServiceBase
    {
        public static string SP_2FAModuleDiagnosticAreaName = "SP_2FAModule";
        public const string SP_2FAModule_Info = "SP_2FAModule_Info";
        public const string SP_2FAModule_Error = "SP_2FAModule_Error";
        private static UlsLoggingService _Current;

        public static UlsLoggingService Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new UlsLoggingService(); ;
                }
                return _Current;
            }
        }

        private UlsLoggingService() : base("SP_2FAModule", SPFarm.Local) { }

        protected override IEnumerable<SPDiagnosticsArea> ProvideAreas()
        {
            //throw new NotImplementedException();

            List<SPDiagnosticsArea> areas = new List<SPDiagnosticsArea>
            {
                new SPDiagnosticsArea(SP_2FAModuleDiagnosticAreaName, new List <SPDiagnosticsCategory>
                {
                    new SPDiagnosticsCategory(SP_2FAModule_Info,TraceSeverity.Medium,EventSeverity.Information),
                    new SPDiagnosticsCategory(SP_2FAModule_Error, TraceSeverity.Unexpected, EventSeverity.Error)
                })
            };
            return areas;
        }

        public static void LogInfo(string categoryName, string errorMessage)
        {
            SPDiagnosticsCategory category = UlsLoggingService.Current.Areas[SP_2FAModuleDiagnosticAreaName].Categories[categoryName];
            UlsLoggingService.Current.WriteTrace(0, category, TraceSeverity.Medium, errorMessage);
        }

        public static void LogError(string categoryName, string errorMessage)
        {
            SPDiagnosticsCategory category = UlsLoggingService.Current.Areas[SP_2FAModuleDiagnosticAreaName].Categories[categoryName];
            UlsLoggingService.Current.WriteTrace(0, category, TraceSeverity.Unexpected, errorMessage);
        }
    }
}
