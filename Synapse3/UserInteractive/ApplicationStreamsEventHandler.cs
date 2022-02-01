#define TRACE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.DeviceCallbacks;
using Contract.Audio.ApplicationStreamsLib;
using Contract.Common;
using Microsoft.Win32;
using ProtoBuf;

namespace Synapse3.UserInteractive
{
    public class ApplicationStreamsEventHandler : IAudioApplicationStreamsCallback
    {
        private IApplicationStreamsEvent _appStreamsEvent;

        private IAccountsClient _accountEvent;

        private List<Device> _processedDeviceList;

        private ApplicationStreamsClient _client;

        private CRSy3_AudioAppStreamsWrapper _audioAppStreamsResolver;

        private readonly object _lock;

        private readonly ConcurrentDictionary<long, List<ApplicationStream>> _cache;

        private readonly string RIOTCLIENTSERVICES = "riotclientservices";

        private readonly string VALORANT = "valorant";

        private readonly List<string> FILTER = new List<string> { "chromavisualizer", "redlauncher" };

        private readonly ConcurrentDictionary<string, ApplicationStream> _applicationStreamWorkAround;

        public event OnAudioApplicationStreamsChanged AudioApplicationStreamsChangedEvent;

        public event OnAudioApplicationStreamsChangedSR AudioApplicationStreamsChangedSREvent;

        public ApplicationStreamsEventHandler(IAccountsClient accountEvent, IApplicationStreamsEvent appStreamsEvent)
        {
            _audioAppStreamsResolver = new CRSy3_AudioAppStreamsWrapper(this);
            _client = new ApplicationStreamsClient(accountEvent);
            _lock = new object();
            _appStreamsEvent = appStreamsEvent;
            _appStreamsEvent.OnApplicationStreamsDeviceAddedEvent += OnApplicationStreamsDeviceAdded;
            _appStreamsEvent.OnApplicationStreamsDeviceRemovedEvent += OnApplicationStreamsDeviceRemoved;
            _appStreamsEvent.OnApplicationStreamsDeviceGetStreamsEvent += OnApplicationStreamsDeviceGetStreamsEvent;
            _appStreamsEvent.OnApplicationStreamsSetEvent += OnApplicationStreamsSetEvent;
            _appStreamsEvent.OnApplicationStreamSetEvent += OnApplicationStreamSetEvent;
            _accountEvent = accountEvent;
            _processedDeviceList = new List<Device>();
            _cache = new ConcurrentDictionary<long, List<ApplicationStream>>();
            _applicationStreamWorkAround = new ConcurrentDictionary<string, ApplicationStream>();
        }

        private void ProcessDevices(bool bAdd)
        {
            Trace.TraceInformation("ProcessDevices::ProcessDevices action " + (bAdd ? "Add" : "Remove"));
            List<Device> appStreamDevices = _client.GetAppStreamDevices();
            if (appStreamDevices != null)
            {
                Trace.TraceInformation($"ProcessDevices::GetAppStreamDevices return {appStreamDevices.Count} devices");
            }
            else
            {
                Trace.TraceError("ProcessDevices::GetAppStreamDevices return null");
            }
            if (appStreamDevices == null)
            {
                return;
            }
            if (bAdd)
            {
                foreach (Device item in appStreamDevices)
                {
                    ProcessDevice(item, bAdd: true);
                }
                return;
            }
            Device[] array = _processedDeviceList.ToArray();
            foreach (Device conn in array)
            {
                if (!appStreamDevices.Any((Device x) => x.Handle.Equals(conn.Handle)))
                {
                    ProcessDevice(conn, bAdd: false);
                }
            }
        }

