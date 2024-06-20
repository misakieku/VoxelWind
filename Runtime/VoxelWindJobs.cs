using System;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelWind
{
    [BurstCompile]
    public struct BuildVoxelsJob : IJobParallelFor
    {
        //public NativeArray<Voxel> voxels;
        public VoxelGrid voxelGrid;

        public void Execute(int index)
        {
            var voxel = voxelGrid.Voxels[index];
            var voxelDensity = voxelGrid.VoxelDensity;
            var voxelSize = voxelGrid.VoxelSize;
            var gridOffset = voxelGrid.Offset;
            var gridMatrix = voxelGrid.WorldMatrix;

            var x = index % voxelDensity.x;
            var y = (index / voxelDensity.x) % voxelDensity.y;
            var z = index / (voxelDensity.x * voxelDensity.y);

            var gridSize = new float3(voxelDensity.x * voxelSize, voxelDensity.y * voxelSize, voxelDensity.z * voxelSize);
            var localPosition = new float3(x, y, z) * voxelSize - gridSize / 2 + gridOffset;
            var worldPosition = math.transform(gridMatrix, localPosition);
            //var matrix = float4x4.TRS(worldPosition, gridMatrix.Rotation(), gridMatrix.Scale());

            voxelGrid.Voxels[index] = new Voxel(new float4(worldPosition, 1.0f), new int4(x, y, z, 0), voxelSize, voxel.Velocity);
        }
    }

    [BurstCompile]
    public struct UpdateVoxelsJob : IJobParallelFor
    {
        //[ReadOnly]
        //public NativeArray<Voxel> voxels;
        public NativeArray<Voxel> updatedVoxels;

        [ReadOnly]
        public VoxelGrid voxelGrid;

        public float time;
        public float deltaTime;

        [ReadOnly]
        public NativeArray<GlobalWindData> globalWinds;
        [ReadOnly]
        public NativeArray<LocalWindData> localWinds;
        [ReadOnly]
        public NativeArray<WindColliderData> windColliders;

        public void Execute(int index)
        {
            var voxel = voxelGrid.Voxels[index];
            var velocity = voxel.Velocity;

            var sourcePosition = voxel.Position - velocity * deltaTime;
            var interpolatedVelocity = InterpolateVelocity(sourcePosition.xyz);
            velocity.xyz = interpolatedVelocity;
            velocity *= 0.3f;

            velocity.xyz = ApplyGlobalWind(voxel, velocity.xyz, out var needDiffusion);
            velocity.xyz = ApplyLocalWindZone(voxel, velocity.xyz);
            velocity.xyz = ApplyColliderPush(voxel, velocity.xyz);

            if (needDiffusion)
            {
                for (var i = 0; i < 5; i++)
                {
                    var diffusedVector = Diffusion(index);
                    velocity += 0.3f * (diffusedVector - velocity);
                }
            }

            updatedVoxels[index] = new Voxel(voxel.Position, voxel.Index, voxel.Size, velocity, voxel.IsActive);
        }

        private float3 InterpolateVelocity(float3 sourcePosition)
        {
            var index = GetVoxelIndex(sourcePosition.xyz);
            var positionIndex = Utilities.GetPositionIndex(index, voxelGrid.VoxelDensity);

            var x0 = positionIndex.x;
            var y0 = positionIndex.y;
            var z0 = positionIndex.z;

            var x1 = x0 + 1;
            var y1 = y0 + 1;
            var z1 = z0 + 1;

            // Ensure coordinates are within grid bounds
            x0 = math.clamp(x0, 0, voxelGrid.VoxelDensity.x - 1);
            y0 = math.clamp(y0, 0, voxelGrid.VoxelDensity.y - 1);
            z0 = math.clamp(z0, 0, voxelGrid.VoxelDensity.z - 1);
            x1 = math.clamp(x1, 0, voxelGrid.VoxelDensity.x - 1);
            y1 = math.clamp(y1, 0, voxelGrid.VoxelDensity.y - 1);
            z1 = math.clamp(z1, 0, voxelGrid.VoxelDensity.z - 1);

            // Calculate interpolation weights, ensuring no division by zero
            var xd = (x1 - x0) > 0 ? (positionIndex.x - x0) / (float)(x1 - x0) : 0;
            var yd = (y1 - y0) > 0 ? (positionIndex.y - y0) / (float)(y1 - y0) : 0;
            var zd = (z1 - z0) > 0 ? (positionIndex.z - z0) / (float)(z1 - z0) : 0;

            // Interpolate along x for each y,z pair
            var c00 = math.lerp(GetVoxelVector(x0, y0, z0), GetVoxelVector(x1, y0, z0), xd);
            var c01 = math.lerp(GetVoxelVector(x0, y0, z1), GetVoxelVector(x1, y0, z1), xd);
            var c10 = math.lerp(GetVoxelVector(x0, y1, z0), GetVoxelVector(x1, y1, z0), xd);
            var c11 = math.lerp(GetVoxelVector(x0, y1, z1), GetVoxelVector(x1, y1, z1), xd);

            // Interpolate along y
            var c0 = math.lerp(c00, c10, yd);
            var c1 = math.lerp(c01, c11, yd);

            // Interpolate along z
            var interpolatedVector = math.lerp(c0, c1, zd);

            return interpolatedVector;
        }

        private float3 GetVoxelVector(int x, int y, int z)
        {
            var index = x + y * voxelGrid.VoxelDensity.x + z * (voxelGrid.VoxelDensity.x * voxelGrid.VoxelDensity.y);
            return voxelGrid.Voxels[index].Velocity.xyz;
        }

        private int GetVoxelIndex(float3 position)
        {
            var gridPosition = (position - voxelGrid.WorldMatrix.Position()) / voxelGrid.VoxelSize + voxelGrid.VoxelDensity / 2;
            var x = (int)math.floor(gridPosition.x);
            var y = (int)math.floor(gridPosition.y);
            var z = (int)math.floor(gridPosition.z);

            return math.clamp(x + y * voxelGrid.VoxelDensity.x + z * (voxelGrid.VoxelDensity.x * voxelGrid.VoxelDensity.y), 0, voxelGrid.Voxels.Length);
        }

        private float3 ApplyGlobalWind(Voxel voxel, float3 velocity, out bool active)
        {
            var isActive = true;
            var arrayLength = globalWinds.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                var globalWind = globalWinds[i];

                if (!globalWind.IsActive)
                {
                    continue;
                }

                switch (globalWind.WindType)
                {
                    case GlobalWindType.Directional:
                        var directionalWind = globalWind.Speed * globalWind.Direction;
                        //var directionalNoise = noise.cnoise(voxel.Position.xyz * globalWind.Scale + 2 * time * globalWind.Speed * - globalWind.Direction);
                        //directionalNoise = (directionalNoise + 1.0f) / 2.0f;
                        //directionalWind *= directionalNoise * globalWind.Strength;

                        velocity.xyz += directionalWind.xyz;
                        break;

                    case GlobalWindType.Turbulent:
                        var offset = time * globalWind.Speed * -globalWind.Direction.xyz;
                        var position = voxel.Position.xyz;

                        var _ = noise.snoise(new float3(position * globalWind.Scale + offset), out var noiseG);
                        var turbulence = noiseG * globalWind.Strength;

                        velocity.xyz += turbulence;
                        break;
                }

                velocity = ApplyCollider(voxel, velocity, globalWind.Direction.xyz, globalWind.Speed, out isActive);
            }

            active = isActive;
            return velocity;
        }

        private float3 ApplyLocalWindZone(Voxel voxel, float3 vector)
        {
            var arrayLength = localWinds.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                var localWind = localWinds[i];

                if (!localWind.IsActive)
                {
                    continue;
                }

                var voxelPosition = voxel.Position.xyz;

                var speed = 0.0f;
                var direction = float3.zero;
                var a = 0.0f;
                var isInside = false;

                switch (localWind.WindType)
                {
                    case LocalWindType.Directional:
                        isInside = Utilities.IsInsideCylinder(voxelPosition, localWind.Position.xyz, localWind.Direction.xyz, localWind.Radius, localWind.Radius, localWind.Speed, out var linear01Cylinder);
                        a = 1 - linear01Cylinder;
                        direction = localWind.Direction.xyz;
                        speed = localWind.Speed;
                        break;

                    case LocalWindType.Omni:
                        isInside = Utilities.IsInsideSphere(voxelPosition, localWind.Position.xyz, localWind.Radius, out var linear01Sphere);
                        a = 1 - linear01Sphere;
                        direction = math.normalize(voxelPosition - localWind.Position.xyz);
                        speed = localWind.Speed;
                        break;

                    case LocalWindType.Vortex:
                        isInside = Utilities.IsInsideSphere(voxelPosition, localWind.Position.xyz, localWind.Radius, out var linear01Vortex);
                        a = 1 - linear01Vortex;
                        direction = math.cross(math.normalize(voxelPosition - localWind.Position.xyz), localWind.Direction.xyz);
                        speed = localWind.Speed;
                        break;
                }

                if (isInside)
                {
                    vector = a * speed * direction + vector * (localWind.IsOverWrite ? 0 : 1);
                }
            }

            return vector;
        }

        private float3 ApplyCollider(Voxel voxel, float3 vector, float3 windDirection, float windSpeed, out bool active)
        {
            var arrayLength = windColliders.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                var collider = windColliders[i];
                var distance = math.distance(voxel.Position.xyz, collider.Position.xyz);

                if (distance <= collider.Radius)
                {
                    active = false;
                    return 1 - collider.ShadowStrength;
                }

                // Collider shadow
                var projection = math.dot(collider.Position.xyz - voxel.Position.xyz, windDirection);
                var shadow = voxel.Position.xyz + projection * windDirection;
                var projectionDistance = math.distance(shadow, collider.Position.xyz);
                if (projectionDistance <= collider.Radius && projection < 0)
                {
                    var distanceToColliderSurface = math.distance(voxel.Position.xyz, collider.Position.xyz) - collider.Radius;
                    var a = math.saturate(distanceToColliderSurface / (collider.ShadowDistance * collider.Radius * 2 * windSpeed));
                    a = math.sqrt(a);

                    active = true;
                    return math.lerp(vector, a * vector, collider.ShadowStrength);
                }
            }

            active = true;
            return vector;
        }

        private float3 ApplyColliderPush(Voxel voxel, float3 velocity)
        {
            var arrayLength = windColliders.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                var collider = windColliders[i];

                if (collider.PushStrength == 0.0f || math.all(collider.Velocity.xyz))
                {
                    continue;
                }

                var distance = math.distance(voxel.Position.xyz, collider.Position.xyz);

                if (distance <= collider.Radius * (1.0f + voxelGrid.VoxelSize))
                {
                    var colliderDirection = math.normalize(collider.Velocity);
                    var colliderToVoxel = voxel.Position - collider.Position;
                    var dot = math.dot(colliderDirection, math.normalize(colliderToVoxel));

                    velocity += collider.PushStrength * math.saturate(dot) * collider.Velocity.xyz;
                }
            }

            return velocity;
        }

        private float4 Diffusion(int index)
        {
            var positionIndex = Utilities.GetPositionIndex(index, voxelGrid.VoxelDensity);

            var averageVector = float4.zero;
            var count = 0;

            // Iterate over neighboring voxels including the voxel itself
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        var nx = positionIndex.x + dx;
                        var ny = positionIndex.y + dy;
                        var nz = positionIndex.z + dz;

                        // Check if neighbor is within grid bounds
                        if (nx >= 0 && nx < voxelGrid.VoxelDensity.x &&
                            ny >= 0 && ny < voxelGrid.VoxelDensity.y &&
                            nz >= 0 && nz < voxelGrid.VoxelDensity.z)
                        {
                            var neighborIndex = nx + ny * voxelGrid.VoxelDensity.x + nz * (voxelGrid.VoxelDensity.x * voxelGrid.VoxelDensity.y);
                            averageVector += voxelGrid.Voxels[neighborIndex].Velocity;
                            count++;
                        }
                    }
                }
            }

            averageVector /= count; // Compute the average vector

            return averageVector;
        }
    }
}
