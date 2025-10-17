using Core.Util;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;

namespace Test.SoftFace
{
    partial struct SpawnSkeletonSystem : ISystem
    {
        BufferLookup<VertexData> VertexLookup;
        BufferLookup<BoneData> BoneLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkeletonData>();
            state.RequireForUpdate<SpawnSkeletonsRequest>();

            VertexLookup = GetBufferLookup<VertexData>();
            BoneLookup = GetBufferLookup<BoneData>();
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VertexLookup.Update(ref state);
            BoneLookup.Update(ref state);

            var ecb = Sys.ECB(state.WorldUpdateAllocator);
            var refE = GetSingletonEntity<SkeletonData>();

            state.Dependency = new SpawnSkeletonsJob
            {
                SkeletonData = GetComponent<SkeletonData>(refE),
                Skeletones = GetBuffer<SkeletonRef>(refE),

                VertexLookup = VertexLookup,
                BoneLookup = BoneLookup,

                ECB = ecb.AsParallelWriter()
            }
            .ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        partial struct SpawnSkeletonsJob : IJobEntity
        {
            [ReadOnly] public SkeletonData SkeletonData;
            [ReadOnly] public DynamicBuffer<SkeletonRef> Skeletones;

            [ReadOnly] public BufferLookup<VertexData> VertexLookup;
            [ReadOnly] public BufferLookup<BoneData> BoneLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute([EntityIndexInQuery] in int EIIQ, in Entity entity, in SpawnSkeletonsRequest request)
            {
                ECB.DestroyEntity(EIIQ, entity);

                var skeleton = Sys.GetBufferElement(request.RenderableID, Skeletones);
                var headE = Sys.InstantiateAt(SkeletonData.Head, request.SpawnPosition, ref ECB, EIIQ);
                var vData = VertexLookup[skeleton.Value];
                ECB.SetComponent(EIIQ, headE, new Head
                {
                    RenderableID = request.RenderableID,
                });

                var bData = BoneLookup[skeleton.Value];
                var bEs = new NativeArray<Entity>(bData.Length, Allocator.Temp);

                for (int b = 0; b < bData.Length; b++)
                {
                    var t = bData[b].BindPose;
                    bEs[b] = Sys.InstantiateAt(SkeletonData.Bone,
                        LocalTransform.FromPositionRotation(request.SpawnPosition + t.pos, t.rot),
                        ref ECB, EIIQ);

                    ECB.SetComponent(EIIQ, bEs[b], new Bone { Holder = headE, Index = b });

                    ECB.AppendToBuffer(EIIQ, headE, new BoneInfo
                    {
                        Value = bEs[b],
                        BindPose = t,
                        Power = bData[b].Power
                    });
                }

                for (int v = 0; v < vData.Length; v++)
                {
                    var vD = vData[v];
                    var vE = Sys.InstantiateAt(SkeletonData.Vertex, request.SpawnPosition + vD.Origin, ref ECB, EIIQ);

                    for (int b = 0; b < vD.Bones.Length; b++)
                    {
                        var bI = vD.Bones[b];
                        ECB.AppendToBuffer(EIIQ, vE, new BoneWeight
                        {
                            Target = bEs[bI],
                            TargetIndex = bI,
                            Weight = vD.Weights[b],
                            RelativePosition = mul(inverse(bData[bI].BindPose.rot), vD.Origin - bData[bI].BindPose.pos),
                        });
                    }

                    ECB.SetComponent(EIIQ, vE, new Vertex
                    {
                        Holder = headE,
                        Index = v,
                        F_Index = vD.F_Index,
                        R_Index = vD.R_Index,
                        Origin = vD.Origin,
                    });

                    ECB.AppendToBuffer(EIIQ, headE, new VertexInfo { Value = vE });
                }
            }
        }
    }

    [System.Serializable]
    public struct SpawnSkeletonsRequest : IComponentData
    {
        public int RenderableID;

        public float3 SpawnPosition;
    }
}