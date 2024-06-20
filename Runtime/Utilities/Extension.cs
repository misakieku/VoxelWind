using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelWind
{
    public static class Extension
    {
        public static Quaternion Rotation(this float4x4 matrix)
        {
            float3 forward;
            forward.x = matrix.c2.x;
            forward.y = matrix.c2.y;
            forward.z = matrix.c2.z;

            float3 upwards;
            upwards.x = matrix.c1.x;
            upwards.y = matrix.c1.y;
            upwards.z = matrix.c1.z;

            return Quaternion.LookRotation(forward, upwards);
        }

        public static float3 Position(this float4x4 matrix)
        {
            return matrix.c3.xyz;
        }

        public static float3 Scale(this float4x4 matrix)
        {
            var scale = float3.zero;
            scale.x = math.length(matrix.c0.xyz);
            scale.y = math.length(matrix.c1.xyz);
            scale.z = math.length(matrix.c2.xyz);
            return scale;
        }
    }
}
