using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelWind
{
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct WindColliderData
    {
        public float4 Position;
        public float4 Velocity;
        public float Radius;
        public float PushStrength;
        public float ShadowStrength;
        public float ShadowDistance;
    }
}
