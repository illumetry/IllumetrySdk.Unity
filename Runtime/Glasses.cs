using Antilatency.Alt.Environment;
using Antilatency.DeviceNetwork;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Illumetry.Unity {

    public interface IHeadTracker {
        Pose? GetEyePose(bool left, float extrapolationTime);
        public uint TrackingTaskRestartCount { get; }
    }

    public interface IBatteryLevelProvider {
        public bool? IsChargerConnected();
        /// <summary>
        /// Returns the battery level in the range 0 to 1, where 1 is 100% battery level.
        /// </summary>
        /// <returns></returns>
        public float? GetBatteryLevel();
    }

    public interface IGlasses : IBatteryLevelProvider, IHeadTracker {
        public float? GlassesPolarizationAngle { get; }
        public float? QuarterWaveplateAngle { get; }
        public uint GlassesTaskRestartCount { get; }
    }

    public class Glasses : LifeTimeControllerStateMachine, IGlasses {

        public float PupillaryDistance = 0.06f;

        public Pose? GetEyePose(bool left, float extrapolationTime) {
            if (TrackingCotask.IsNull()) {
                return null;
            }
            var state = TrackingCotask.getExtrapolatedState(Placement, extrapolationTime);

            if (state.stability.stage == Antilatency.Alt.Tracking.Stage.Tracking6Dof || state.stability.stage == Antilatency.Alt.Tracking.Stage.TrackingBlind6Dof) {
                state.pose.position += (left ? -0.5f : 0.5f) * PupillaryDistance * state.pose.right;
                return state.pose;
            }
            return null;
        }

        public bool? IsChargerConnected() {
            if (GlassesCotask.IsNull()) {
                return null;
            }
            return GlassesCotask.isChargerConnected();
        }

        /// <summary>
        /// Returns the battery level in the range 0 to 1, where 1 is 100% battery level.
        /// </summary>
        /// <returns></returns>
        public float? GetBatteryLevel() {
            if (GlassesCotask.IsNull()) {
                return null;
            }
            return GlassesCotask.getBatteryLevel();
        }

        private Antilatency.Alt.Tracking.ITrackingCotask TrackingCotask { get; set; }
        private Antilatency.StereoGlasses.ICotask GlassesCotask { get; set; }
        public Pose Placement { get; private set; }
        public float? GlassesPolarizationAngle { get; private set; }
        public float? QuarterWaveplateAngle { get; private set; }
        public uint GlassesTaskRestartCount { get; private set; }
        public uint TrackingTaskRestartCount { get; private set; }

        protected override IEnumerable StateMachine() {

            using var glassesLibrary = Antilatency.StereoGlasses.Library.load();
            using var glassesCotaskConstructor = glassesLibrary.getCotaskConstructor();

            using var altLibrary = Antilatency.Alt.Tracking.Library.load();
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
            status = "Glasses not connected";

            if (network.IsNull())
                goto WaitingForNetwork;

            var glassesNodes = glassesCotaskConstructor.findSupportedNodes(network);
            var glassesNode = glassesNodes.FirstOrDefault(x => network.nodeGetStatus(x) == NodeStatus.Idle);

            if (glassesNode == NodeHandle.Null) {
                yield return status;
                goto WaitingForNode;
            }

            var placementString = network.nodeGetStringProperty(glassesNode, $"sys/Placement");
            Placement = altLibrary.createPlacement(placementString);
            {
                using var propertiesReader = new AdnPropertiesReader(network, glassesNode);
                GlassesPolarizationAngle = propertiesReader.TryRead("sys/GlassesPolarizationAngle", AdnPropertiesReader.ReadFloat);
                QuarterWaveplateAngle = propertiesReader.TryRead("sys/QuarterWaveplateAngle", AdnPropertiesReader.ReadFloat);
            }
            status = "Alt not connected";
            var altNodes = altCotaskConstructor.findSupportedNodes(network);
            var altNode = altNodes.FirstOrDefault(x =>
                network.nodeGetStatus(x) == NodeStatus.Idle
                && network.nodeGetParent(x) == glassesNode
            );

            if (altNode == NodeHandle.Null) {
                yield return status;
                goto WaitingForNode;
            }

            if (displayCotask.IsNull()) {
                yield return status;
                goto WaitingForDisplay;
            }

            {
                using var glassesCotask = glassesCotaskConstructor.startTask(network, glassesNode);
                GlassesCotask = glassesCotask;

                displayCotask.setFrameScheduleReceiver(glassesCotask.getFrameScheduleReceiver());
                using var _ = new Disposable(() => {
                    if (!displayCotask.IsNull()) {
                        displayCotask.setFrameScheduleReceiver(null);
                    }
                });


                if (environment.IsNull()) {
                    yield return status;
                    goto WaitingForEnvironment;
                }
                using var trackingCotask = altCotaskConstructor.startTask(network, altNode, environment);
                TrackingCotask = trackingCotask;

                while (!glassesCotask.isTaskFinished() && !trackingCotask.isTaskFinished()) {
                    if (displayCotask.IsNull()) {
                        yield return status;
                        goto WaitingForDisplay;
                    }

                    yield return "";
                    if (Destroying) yield break;
                }

                if (glassesCotask.isTaskFinished()) {
                    ++GlassesTaskRestartCount;
                } else {
                    if (trackingCotask.isTaskFinished()) {
                        ++TrackingTaskRestartCount;
                    }
                }

                goto WaitingForNode;
            }
        }
    }
}