using Core.Util;

using Unity.Entities;

using UnityEngine;

namespace Test.SoftFace
{
    class SkeletonRefBaker : MonoBehaviour
    {
        [SerializeField] GameObject HeadPrefab;
        [SerializeField] GameObject BonePrefab;
        [SerializeField] GameObject VertexPrefab;

        SkeletonBaker[] SkeletonBakers;

#if UNITY_EDITOR
        void OnValidate()
        {
            SkeletonBakers = Resources.LoadAll<SkeletonBaker>("SoftFace_Prefabs/");
        }
#endif

        class SkeletonRefBakerBaker : Baker<SkeletonRefBaker>
        {
            public override void Bake(SkeletonRefBaker authoring)
            {
                if (!authoring.HeadPrefab || !authoring.BonePrefab || !authoring.VertexPrefab)
                    return;

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SkeletonData
                {
                    Vertex = GetEntity(authoring.VertexPrefab, TransformUsageFlags.Dynamic),
                    Head = GetEntity(authoring.HeadPrefab, TransformUsageFlags.Dynamic),
                    Bone = GetEntity(authoring.BonePrefab, TransformUsageFlags.Dynamic),
                });

                AddBuffer<SkeletonRef>(entity);
                if (authoring.SkeletonBakers != null)
                    for (int b = 0; b < authoring.SkeletonBakers.Length; b++)
                    {
                        var baker = authoring.SkeletonBakers[b];
                        AppendToBuffer(entity, new SkeletonRef
                        {
                            ID = baker.GetID(),
                            Value = GetEntity(baker.gameObject, TransformUsageFlags.None),
                        });
                    }
            }
        }
    }

    public struct SkeletonData : IComponentData
    {
        public Entity Vertex;
        public Entity Head;
        public Entity Bone;
    }
    public struct SkeletonRef : IKeyBuffer
    {
        public int ID;
        public Entity Value;

        public int GetID() => ID;
        public Entity GetEntity() => Value;
    }
}