using Core.Util;

using Unity.Mathematics;

using UnityEngine;

namespace Test.SoftFace
{
    [CreateAssetMenu(fileName = "_Expression", menuName = "SoftFace/Expression")]
    public class Expression : KeyScriptable
    {
        public RigidTransform[] Bones;
    }
}