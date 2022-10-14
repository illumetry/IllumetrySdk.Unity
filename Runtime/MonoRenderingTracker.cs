using Antilatency.Alt.Environment;
using Antilatency.DeviceNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Illumetry.Unity {
    
    public class MonoRenderingTracker : LifeTimeControllerStateMachine, IBatteryLevelProvider, IHeadTracker {

        public string TrackerSocketTag = "MonoRenderingTracker";

        public Pose? GetEyePose(bool left, float extrapolationTime) {
            if (TrackingCotask.IsNull())
                return null;
            var state = TrackingCotask.getExtrapolatedState(Placement, extrapolationTime);

            if (state.stability.stage == Antilatency.Alt.Tracking.Stage.Tracking6Dof
                || state.stability.stage == Antilatency.Alt.Tracking.Stage.TrackingBlind6Dof) {
                return state.pose;
            }
            return null;
        }
        
        private void Reset() {
            this.enabled = false;
        }

        public bool? IsChargerConnected() {
            if (BatteryCotask.IsNull()) {
                return null;
            }
            return false;
            //return BatteryCotask.isChargerConnected();
        }

        /// <summary>
        /// Returns the battery level in the range 0 to 1, where 1 is 100% battery level.
        /// </summary>
        /// <returns></returns>
        public float? GetBatteryLevel() {
            if (BatteryCotask.IsNull()) {
                return null;
            }
            return BatteryCotask.getBatteryLevel();
        }

        private Antilatency.Alt.Tracking.ITrackingCotask TrackingCotask { get; set; }
        private Antilatency.DeviceNetwork.ICotaskBatteryPowered BatteryCotask { get; set; }
        public Pose Placement { get; private set; }
        public uint TrackingTaskRestartCount { get; private set; }

        protected override IEnumerable StateMachine() {
            using var altLibrary = Antilatency.Alt.Tracking.Library.load();
            using var storageClientLibrary = Antilatency.StorageClient.Library.load();
            using var altCotaskConstructor = altLibrary.createTrackingCotaskConstructor();
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
           
            yield return status;

            if (network.IsNull()) {
                goto WaitingForNetwork;
            }

            var altNodes = altCotaskConstructor.findSupportedNodes(network);
            var altNode = altNodes.FirstOrDefault(x => {
                if (network.nodeGetStatus(x) != NodeStatus.Idle) {
                    status = "Alt not connected";
                    return false;
                }
                var parent = network.nodeGetParent(x);
                try {
                    
                    return network.nodeGetStringProperty(parent, "Tag") == TrackerSocketTag;
                }
                catch (Exception) {
                    status = $"No Alt with socket tag \"{TrackerSocketTag}\"";
                    return false;
                }
            });


            if (altNode == NodeHandle.Null) {
                yield return status;
                goto WaitingForNode;
            }


            string placementString = null;
            try {
                //Try to read Placement from socket property
                placementString = network.nodeGetStringProperty(network.nodeGetParent(altNode), $"Placement");
            }
            catch (Exception) {
                status = $"No \"Placement\" property in socket with tag \"{TrackerSocketTag}\"";
            }


            if (placementString == null && storageClientLibrary != null) {
                try {
                    using var localStorage = storageClientLibrary.getLocalStorage();
                    placementString = localStorage.read("placement", "default");
                }
                catch (Exception) {
                    status = $"No \"Placement\" property in socket with tag \"{TrackerSocketTag}\" and no placement in AntilatencyStorage";
                }
            }

            if (placementString == null) {
                goto WaitingForNode;
            }
                

            Placement = altLibrary.createPlacement(placementString);

            if (displayCotask.IsNull()) {
                yield return status;
                goto WaitingForDisplay;
            }

            {
                if (environment.IsNull()) {
                    yield return status;
                    goto WaitingForEnvironment;
                }
                using var trackingCotask = altCotaskConstructor.startTask(network, altNode, environment);
                TrackingCotask = trackingCotask;

                while (!trackingCotask.isTaskFinished()) {
                    if (displayCotask.IsNull()) {
                        yield return status;
                        goto WaitingForDisplay;
                    }

                    yield return "";
                    if (Destroying) yield break;
                }

                ++TrackingTaskRestartCount;
                goto WaitingForNode;
            }
        }
    }
}