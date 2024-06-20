using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelWind
{
    public class VoxelWindZone : MonoBehaviour
    {
        public enum DebugDrawMode
        {
            None,
            VoxelVelocity,
            VoxelForce,
            OneMinusVoxelForce,
            VoxelGrid,
        }

        public enum ComputeDevice
        {
            CPU,
            GPU,
        }

        private VoxelGrid voxelGrid = new();
        private bool _isVoxelGridBuilt = false;

        public ComputeDevice computeDevice = ComputeDevice.CPU;
        public float voxelSize = 1.0f;
        public Vector3 voxelGridSize = new (10.0f, 10.0f, 10.0f);
        public Vector3 voxelGridOffset = new(0.0f, 0.0f, 0.0f);

        public List<GlobalWind> globalWinds = new();
        public List<LocalWind> localWinds = new();
        public List<WindSphereCollider> windColliders = new();

        public ComputeShader updatingShader;
        public ComputeShader packingShader;
        private RenderTexture _voxelTexture;

        public DebugDrawMode debugMode;
        public float debugLineLength = 0.5f;

        private NativeArray<GlobalWindData> _globalWindDataArray;
        private NativeArray<LocalWindData> _localWindDataArray;
        private NativeArray<WindColliderData> _windColliderDataArray;

        private ComputeBuffer _voxelBuffer;
        private ComputeBuffer _globalWindBuffer;
        private ComputeBuffer _localWindBuffer;
        private ComputeBuffer _windColliderBuffer;

        private Material _glMaterial;

        private void Start()
        {
            if (packingShader == null)
            {
                Debug.LogError("VoxelWindZone: Packing shader is not assigned.");
                return;
            }

            if (computeDevice == ComputeDevice.GPU)
            {
                _globalWindBuffer = new ComputeBuffer(globalWinds.Count, sizeof(float) * 9);
                _localWindBuffer = new ComputeBuffer(localWinds.Count, sizeof(float) * 13);
                _windColliderBuffer = new ComputeBuffer(windColliders.Count, sizeof(float) * 12);
            }

            CreateGLMaterial();
            BuildVoxelGrid();
        }

        private void OnDestroy()
        {
            if (_globalWindDataArray.IsCreated)
                _globalWindDataArray.Dispose();

            if (_localWindDataArray.IsCreated)
                _localWindDataArray.Dispose();

            if (_windColliderDataArray.IsCreated)
                _windColliderDataArray.Dispose();

            _voxelBuffer?.Release();
            _globalWindBuffer?.Release();
            _localWindBuffer?.Release();
            _windColliderBuffer?.Release();

            if (_glMaterial != null)
            {
                Destroy(_glMaterial);
                _glMaterial = null;
            }

            // We need to wait for all requests to complete before releasing the buffers, otherwise errors will occur
            AsyncGPUReadback.WaitAllRequests();

            if (voxelGrid.Voxels.IsCreated)
                voxelGrid.Voxels.Dispose();
        }

        private void CreateGLMaterial()
        {
            if (!_glMaterial)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                _glMaterial = new Material(shader);
                _glMaterial.hideFlags = HideFlags.HideAndDontSave;

                _glMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _glMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _glMaterial.SetInt("_Cull", (int)CullMode.Off);
                _glMaterial.SetInt("_ZWrite", 0);
            }
        }

        public void BuildVoxelGrid()
        {
            if (packingShader == null)
                return;

            UpdateVoxelGridData();

            UpdateVoxelBuffer();

            UpdateVoxelTexture();

            var initJob = new BuildVoxelsJob
            {
                voxelGrid = voxelGrid,
            };

            var handle = initJob.Schedule(voxelGrid.VoxelCount, 32);
            handle.Complete();

            _voxelBuffer.SetData(voxelGrid.Voxels);

            _isVoxelGridBuilt = true;
        }

        private void UpdateVoxelBuffer()
        {
            if (!voxelGrid.Voxels.IsCreated || voxelGrid.Voxels.Length != voxelGrid.VoxelCount)
            {
                voxelGrid.Voxels.Dispose();
                voxelGrid.Voxels = new NativeArray<Voxel>(voxelGrid.VoxelCount, Allocator.Persistent);
                _voxelBuffer?.Dispose();
                _voxelBuffer = new ComputeBuffer(voxelGrid.VoxelCount, sizeof(float) * 14);
            }
        }

        private void UpdateVoxelTexture()
        {
            if (_voxelTexture == null
                || _voxelTexture.width != voxelGrid.VoxelDensity.x
                || _voxelTexture.height != voxelGrid.VoxelDensity.y
                || _voxelTexture.volumeDepth != voxelGrid.VoxelDensity.z
                || _voxelTexture.dimension != TextureDimension.Tex3D)
            {
                if (_voxelTexture != null)
                {
                    _voxelTexture.Release();
                }

                _voxelTexture = new RenderTexture(voxelGrid.VoxelDensity.x, voxelGrid.VoxelDensity.y, 0, RenderTextureFormat.ARGBFloat)
                {
                    enableRandomWrite = true,
                    dimension = TextureDimension.Tex3D,
                    volumeDepth = voxelGrid.VoxelDensity.z
                };
                _voxelTexture.Create();

                Shader.SetGlobalTexture("_VoxelWindTexture", _voxelTexture);
            }
        }

        private void UpdateVoxelGridData()
        {
            voxelGrid.WorldMatrix = transform.localToWorldMatrix;
            voxelGrid.VoxelSize = voxelSize;
            voxelGrid.Size = voxelGridSize;
            voxelGrid.Offset = voxelGridOffset;

            var voxelCountX = Mathf.FloorToInt(voxelGridSize.x / voxelSize);
            var voxelCountY = Mathf.FloorToInt(voxelGridSize.y / voxelSize);
            var voxelCountZ = Mathf.FloorToInt(voxelGridSize.z / voxelSize);
            voxelGrid.VoxelDensity = new int3(voxelCountX, voxelCountY, voxelCountZ);
            voxelGrid.VoxelCount = voxelCountX * voxelCountY * voxelCountZ;

            Shader.SetGlobalMatrix("_VoxelWindGridMatrix", math.inverse(voxelGrid.WorldMatrix));
            Shader.SetGlobalVector("_VoxelWindGridOffset", new Vector3(voxelGrid.Offset.x, voxelGrid.Offset.y, voxelGrid.Offset.z));
            Shader.SetGlobalVector("_VoxelWindGridSize", new Vector3(voxelGrid.Size.x, voxelGrid.Size.y, voxelGrid.Size.z));
        }

        private void Update()
        {
            if (!voxelGrid.Voxels.IsCreated)
            {
                _isVoxelGridBuilt = false;
                BuildVoxelGrid();
                return;
            }

            if (transform.hasChanged)
            {
                _isVoxelGridBuilt = false;
                BuildVoxelGrid();
                transform.hasChanged = false;
            }

            UpdateVoxelWindVectors();
        }

        private void UpdateVoxelWindVectors()
        {
            if (!_isVoxelGridBuilt)
                return;

            if (packingShader == null)
                return;

            UpdateWindDataArray();

            switch (computeDevice)
            {
                case ComputeDevice.CPU:
                    var updatedVoxels = new NativeArray<Voxel>(voxelGrid.Voxels.Length, Allocator.TempJob);
                    var updateJob = new UpdateVoxelsJob
                    {
                        //voxels = tempVoxels,
                        updatedVoxels = updatedVoxels,

                        voxelGrid = voxelGrid,

                        time = Time.time,
                        deltaTime = Time.deltaTime,

                        globalWinds = _globalWindDataArray,
                        localWinds = _localWindDataArray,
                        windColliders = _windColliderDataArray,
                    };

                    var handle = updateJob.Schedule(voxelGrid.Voxels.Length, 64);
                    handle.Complete();

                    updatedVoxels.CopyTo(voxelGrid.Voxels);
                    updatedVoxels.Dispose();

                    _voxelBuffer.SetData(voxelGrid.Voxels);

                    // Pack the voxel data into a 3D texture
                    packingShader.SetBuffer(0, "VoxelsIn", _voxelBuffer);
                    packingShader.SetVector("VoxelDensity", new Vector3(voxelGrid.VoxelDensity.x, voxelGrid.VoxelDensity.y, voxelGrid.VoxelDensity.z));
                    packingShader.SetTexture(0, "VoxelTexture", _voxelTexture);

                    packingShader.Dispatch(0, Mathf.Max(voxelGrid.VoxelDensity.x / 8, 1), Mathf.Max(voxelGrid.VoxelDensity.y / 8, 1), Mathf.Max(voxelGrid.VoxelDensity.z / 8, 1));
                    break;

                case ComputeDevice.GPU:
                    _globalWindBuffer.SetData(_globalWindDataArray);
                    _localWindBuffer.SetData(_localWindDataArray);
                    _windColliderBuffer.SetData(_windColliderDataArray);

                    updatingShader.SetBuffer(0, "voxelsIn", _voxelBuffer);
                    updatingShader.SetTexture(0, "voxelsOut", _voxelTexture);

                    updatingShader.SetVector("voxelGridOffset", (Vector3)voxelGrid.Offset);
                    updatingShader.SetVector("voxelDensity", new Vector3(voxelGrid.VoxelDensity.x, voxelGrid.VoxelDensity.y, voxelGrid.VoxelDensity.z));
                    updatingShader.SetFloat("voxelSize", voxelGrid.VoxelSize);
                    updatingShader.SetMatrix("worldMatrix", voxelGrid.WorldMatrix);
                    updatingShader.SetFloat("time", Time.time);
                    updatingShader.SetFloat("deltaTime", Time.deltaTime);

                    updatingShader.SetBuffer(0, "globalWinds", _globalWindBuffer);
                    updatingShader.SetBuffer(0, "localWinds", _localWindBuffer);
                    updatingShader.SetBuffer(0, "windColliders", _windColliderBuffer);

                    int threadGroupSizeX = Mathf.CeilToInt(voxelGrid.VoxelDensity.x / 8.0f);
                    int threadGroupSizeY = Mathf.CeilToInt(voxelGrid.VoxelDensity.y / 8.0f);
                    int threadGroupSizeZ = Mathf.CeilToInt(voxelGrid.VoxelDensity.z / 8.0f);

                    updatingShader.Dispatch(0, threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ);
                    
                    if (debugMode == DebugDrawMode.None || debugMode == DebugDrawMode.VoxelGrid)
                        break;

                    // Read back the voxel data from the GPU for debugging
                    AsyncGPUReadback.Request(_voxelBuffer, (AsyncGPUReadbackRequest request) =>
                    {
                        if (request.hasError)
                        {
                            Debug.LogError("GPU readback error detected.");
                            return;
                        }

                        request.GetData<Voxel>().CopyTo(voxelGrid.Voxels);
                    });
                    break;
            }
        }

        private void UpdateWindDataArray()
        {
            BuildBuffer(ref _globalWindDataArray, globalWinds.Count, globalWinds.ConvertAll(w => w.WindData));
            BuildBuffer(ref _localWindDataArray, localWinds.Count, localWinds.ConvertAll(w => w.WindData));
            BuildBuffer(ref _windColliderDataArray, windColliders.Count, windColliders.ConvertAll(w => w.WindCollider));
        }

        private void BuildBuffer<T>(ref NativeArray<T> buffer, int count, List<T> sourceData) where T : struct
        {
            if (!buffer.IsCreated || buffer.Length != count)
            {
                if (buffer.IsCreated)
                {
                    buffer.Dispose();
                }
                buffer = new NativeArray<T>(count, Allocator.Persistent);
            }

            for (var i = 0; i < count; i++)
            {
                buffer[i] = sourceData[i];
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(voxelGridOffset, voxelGridSize);

            if (debugMode != DebugDrawMode.VoxelGrid)
                return;

            var lineArray = new List<Vector3>();

            // Calculate the number of lines to draw based on the voxel size
            var linesX = Mathf.FloorToInt(voxelGridSize.x / voxelSize);
            var linesY = Mathf.FloorToInt(voxelGridSize.y / voxelSize);
            var linesZ = Mathf.FloorToInt(voxelGridSize.z / voxelSize);

            // Calculate the start and end points for the grid lines
            var start = voxelGridOffset - voxelGridSize / 2;
            var end = voxelGridOffset + voxelGridSize / 2;

            // Draw lines parallel to the X-axis
            for (var y = 0; y <= linesY; y++)
            {
                for (var z = 0; z <= linesZ; z++)
                {
                    var lineStart = start + new Vector3(0, y * voxelSize, z * voxelSize);
                    var lineEnd = lineStart + new Vector3(voxelGridSize.x, 0, 0);
                    lineArray.Add(lineStart);
                    lineArray.Add(lineEnd);
                }
            }

            // Draw lines parallel to the Y-axis
            for (var x = 0; x <= linesX; x++)
            {
                for (var z = 0; z <= linesZ; z++)
                {
                    var lineStart = start + new Vector3(x * voxelSize, 0, z * voxelSize);
                    var lineEnd = lineStart + new Vector3(0, voxelGridSize.y, 0);
                    lineArray.Add(lineStart);
                    lineArray.Add(lineEnd);
                }
            }

            // Draw lines parallel to the Z-axis
            for (var x = 0; x <= linesX; x++)
            {
                for (var y = 0; y <= linesY; y++)
                {
                    var lineStart = start + new Vector3(x * voxelSize, y * voxelSize, 0);
                    var lineEnd = lineStart + new Vector3(0, 0, voxelGridSize.z);
                    lineArray.Add(lineStart);
                    lineArray.Add(lineEnd);
                }
            }

            Gizmos.DrawLineList(lineArray.ToArray());
        }

        private void OnRenderObject()
        {
            if (voxelGrid.Voxels == null || _glMaterial == null)
                return;

            if (debugMode == DebugDrawMode.None || debugMode == DebugDrawMode.VoxelGrid)
                return;

            GL.PushMatrix();
            _glMaterial.SetPass(0);
            GL.Begin(GL.LINES);

            var startPointOffset = voxelGrid.VoxelSize / 2;

            foreach (var voxel in voxelGrid.Voxels)
            {
                if (!voxel.IsActive)
                    continue;

                var rgb = float3.zero;
                switch (debugMode)
                {
                    case DebugDrawMode.None:
                        break;
                    case DebugDrawMode.VoxelVelocity:
                        rgb = math.normalize(voxel.Velocity.xyz);
                        break;
                    case DebugDrawMode.VoxelForce:
                        rgb = math.length(voxel.Velocity);
                        break;
                    case DebugDrawMode.OneMinusVoxelForce:
                        rgb = 1 - math.length(voxel.Velocity);
                        break;
                }

                var color = new Color(rgb.x, rgb.y, rgb.z);
                GL.Color(color);

                var start = (Vector3)(voxel.Position.xyz + startPointOffset);
                var end = start + (Vector3)voxel.Velocity.xyz * debugLineLength;

                GL.Vertex(start);
                GL.Vertex(end);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}