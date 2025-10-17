using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

namespace Test.SoftFace
{
    class VertexBaker : MonoBehaviour
    {

        class VertexBakerBaker : Baker<VertexBaker>
        {
            public override void Bake(VertexBaker authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VertexTag>(entity);
                AddComponent<Vertex>(entity);
                AddBuffer<BoneWeight>(entity);
            }
        }
    }

    public struct VertexTag : IComponentData { }
    public struct BoneWeight : IBufferElementData
    {
        public Entity Target;
        public int TargetIndex;
        public float Weight;
        public float3 RelativePosition;
    }
    public struct Vertex : IComponentData
    {
        public Entity Holder;
        public int Index;
        public int F_Index;
        public int R_Index;
        public float3 Origin;
    }
}