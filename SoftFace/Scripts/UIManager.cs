#if UNITY_EDITOR
using Core.GamePlay;
using Core.Rendering;
using Core.UI;
using Core.Util;

using UnityEngine;

namespace Test.SoftFace
{
    public class UIManager : UIManagerBase
    {
        [SerializeField] float ReplacementPower = 0.01f;

        protected override void Start()
        {
            base.Start();

            Sys.Add(new SetCameraRequest
            {
                TargetPosition = 10f * Vector3.forward,
            },
            EntityManager);
        }

        public void Save() => Sys.Add(new GameEvent { Set = GameEvent.Type.Saving }, EntityManager);
        public void Load() => Sys.Add(new GameEvent { Set = GameEvent.Type.Loading }, EntityManager);

        public float GetReplacementPower() => ReplacementPower;
    }
}
#endif