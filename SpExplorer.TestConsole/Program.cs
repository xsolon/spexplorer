using SPExplorer.ReportSamples;
using SPExplorer.UI.Auth;
using System;
using System.Collections.Generic;

namespace SPExplorer.TestConsole
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var url = "https://sp.dev.local/sites/test";

            var opts = new CSOMAuthenticationParams();
            opts.Authentications = CSOMAuthenticationParams.SP14AuthenticationType.Claims;
            var ctx = new SpAuthHelper().SetUpAuthentication(url, opts);

            var report = new SiteSizeReport();

            var result = report.SiteSize(ctx, new List<SiteSizeReport.ListInfo>());


        }
    }
}