        private bool ProcessDevice(Device device, bool bAdd)
        {
            if (bAdd)
            {
                int num = _processedDeviceList.FindIndex((Device x) => x.Handle == device.Handle);
                if (num < 0)
                {
                    if (_audioAppStreamsResolver?.DeviceAdded(device) ?? false)
                    {
                        ApplicationStreams applicationStreams = new ApplicationStreams();
                        applicationStreams.Device = device.Clone();
                        UpdateApplicationStreams(applicationStreams);
                        lock (_lock)
                        {
                            _processedDeviceList.Add(device.Clone());
                        }
                    }
                    else
                    {
                        Trace.TraceError($"ProcessDevice::Failed to add {device.Name} in audioAppStreamsResolver.");
                    }
                }
                else
                {
                    ApplicationStreams applicationStreams2 = new ApplicationStreams();
                    applicationStreams2.Device = device.Clone();
                    UpdateApplicationStreams(applicationStreams2);
                }
            }
            else
            {
                int num2 = _processedDeviceList.FindIndex((Device x) => x.Handle == device.Handle);
                if (num2 >= 0)
                {
                    _audioAppStreamsResolver.DeviceRemoved(device);
                    lock (_lock)
                    {
                        _processedDeviceList.RemoveAt(num2);
                    }
                    return true;
                }
            }
            return false;
        }

        private void OnApplicationStreamsDeviceAdded(Device device)
        {
            Trace.TraceInformation($"OnApplicationStreamsDeviceAdded {device.Name}");
            ProcessDevices(bAdd: true);
        }

        private void OnApplicationStreamsDeviceRemoved(Device device)
        {
            Trace.TraceInformation($"OnApplicationStreamsDeviceRemoved {device.Name}");
            ProcessDevices(bAdd: false);
            if (_cache.ContainsKey(device.Handle))
            {
                _cache.TryRemove(device.Handle, out var _);
            }
        }

        private void OnApplicationStreamsDeviceGetStreamsEvent(Device device)
        {
            Trace.TraceInformation($"OnApplicationStreamsDeviceGetStreamsEvent {device.Name}");
            ProcessDevice(device, bAdd: true);
        }

        private void OnApplicationStreamsSetEvent(ApplicationStreams streams)
        {
            Trace.TraceInformation("OnApplicationStreamsSetEvent START streams");
            if (streams == null)
            {
                return;
            }
            Trace.TraceInformation($"OnApplicationStreamsSetEvent {streams.Device.Handle}");
            int num = _processedDeviceList.FindIndex((Device x) => x.Handle == streams.Device.Handle);
            Trace.TraceInformation($"OnApplicationStreamsSetEvent {num} initial.");
            if (num < 0)
            {
                Trace.TraceInformation($"OnApplicationStreamsSetEvent Adding {streams.Device.Handle}.");
                ProcessDevice(streams.Device, bAdd: true);
                num = _processedDeviceList.FindIndex((Device x) => x.Handle == streams.Device.Handle);
                Trace.TraceInformation($"OnApplicationStreamsSetEvent {num} after add.");
            }
            if (num >= 0)
            {
                string arg = string.Join(Environment.NewLine, (IEnumerable<ApplicationStream>)streams.ApplicationStreamList.ToArray());
                Trace.TraceInformation($"OnApplicationStreamsSetEvent back-end set streams called {arg}.");
                _audioAppStreamsResolver?.Set(streams);
            }
            else
            {
                Trace.TraceError("OnApplicationStreamsSetEvent cant find device.");
            }
            Trace.TraceInformation("OnApplicationStreamsSetEvent END streams");
        }

        private static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }
            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private string ServicePath()
        {
            string result = Path.Combine(ProgramFilesx86(), "Razer", "Synapse3", "Service", "Razer Synapse Service.exe");
            string name = "SOFTWARE\\Razer\\Synapse3\\RazerSynapse";
            string name2 = "InstallDir";
            using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
            if (registryKey != null)
            {
                if (registryKey.GetValue(name2) != null)
                {
                    string text = registryKey.GetValue(name2) as string;
                    if (text != string.Empty)
                    {
                        return Path.Combine(text, "Service", "Razer Synapse Service.exe");
                    }
                    return result;
                }
                return result;
            }
            return result;
        }

