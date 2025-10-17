using Core.Rendering;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;

namespace Test.SoftFace
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    partial struct SyncVerticesSystem : ISystem
    {
        ComponentLookup<LocalToWorld> LTWLookup;
        ComponentLookup<Vertex> InfoLookup;

        BufferLookup<BoneInfo> BoneLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoneWeight>();
            state.RequireForUpdate<Vertex>();

            LTWLookup = GetComponentLookup<LocalToWorld>();
            InfoLookup = GetComponentLookup<Vertex>();

            BoneLookup = GetBufferLookup<BoneInfo>();
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            LTWLookup.Update(ref state);
            InfoLookup.Update(ref state);

            BoneLookup.Update(ref state);

            state.Dependency = new PlaceVerticesJob
            {
                LTWLookup = LTWLookup,
                BoneLookup = BoneLookup,
            }
            .ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }
        [BurstCompile]
        partial struct PlaceVerticesJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalToWorld> LTWLookup;
            [ReadOnly] public BufferLookup<BoneInfo> BoneLookup;

            public void Execute([EntityIndexInQuery] in int EIIQ, in DynamicBuffer<BoneWeight> bWeights, ref LocalTransform t, ref Vertex vertex)
            {
                var holderLTW = LTWLookup[vertex.Holder];
                var worldPosition = mul(holderLTW.Value, float4(vertex.Origin, 1f)).xyz;
                var bones = BoneLookup[vertex.Holder];

                for (int i = 0; i < bWeights.Length; i++)
                {
                    var boneWeight = bWeights[i];
                    var boneLTW = LTWLookup[boneWeight.Target];

                    var boneLocalPos = mul(inverse(holderLTW.Value), float4(boneLTW.Position, 1f)).xyz;
                    var boneRelRot = mul(inverse(holderLTW.Rotation), boneLTW.Rotation);
                    var vertLocalPos = boneLocalPos + mul(boneRelRot, boneWeight.RelativePosition);

                    worldPosition += boneWeight.Weight * mul(holderLTW.Value, float4(vertLocalPos - vertex.Origin, 1f)).xyz;
                }

                t.Position = worldPosition;
            }
        }
    }
}