using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Windows.Forms;

namespace SPExplorer.UI.Auth
{
    public partial class AuthenticationBrowser : Form
    {
        public AuthenticationBrowser()

        {
            InitializeComponent();

            webBrowser1.Navigated += ClaimsWebBrowser_Navigated;
        }

        public AuthenticationBrowser(string url)

        {
            InitializeComponent();

            fldLoginPageUrl = url;

            webBrowser1.Navigated += ClaimsWebBrowser_Navigated;

            MyInit(url);
        }

        public CookieCollection fldCookies;

        private string fldTargetSiteUrl;

        private string fldLoginPageUrl;

        private Uri fldNavigationEndUrl;

        private void ClaimsWebBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            SetCookieText(fldLoginPageUrl ?? fldTargetSiteUrl);
        }

        public void MyInit(string targetSiteUrl)
        {
            if (string.IsNullOrEmpty(targetSiteUrl))
            {
                throw new ArgumentException(Constants.MSG_REQUIRED_SITE_URL);
            }

            fldTargetSiteUrl = targetSiteUrl;

            // set login page url and success url from target site
            try
            {
                GetClaimParams(this.fldTargetSiteUrl, out this.fldLoginPageUrl, out this.fldNavigationEndUrl);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }

            if (!string.IsNullOrEmpty(fldLoginPageUrl) && fldLoginPageUrl.ToLower().Contains("/error.aspx"))
            {
                fldLoginPageUrl = fldLoginPageUrl.Replace("/_layouts/authenticate.aspx", "/");

                webBrowser1.Navigate(fldLoginPageUrl);

                txtUrl.Text = fldLoginPageUrl;
            }
            else if (fldNavigationEndUrl != null)
            {
                webBrowser1.Navigate(fldNavigationEndUrl.AbsoluteUri);

                txtUrl.Text = fldNavigationEndUrl.AbsoluteUri;
            }
            else if (!String.IsNullOrEmpty(fldTargetSiteUrl))
            {
                webBrowser1.Navigate(fldTargetSiteUrl);

                txtUrl.Text = fldTargetSiteUrl;
            }
        }

        #region Privatee Methods

        private void GetClaimParams(string targetUrl, out string loginUrl, out Uri navigationEndUrl)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(targetUrl);

            webRequest.Method = Constants.WR_METHOD_OPTIONS;

            #if DEBUG

            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(IgnoreCertificateErrorHandler);

            #endif

            try
            {
                WebResponse response = webRequest.GetResponse();

                ExtraHeadersFromResponse(response, out loginUrl, out navigationEndUrl);
            }
            catch (WebException webEx)
            {
                ExtraHeadersFromResponse(webEx.Response, out loginUrl, out navigationEndUrl);

                loginUrl=loginUrl.Split('?').First() + "?ReturnUrl=" + targetUrl;
                navigationEndUrl = new Uri(targetUrl);
            }
        }

        private bool ExtraHeadersFromResponse(WebResponse response, out string loginUrl, out Uri navigationEndUrl)
        {
            loginUrl = null;

            navigationEndUrl = null;

            try
            {
                navigationEndUrl = new Uri(response.Headers[Constants.CLAIM_HEADER_RETURN_URL]);

                loginUrl = (response.Headers[Constants.CLAIM_HEADER_AUTH_REQUIRED]);

                return true;
            }
            catch
            {
                return false;
            }
        }

        void SetCookieText(string url)
        {
            Uri uriBase = new Uri(url);

            Uri uri = new Uri(uriBase, "/");

            // call WinInet.dll to get cookie.

            string stringCookie = CookieReader.GetCookie(uri.ToString());

            if (string.IsNullOrEmpty(stringCookie))
            {
                txtCookies.Text = string.Empty;

                return;
            }

            stringCookie = stringCookie.Replace("; ", ",").Replace(";", ",");

            txtCookies.Text = stringCookie;

            if (stringCookie.Contains("FedAuth"))
            {
                btnOk_Click(this, null);
            }
        }

        private CookieCollection ExtractAuthCookiesFromUrl(string url)
        {
            Uri uriBase = new Uri(url);

            Uri uri = new Uri(uriBase, "/");

            // call WinInet.dll to get cookie.

            string stringCookie = txtCookies.Text;

            stringCookie = stringCookie.Replace("; ", ",").Replace(";", ",");

            txtCookies.Text = stringCookie;

            // use CookieContainer to parse the string cookie to CookieCollection

            CookieContainer cookieContainer = new CookieContainer();

            cookieContainer.SetCookies(uri, stringCookie);

            return cookieContainer.GetCookies(uri);
        }

        #endregion

        #region Utilities

        #if DEBUG

        private bool IgnoreCertificateErrorHandler(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        #endif // DEBUG

        #endregion

        private void btnOk_Click(object sender, EventArgs e)
        {
            fldCookies = ExtractAuthCookiesFromUrl(fldLoginPageUrl ?? fldTargetSiteUrl);

            DialogResult = DialogResult.OK;

            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            webBrowser1.Navigate(txtUrl.Text);
        }
    }
}