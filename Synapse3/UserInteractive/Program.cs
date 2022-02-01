#define TRACE
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.Logging;

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
            SynapseProcessListener synapseProcessListener = new SynapseProcessListener(text);
            synapseProcessListener.Location = LogFileLocation.Custom;
            synapseProcessListener.BaseFileName = Path.GetFileNameWithoutExtension(text);
            synapseProcessListener.CustomLocation = Path.GetDirectoryName(text);
            synapseProcessListener.MaxFileSize = 1048576L;
            Trace.Listeners.Add(synapseProcessListener);
            Trace.AutoFlush = true;
            Trace.TraceInformation("****************************Starting Synapse3 UserInteractive Process****************************");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            Application.Run(new HiddenForm());
            mutex.ReleaseMutex();
            Trace.TraceInformation("****************************Synapse3 UserInteractive Process Stopped****************************");
        }
    }
}
