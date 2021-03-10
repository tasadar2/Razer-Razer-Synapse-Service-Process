using System;
using Microsoft.VisualBasic.Logging;

namespace Synapse3.UserInteractive
{
    internal class SynapseProcessListener : FileLogTraceListener
    {
        public SynapseProcessListener(string name)
            : base(name)
        {
        }

        public override void Write(string message)
        {
            base.Write(string.Format("{0} {1} ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"), message));
        }

        public override void WriteLine(string message)
        {
            base.WriteLine(string.Format("{0} {1} ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"), message));
        }
    }
}
