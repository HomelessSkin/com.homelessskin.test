using Core.GamePlay;

using Unity.Entities;

using UnityEngine;

namespace Test.SoftFace
{
    class CursorBaker : MonoBehaviour
    {
        [SerializeField] Cursor Cursor;

        class CursorBakerBaker : Baker<CursorBaker>
        {
            public override void Bake(CursorBaker authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CursorTag>(entity);
                AddComponent(entity, authoring.Cursor);
            }
        }
    }

    public struct CursorTag : IComponentData { }
    [System.Serializable]
    public struct Cursor : IComponentData
    {
        public GameEvent.Type Type;
    }
}