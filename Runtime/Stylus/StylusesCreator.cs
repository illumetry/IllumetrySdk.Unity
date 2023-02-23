using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Antilatency.Alt.Environment;
using Antilatency.Alt.Tracking;
using Antilatency.DeviceNetwork;
using Antilatency.HardwareExtensionInterface;
using Antilatency.HardwareExtensionInterface.Interop;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Illumetry.Unity.Stylus {
    public class StylusesCreator : LifeTimeControllerStateMachine {
        public static event Action<Stylus> OnCreatedStylus;
        private static Stylus[] _cachedStyluses = new Stylus[0];
        public static Stylus[] Styluses {
            get {
                return _cachedStyluses;
            }
        }

        private Antilatency.HardwareExtensionInterface.ILibrary _hardwareExtensionLibrary;
        private Antilatency.HardwareExtensionInterface.ICotaskConstructor _hardwareExtensionCotaskConstructor;

        private Antilatency.Alt.Tracking.ILibrary _altLibrary;
        private Antilatency.Alt.Tracking.ITrackingCotaskConstructor _altCotaskConstructor;

        private static StylusesCreator _instance;
        private static int _counterStyluses = -1;

        [SerializeField] private GameObject stylusGoTemplate;
        [SerializeField, Header("Default: Stylus\nYou can add your tags to the list.")] private List<string> _requaredTags = new List<string>() { "Stylus" };
        private const string _hardwareStylusName = "AntilatencyStylusAlpha";
        private uint _lastCheckedUpdateId;
        private Dictionary<int, Stylus> _createdStyluses = new Dictionary<int, Stylus>();

#if UNITY_EDITOR
        private void OnValidate() {
            if (stylusGoTemplate != null && stylusGoTemplate.GetComponent<Stylus>() == null) {
                Debug.LogError("Stylus template gameobject requares a Stylus component. (Stylus.cs)");
                stylusGoTemplate = null;
            }

            ValidateStylusTemplate(true);
        }

        public void ValidateStylusTemplate(bool showWarning = true) {
            if (stylusGoTemplate == null) {

                if (showWarning) {
                    Debug.LogWarning("Stylus template can not be null!");
                }

                stylusGoTemplate = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.illumetry.sdk/Source/StylusTemplate.prefab");

                if (stylusGoTemplate.GetComponent<Stylus>() == null) {
                    Debug.LogError("Default stylus template don't contain Stylus.cs.");
                }
            }
        }

#endif

        protected override void Create() {
            _hardwareExtensionLibrary = Antilatency.HardwareExtensionInterface.Library.load();
            _hardwareExtensionCotaskConstructor = _hardwareExtensionLibrary.getCotaskConstructor();

            _altLibrary = Antilatency.Alt.Tracking.Library.load();
            _altCotaskConstructor = _altLibrary.createTrackingCotaskConstructor();
            
            base.Create();

            if (_instance != null) {
                Debug.LogError("StylusesCreator has dublicates. Will remove this new instance!");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        protected override void Destroy() {
            base.Destroy();
            _instance = null;
            
            Antilatency.Utils.SafeDispose(ref _altCotaskConstructor);
            Antilatency.Utils.SafeDispose(ref _altLibrary);
            Antilatency.Utils.SafeDispose(ref _hardwareExtensionCotaskConstructor);
            Antilatency.Utils.SafeDispose(ref _hardwareExtensionLibrary);
        }

        protected override IEnumerable StateMachine() {
            string status = string.Empty;
            
            WaitForNetworks:
            if (Destroying) { yield break; }

            var deviceNetworkProvider = GetComponent<DeviceNetworkProvider>();

            status = "Finding network provider";
            if (deviceNetworkProvider == null) {
                yield return status;
                goto WaitForNetworks;
            }

            var network = deviceNetworkProvider.Network;
            var environmentProvider = GetComponent<IEnvironmentProvider>();

            if (network.IsNull()) {
                yield return status;
                goto WaitForNetworks;
            }

            status = "Finding environment";
            
            if (environmentProvider == null || environmentProvider.Environment.IsNull()) {
                yield return status;
                goto WaitForNetworks;
            }

            status = "";
            
            WaitForFindStyluses:
            if(Destroying) { yield break;}
            var isCaught = false;

            if (_lastCheckedUpdateId != deviceNetworkProvider.LastUpdateId) {
                _lastCheckedUpdateId = deviceNetworkProvider.LastUpdateId;

                if (environmentProvider.Environment.IsNull()) {
                    yield return status;
                    goto WaitForNetworks;
                }

                NodeHandle[] extensionSupportedNodes;
                NodeHandle[] allAltTrackingNodes = Array.Empty<NodeHandle>();
                NodeHandle extensionNode;

                try {
                    extensionSupportedNodes = _hardwareExtensionCotaskConstructor.findSupportedNodes(network);
                    allAltTrackingNodes = _altCotaskConstructor.findSupportedNodes(network);

                    //Finding builded by antilatency stylus.
                    extensionNode = extensionSupportedNodes.FirstOrDefault(n =>
                        network.nodeGetStringProperty(n, Antilatency.DeviceNetwork.Interop.Constants.HardwareNameKey).Contains(_hardwareStylusName) &&
                        network.nodeGetStatus(n) == NodeStatus.Idle);

                    //Try find crafted by user stylus.
                    if (extensionNode == NodeHandle.Null) {
                        foreach (string customStylusTag in _requaredTags) {
                            if (customStylusTag != string.Empty) {
                                extensionNode = extensionSupportedNodes.FirstOrDefault(n =>
                                    network.nodeGetStringProperty(n, "Tag").Equals(customStylusTag) &&
                                    network.nodeGetStatus(n) == NodeStatus.Idle);
                            } else {

                                if (Application.isEditor || Debug.isDebugBuild) {
                                    Debug.LogError("Stylus tag == string.Empty. Fix the list of tags for the styluses.");
                                }
                            }
                        }
                    }
                }
                catch {
                    extensionNode = NodeHandle.Null;
                }

                if (extensionNode == NodeHandle.Null) {
                    yield return status;
                    goto WaitForFindStyluses;
                }

                NodeHandle altNode = allAltTrackingNodes.FirstOrDefault(n => network.nodeGetParent(n) == extensionNode);

                if (altNode == NodeHandle.Null) {
                    yield return status;
                    goto WaitForFindStyluses;
                }

                ITrackingCotask trackingCotask = null;
                Antilatency.HardwareExtensionInterface.ICotask extensionsCotask = null;
                IInputPin inputPin = null;
                
                try {
                    extensionsCotask = _hardwareExtensionCotaskConstructor.startTask(network, extensionNode);
                    inputPin = extensionsCotask.createInputPin(Pins.IO1);
                    extensionsCotask.run();
                }
                catch (Exception e) {

                    if (Application.isEditor || Debug.isDebugBuild) {
                        Debug.LogError($"Stylus start extension task failed. {e.Message} \n {e.StackTrace}");
                    }

                    Antilatency.Utils.SafeDispose(ref inputPin);
                    Antilatency.Utils.SafeDispose(ref extensionsCotask);
                    isCaught = true;
                }

                if (isCaught) {
                    yield return status;
                    goto WaitForFindStyluses;
                }

                try {
                    trackingCotask = _altCotaskConstructor.startTask(network, altNode, environmentProvider.Environment);

                    if (trackingCotask.IsNull()) {
                        throw new Exception();
                    }
                }
                catch (Exception e) {

                    if (Application.isEditor || Debug.isDebugBuild) {
                        Debug.LogError($"Stylus start tracking task failed. {e.Message} \n {e.StackTrace}");
                    }

                    Antilatency.Utils.SafeDispose(ref inputPin);
                    Antilatency.Utils.SafeDispose(ref trackingCotask);
                    Antilatency.Utils.SafeDispose(ref extensionsCotask);
                    isCaught = true;
                }
                
                if (isCaught) {
                    yield return status;
                    goto WaitForFindStyluses;
                }

                _counterStyluses = _counterStyluses + 1;
                int idStylus = _counterStyluses;

                GameObject go = Instantiate(stylusGoTemplate, Vector3.zero, Quaternion.identity, transform);
                Stylus stylus = go.GetComponent<Stylus>();
                stylus.Initialize(idStylus, trackingCotask, extensionsCotask, inputPin);
                status = string.Empty;
                stylus.OnDestroying += OnDestroyingStylus;
                OnCreatedStylus?.Invoke(stylus);
                _createdStyluses[idStylus] = stylus;
                UpdateCachedStyluses();

                if (Application.isEditor || Debug.isDebugBuild) {
                    Debug.Log($"StylusCreator: Stylus created successfully -> {idStylus}");
                    Debug.Log($"Device network update id: {_lastCheckedUpdateId}");
                }
            }

            if (!Destroying) {
                yield return status;
                goto WaitForFindStyluses;
            }
        }

        private void OnDestroyingStylus(Stylus stylus) {
            if(stylus == null) {
                return;
            }

            stylus.OnDestroying -= OnDestroyingStylus;
            _createdStyluses.Remove(stylus.Id);
            UpdateCachedStyluses();
        }

        private void UpdateCachedStyluses() {
            int countStyluses = _createdStyluses.Count;
            _cachedStyluses = new Stylus[countStyluses];

            int index = 0;
            foreach (var kvp in _createdStyluses) {
                if (kvp.Value == null) {
                    if (Application.isEditor || Debug.isDebugBuild) {
                        Debug.LogError("Stylus value in _createdStyluses is null!");
                    }

                    continue;
                }

                _cachedStyluses[index] = kvp.Value;
                index++;
            }
        }
    }
}