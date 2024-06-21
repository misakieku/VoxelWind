using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelWind
{
    public enum LocalWindType
    {
        Directional,
        Omni,
        Vortex,
    }

    [GenerateHLSL(PackingRules.Exact, false)]
    public struct LocalWindData
    {
        public bool IsActive;
        public LocalWindType WindType;
        public bool IsOverWrite;
        public float4 Position;
        public float4 Direction;
        public float Speed;
        public float Radius;
    }

    public class LocalWind : MonoBehaviour
    {
        public LocalWindType WindType = LocalWindType.Directional;
        public bool IsOverWrite = false;
        public float Speed = 1.0f;
        public float Radius = 5.0f;

        private bool _isActive = true;

        private LocalWindData _windData = new();
        public LocalWindData WindData => _windData;

        private void Update()
        {
            UpdateWind();
        }

        private void UpdateWind()
        {
            _windData.WindType = WindType;
            _windData.IsOverWrite = IsOverWrite;
            _windData.Position.xyz = transform.position;
            _windData.Direction.xyz = transform.forward;
            _windData.Speed = Speed;
            _windData.Radius = Radius;

            if (Speed == 0)
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

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            switch (WindType)
            {
                case LocalWindType.Directional:
                    Handles.color = Color.cyan;
                    Handles.DrawWireDisc(transform.position, transform.forward, Radius);
                    Handles.DrawWireDisc(transform.position + Speed * Radius * transform.forward, transform.forward, Radius);
                    break;
                case LocalWindType.Omni:
                case LocalWindType.Vortex:
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(transform.position, Radius);
                    break;
                default:
                    break;
            }
        }
#endif
    }
}
