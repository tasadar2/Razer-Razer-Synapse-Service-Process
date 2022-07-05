using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Synapse3.UserInteractive
{
    internal static class Program
    {
        private static Mutex mutex = new Mutex(initiallyOwned: true, "{BBA5EC32-6453-4464-984B-EF9DBF1E2E38}");

        [STAThread]
        private static void Main()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string text = Path.Combine(folderPath, ConfigurationManager.AppSettings["log_filepath"]);
            text = text + "_" + Environment.UserName + ".log";
            string directoryName = Path.GetDirectoryName(text);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            if (!mutex.WaitOne(TimeSpan.Zero, exitContext: true))
            {
                return;
            }
            if (File.Exists(text))
            {
                try
                {
                    File.Delete(text);
                }
                catch (Exception)
                {
                }
            }
            Logger.Instance.Debug("****************************Starting Synapse3 UserInteractive Process****************************");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            Application.Run(new HiddenForm());
            mutex.ReleaseMutex();
            Logger.Instance.Debug("****************************Synapse3 UserInteractive Process Stopped****************************");
        }
    }
}
