#if UNITY_EDITOR
using Core;

using Unity.Entities;
using Unity.Transforms;

using UnityEngine;

namespace Test.Angular
{
    public partial class RotatorGameSystem : GameSystemBase
    {
        UIManager UIManager;

        protected override void OnCreate()
        {
            base.OnCreate();

            RequireForUpdate<RotatorTag>();

            SetState(IStateMachine.State.Idle);
        }
        protected override void GetRef()
        {
            if (!UIManager)
                UIManager = GameObject
                    .FindGameObjectWithTag("UIManager")
                    .GetComponent<UIManager>();
        }
        protected override void Proceed()
        {
            var rotatorE = SystemAPI.GetSingletonEntity<RotatorTag>();
            var transform = SystemAPI.GetComponent<LocalTransform>(rotatorE);
            var info = SystemAPI.GetComponent<RotatorInfo>(rotatorE);

            UIManager.SetTarget(transform.Position, transform.Up(), info.TargetUp);
        }
    }
}
#endif