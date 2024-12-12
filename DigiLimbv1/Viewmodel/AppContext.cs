using System;
using System.Windows.Forms;

namespace DigiLimbv1
{
    public class AppContext : ApplicationContext
    {
        public AppContext()
        {
            // Start with the LoginForm
            LoginForm loginForm = new LoginForm();
            loginForm.FormClosed += OnLoginFormClosed;
            loginForm.Show();
        }

        private void OnLoginFormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender is LoginForm loginForm && loginForm.DialogResult == DialogResult.OK)
            {
                // Open DashboardForm if login was successful
                DashboardForm dashboardForm = new DashboardForm();
                dashboardForm.FormClosed += (s, args) => ExitThread(); // Exit app when DashboardForm is closed
                dashboardForm.Show();
            }
            else
            {
                ExitThread(); // Exit application if login was canceled
            }
        }
    }
}
