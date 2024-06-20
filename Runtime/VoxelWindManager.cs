using System.Collections.Generic;
using UnityEngine;

namespace VoxelWind
{
    [ExecuteAlways]
    public class VoxelWindManager : MonoBehaviour
    {
        public static VoxelWindManager Instance { get; private set; }

        public List<VoxelWindZone> VoxelWindZones = new();
        public List<LocalWind> ConstantLocalWinds = new();
        
        public List<LocalWindData> LocalWinds { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            LocalWinds.Clear();
            foreach (var localWind in ConstantLocalWinds)
            {
                LocalWinds.Add(localWind.WindData);
            }
        }
    }
}
