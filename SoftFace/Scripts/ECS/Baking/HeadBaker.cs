using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

namespace Test.SoftFace
{
    class HeadBaker : MonoBehaviour
    {


        class HeadBakerBaker : Baker<HeadBaker>
        {
            public override void Bake(HeadBaker authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<HeadTag>(entity);
                AddComponent<Head>(entity);
                AddBuffer<VertexInfo>(entity);
                AddBuffer<BoneInfo>(entity);
            }
        }
    }

    public struct HeadTag : IComponentData { }
    public struct Head : IComponentData
    {
        public Matrix4x4 objectToWorld;

        public int RenderableID;
    }
    public struct VertexInfo : IBufferElementData
    {
        public Entity Value;
    }
    public struct BoneInfo : IBufferElementData
    {
        public Entity Value;
        public RigidTransform BindPose;
        public float Power;
    }
}