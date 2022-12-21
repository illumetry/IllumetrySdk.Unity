using System;
using System.Collections;
using Antilatency.DeviceNetwork;
using UnityEngine;

namespace Illumetry.Unity {

    public class DeviceNetworkProvider : LifeTimeControllerStateMachine, IDeviceNetworkProvider
    {
        public INetwork Network { get; private set; }
        public bool UseIpNetworking = false;
        public uint LastUpdateId { get; private set; }
        
        private Antilatency.DeviceNetwork.ILibrary _library;

        protected override void Destroy() {
            base.Destroy();
            
            Antilatency.Utils.SafeDispose(ref _library);
        }

        protected override IEnumerable StateMachine() {
            _library = Antilatency.DeviceNetwork.Library.load();
            string status = "";

#if UNITY_ANDROID && !UNITY_EDITOR
            var jni = library.QueryInterface<AndroidJniWrapper.IAndroidJni>();
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity")) {
                    jni.initJni(IntPtr.Zero, activity.GetRawObject());
                }
            }
            jni.Dispose();
#endif
            _library.setLogLevel(LogLevel.Info);

            var deviceFilter = _library.createFilter();
            deviceFilter.addUsbDevice(new UsbDeviceFilter { vid = UsbVendorId.Antilatency, pid = 0x0000 });

            if (UseIpNetworking) {
                deviceFilter.addIpDevice(Antilatency.DeviceNetwork.Constants.AllIpDevicesIp, Antilatency.DeviceNetwork.Constants.AllIpDevicesMask);
            }

            TryCreateNetwork:
            {
                if (Destroying) yield break;
                status = "Trying to create Network";

                using (var network = _library.createNetwork(deviceFilter)) {
                    Network = network;

                    while (!Destroying) {
                        if (network.IsNull()) {
                            yield return status;
                            goto TryCreateNetwork;
                        }

                        try {
                            if (LastUpdateId != network.getUpdateId()) {
                                LastUpdateId = network.getUpdateId();
                            }
                        } catch(Antilatency.InterfaceContract.Exception e) {
                            Debug.LogError($"Device Network is null! Message: {e.Message} \n {e.StackTrace}");
                            goto TryCreateNetwork;
                        }

                        yield return null;
                    }
                }
            }
        }  
    }
}
