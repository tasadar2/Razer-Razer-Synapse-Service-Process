using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Synapse3.UserInteractive
{
    public class RegistryWatcher : IDisposable
    {
        private const int KEY_QUERY_VALUE = 1;

        private const int KEY_NOTIFY = 16;

        private const int STANDARD_RIGHTS_READ = 131072;

        private static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(int.MinValue);

        private static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(-2147483647);

        private static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(-2147483646);

        private static readonly IntPtr HKEY_USERS = new IntPtr(-2147483645);

        private static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(-2147483644);

        private static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(-2147483643);

        private static readonly IntPtr HKEY_DYN_DATA = new IntPtr(-2147483642);

        private IntPtr _registryHive;

        private string _registrySubName;

        private object _threadLock = new object();

        private Thread _thread;

        private bool _disposed;

        private ManualResetEvent _eventTerminate = new ManualResetEvent(initialState: false);

        private RegChangeNotifyFilter _regFilter = RegChangeNotifyFilter.Key | RegChangeNotifyFilter.Attribute | RegChangeNotifyFilter.Value | RegChangeNotifyFilter.Security;

        public RegChangeNotifyFilter RegChangeNotifyFilter
        {
            get
            {
                return _regFilter;
            }
            set
            {
                lock (_threadLock)
                {
                    if (IsMonitoring)
                    {
                        throw new InvalidOperationException("Monitoring thread is already running");
                    }
                    _regFilter = value;
                }
            }
        }

        public bool IsMonitoring => _thread != null;

        public event EventHandler RegChanged;

        public event ErrorEventHandler Error;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint options, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, RegChangeNotifyFilter dwNotifyFilter, SafeWaitHandle hEvent, bool fAsynchronous);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(IntPtr hKey);

        protected virtual void OnRegChanged()
        {
            this.RegChanged?.Invoke(this, null);
        }

        protected virtual void OnError(Exception e)
        {
            this.Error?.Invoke(this, new ErrorEventArgs(e));
        }

        public RegistryWatcher(RegistryKey registryKey)
        {
            InitRegistryKey(registryKey.Name);
        }

        public RegistryWatcher(string name)
        {
            if (name == null || name.Length == 0)
            {
                throw new ArgumentNullException("name");
            }
            InitRegistryKey(name);
        }

        public RegistryWatcher(RegistryHive registryHive, string subKey)
        {
            InitRegistryKey(registryHive, subKey);
        }

        public void Dispose()
        {
            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void InitRegistryKey(RegistryHive hive, string name)
        {
            switch (hive)
            {
            case RegistryHive.ClassesRoot:
                _registryHive = HKEY_CLASSES_ROOT;
                break;
            case RegistryHive.CurrentConfig:
                _registryHive = HKEY_CURRENT_CONFIG;
                break;
            case RegistryHive.CurrentUser:
                _registryHive = HKEY_CURRENT_USER;
                break;
            case RegistryHive.DynData:
                _registryHive = HKEY_DYN_DATA;
                break;
            case RegistryHive.LocalMachine:
                _registryHive = HKEY_LOCAL_MACHINE;
                break;
            case RegistryHive.PerformanceData:
                _registryHive = HKEY_PERFORMANCE_DATA;
                break;
            case RegistryHive.Users:
                _registryHive = HKEY_USERS;
                break;
            default:
                throw new InvalidEnumArgumentException("hive", (int)hive, typeof(RegistryHive));
            }
            _registrySubName = name;
        }

        private void InitRegistryKey(string name)
        {
            string[] array = name.Split('\\');
            switch (array[0])
            {
            case "HKEY_CLASSES_ROOT":
            case "HKCR":
                _registryHive = HKEY_CLASSES_ROOT;
                break;
            case "HKEY_CURRENT_USER":
            case "HKCU":
                _registryHive = HKEY_CURRENT_USER;
                break;
            case "HKEY_LOCAL_MACHINE":
            case "HKLM":
                _registryHive = HKEY_LOCAL_MACHINE;
                break;
            case "HKEY_USERS":
                _registryHive = HKEY_USERS;
                break;
            case "HKEY_CURRENT_CONFIG":
                _registryHive = HKEY_CURRENT_CONFIG;
                break;
            default:
                _registryHive = IntPtr.Zero;
                throw new ArgumentException("The registry hive '" + array[0] + "' is not supported", "value");
            }
            _registrySubName = string.Join("\\", array, 1, array.Length - 1);
        }

        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null, "This instance is already disposed");
            }
            lock (_threadLock)
            {
                if (!IsMonitoring)
                {
                    _eventTerminate.Reset();
                    _thread = new Thread(MonitorThread);
                    _thread.IsBackground = true;
                    _thread.Start();
                }
            }
        }

        public void Stop()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null, "This instance is already disposed");
            }
            lock (_threadLock)
            {
                Thread thread = _thread;
                if (thread != null)
                {
                    _eventTerminate.Set();
                    thread.Join();
                }
            }
        }

        private void MonitorThread()
        {
            try
            {
                ThreadLoop();
            }
            catch (Exception e)
            {
                OnError(e);
            }
            _thread = null;
        }

        private void ThreadLoop()
        {
            int num = RegOpenKeyEx(_registryHive, _registrySubName, 0u, 131089, out var phkResult);
            if (num != 0)
            {
                throw new Win32Exception(num);
            }
            try
            {
                AutoResetEvent autoResetEvent = new AutoResetEvent(initialState: false);
                WaitHandle[] waitHandles = new WaitHandle[2] { autoResetEvent, _eventTerminate };
                while (!_eventTerminate.WaitOne(0, exitContext: true))
                {
                    num = RegNotifyChangeKeyValue(phkResult, bWatchSubtree: true, _regFilter, autoResetEvent.SafeWaitHandle, fAsynchronous: true);
                    if (num != 0)
                    {
                        throw new Win32Exception(num);
                    }
                    if (WaitHandle.WaitAny(waitHandles) == 0)
                    {
                        OnRegChanged();
                    }
                }
            }
            finally
            {
                if (phkResult != IntPtr.Zero)
                {
                    RegCloseKey(phkResult);
                }
            }
        }
    }
}
