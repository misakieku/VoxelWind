using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace VoxelWind
{
    [Serializable]
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct VoxelGrid
    {
        public NativeArray<Voxel> Voxels;

        public float4x4 WorldMatrix;

        public float VoxelSize;
        public float3 Size;
        public float3 Offset;

        public int3 VoxelDensity;
        public int VoxelCount;
    }
}
