using System.CodeDom.Compiler;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace VoxelWind
{
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct Voxel
    {
        public float4 Position;
        public int4 Index;
        public float Size;
        public float4 Velocity;
        public bool IsActive;

        public Voxel(float4 position, int4 index, float size, float4 vector, bool isActive = true)
        {
            Position = position;
            Index = index;
            Size = size;
            Velocity = vector;
            IsActive = isActive;
        }
    }
}
