using Unity.Entities;

using UnityEngine;

namespace Test.Angular
{
    class RotatorBaker : MonoBehaviour
    {


        class RotatorBakerBaker : Baker<RotatorBaker>
        {
            public override void Bake(RotatorBaker authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<RotatorTag>(entity);
                AddComponent(entity, new RotatorInfo { Velocity = 1f, TargetUp = Vector3.up, });
            }
        }
    }

    public struct RotatorTag : IComponentData { }
    public struct RotatorInfo : IComponentData
    {
        public float Velocity;
        public float Damping;
        public Vector3 TargetUp;
    }
}