using System;
using Antilatency.Alt.Environment;
using Antilatency.DeviceNetwork;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Illumetry.Unity {

    public interface IHeadTracker {
        Pose? GetEyePose(bool left, float extrapolationTime);
        uint TrackingTaskRestartCount { get; }
    }

    public interface IBatteryLevelProvider {
        bool? IsChargerConnected();
        /// <summary>
        /// Returns the battery level in the range 0 to 1, where 1 is 100% battery level.
        /// </summary>
        /// <returns></returns>
        float? GetBatteryLevel();
    }

    public interface IGlasses : IBatteryLevelProvider, IHeadTracker {
        float? GlassesPolarizationAngle { get; }
        float? QuarterWaveplateAngle { get; }
        uint GlassesTaskRestartCount { get; }
    }

    public class Glasses : LifeTimeControllerStateMachine, IGlasses {

        public float PupillaryDistance = 0.06f;

        public virtual Pose? GetEyePose(bool left, float extrapolationTime) {
            if (_trackingCotask.IsNull()) {
                return null;
            }
            var state = _trackingCotask.getExtrapolatedState(Placement, extrapolationTime);

            if (state.stability.stage == Antilatency.Alt.Tracking.Stage.Tracking6Dof || state.stability.stage == Antilatency.Alt.Tracking.Stage.TrackingBlind6Dof) {
                state.pose.position += (left ? -0.5f : 0.5f) * PupillaryDistance * state.pose.right;
                return state.pose;
            }
            return null;
        }

        public bool? IsChargerConnected() {
            if (_glassesCotask.IsNull()) {
                return null;
            }
            return _glassesCotask.isChargerConnected();
        }

        /// <summary>
        /// Returns the battery level in the range 0 to 1, where 1 is 100% battery level.
        /// </summary>
        /// <returns></returns>
        public float? GetBatteryLevel() {
            if (_glassesCotask.IsNull()) {
                return null;
            }
            return _glassesCotask.getBatteryLevel();
        }

        protected Antilatency.Alt.Tracking.ITrackingCotask _trackingCotask;
        protected Antilatency.StereoGlasses.ICotask _glassesCotask;
        
        public Pose Placement { get; private set; }
        public float? GlassesPolarizationAngle { get; private set; }
        public float? QuarterWaveplateAngle { get; private set; }
        public uint GlassesTaskRestartCount { get; private set; }
        public uint TrackingTaskRestartCount { get; private set; }

        private Antilatency.StereoGlasses.ILibrary _glassesLibrary;
        private Antilatency.StereoGlasses.ICotaskConstructor _glassesCotaskConstructor;
        
        private Antilatency.Alt.Tracking.ILibrary _altLibrary;
        private Antilatency.Alt.Tracking.ITrackingCotaskConstructor _altCotaskConstructor;

        protected override void Destroy() {
            base.Destroy();
            
            Antilatency.Utils.SafeDispose(ref _trackingCotask);
            Antilatency.Utils.SafeDispose(ref _glassesCotask);

            Antilatency.Utils.SafeDispose(ref _glassesCotaskConstructor);
            Antilatency.Utils.SafeDispose(ref _glassesLibrary);
            Antilatency.Utils.SafeDispose(ref _altCotaskConstructor);
            Antilatency.Utils.SafeDispose(ref _altLibrary);
        }

        protected override IEnumerable StateMachine() {

            _glassesLibrary = Antilatency.StereoGlasses.Library.load();
            _glassesCotaskConstructor = _glassesLibrary.getCotaskConstructor();

            _altLibrary = Antilatency.Alt.Tracking.Library.load();
            _altCotaskConstructor = _altLibrary.createTrackingCotaskConstructor();

            string status = "";


        WaitingForNetwork:
            if (Destroying) yield break;
            status = "Waiting for Network";

            var network = GetComponent<IDeviceNetworkProvider>()?.Network;
            if (network.IsNull()) {
                yield return status;
                goto WaitingForNetwork;
            }


        WaitingForEnvironment:
            if (Destroying) yield break;
            status = "Waiting for Environment";

            var environment = GetComponent<IEnvironmentProvider>()?.Environment;
            if (environment.IsNull()) {
                yield return status;
                goto WaitingForEnvironment;
            }

        WaitingForDisplay:
            if (Destroying) yield break;
            status = "Display not found";

            var displayCotask = GetComponent<IDisplay>()?.Cotask;
            if (displayCotask.IsNull()) {
                yield return status;
                goto WaitingForDisplay;
            }

        WaitingForNode:
            if (Destroying) yield break;
            status = "Glasses not connected";

            if (network.IsNull()) {
                goto WaitingForNetwork;
            }

            NodeHandle glassesNode;
            try {
                var glassesNodes = _glassesCotaskConstructor.findSupportedNodes(network);
                glassesNode = glassesNodes.FirstOrDefault(x => network.nodeGetStatus(x) == NodeStatus.Idle);
            }
            catch (Antilatency.InterfaceContract.Exception e){
                Debug.LogError(e.Message);
                goto WaitingForNode;
            }

            if (glassesNode == NodeHandle.Null) {
                yield return status;
                goto WaitingForNode;
            }

            try {
                var placementString = network.nodeGetStringProperty(glassesNode, $"sys/Placement");
                Placement = _altLibrary.createPlacement(placementString);
                
                using (var propertiesReader = new AdnPropertiesReader(network, glassesNode)) {
                    GlassesPolarizationAngle = propertiesReader.TryRead("sys/GlassesPolarizationAngle", AdnPropertiesReader.ReadFloat);
                    QuarterWaveplateAngle = propertiesReader.TryRead("sys/QuarterWaveplateAngle", AdnPropertiesReader.ReadFloat);
                }
            }
            catch (Antilatency.InterfaceContract.Exception e){
                Debug.LogError(e.Message);
                goto WaitingForNode;
            }

            NodeHandle altNode;
            try {
                status = "Alt not connected";
                var altNodes = _altCotaskConstructor.findSupportedNodes(network);
                altNode = altNodes.FirstOrDefault(x =>
                    network.nodeGetStatus(x) == NodeStatus.Idle
                    && network.nodeGetParent(x) == glassesNode
                );
            }
            catch (Antilatency.InterfaceContract.Exception e){
                Debug.LogError(e.Message);
                goto WaitingForNode;
            }

            if (altNode == NodeHandle.Null) {
                yield return status;
                goto WaitingForNode;
            }

            if (displayCotask.IsNull()) {
                yield return status;
                goto WaitingForDisplay;
            }

            {
                try {
                    _glassesCotask = _glassesCotaskConstructor.startTask(network, glassesNode);
                }
                catch (Antilatency.InterfaceContract.Exception e) {
                    Antilatency.Utils.SafeDispose(ref _glassesCotask);
                    Debug.LogError(e.Message);
                    goto WaitingForNode;
                }

                if (_glassesCotask.IsNull()) {
                    Antilatency.Utils.SafeDispose(ref _glassesCotask);
                    Debug.LogError("Glasses::StateMachine: failed to start glasses task");
                    goto WaitingForNode;
                }

                try {
                    displayCotask.setFrameScheduleReceiver(_glassesCotask.getFrameScheduleReceiver());
                }
                catch (Exception e) {
                    Debug.LogError("Glasses::StateMachine: exception has been thrown at setFrameScheduleReceiver, what: " + e.Message);
                    Antilatency.Utils.SafeDispose(ref _glassesCotask);
                    goto WaitingForNode;
                }
                
                using (var _ = new Disposable(() => {
                        if (!displayCotask.IsNull()) {
                            displayCotask.setFrameScheduleReceiver(null);
                        }
                    }
                )) {
                    
                    if (environment.IsNull()) {
                        Antilatency.Utils.SafeDispose(ref _glassesCotask);
                    
                        yield return status;
                        goto WaitingForEnvironment;
                    }

                    try {
                        _trackingCotask = _altCotaskConstructor.startTask(network, altNode, environment);
                    }
                    catch (Antilatency.InterfaceContract.Exception e) {
                        Antilatency.Utils.SafeDispose(ref _glassesCotask);
                        Antilatency.Utils.SafeDispose(ref _trackingCotask);
                    
                        Debug.LogError("Glasses::StateMachine: failed to start tracking task, what: " + e.Message);
                        goto WaitingForNode;
                    }

                    if (_trackingCotask.IsNull()) {
                        Antilatency.Utils.SafeDispose(ref _glassesCotask);
                        Antilatency.Utils.SafeDispose(ref _trackingCotask);
                    
                        Debug.LogError("Glasses::StateMachine: failed to start tracking task");
                        goto WaitingForNode;
                    }

                    while (!_glassesCotask.isTaskFinished() && !_trackingCotask.isTaskFinished()) {
                        if (displayCotask.IsNull()) {
                            Antilatency.Utils.SafeDispose(ref _glassesCotask);
                            Antilatency.Utils.SafeDispose(ref _trackingCotask);
                        
                            yield return status;
                            goto WaitingForDisplay;
                        }

                        yield return "";
                        if (Destroying) yield break;
                    }

                    if (_glassesCotask.isTaskFinished()) {
                        ++GlassesTaskRestartCount;
                    } else {
                        if (_trackingCotask.isTaskFinished()) {
                            ++TrackingTaskRestartCount;
                        }
                    }

                    Antilatency.Utils.SafeDispose(ref _glassesCotask);
                    Antilatency.Utils.SafeDispose(ref _trackingCotask);

                    goto WaitingForNode;
                }
            }
        }
    }
}