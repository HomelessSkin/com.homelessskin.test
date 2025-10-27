#if UNITY_EDITOR
using UI;

using Unity.Mathematics;

using UnityEngine;

using static Unity.Mathematics.math;

namespace Test.Angular
{
    public class UIManager : UIManagerBase
    {
        [SerializeField] bool DrawGizmos;
        [SerializeField, Range(0f, 3f)] float SphereRadius = 0.1f;

        float3 Pos;
        float3 Up;
        float3 Target;

        public void SetTarget(Vector3 pos, Vector3 up, Vector3 target)
        {
            Pos = pos;
            Up = 2f * up;
            Target = 2f * target;
        }

        void OnDrawGizmos()
        {
            if (!DrawGizmos)
                return;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Pos, Up);
            Gizmos.DrawSphere(Pos + Up, SphereRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(Pos, Target);
            Gizmos.DrawSphere(Pos + Target, SphereRadius);
            Gizmos.color = Color.green;
            var axis = normalizesafe(cross(Up, Target));
            Gizmos.DrawRay(Pos, axis);
            Gizmos.DrawSphere(Pos + axis, SphereRadius);
        }
    }
}
#endif