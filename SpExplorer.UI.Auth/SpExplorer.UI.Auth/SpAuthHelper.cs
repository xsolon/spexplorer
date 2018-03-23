using Microsoft.SharePoint.Client;
using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using SPExplorer.UI.UI.Controls.Panels;
using static SPExplorer.UI.Auth.CSOMAuthenticationParams;

namespace SPExplorer.UI.Auth
{
    public static class ClaimClientContext
    {
        /// <summary>
        ///     Displays a pop up to login the user. An authentication Cookie is returned if the user is sucessfully authenticated.
        /// </summary>
        /// <param name="targetSiteUrl"></param>
        /// <param name="popUpWidth"></param>
        /// <param name="popUpHeight"></param>
        /// <returns></returns>
        public static CookieCollection GetAuthenticatedCookies(string targetSiteUrl, int popUpWidth, int popUpHeight)
        {
            CookieCollection authCookie = null;

            var form = new AuthenticationBrowser(targetSiteUrl);

            var res = form.ShowDialog();

            if (res == DialogResult.OK)
            {
                authCookie = form.fldCookies;
            }
            else
            {
                authCookie = new CookieCollection();
            }

            return authCookie;
        }
    }

    public class SpAuthHelper
    {
        //[XmlIgnore()]
        public CookieCollection cookies = null;

