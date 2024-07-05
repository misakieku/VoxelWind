#ifndef VOXEL_WIND
#define VOXEL_WIND

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

TEXTURE3D (_VoxelWindTexture);
float4x4 _VoxelWindGridMatrix;
float3 _VoxelWindGridOffset;
float3 _VoxelWindGridSize;

float4 SampleVoxelWind(float3 PositionWS)
{
    float3 position = (PositionWS + _VoxelWindGridSize / 2.0f) - _VoxelWindGridOffset;
    float3 gridPosition = mul(_VoxelWindGridMatrix, float4(position, 1.0f)).xyz / _VoxelWindGridSize;
    float3 velocity = SAMPLE_TEXTURE3D_LOD(_VoxelWindTexture, s_linear_clamp_sampler, gridPosition, 0).xyz;
    float alpha = 1.0;
    
    if (gridPosition.x >= 0.0f && gridPosition.x <= 1.0f &&
        gridPosition.y >= 0.0f && gridPosition.y <= 1.0f &&
        gridPosition.z >= 0.0f && gridPosition.z <= 1.0f)
    {
        alpha = 1.0;
    }
    else
    {
        alpha = 0.0;
    }
    
    return float4(velocity, alpha);
}

void SampleVoxelWind_float(float3 PositionWS, out float4 Velocity)
{
    Velocity = SampleVoxelWind(PositionWS);
}

void SampleVoxelWind_half(float3 PositionWS, out float4 Velocity)
{
    Velocity = SampleVoxelWind(PositionWS);
}

#endif