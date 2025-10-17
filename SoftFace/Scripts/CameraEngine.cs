using UnityEngine;

namespace Core.Rendering
{
    public class CameraEngine : CameraEngineBase
    {
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, TargetPosition);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, (TargetPosition - transform.position).normalized);
        }
#endif
    }
}