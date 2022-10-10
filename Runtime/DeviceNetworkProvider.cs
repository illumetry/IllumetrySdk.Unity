using System.Collections;
using Antilatency.DeviceNetwork;

namespace Illumetry {

    public class DeviceNetworkProvider : LifeTimeControllerStateMachine, IDeviceNetworkProvider {
        public INetwork Network { get; private set; }
        protected override IEnumerable StateMachine() {

            using var library = Antilatency.DeviceNetwork.Library.load();
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
            library.setLogLevel(LogLevel.Info);

            var deviceFilter = library.createFilter();
            deviceFilter.addUsbDevice(new UsbDeviceFilter { vid = UsbVendorId.Antilatency, pid = 0x0000 });

            TryCreateNetwork:
            {
                if (Destroying) yield break;
                status = "Trying to create Network";

                using var network = library.createNetwork(deviceFilter);
                Network = network;


                while (!Destroying) {
                    if (network.IsNull()) {
                        yield return status;
                        goto TryCreateNetwork;
                    }
                    yield return null;
                }

            }
        }  
    }
}