        void Prompt4Credentials(ClientContext context, SP14AuthenticationType authType, NetworkCredential credentials)
        {
            if (authType == SP14AuthenticationType.Claims)
            {
                var t = new Thread(new ThreadStart(() =>
                {
                    cookies = ClaimClientContext.GetAuthenticatedCookies(context.Url, 0, 0);
                }));

                t.SetApartmentState(ApartmentState.STA);

                t.Start();

                while (cookies == null)
                {
                    Thread.Sleep(500);
                }

                context.ExecutingWebRequest += delegate (object sender, WebRequestEventArgs e)

                {
                    e.WebRequestExecutor.WebRequest.CookieContainer = new CookieContainer();

                    foreach (Cookie cookie in cookies)
                    {
                        e.WebRequestExecutor.WebRequest.CookieContainer.Add(cookie);
                    }
                };

                return;
            }

            #region Resolve Credentials

            var form = new CredentialsPanel();

            if (credentials == null)
            {
                DialogResult dialogResult = form.ShowDialog();

                if (dialogResult == DialogResult.OK)
                {
                    if (form.NetworkCredentials != null)
                    {
                        credentials = form.NetworkCredentials;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (authType.HasFlag(SP14AuthenticationType.FBA))
            {
                context.AuthenticationMode = ClientAuthenticationMode.FormsAuthentication;

                context.FormsAuthenticationLoginInfo = new FormsAuthenticationLoginInfo(string.Format("{0}\\{1}", credentials.Domain, credentials.UserName).Trim('\\'), credentials.Password);

                if (authType.HasFlag(SP14AuthenticationType.Windows))
                {
                    context.ExecutingWebRequest += (sender, arg) =>

                    {
                        arg.WebRequestExecutor.WebRequest.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");
                    };
                }
            }
            else
            {
                context.Credentials = credentials;
            }

            #endregion
        }


        public ClientContext SetUpAuthentication(string url, CSOMAuthenticationParams params1)
        {
            var context = new ClientContext(url);

            Prompt4Credentials(context, params1);

            return context;
        }

        void Prompt4Credentials(ClientContext context, CSOMAuthenticationParams params1)
        {
            NetworkCredential credentials = null;

            if (params1.UserName != null)
            {
                credentials = new NetworkCredential(params1.UserName, params1.Password, params1.Domain);
            }

            if (params1.Authentications.HasFlag(SP14AuthenticationType.Claims))
            {
                #region MyRegion

                if (String.IsNullOrEmpty(params1.ClaimsCookie))
                {
                    var t = new Thread(new ThreadStart(() =>
                    {
                        cookies = ClaimClientContext.GetAuthenticatedCookies(context.Url, 0, 0);
                    }));

                    t.SetApartmentState(ApartmentState.STA);

                    t.Start();

                    while (cookies == null)
                    {
                        Thread.Sleep(500);
                    }
                }
                else
                {
                    string stringCookie = params1.ClaimsCookie;

                    stringCookie = stringCookie.Replace("; ", ",").Replace(";", ",");

                    var c = new CookieContainer();

                    cookies = new CookieCollection();

                    var uri = new Uri(new Uri(context.Url), "/");

                    c.SetCookies(uri, stringCookie);

                    cookies = c.GetCookies(uri);
                }

                context.ExecutingWebRequest += delegate (object sender, WebRequestEventArgs e)

                {
                    e.WebRequestExecutor.WebRequest.CookieContainer = new CookieContainer();

                    foreach (Cookie cookie in cookies)
                    {
                        e.WebRequestExecutor.WebRequest.CookieContainer.Add(cookie);
                    }
                };

                #endregion
            }
            else
            {
                #region Resolve Credentials

                var form = new CredentialsPanel();

                if (credentials == null)
                {
                    DialogResult dialogResult = form.ShowDialog();

                    if (dialogResult == DialogResult.OK)
                    {
                        if (form.NetworkCredentials != null)
                        {
                            credentials = form.NetworkCredentials;
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                if (params1.Authentications.HasFlag(SP14AuthenticationType.FBA))
                {
                    context.AuthenticationMode = ClientAuthenticationMode.FormsAuthentication;

                    context.FormsAuthenticationLoginInfo = new FormsAuthenticationLoginInfo(string.Format("{0}\\{1}", credentials.Domain, credentials.UserName).Trim('\\'), credentials.Password);

                    if (params1.Authentications.HasFlag(SP14AuthenticationType.Windows))
                    {
                        context.ExecutingWebRequest += (sender, arg) =>

                        {
                            arg.WebRequestExecutor.WebRequest.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");
                        };
                    }
                }
                else
                {
                    context.Credentials = form.NetworkCredentials;
                }

                #endregion
            }
        }

    }
    public class CSOMAuthenticationParams
    {
        public SP14AuthenticationType Authentications = SP14AuthenticationType.Windows | SP14AuthenticationType.FBA;

        public string ClaimsCookie = string.Empty;

        public string Domain;

        public bool UseDefaultCredentials;

        public string UserName;

        internal static string secret = "ws1";

        internal string _Password = string.Empty;

        public string Password

        {
            get

            {
                return _Password;
            }

            set

            {
                if (value == null || String.IsNullOrEmpty(value))
                {
                }
                //else if (value.Length != 48)
                //{
                //    _Password = Crypto.EncryptStringAES(value, secret);
                //}
                else
                {
                    _Password = value;
                }
            }
        }

        [Flags]
        public enum SP14AuthenticationType
        {
            Anonymous = 0,
            FBA = 1,
            Windows = 2,
            Claims = 4
        }

        public static class Prompt
        {
            public static string ShowDialog(string text, string caption)
            {
                System.Windows.Forms.Form prompt = new System.Windows.Forms.Form();

                prompt.Width = 500;

                prompt.Height = 150;

                prompt.Text = caption;

                Label textLabel = new Label()

                {
                    Left = 50,
                    Top = 20,
                    Text = text
                };

                TextBox textBox = new TextBox()

                {
                    Left = 50,
                    Top = 50,
                    Width = 400,
                    PasswordChar = '*'
                };

                Button confirmation = new Button()

                {
                    Text = "Ok",
                    Left = 350,
                    Width = 100,
                    Top = 70
                };

                confirmation.Click += (sender, e) => { prompt.Close(); };

                prompt.Controls.Add(confirmation);

                prompt.Controls.Add(textLabel);

                prompt.Controls.Add(textBox);

                prompt.ShowDialog();

                return textBox.Text;
            }
        }
    }

    
}
