using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.ComponentModel;

namespace VoxelWind
{
    public enum GlobalWindType
    {
        Directional,
        Turbulent,
    }

    [GenerateHLSL(PackingRules.Exact, false)]
    public struct GlobalWindData
    {
        public bool IsActive;
        public GlobalWindType WindType;
        public float4 Direction;
        public float Strength;
        public float Speed;
        public float Scale;
    }

    public class GlobalWind : MonoBehaviour
    {
        public GlobalWindType WindType = GlobalWindType.Directional;
        public float Strength = 1.0f;
        public float Speed = 1.0f;
        public float NoiseScale = 1.0f;

        private bool _isActive = true;

        private GlobalWindData _windData = new();
        public GlobalWindData WindData => _windData;

        void Update()
        {
            UpdateWind();
        }

        private void UpdateWind()
        {
            _windData.Direction.xyz = transform.forward;
            _windData.WindType = WindType;
            _windData.Strength = Strength;
            _windData.Speed = Speed;
            _windData.Scale = NoiseScale;

            if (Strength == 0)
            {
                _isActive = false;
            }
            else
            {
                _isActive = true;
            }

            _windData.IsActive = _isActive;
        }

        private void OnEnable()
        {
            _isActive = true;
            _windData.IsActive = _isActive;
        }

        private void OnDisable()
        {
            _isActive = false;
            _windData.IsActive = _isActive;
        }
    }
}