        private void OnApplicationStreamSetEvent(Device device, ApplicationStream stream)
        {
            Trace.TraceInformation("OnApplicationStreamsSetEvent START stream");
            if (device == null || stream == null)
            {
                return;
            }
            if (stream.StreamID.Equals(-2))
            {
                stream.Name = "Razer Synapse Service";
                stream.ExePath = ServicePath();
                Trace.TraceInformation($"OnApplicationStreamsSetEvent Setting Service Path {stream.ExePath} source:{stream.Source} ouput:{stream.OutputMode}");
            }
            Trace.TraceInformation($"OnApplicationStreamsSetEvent {device.Name} {stream.Name} id:{stream.StreamID} source:{stream.Source} ouput:{stream.OutputMode}");
            int num = _processedDeviceList.FindIndex((Device x) => x.Handle == device.Handle);
            if (num >= 0)
            {
                CRSy3_AudioAppStreamsWrapper audioAppStreamsResolver = _audioAppStreamsResolver;
                if (audioAppStreamsResolver != null && !audioAppStreamsResolver.SetApplicationStream(device, stream))
                {
                    Trace.TraceError($"OnApplicationStreamsSetEvent: Failed to set application stream {device.Name} {stream.Name} id:{stream.StreamID} source:{stream.Source} ouput:{stream.OutputMode}");
                }
                string key = stream.Name.ToLower();
                if (_applicationStreamWorkAround.ContainsKey(key))
                {
                    ApplicationStream applicationStream = _applicationStreamWorkAround[key];
                    if (applicationStream != null)
                    {
                        applicationStream.OutputMode = stream.OutputMode;
                        Trace.TraceInformation($"OnApplicationStreamsSetEvent Second stream found. Setting {applicationStream.OutputMode} to {applicationStream.Name} id:{applicationStream.StreamID}");
                        CRSy3_AudioAppStreamsWrapper audioAppStreamsResolver2 = _audioAppStreamsResolver;
                        if (audioAppStreamsResolver2 != null && !audioAppStreamsResolver2.SetApplicationStream(device, applicationStream))
                        {
                            Trace.TraceError($"OnApplicationStreamsSetEvent: Failed to set for second application stream {device.Name} {applicationStream.Name} id:{applicationStream.StreamID} source:{applicationStream.Source} ouput:{applicationStream.OutputMode}");
                        }
                    }
                }
            }
            else
            {
                Trace.TraceError($"OnApplicationStreamsSetEvent: device not found in the device list. Retrying. {device.Name} {stream.Name} id:{stream.StreamID} source:{stream.Source} ouput:{stream.OutputMode}");
                if (_audioAppStreamsResolver?.DeviceAdded(device) ?? false)
                {
                    lock (_lock)
                    {
                        _processedDeviceList.Add(device.Clone());
                    }
                    CRSy3_AudioAppStreamsWrapper audioAppStreamsResolver3 = _audioAppStreamsResolver;
                    if (audioAppStreamsResolver3 != null && !audioAppStreamsResolver3.SetApplicationStream(device, stream))
                    {
                        Trace.TraceError($"OnApplicationStreamsSetEvent: Failed to set application stream {device.Name} {stream.Name} id:{stream.StreamID} source:{stream.Source} ouput:{stream.OutputMode}");
                    }
                    string key2 = stream.Name.ToLower();
                    if (_applicationStreamWorkAround.ContainsKey(key2))
                    {
                        ApplicationStream applicationStream2 = _applicationStreamWorkAround[key2];
                        if (applicationStream2 != null)
                        {
                            applicationStream2.OutputMode = stream.OutputMode;
                            Trace.TraceInformation($"OnApplicationStreamsSetEvent Second stream found. Setting {applicationStream2.OutputMode} to {applicationStream2.Name} id:{applicationStream2.StreamID}");
                            CRSy3_AudioAppStreamsWrapper audioAppStreamsResolver4 = _audioAppStreamsResolver;
                            if (audioAppStreamsResolver4 != null && !audioAppStreamsResolver4.SetApplicationStream(device, applicationStream2))
                            {
                                Trace.TraceError($"OnApplicationStreamsSetEvent: Failed to set for second application stream {device.Name} {applicationStream2.Name} id:{applicationStream2.StreamID} source:{applicationStream2.Source} ouput:{applicationStream2.OutputMode}");
                            }
                        }
                    }
                }
            }
            Trace.TraceInformation("OnApplicationStreamsSetEvent END stream");
        }

