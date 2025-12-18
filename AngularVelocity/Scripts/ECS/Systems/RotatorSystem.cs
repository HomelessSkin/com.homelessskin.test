#if UNITY_EDITOR
using Core;

using Physics;

using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

using UnityEngine;

using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;

namespace Test.Angular
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    partial struct RotatorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RotatorTag>();
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = Sys.ECB(state.WorldUpdateAllocator);

            var rotatorE = GetSingletonEntity<RotatorTag>();
            var rotator = GetComponent<RotatorInfo>(rotatorE);
            var transform = GetComponent<LocalTransform>(rotatorE);
            var vel = GetComponent<PhysicsVelocity>(rotatorE);
            var mass = GetComponent<PhysicsMass>(rotatorE);

            foreach (var (request, entity) in Query<SetTargetRequest>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);

                rotator.Velocity = request.Velocity;
                rotator.Damping = request.Damping;
                rotator.TargetUp = normalizesafe(request.Target);
            }

            vel.Linear *= 0.9f;
            vel.Linear += -rotator.Velocity * transform.Position;

            vel.Angular *= 1f - rotator.Damping;
            vel.Angular += Phys.GetLocalAngular(rotatorE, transform.Up(), rotator.TargetUp, rotator.Velocity, state.EntityManager);

            ecb.SetComponent(rotatorE, rotator);
            ecb.SetComponent(rotatorE, vel);

            ecb.Playback(state.EntityManager);
        }
    }

    [System.Serializable]
    public struct SetTargetRequest : IComponentData
    {
        public float Velocity;
        [Range(0f, 1f)] public float Damping;
        public Vector3 Target;
    }
}
#endif