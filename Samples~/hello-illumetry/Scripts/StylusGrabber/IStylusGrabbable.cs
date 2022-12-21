using System;

namespace Illumetry.Unity.Demo
{
    public interface IStylusGrabbable
    {
        event Action<IStylusGrabbable> OnDestroing;
        int InstanceId { get; }
        void OnStartGrab(StylusGrabber stylusGrabber);
        void OnGrabProcess(StylusGrabber stylusGrabber);
        void OnEndGrab(StylusGrabber stylusGrabber);
    }
}