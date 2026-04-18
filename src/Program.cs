using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace NLHETheoryAdvisor
{
    static class Program
    {
        private static bool _fatalShown;

        [STAThread]
        static void Main()
        {
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                ShowFatalError(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                var ex = e.ExceptionObject as Exception ?? new Exception("Unknown fatal error.");
                ShowFatalError(ex);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
            }
        }

        private static void ShowFatalError(Exception ex)
        {
            if (_fatalShown)
            {
                return;
            }

            _fatalShown = true;

            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-error.log");
                string body = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + ex + Environment.NewLine;
                File.WriteAllText(path, body);
            }
            catch
            {
            }

            try
            {
                MessageBox.Show(
                    "The application failed to start." + Environment.NewLine +
                    "A diagnostic file was written to startup-error.log in the application folder." + Environment.NewLine + Environment.NewLine +
                    ex.Message,
                    "NLHE Theory Advisor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }
}
