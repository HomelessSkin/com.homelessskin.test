using System.Collections.Generic;

using Core.Rendering;
using Core.Util;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

namespace Test.SoftFace
{
    class SkeletonBaker : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer Renderer;
        [SerializeField] RenderableObject Renderable;
        [SerializeField] float[] Powers;

        public int GetID() => Renderable.ID;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Renderer)
                return;

            DataUtil.FitSize(ref Powers, Renderer.bones);
        }
        void Reset()
        {
            Renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            Powers = new float[Renderer.bones.Length];
            for (int p = 0; p < Powers.Length; p++)
                Powers[p] = 1f;
        }
#endif

        class SkeletonBakerBaker : Baker<SkeletonBaker>
        {
            public override void Bake(SkeletonBaker authoring)
            {
                if (!authoring.Renderer)
                    return;

                var entity = GetEntity(TransformUsageFlags.None);

                AddBuffer<VertexData>(entity);
                var list = new List<Vertex>();
                var triangles = authoring.Renderer.sharedMesh.triangles;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    var a = triangles[t];
                    var b = triangles[t + 1];
                    var c = triangles[t + 2];

                    if (!Contains(list, a))
                        list.Add(new Vertex { Index = a, F_Index = b, R_Index = c });
                    if (!Contains(list, b))
                        list.Add(new Vertex { Index = b, F_Index = c, R_Index = a });
                    if (!Contains(list, c))
                        list.Add(new Vertex { Index = c, F_Index = a, R_Index = b });
                }
                for (int v = 0; v < list.Count; v++)
                {
                    var vertex = list[v];

                    GetBW(authoring.Renderer.sharedMesh.boneWeights[vertex.Index],
                        out var bones,
                        out var weights);

                    AppendToBuffer(entity, new VertexData
                    {
                        Origin = authoring.Renderer.sharedMesh.vertices[vertex.Index],
                        F_Index = vertex.F_Index,
                        R_Index = vertex.R_Index,
                        Bones = bones,
                        Weights = weights
                    });
                }

                AddBuffer<BoneData>(entity);
                for (int b = 0; b < authoring.Renderer.bones.Length; b++)
                {
                    var t = authoring.Renderer.bones[b].GetChild(0);
                    AppendToBuffer(entity, new BoneData
                    {
                        BindPose = new RigidTransform
                        {
                            pos = t.position,
                            rot = t.rotation,
                        },
                        Power = authoring.Powers[b],
                    });
                }
            }

            void GetBW(UnityEngine.BoneWeight bw, out FixedList32Bytes<int> bones, out FixedList32Bytes<float> weights)
            {
                bones = new FixedList32Bytes<int>();
                weights = new FixedList32Bytes<float>();

                if (bw.weight0 > 0f)
                {
                    bones.Add(bw.boneIndex0);
                    weights.Add(bw.weight0);
                }
                if (bw.weight1 > 0f)
                {
                    bones.Add(bw.boneIndex1);
                    weights.Add(bw.weight1);
                }
                if (bw.weight2 > 0f)
                {
                    bones.Add(bw.boneIndex2);
                    weights.Add(bw.weight2);
                }
                if (bw.weight3 > 0f)
                {
                    bones.Add(bw.boneIndex3);
                    weights.Add(bw.weight3);
                }
            }

            bool Contains(List<Vertex> vertices, int index)
            {
                for (int v = 0; v < vertices.Count; v++)
                    if (vertices[v].Index == index)
                        return true;

                return false;
            }
        }

        struct Vertex
        {
            public int Index;
            public int F_Index;
            public int R_Index;
        }
    }

    public struct VertexData : IBufferElementData
    {
        public int F_Index;
        public int R_Index;
        public float3 Origin;
        public FixedList32Bytes<int> Bones;
        public FixedList32Bytes<float> Weights;
    }
    public struct BoneData : IBufferElementData
    {
        public RigidTransform BindPose;
        public float Power;
    }
}