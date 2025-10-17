using System.Collections.Generic;

using Core.Rendering;
using Core.Util;

using Unity.Entities;

using UnityEngine;

namespace Test.SoftFace
{
    public class FaceManager : MonoBehaviour
    {
        [SerializeField] RenderableObject[] Faces;

        internal RenderableObject[] GetFaces() => Faces;

        void Start()
        {
            var g = World
                .DefaultGameObjectInjectionWorld
                .GetOrCreateSystemManaged<RenderSystemGroup>();

            g.AddSystemToUpdateList(World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystemManaged<ExpressionEditorSystem>());

            g.SortSystems();
        }

#if UNITY_EDITOR
        void Reset()
        {
            Tool.CreateTag("FaceManager");
            gameObject.tag = "FaceManager";
        }
        void OnValidate()
        {
            var list = new List<RenderableObject>();
            if (Faces != null)
                list.AddRange(Faces);

            var objs = Resources.LoadAll<RenderableObject>("SoftFace_Renderables/");
            for (int o = 0; o < objs.Length; o++)
                if (!list.Contains(objs[o]))
                    list.Add(objs[o]);

            Faces = list.ToArray();
        }
#endif
    }
}