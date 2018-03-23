using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;


namespace SPExplorer.UI.UI.Controls.Panels
{
    public partial class CredentialsPanel : Form
    {
        public NetworkCredential NetworkCredentials = null;

        public CredentialsPanel()
        {
            InitializeComponent();
        }

        public DialogResult OpenDialog()
        {
            return ShowDialog();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtUserName.Text))
            {
                NetworkCredentials = new NetworkCredential(txtUserName.Text.Trim(),
                    txtPassword.Text.Trim(),
                    txtDomain.Text.Trim());
            }

            Close();
        }
    }
}