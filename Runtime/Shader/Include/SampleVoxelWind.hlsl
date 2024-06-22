#ifndef VOXEL_WIND
#define VOXEL_WIND

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

TEXTURE3D (_VoxelWindTexture);
float4x4 _VoxelWindGridMatrix;
float3 _VoxelWindGridOffset;
float3 _VoxelWindGridSize;

float3 SampleVoxelWind(float3 PositionWS)
{
    float3 position = (PositionWS + _VoxelWindGridSize / 2.0f) - _VoxelWindGridOffset;
    float3 gridPosition = mul(float4(position, 1.0f), _VoxelWindGridMatrix).xyz / _VoxelWindGridSize;
    
    if (gridPosition.x >= 0.0f && gridPosition.x <= 1.0f &&
        gridPosition.y >= 0.0f && gridPosition.y <= 1.0f &&
        gridPosition.z >= 0.0f && gridPosition.z <= 1.0f)
    {
        float3 velocity = SAMPLE_TEXTURE3D(_VoxelWindTexture, s_linear_clamp_sampler, gridPosition).xyz;
        return velocity;
    }
    else
    {
        // PositionWS is outside the voxel grid, return zero velocity or handle as needed
        return float3(0.0f, 0.0f, 0.0f);
    }
}

void SampleVoxelWind_float(float3 PositionWS, out float3 Velocity)
{
    Velocity = SampleVoxelWind(PositionWS);
}

void SampleVoxelWind_half(float3 PositionWS, out float3 Velocity)
{
    Velocity = SampleVoxelWind(PositionWS);
}

#endif