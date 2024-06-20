int3 GetPositionIndex(uint index, uint3 voxelDensity)
{
    int3 positionIndex = int3(0, 0, 0);

    positionIndex.x = index % voxelDensity.x;
    positionIndex.y = (index / voxelDensity.x) % voxelDensity.y;
    positionIndex.z = index / (voxelDensity.x * voxelDensity.y);

    return positionIndex;
}


float3 GetMatrixPosition(float4x4 input)
{
    return input[3].xyz;
}

bool IsInsideCylinder(float3 voxelPosition, float3 basePosition, float3 direction, float lowerRadius, float upperRadius, float height, out float linear01)
{
    float3 normalizedDirection = normalize(direction);
    float3 baseToVoxel = voxelPosition - basePosition;

    float projectionLength = dot(baseToVoxel, normalizedDirection);
    float3 projectedPoint = basePosition + normalizedDirection * projectionLength;

    if (projectionLength < 0 || projectionLength > height)
    {
        linear01 = 0;
        return false;
    }

    float a = projectionLength / height;

    float effectiveRadius = lerp(lowerRadius, upperRadius, a);
    float distanceToAxis = length(projectedPoint - voxelPosition);

    if (distanceToAxis < 0 || distanceToAxis > effectiveRadius)
    {
        linear01 = 0;
        return false;
    }

    linear01 = a;
    return true;
}

bool IsInsideSphere(float3 voxelPosition, float3 center, float radius, out float linear01)
{
    float distance = length(voxelPosition - center);

    if (distance > radius)
    {
        linear01 = 0;
        return false;
    }

    linear01 = distance / radius;
    return true;
}