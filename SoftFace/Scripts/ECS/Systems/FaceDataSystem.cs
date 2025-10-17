using System.Collections.Generic;

using Core.Rendering;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

using UnityEngine;

using static Unity.Mathematics.math;

namespace Test.SoftFace
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    [UpdateAfter(typeof(SyncVerticesSystem))]
    partial class FaceDataSystem : SystemBase
    {
        ComponentLookup<LocalToWorld> LTWLookup;
        ComponentLookup<Head> HeadsLookup;
        ComponentLookup<SoftFace.Vertex> InfoLookup;
        BufferLookup<VertexInfo> VertexLookup;

        FaceManager Manager;
        List<ComputeBuffer> VertexData = new List<ComputeBuffer>();

        protected override void OnCreate()
        {
            base.OnCreate();

            this.RequireForUpdate<HeadTag>();
            this.RequireForUpdate<SkeletonRef>();

            LTWLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
            HeadsLookup = SystemAPI.GetComponentLookup<Head>();
            InfoLookup = SystemAPI.GetComponentLookup<SoftFace.Vertex>();
            VertexLookup = SystemAPI.GetBufferLookup<VertexInfo>();
        }
        protected override void OnUpdate()
        {
            if (!Manager)
            {
                var go = GameObject
                      .FindGameObjectWithTag("FaceManager");

                if (!go.TryGetComponent<FaceManager>(out Manager))
                    return;
            }

            LTWLookup.Update(this);
            HeadsLookup.Update(this);
            InfoLookup.Update(this);
            VertexLookup.Update(this);

            var query = SystemAPI
                .QueryBuilder()
                .WithAll<Head>()
                .Build()
                .ToEntityArray(Allocator.TempJob);

            var count = 0;
            var renderables = Manager.GetFaces();
            for (int r = 0; r < renderables.Length; r++)
            {
                var renderable = renderables[r];
                var list = new NativeList<Entity>(Allocator.TempJob);
                for (int q = 0; q < query.Length; q++)
                    if (SystemAPI.GetComponent<Head>(query[q]).RenderableID == renderable.ID)
                        list.Add(query[q]);

                if (list.Length == 0)
                {
                    list.Dispose();

                    continue;
                }

                var vertexCount = renderable.Mesh.vertexCount;
                var heads = new NativeArray<Head>(list.Length, Allocator.TempJob);
                var vertices = new NativeArray<Vertex>(list.Length * vertexCount, Allocator.TempJob);

                var job = new HeadsJob
                {
                    LTWLookup = LTWLookup,
                    HeadsLookup = HeadsLookup,
                    InfoLookup = InfoLookup,
                    VertexLookup = VertexLookup,

                    Entities = list,

                    Heads = heads,
                    Vertices = vertices
                }
                .Schedule(list.Length, list.Length / JobsUtility.JobWorkerCount);
                job.Complete();

                if (r >= VertexData.Count)
                    VertexData.Add(null);
                if (VertexData[count] != null)
                    VertexData[count].Dispose();

                VertexData[count] = new ComputeBuffer(vertices.Length, 24);
                VertexData[count].SetData(vertices);

                renderable.Material.SetFloat("_VertexLen", vertexCount);
                renderable.Material.SetBuffer("_VertexData", VertexData[count]);

                Graphics.RenderMeshInstanced(new RenderParams(renderable.Material), renderable.Mesh, 0, heads);

                list.Dispose();
                heads.Dispose();
                vertices.Dispose();

                count++;
            }

            query.Dispose();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            for (int v = 0; v < VertexData.Count; v++)
                if (VertexData[v] != null)
                    VertexData[v].Dispose();
        }

        [BurstCompile]
        struct HeadsJob : IJobParallelFor
        {
            [ReadOnly] public ComponentLookup<LocalToWorld> LTWLookup;
            [ReadOnly] public ComponentLookup<Head> HeadsLookup;
            [ReadOnly] public ComponentLookup<SoftFace.Vertex> InfoLookup;
            [ReadOnly] public BufferLookup<VertexInfo> VertexLookup;

            [ReadOnly] public NativeList<Entity> Entities;

            [WriteOnly] public NativeArray<Head> Heads;
            [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Vertex> Vertices;

            public void Execute(int index)
            {
                var entity = Entities[index];
                Heads[index] = HeadsLookup[entity];

                var wtl = inverse(LTWLookup[entity].Value);
                var vertices = VertexLookup[entity];
                var start = index * vertices.Length;

                for (int v = 0; v < vertices.Length; v++)
                {
                    var iE = vertices[v].Value;
                    var info = InfoLookup[iE];
                    var vertex = new Vertex();

                    vertex.Position = mul(wtl, float4(LTWLookup[iE].Position, 1f)).xyz;
                    var f = normalizesafe(mul(wtl, float4(LTWLookup[vertices[info.F_Index].Value].Position, 1f)).xyz - vertex.Position);
                    var r = normalizesafe(mul(wtl, float4(LTWLookup[vertices[info.R_Index].Value].Position, 1f)).xyz - vertex.Position);
                    vertex.Normal = normalizesafe(cross(f, r));

                    Vertices[start + v] = vertex;
                }
            }
        }

        struct Vertex
        {
            public float3 Position;
            public float3 Normal;
        }
    }
}