        public void AudioApplicationStreamsChanged(long handle)
        {
            Trace.TraceInformation($"AudioApplicationStreamsChanged {handle}");
            this.AudioApplicationStreamsChangedEvent?.Invoke(handle);
            this.AudioApplicationStreamsChangedSREvent?.Invoke(handle);
            int index = _processedDeviceList.FindIndex((Device x) => x.Handle == handle);
            if (index >= 0)
            {
                ApplicationStreams applicationStreams = new ApplicationStreams();
                applicationStreams.Device = _processedDeviceList[index].Clone();
                UpdateApplicationStreams(applicationStreams);
                new Task(delegate
                {
                    NotifyStreamChanged(_processedDeviceList[index].Handle);
                }).Start();
            }
        }

        private void NotifyStreamChanged(long handle)
        {
            Trace.TraceInformation($"NotifyStreamChanged {handle}");
            _client.UpdateAppStream(handle);
        }

        private void UpdateApplicationStreams(ApplicationStreams streams)
        {
            Device device = streams.Device.Clone();
            if (_audioAppStreamsResolver?.Get(ref streams) ?? false)
            {
                Trace.TraceInformation("UpdateApplicationStreams::Success!");
            }
            else
            {
                streams = new ApplicationStreams();
                streams.ApplicationStreamList.Clear();
                Trace.TraceError("UpdateApplicationStreams::Failed!");
            }
            streams.Device = device;
            if (_cache.ContainsKey(streams.Device.Handle))
            {
                List<ApplicationStream> first = _cache[streams.Device.Handle];
                if (first.SequenceEqual(streams.ApplicationStreamList))
                {
                    Trace.TraceInformation("UpdateApplicationStreams:: stream list already sent.");
                    return;
                }
            }
            Trace.TraceInformation("UpdateApplicationStreams::Success!");
            foreach (string item in FILTER)
            {
                streams.ApplicationStreamList.RemoveAll((ApplicationStream x) => x.Name.ToLower().Contains(item));
            }
            ApplicationStream riotclientservices = streams.ApplicationStreamList.FirstOrDefault((ApplicationStream x) => x.ExePath.ToLower().Contains(RIOTCLIENTSERVICES));
            if (riotclientservices != null)
            {
                Trace.TraceInformation($"UpdateApplicationStreams:: {RIOTCLIENTSERVICES} found! Cloning.");
                ApplicationStream applicationStream = Serializer.DeepClone(riotclientservices);
                if (applicationStream != null)
                {
                    _applicationStreamWorkAround.AddOrUpdate(VALORANT, applicationStream, (string key, ApplicationStream value) => riotclientservices);
                }
            }
            streams.ApplicationStreamList.RemoveAll((ApplicationStream x) => x.ExePath.ToLower().Contains(RIOTCLIENTSERVICES));
            string arg = string.Join(", ", streams.ApplicationStreamList.Select((ApplicationStream x) => x.Name + " id:" + x.StreamID + " source:" + x.Source + " output:" + x.OutputMode + " path:" + x.ExePath));
            Trace.TraceInformation($"UpdateApplicationStreams::Sending {arg}");
            if (_client.SetAppStream(streams))
            {
                _cache[streams.Device.Handle] = streams.ApplicationStreamList;
            }
            Trace.TraceInformation("UpdateApplicationStreams::SetAppStream done.");
        }
    }
}
