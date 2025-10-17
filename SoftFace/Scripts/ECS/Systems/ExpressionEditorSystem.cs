#if UNITY_EDITOR
using Core.GamePlay;
using Core.Rendering;
using Core.Util;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

using UnityEditor;

using UnityEngine;

using static Unity.Mathematics.math;

using RaycastHit = Unity.Physics.RaycastHit;

namespace Test.SoftFace
{
    [DisableAutoCreation]
    public partial class ExpressionEditorSystem : GameSystemBase
    {
        float CameraOffset = 10f;

        int SelectedIndex;
        CollisionFilter SelectionRayFilter;

        float3 ReplacementAxis;
        CollisionFilter CursorRayFilter;

        ComponentLookup<LocalTransform> TransformLookup;

        UIManager UIManager => (UIManager)UIManagerBase;
        CameraEngine CameraEngine => (CameraEngine)CameraEngineBase;

        protected override void OnCreate()
        {
            base.OnCreate();

            RequireForUpdate<SkeletonRef>();

            SelectionRayFilter = new CollisionFilter
            {
                BelongsTo = 8,
                CollidesWith = 16,
            };
            CursorRayFilter = new CollisionFilter
            {
                BelongsTo = 8,
                CollidesWith = 32,
            };

            TransformLookup = GetComponentLookup<LocalTransform>();

            SetState(GameEvent.Type.Idle);
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
        }
        protected override void UpdateState()
        {
            base.UpdateState();
        }
        protected override void SetState(GameEvent.Type state)
        {
            Debug.Log(state);

            switch (state)
            {
                case GameEvent.Type.Selection:
                NextState = GameEvent.Type.Transition;
                break;
            }

            base.SetState(state);
        }
        protected override void Proceed()
        {
            base.Proceed();

            SystemAPI.TryGetSingletonEntity<HeadTag>(out var hE);

            TransformLookup.Update(this);

            PositionCursor();

            switch (State)
            {
                case GameEvent.Type.Saving:
                HandleSaving(hE);
                SetState(PrevState);
                break;
                case GameEvent.Type.Loading:
                HandleLoading(hE);
                break;
            }
        }
        protected override void HandleControl()
        {
            base.HandleControl();

            var delta = Input.mousePositionDelta;

            var target = CameraEngine.GetTargetPosition();
            target.Normalize();

            var camF = -target;
            var camR = Vector3.Cross(Vector3.up, target).normalized;
            camF.y = camR.y = 0f;

            if (Input.GetKeyDown(KeyCode.Q))
                HandleActionSwitch();

            if (Input.GetKeyDown(KeyCode.Mouse0))
                HandleLeftDown();
            else if (Input.GetKeyUp(KeyCode.Mouse0))
                HandleLeftUp();
            else if (Input.GetKey(KeyCode.Mouse0))
                HandleLMB();

            if (Input.GetKeyDown(KeyCode.Mouse1))
                HandleRightDown();
            else if (Input.GetKeyUp(KeyCode.Mouse1))
                HandleRightUp();
            else if (Input.GetKey(KeyCode.Mouse1))
                HandleRMB();

            CameraOffset = clamp(CameraOffset - Input.mouseScrollDelta.y, 0.1f, 20f);
            target = CameraOffset * target;
            CameraEngine.SetTarget(target, 0f);

            void HandleLeftDown()
            {
                switch (State)
                {
                    case GameEvent.Type.Idle:
                    {
                        var ray = CameraEngine.ScreenPointToRay(Input.mousePosition);
                        var physW = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
                        if (physW.CastRay(new RaycastInput
                        {
                            Start = ray.origin,
                            End = ray.origin + 100f * ray.direction,
                            Filter = SelectionRayFilter
                        },
                        out var hit))
                        {
                            SelectBone(hit);
                            SetState(GameEvent.Type.Selection);
                        }
                    }
                    break;
                    case GameEvent.Type.Selection:
                    {
                        var ray = CameraEngine.ScreenPointToRay(Input.mousePosition);
                        var physW = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
                        if (physW.CastRay(new RaycastInput
                        {
                            Start = ray.origin,
                            End = ray.origin + 100f * ray.direction,
                            Filter = CursorRayFilter
                        },
                        out var hit))
                        {
                            var transform = TransformLookup[hit.Entity];

                            ReplacementAxis = normalizesafe(hit.Position - transform.Position);

                            //var du = dot(vec, Vector3.up);
                            //var df = dot(vec, Vector3.forward);
                            //var dr = dot(vec, Vector3.right);

                            //ReplacementAxis = du > df ?
                            //    (du > dr ? Vector3.up : Vector3.right) :
                            //    (df > dr ? Vector3.forward : Vector3.right);

                            SetState(SystemAPI.GetComponent<Cursor>(hit.Entity).Type);
                        }
                        else
                        {
                            DeselectBone();
                            SetState(GameEvent.Type.Idle);

                            goto case GameEvent.Type.Idle;
                        }
                    }
                    break;
                }
            }
            void HandleLeftUp()
            {
                switch (State)
                {
                    case GameEvent.Type.Transition:
                    SetState(GameEvent.Type.Selection);
                    break;
                }
            }
            void HandleLMB()
            {
                switch (State)
                {
                    case GameEvent.Type.Transition:
                    {
                        var vec = sign(dot(normalize(sign(delta.x) * CameraEngine.transform.right +
                                                                                                              sign(delta.y) * CameraEngine.transform.up), ReplacementAxis)) *
                                                                                                              ReplacementAxis;

                        var buffer = SystemAPI.GetSingletonBuffer<BoneInfo>();
                        var bInfo = buffer[SelectedIndex];
                        bInfo.BindPose.pos += UIManager.GetReplacementPower() * length(delta) * vec;
                        buffer[SelectedIndex] = bInfo;
                    }
                    break;
                    case GameEvent.Type.Rotation:
                    {

                    }
                    break;
                }
            }

            void HandleRightDown()
            {

            }
            void HandleRightUp()
            {

            }
            void HandleRMB()
            {
                target = Quaternion.AngleAxis(delta.x, CameraEngine.transform.up) *
                    Quaternion.AngleAxis(-delta.y, CameraEngine.transform.right) *
                    target;

                switch (State)
                {
                    case GameEvent.Type.Idle:
                    break;
                    case GameEvent.Type.Selection:
                    break;
                }
            }

            void HandleActionSwitch()
            {
                switch (State)
                {
                    case GameEvent.Type.Selection:
                    NextState = NextState == GameEvent.Type.Transition ? GameEvent.Type.Rotation : GameEvent.Type.Transition;
                    break;
                }
            }
        }
        protected override void HandleSaving(Entity playerE)
        {
            base.HandleSaving(playerE);

            if (!KeyScriptable.Create<Expression>(out var expression))
                return;

            var buffer = SystemAPI.GetBuffer<BoneInfo>(playerE);

            expression.Bones = new RigidTransform[buffer.Length];
            for (int b = 0; b < buffer.Length; b++)
                expression.Bones[b] = buffer[b].BindPose;

            EditorUtility.SetDirty(expression);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        protected override void HandleLoading(Entity playerE)
        {
            base.HandleLoading(playerE);

            if (playerE == Entity.Null)
            {
                if (KeyScriptable.Load<RenderableObject>(out var faceData))
                {
                    Sys.Add(new SpawnSkeletonsRequest
                    {
                        RenderableID = faceData.ID,
                        SpawnPosition = 0f
                    },
                    EntityManager);

                    Debug.Log("Spawn requested");
                }

                SetState(GameEvent.Type.Idle);

                return;
            }

            if (!KeyScriptable.Load<Expression>(out var expression))
                return;

            var buffer = SystemAPI.GetBuffer<BoneInfo>(playerE);

            if (expression.Bones.Length < buffer.Length)
            {
                Debug.LogWarning("Saved expression's bones length shorter than current's face bones count!");

                for (int b = 0; b < expression.Bones.Length; b++)
                {
                    var bInfo = buffer[b];
                    bInfo.BindPose = expression.Bones[b];
                    buffer[b] = bInfo;
                }

                SetState(GameEvent.Type.Saving);
            }
            else if (expression.Bones.Length > buffer.Length)
            {
                Debug.LogError("Saved expression's bones length greater than current's face bones count!");

                SetState(PrevState);
            }
            else
            {
                for (int b = 0; b < expression.Bones.Length; b++)
                {
                    var bInfo = buffer[b];
                    bInfo.BindPose = expression.Bones[b];
                    buffer[b] = bInfo;

                }

                SetState(PrevState);
            }
        }

        void PositionCursor()
        {
            var cE = Entity.Null;
            foreach (var (cursor, entity) in SystemAPI.Query<Cursor>().WithEntityAccess())
            {
                switch (State)
                {
                    case GameEvent.Type.Selection:
                    case GameEvent.Type.Transition:
                    case GameEvent.Type.Rotation:
                    if (cursor.Type == NextState)
                        cE = entity;
                    else
                    {
                        var t = TransformLookup[entity];
                        t.Position = float3(1000f);
                        TransformLookup[entity] = t;
                    }
                    break;
                }
            }

            if (cE == Entity.Null)
                return;

            var cT = TransformLookup[cE];
            if (SystemAPI.TryGetSingletonBuffer<BoneInfo>(out var buffer))
                cT = TransformLookup[buffer[SelectedIndex].Value];
            else
                cT.Position = 1000f;

            TransformLookup[cE] = cT;
        }
        void SelectBone(RaycastHit hit)
        {
            SelectedIndex = SystemAPI.GetComponent<Bone>(hit.Entity).Index;
        }
        void DeselectBone()
        {
            SelectedIndex = -1;
        }
    }
}
#endif