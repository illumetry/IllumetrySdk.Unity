using System;
using System.Collections;
using Antilatency.Alt.Tracking;
using Antilatency.HardwareExtensionInterface;
using Antilatency.HardwareExtensionInterface.Interop;
using UnityEngine;
using UnityEngine.Rendering;

namespace Illumetry.Unity.Stylus
{
    public class Stylus : LifeTimeControllerStateMachine
    {
        /// <summary>
        /// bool - If true, button pressed, else unpressed.
        /// </summary>
        public event Action<Stylus,bool> OnUpdatedButtonPhase;
        /// <summary>
        /// Pose - Extrapolated world pose.
        /// Vector3 - Extrapolated world velocity.
        /// Vector3 - Extrapolated world angular velocity.
        /// </summary>
        public event Action<UnityEngine.Pose, Vector3,Vector3> OnUpdatedPose;
        public event Action<Stylus> OnDestroying;
        public int Id => _id;
        public UnityEngine.Pose ExtrapolatedPose => _extrapolatedPose;
        public Vector3 ExtrapolatedVelocity => _extrapolatedVelocity;
        public Vector3 ExtrapolatedAngularVelocity => _extrapolatedAngularVelocity;
       
        [Header("Default: 0.042f")]
        public float extrapolatedTimeDx11 = 0.042f;
        [Header("Default: 0.0305f")]
        public float extrapolatedTimeDx12 = 0.0305f;

        private ITrackingCotask _trackingCotask;
        private Antilatency.HardwareExtensionInterface.ICotask _extensionCotask;
        private IInputPin _inputPin;
        private int _id;
        private UnityEngine.Pose _extrapolatedPose;
        private Vector3 _extrapolatedVelocity;
        private Vector3 _extrapolatedAngularVelocity;

        public float extrapolateTime
        {
            get
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
                {
                    return extrapolatedTimeDx12;
                }

                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
                {
                    return extrapolatedTimeDx11;
                }

                return 0.04f;
            }
        }

        internal void Initialize(int id,ITrackingCotask trackingCotask, Antilatency.HardwareExtensionInterface.ICotask extensionsCotask,
            IInputPin inputPin)
        {
            _id = id;
            _trackingCotask = trackingCotask;
            _extensionCotask = extensionsCotask;
            _inputPin = inputPin;
        }

        protected override IEnumerable StateMachine()
        {
            if (!Destroying)
            {
                Subscribe();
            }
            
            string status = "Waiting cotasks.";

            WaitCotasks:
            if (Destroying) yield break;

            if (_trackingCotask.IsNull() || _extensionCotask.IsNull() || _inputPin.IsNull())
            {
                yield return status;
                goto WaitCotasks;
            }

            status = string.Empty;
            while (!_inputPin.IsNull() && !_trackingCotask.IsNull() && !_extensionCotask.IsNull() && !_trackingCotask.isTaskFinished() &&
                   !_extensionCotask.isTaskFinished())
            {
                OnUpdatedButtonPhase?.Invoke(this, _inputPin.getState() == PinState.Low);
                if (Destroying)
                {
                    Debug.LogWarning("Stylus has been destroyed! Will call dispose all cotasks and pin.");
                    yield break;
                }
                else yield return status;
            }

            UnSubscribeAndDisposeAll();
            DestroyGameObject();
        }
        
        private void OnBeforeRendererLeft()
        {
            UpdateStylusPose(extrapolateTime);
        }

        private void OnBeforeRendererRight()
        { 
            UpdateStylusPose(extrapolateTime);
        }

        private void OnBeforeRendererMono()
        {
            //For mono mode.   
            UpdateStylusPose(extrapolateTime);
        }

        private void UpdateStylusPose(float time)
        {
            if (_trackingCotask.IsNull() || _trackingCotask.isTaskFinished())
            {
                return;
            }

            if (DisplayHandle.Instance == null)
            {
                if (Application.isEditor || Debug.isDebugBuild)
                {
                    Debug.LogError("Stylus: Not found display handle, UpdateStylusPose will skiped.");
                }

                return;
            }

            Antilatency.Alt.Tracking.State extrapolatedState = _trackingCotask.getExtrapolatedState(Pose.identity, time);
            UnityEngine.Pose extrapolatedPose = extrapolatedState.pose;

            transform.localPosition = extrapolatedPose.position;
            transform.localRotation = extrapolatedPose.rotation;

            Transform displayHandleT = DisplayHandle.Instance.transform;
            Vector3 worldVelocity = displayHandleT.TransformDirection(extrapolatedState.velocity);
            Vector3 angularVelocity = displayHandleT.TransformDirection(extrapolatedState.localAngularVelocity);

            UnityEngine.Pose worldPose = new UnityEngine.Pose(transform.position, transform.rotation);

            _extrapolatedPose = worldPose;
            _extrapolatedVelocity = worldVelocity;
            _extrapolatedAngularVelocity = angularVelocity;

            OnUpdatedPose?.Invoke(ExtrapolatedPose, ExtrapolatedVelocity, ExtrapolatedAngularVelocity);
        }

        private void Subscribe()
        {
            RendererLCD.OnBeforeRendererL += OnBeforeRendererLeft;
            RendererLCD.OnBeforeRendererR += OnBeforeRendererRight;
            MonoRendererLCD.OnBeforeRenderer += OnBeforeRendererMono;
        }

        private void UnSubscribeAll()
        {
            RendererLCD.OnBeforeRendererL -= OnBeforeRendererLeft;
            RendererLCD.OnBeforeRendererR -= OnBeforeRendererRight;
            MonoRendererLCD.OnBeforeRenderer -= OnBeforeRendererMono;
        }
        
        private void UnSubscribeAndDisposeAll()
        {
            UnSubscribeAll();
            Antilatency.Utils.SafeDispose(ref _inputPin);
            Antilatency.Utils.SafeDispose(ref _extensionCotask);
            Antilatency.Utils.SafeDispose(ref _trackingCotask);
        }

        private void DestroyGameObject()
        {
            Destroy(gameObject);
        }

        protected override void Destroy()
        {
            base.Destroy();

            UnSubscribeAndDisposeAll();
            OnUpdatedButtonPhase?.Invoke(this,false);
            OnDestroying?.Invoke(this);
        }
    }
}