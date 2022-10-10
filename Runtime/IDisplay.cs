#if UNITY_EDITOR
#endif

using UnityEngine;

namespace Illumetry.Unity {
    public interface IDisplay {
        DisplayProperties DisplayProperties { get; }

        Illumetry.Display.ICotask Cotask { get; }
        Matrix4x4 GetScreenToEnvironment();
        Vector3 GetCameraPositionRelativeToFrame(Vector3 environmentSpaceCameraPosition);
        Matrix4x4 GetProjectionMatrix(Vector3 environmentSpaceCameraPosition, float nearClip, float farClip);
        Antilatency.Alt.Environment.IEnvironment Environment { get; }
    }
}
