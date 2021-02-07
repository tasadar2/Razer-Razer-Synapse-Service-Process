#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace Synapse3.UserInteractive
{
	public class ForegroundWindowMonitor
	{
		public struct RECT
		{
			public uint left;

			public uint top;

			public uint right;

			public uint bottom;
		}

		public struct GUITHREADINFO
		{
			public int cbSize;

			public uint flags;

			public IntPtr hwndActive;

			public IntPtr hwndFocus;

			public IntPtr hwndCapture;

			public IntPtr hwndMenuOwner;

			public IntPtr hwndMoveSize;

			public IntPtr hwndCapred;

			public RECT rcCaret;
		}

		private delegate void WinEventProc(IntPtr hWinEventHook, int iEvent, IntPtr hWnd, int idObject, int idChild, int dwEventThread, int dwmsEventTime);

		public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

		private enum SetWinEventHookFlags
		{
			WINEVENT_INCONTEXT = 4,
			WINEVENT_OUTOFCONTEXT = 0,
			WINEVENT_SKIPOWNPROCESS = 2,
			WINEVENT_SKIPOWNTHREAD = 1
		}

		private ISetForegroundWindow _foregroundWindowImpl;

		private string _active = string.Empty;

		private Process _realProcess;

		private const int EVENT_SYSTEM_FOREGROUND = 3;

		private IntPtr _handle = IntPtr.Zero;

		private WinEventProc _listener;

		private Timer _foregroundTimer;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, int idProcess, int idThread, SetWinEventHookFlags dwflags);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int UnhookWinEvent(IntPtr hWinEventHook);

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

		public ForegroundWindowMonitor(ISetForegroundWindow foregroundWindowImpl)
		{
			_foregroundWindowImpl = foregroundWindowImpl;
			_listener = EventCallback;
			_handle = SetWinEventHook(3, 3, IntPtr.Zero, _listener, 0, 0, SetWinEventHookFlags.WINEVENT_SKIPOWNPROCESS);
			_foregroundTimer = new Timer();
			_foregroundTimer.AutoReset = false;
			_foregroundTimer.Interval = 10.0;
			_foregroundTimer.Elapsed += _foregroundTimer_Elapsed;
		}

		private void _foregroundTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			ProcessForegroundWindow();
		}

		~ForegroundWindowMonitor()
		{
			if (_handle != IntPtr.Zero)
			{
				UnhookWinEvent(_handle);
			}
		}

		private void EventCallback(IntPtr hWinEventHook, int iEvent, IntPtr hWnd, int idObject, int idChild, int dwEventThread, int dwmsEventTime)
		{
			if (iEvent == 3)
			{
				ResetForegroundTimer();
			}
		}

		private async void ProcessForegroundWindow()
		{
			bool bRetry = false;
			try
			{
				IntPtr foregroundWindow = GetForegroundWindow();
				if (foregroundWindow != IntPtr.Zero)
				{
					uint ProcessId = 0u;
					IntPtr threadWindowHandle = getThreadWindowHandle(0u);
					GetWindowThreadProcessId(threadWindowHandle, out ProcessId);
					Process activeProcess = GetActiveProcess((int)ProcessId);
					string text = ProcessExecutablePath(activeProcess, foregroundWindow);
					if (!string.IsNullOrEmpty(text) && !_active.Equals(text))
					{
						_active = text;
						Trace.TraceInformation($"SetForegroundWindow: {text}.");
						await _foregroundWindowImpl.SetForegroundWindow(text);
						bRetry = true;
					}
				}
				else
				{
					Trace.TraceInformation("ProcessForegroundWindow: GetForegroundWindow Return Zero");
				}
			}
			catch (Exception arg)
			{
				Trace.TraceInformation($"ProcessForegroundWindow: exception {arg}");
			}
			finally
			{
				if (bRetry)
				{
					ResetForegroundTimer(500.0);
				}
			}
		}

		private void ResetForegroundTimer(double interval = 10.0)
		{
			_foregroundTimer?.Stop();
			_foregroundTimer.Interval = interval;
			Trace.TraceInformation($"ResetForegroundTimer: Check again after {_foregroundTimer.Interval} ms.");
			_foregroundTimer?.Start();
		}

		private string ProcessExecutablePath(Process process, IntPtr hWnd)
		{
			string text = string.Empty;
			try
			{
				text = process.MainModule.FileName;
			}
			catch
			{
				string text2 = process.ProcessName.ToLower();
				if (!(text2 == "wwahost"))
				{
					if (text2 == "applicationframehost")
					{
						Process realProcess = GetRealProcess(process);
						if (realProcess != null)
						{
							return ProcessExecutablePath(realProcess, realProcess.MainWindowHandle);
						}
						text = GetTitle(hWnd);
					}
				}
				else
				{
					text = GetTitle(hWnd);
				}
				if (string.IsNullOrEmpty(text))
				{
					text = QueryManagementObject(process, text);
				}
				if (string.IsNullOrEmpty(text))
				{
					text = process.ProcessName;
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				try
				{
					text = Path.GetFileNameWithoutExtension(text) + ".exe";
				}
				catch
				{
					text = string.Empty;
				}
			}
			_realProcess = null;
			return text;
		}

		private Process GetRealProcess(Process foregroundProcess)
		{
			EnumChildWindows(foregroundProcess.MainWindowHandle, ChildWindowCallback, IntPtr.Zero);
			return _realProcess;
		}

		private bool ChildWindowCallback(IntPtr hwnd, IntPtr lparam)
		{
			uint ProcessId = 0u;
			GetWindowThreadProcessId(hwnd, out ProcessId);
			Process processById = Process.GetProcessById((int)ProcessId);
			if (processById.ProcessName != "ApplicationFrameHost")
			{
				_realProcess = processById;
			}
			return true;
		}

		private string QueryManagementObject(Process process, string name)
		{
			try
			{
				string queryString = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
				ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(queryString);
				foreach (ManagementObject item in managementObjectSearcher.Get())
				{
					object obj = item["ProcessID"];
					object obj2 = item["ExecutablePath"];
					if (obj2 != null && obj.ToString() == process.Id.ToString())
					{
						name = obj2.ToString();
					}
				}
				return name;
			}
			catch (Exception arg)
			{
				Trace.TraceInformation($"QueryManagementObject: exception handled {arg}");
				name = string.Empty;
				return name;
			}
		}

		private string GetTitle(IntPtr handle)
		{
			string result = "";
			try
			{
				StringBuilder stringBuilder = new StringBuilder(256);
				if (GetWindowText(handle, stringBuilder, 256) > 0)
				{
					return stringBuilder.ToString();
				}
				return result;
			}
			catch (Exception arg)
			{
				Trace.TraceInformation($"GetTitle: exception handled {arg}");
				return "";
			}
		}

		public static IntPtr getThreadWindowHandle(uint dwThreadId)
		{
			GUITHREADINFO lpgui = default(GUITHREADINFO);
			lpgui.cbSize = Marshal.SizeOf((object)lpgui);
			GetGUIThreadInfo(dwThreadId, ref lpgui);
			IntPtr intPtr = lpgui.hwndFocus;
			if (intPtr == IntPtr.Zero)
			{
				intPtr = lpgui.hwndActive;
			}
			return intPtr;
		}

		private static Process GetActiveProcess(int activeWindowProcessId)
		{
			Process result = null;
			try
			{
				result = Process.GetProcessById(activeWindowProcessId);
				return result;
			}
			catch (Exception arg)
			{
				Console.WriteLine($"{arg}");
				return result;
			}
		}
	}
}
