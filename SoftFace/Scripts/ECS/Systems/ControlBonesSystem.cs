using Core.Util;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

using UnityEngine;

using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;

namespace Test.SoftFace
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    partial struct ControlBonesSystem : ISystem
    {
        ComponentLookup<LocalTransform> TransformLookup;
        ComponentLookup<PhysicsVelocity> VelocityLookup;
        ComponentLookup<PhysicsMass> MassLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoneInfo>();

            TransformLookup = GetComponentLookup<LocalTransform>();
            VelocityLookup = GetComponentLookup<PhysicsVelocity>();
            MassLookup = GetComponentLookup<PhysicsMass>();
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TransformLookup.Update(ref state);
            VelocityLookup.Update(ref state);
            MassLookup.Update(ref state);

            var ecb = Sys.ECB(state.WorldUpdateAllocator);

            state.Dependency = new ControlBonesJob
            {
                Delta = SystemAPI.Time.DeltaTime,

                TransformLookup = TransformLookup,
                VelocityLookup = VelocityLookup,
                MassLookup = MassLookup,

                ECB = ecb.AsParallelWriter()
            }
            .ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        partial struct ControlBonesJob : IJobEntity
        {
            [ReadOnly] public float Delta;

            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<PhysicsVelocity> VelocityLookup;
            [ReadOnly] public ComponentLookup<PhysicsMass> MassLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute([EntityIndexInQuery] in int EIIQ, in Entity entity, in DynamicBuffer<BoneInfo> bones, ref Head head)
            {
                var transform = TransformLookup[entity];
                head.objectToWorld = transform.ToMatrix();

                for (int b = 0; b < bones.Length; b++)
                {
                    var bInfo = bones[b];
                    var bT = TransformLookup[bInfo.Value];
                    var velocity = VelocityLookup[bInfo.Value];

                    var target = transform.Position + mul(transform.Rotation, bInfo.BindPose.pos);
                    var vec = target - bT.Position;

                    velocity.Linear *= 0.9f;
                    velocity.Angular *= 0.9f;

                    velocity.Linear += bInfo.Power * vec;
                    velocity.Linear = clamp(velocity.Linear, -5f, 5f);

                    var worldUp = mul(transform.ToMatrix(), float4(mul(bInfo.BindPose.rot, Vector3.up), 1f)).xyz;
                    var worldFwd = mul(transform.ToMatrix(), float4(mul(bInfo.BindPose.rot, Vector3.forward), 1f)).xyz;

                    velocity.Angular += Phys
                        .GetLocalAngular(bInfo.Value,
                        bT.Up(),
                        worldUp,
                        3f * bInfo.Power,
                        TransformLookup,
                        MassLookup);

                    velocity.Angular += Phys
                        .GetLocalAngular(bInfo.Value,
                        bT.Forward(),
                        worldFwd,
                        3f * bInfo.Power,
                        TransformLookup,
                        MassLookup);

                    //Debug.DrawLine(bT.Position, target);

                    //Debug.DrawRay(bT.Position, bT.Right(), Color.red);
                    //Debug.DrawRay(bT.Position, bT.Up(), Color.green);
                    //Debug.DrawRay(bT.Position, bT.Forward(), Color.blue);

                    //Debug.DrawRay(bT.Position, worldUp, Color.magenta);
                    //Debug.DrawRay(bT.Position, worldFwd, Color.magenta);

                    ECB.SetComponent(EIIQ, bInfo.Value, velocity);
                }
            }
        }
    }
}