using Unity.Entities;

using UnityEngine;

namespace Test.SoftFace
{
    class BoneBaker : MonoBehaviour
    {


        class BoneBakerBaker : Baker<BoneBaker>
        {
            public override void Bake(BoneBaker authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BoneTag>(entity);
                AddComponent<Bone>(entity);
            }
        }
    }

    public struct BoneTag : IComponentData { }
    public struct Bone : IComponentData
    {
        public Entity Holder;
        public int Index;
    }
}