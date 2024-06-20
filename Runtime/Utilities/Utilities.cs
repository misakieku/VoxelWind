using GitHub.Unity;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelWind
{
    public static class Utilities
    {
        public static bool IsInsideCylinder(float3 voxelPosition, float3 basePosition, float3 direction, float lowerRadius, float upperRadius, float height, out float linear01)
        {
            var normalizedDirection = math.normalize(direction);
            var baseToVoxel = voxelPosition - basePosition;

            var projectionLength = math.dot(baseToVoxel, normalizedDirection);
            var projectedPoint = basePosition + normalizedDirection * projectionLength;

            if (projectionLength < 0 || projectionLength > height)
            {
                linear01 = 0;
                return false;
            }

            var a = projectionLength / height;

            var effectiveRadius = math.lerp(lowerRadius, upperRadius, a);
            var distanceToAxis = math.length(projectedPoint - voxelPosition);

            if (distanceToAxis < 0 || distanceToAxis > effectiveRadius) 
            {
                linear01 = 0;
                return false;
            }

            linear01 = a;
            return true;
        }

        public static bool IsInsideSphere(float3 voxelPosition, float3 center, float radius, out float linear01)
        {
            var distance = math.length(voxelPosition - center);

            if (distance > radius)
            {
                linear01 = 0;
                return false;
            }

            linear01 = distance / radius;
            return true;
        }

        public static int3 GetPositionIndex(int index, int3 voxelDensity)
        {
            var positionIndex = new int3();

            positionIndex.x = index % voxelDensity.x;
            positionIndex.y = (index / voxelDensity.x) % voxelDensity.y;
            positionIndex.z = index / (voxelDensity.x * voxelDensity.y);

            return positionIndex;
        }

        public static int GetIndex(int3 positionIndex, int3 voxelDensity)
        {
            return positionIndex.x + positionIndex.y * voxelDensity.x + positionIndex.z * voxelDensity.x * voxelDensity.y;
        }
    }
}
