#define TRACE
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Contract.Common;
using Contract.MacroLib;

namespace Synapse3.UserInteractive
{
    public class OTFMacroRecorderEventHandler
    {
        private IOTFMacroRecorderEvent _oftMacroRecorderEvent;

        private BackgroundWorker _backgroundWorker;

        private OTFMacroRecorder _recorder;

        private bool _isCancelled;

        public OTFMacroRecorderEventHandler(IOTFMacroRecorderEvent oftMacroRecorderEvent)
        {
            _isCancelled = false;
            _oftMacroRecorderEvent = oftMacroRecorderEvent;
            _oftMacroRecorderEvent.StartOTFEvent += _oftMacroRecorderEvent_StartOTFEvent;
            _oftMacroRecorderEvent.StopOTFEvent += _oftMacroRecorderEvent_StopOTFEvent;
            _oftMacroRecorderEvent.CancelOTFEvent += _oftMacroRecorderEvent_CancelOTFEvent;
            _recorder = new OTFMacroRecorder();
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.WorkerSupportsCancellation = true;
        }

        private void _oftMacroRecorderEvent_StartOTFEvent(Device device)
        {
            if (_backgroundWorker.IsBusy)
            {
                _oftMacroRecorderEvent_CancelOTFEvent(device);
                Thread.Sleep(100);
            }
            _isCancelled = false;
            _backgroundWorker.RunWorkerAsync();
        }

        private void _oftMacroRecorderEvent_StopOTFEvent(Device device, ref Macro macro)
        {
            Trace.TraceInformation("_oftMacroRecorderEvent_StopOTFEvent - start");
            _backgroundWorker.CancelAsync();
            _recorder.ActiveDevice = device;
            bool flag = SpinWait.SpinUntil(() => _recorder.IsDone, -1);
            macro = (flag ? _recorder.GetMacro() : null);
            Trace.TraceInformation($"_oftMacroRecorderEvent_StopOTFEvent result: {flag}  - end");
        }

        private void _oftMacroRecorderEvent_CancelOTFEvent(Device device)
        {
            _isCancelled = true;
            _backgroundWorker.CancelAsync();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Trace.TraceInformation("BackgroundWorker_DoWork - start");
            _recorder.Start();
            while (!_recorder.IsDone)
            {
                Application.DoEvents();
                if (_backgroundWorker.CancellationPending && !_recorder.IsDone)
                {
                    _recorder.Stop(_isCancelled);
                }
            }
            Trace.TraceInformation("BackgroundWorker_DoWork - end");
        }
    }
}
