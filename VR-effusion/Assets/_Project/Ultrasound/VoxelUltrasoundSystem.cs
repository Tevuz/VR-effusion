#define VISUALIZE_VOXELS
#define PROFILE_SHADERS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using static Ultrasound.VoxelUltrasoundSystem.KernelParam<int>;


namespace Ultrasound {

    //[ExecuteAlways]
    public class VoxelUltrasoundSystem : MonoBehaviour {

        [SerializeField] internal ComputeShader _voxelize;
        [SerializeField] internal ComputeShader _simulate;

        private ComputeBuffer _outVoxels;
        private ComputeBuffer _outVoxelMasks;
        private ComputeBuffer _varTriangles;
        private ComputeBuffer _varPointers;

        private ComputeBuffer _bufferMaterial;
        private ComputeBuffer _bufferNormal;
        private RenderTexture _bufferSimulate;
        private RenderTexture _bufferPost;

#if PROFILE_SHADERS
        private ComputeBuffer _profilerFlag;
        private int _profilerAcc;
#endif

        private Kernel<int, int, int> _kernelInitCull;
        private Kernel<int, int, int> _kernelMainCull;
        private Kernel<int, int, int> _kernelMainRaster;
        private Kernel<int, int, int> _kernelFill;
        private Kernel<int, int, int> _kernelClear;
        private Kernel<int, int, int> _kernelRender;

        private Kernel<int, int, int> _kernelSimulateNormal;
        private Kernel<int, int, int> _kernelSimulateMain;
        private Kernel<int, int, int> _kernelSimulatePost;
        private Kernel<int, int, int> _kernelSimulateClear;

        [SerializeField] private UltrasoundController _controller;
        [SerializeField] private UltrasoundMonitor[] _monitors;
        [SerializeField] private UltrasoundTarget[] _targets;

        private const int MAX_TRIANGLE_COUNT =  1024 * 1024 * 32;
        private const int WIDTH = 128, HEIGHT = 1, DEPTH = 128;

        private Mesh _mesh;
        private GraphicsBuffer _vertices;

        private static readonly int ID_InIndices = Shader.PropertyToID("_inIndices");
        private static readonly int ID_InVertices = Shader.PropertyToID("_inVertices");
        private static readonly int ID_InLayer = Shader.PropertyToID("_inLayer");
        private static readonly int ID_InCount = Shader.PropertyToID("_inCount");
        private static readonly int ID_InStart = Shader.PropertyToID("_inStart");
        private static readonly int ID_Matrix = Shader.PropertyToID("_matrix");
        private static readonly int ID_VertexStride = Shader.PropertyToID("VERTEX_STRIDE");
        private static readonly int ID_IndexStride = Shader.PropertyToID("INDEX_STRIDE");
        private static readonly int ProfilerFlag = Shader.PropertyToID("_profilerFlag");
        private static readonly int ID_Frame = Shader.PropertyToID("FRAME");

        internal void Start() {
            InitBuffers();
            InitVoxelize();
            InitSimulate();
            InitProfiling();
            InitTargets();
            InitMesh();
            InitMonitor();
        }

        private void InitBuffers() {
            const int count = WIDTH * HEIGHT * DEPTH;

            _outVoxels?.Release();
            _outVoxels = new ComputeBuffer(count, Marshal.SizeOf<Voxel>(), ComputeBufferType.Structured);

            _outVoxelMasks?.Release();
            _outVoxelMasks = new ComputeBuffer(count, Marshal.SizeOf<VoxelMask>(), ComputeBufferType.Structured);

            _varTriangles?.Release();
            _varTriangles = new ComputeBuffer(MAX_TRIANGLE_COUNT, Marshal.SizeOf<Triangle>(), ComputeBufferType.Raw);

            _varPointers?.Release();
            _varPointers = new ComputeBuffer(2, Marshal.SizeOf<int>(), ComputeBufferType.Structured);

            _bufferMaterial?.Release();
            _bufferMaterial = new ComputeBuffer(32, Marshal.SizeOf<Voxel>(), ComputeBufferType.Structured);
            Voxel[] data = new Voxel[32];
            data[0] = new Voxel{density = 0.30f, attenuation = 0.30f};
            data[1] = new Voxel{density = 0.31f, attenuation = 0.30f};
            data[2] = new Voxel{density = 0.32f, attenuation = 0.30f};
            data[3] = new Voxel{density = 0.33f, attenuation = 0.30f};
            data[4] = new Voxel{density = 0.34f, attenuation = 0.30f};
            data[5] = new Voxel{density = 0.35f, attenuation = 0.30f};
            data[6] = new Voxel{density = 0.36f, attenuation = 0.30f};
            data[7] = new Voxel{density = 0.37f, attenuation = 0.30f};
            data[8] = new Voxel{density = 0.38f, attenuation = 0.30f};
            _bufferMaterial.SetData(data);

        _bufferNormal?.Release();
            _bufferNormal = new ComputeBuffer(count, Marshal.SizeOf<Vector3>(), ComputeBufferType.Structured);

            _bufferSimulate?.Release();
            _bufferSimulate = new RenderTexture(WIDTH, DEPTH, 0, RenderTextureFormat.RFloat) {
                enableRandomWrite = true };
            _bufferSimulate.Create();

            _bufferPost?.Release();
            _bufferPost = new RenderTexture(WIDTH, DEPTH, 0, RenderTextureFormat.Default) {
                enableRandomWrite = true };
            _bufferPost.Create();
        }

        private void InitVoxelize() {
            _kernelInitCull= init("InitCull", _voxelize, Const(1), Const(1), Const(1));
            _voxelize.SetBuffer(_kernelInitCull.i, "_varTriangles", _varTriangles);
            _voxelize.SetBuffer(_kernelInitCull.i, "_varPointers", _varPointers);

            _kernelMainCull = init("MainCull", _voxelize, Const(1), Const(1), Const(1));
            _voxelize.SetBuffer(_kernelMainCull.i, "_outVoxels", _outVoxels);
            _voxelize.SetBuffer(_kernelMainCull.i, "_outVoxelMasks", _outVoxelMasks);
            _voxelize.SetBuffer(_kernelMainCull.i, "_varTriangles", _varTriangles);
            _voxelize.SetBuffer(_kernelMainCull.i, "_varPointers", _varPointers);

            _kernelMainRaster = init("MainRaster", _voxelize, Const(1), Const(1), Const(1));
            _voxelize.SetBuffer(_kernelMainRaster.i, "_outVoxels", _outVoxels);
            _voxelize.SetBuffer(_kernelMainRaster.i, "_outVoxelMasks", _outVoxelMasks);
            _voxelize.SetBuffer(_kernelMainRaster.i, "_varTriangles", _varTriangles);
            _voxelize.SetBuffer(_kernelMainRaster.i, "_varPointers", _varPointers);

            _kernelFill = init("Fill", _voxelize, Const(WIDTH), Const(HEIGHT), Const(1));
            _voxelize.SetBuffer(_kernelFill.i, "_outVoxels", _outVoxels);
            _voxelize.SetBuffer(_kernelFill.i, "_outVoxelMasks", _outVoxelMasks);

            _kernelClear = init("Clear", _voxelize, Const(WIDTH), Const(HEIGHT), Const(DEPTH));
            _voxelize.SetBuffer(_kernelClear.i, "_outVoxels", _outVoxels);
            _voxelize.SetBuffer(_kernelClear.i, "_outVoxelMasks", _outVoxelMasks);
#if VISUALIZE_VOXELS
            _kernelRender = init("Render", _voxelize, Const(WIDTH), Const(HEIGHT), Const(DEPTH));
            _voxelize.SetBuffer(_kernelRender.i, "_outVoxels", _outVoxels);
            _voxelize.SetBuffer(_kernelRender.i, "_outVoxelMasks", _outVoxelMasks);
#endif
            _voxelize.SetInt("WIDTH", WIDTH);
            _voxelize.SetInt("HEIGHT", HEIGHT);
            _voxelize.SetInt("DEPTH", DEPTH);

            _voxelize.SetInt("MAX_TRIANGLE_COUNT", MAX_TRIANGLE_COUNT);
        }

        private void InitSimulate() {
            _kernelSimulateNormal = init("SimulateNormal", _simulate, Const(WIDTH), Const(HEIGHT), Const(DEPTH));
            _simulate.SetBuffer(_kernelSimulateNormal.i, "buffer_material", _bufferMaterial);
            _simulate.SetBuffer(_kernelSimulateNormal.i, "buffer_source", _outVoxelMasks);
            _simulate.SetBuffer(_kernelSimulateNormal.i, "buffer_normal", _bufferNormal);

            _kernelSimulateMain = init("SimulateMain", _simulate, Const(WIDTH), Const(HEIGHT), Const(1));
            _simulate.SetBuffer(_kernelSimulateMain.i, "buffer_material", _bufferMaterial);
            _simulate.SetBuffer(_kernelSimulateMain.i, "buffer_source", _outVoxelMasks);
            _simulate.SetTexture(_kernelSimulateMain.i, "buffer_simulate", _bufferSimulate);
            _simulate.SetBuffer(_kernelSimulateMain.i, "buffer_normal", _bufferNormal);

            _kernelSimulatePost = init("SimulatePost", _simulate, Const(WIDTH), Const(HEIGHT), Const(1));
            _simulate.SetTexture(_kernelSimulatePost.i, "buffer_simulate", _bufferSimulate);
            _simulate.SetTexture(_kernelSimulatePost.i, "buffer_post", _bufferPost);

            _simulate.SetInt("WIDTH", WIDTH);
            _simulate.SetInt("HEIGHT", HEIGHT);
            _simulate.SetInt("DEPTH", DEPTH);
        }

        private void InitProfiling() {
#if PROFILE_SHADERS
            _profilerFlag?.Release();
            _profilerFlag = new ComputeBuffer(1, 4, ComputeBufferType.Raw);

            _voxelize.SetBuffer(_kernelInitCull.i, ProfilerFlag, _profilerFlag);
            _voxelize.SetBuffer(_kernelMainCull.i, ProfilerFlag, _profilerFlag);
            _voxelize.SetBuffer(_kernelMainRaster.i, ProfilerFlag, _profilerFlag);
            _voxelize.SetBuffer(_kernelFill.i, ProfilerFlag, _profilerFlag);
            _voxelize.SetBuffer(_kernelClear.i, ProfilerFlag, _profilerFlag);
#endif

#if PROFILE_SHADERS && VISUALIZE_VOXELS
            _voxelize.SetBuffer(_kernelRender.i, ProfilerFlag, _profilerFlag);
#endif
        }

        private void InitTargets() {
            foreach (var target in _targets) {
                if (target.TryGetComponent(out SkinnedMeshRenderer skin)) {
                    skin.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    skin.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    skin.sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
                } else if (target.TryGetComponent(out MeshFilter filter)) {
                    filter.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    filter.sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
                }
            }
        }

        private void InitMesh() {
#if VISUALIZE_VOXELS
            if (!TryGetComponent(out MeshFilter filter))
                return;

            _mesh = filter.mesh;

            _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
            _mesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;

            var attributes = new[]{
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.Color)
            };

            int count = 8 * WIDTH * HEIGHT * DEPTH;

            _mesh.SetVertexBufferParams(count, attributes);
            _mesh.SetIndexBufferParams(count, IndexFormat.UInt32);

            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, count, MeshTopology.Lines), MeshUpdateFlags.DontRecalculateBounds);

            _mesh.SetIndices(Enumerable.Range(0, count).ToArray(), MeshTopology.Lines, 0, false);

            _mesh.bounds.Encapsulate(_targets[0].GetMesh.bounds);

            _vertices = _mesh.GetVertexBuffer(0);
            _voxelize.SetBuffer(_kernelRender.i, "_outVertices", _vertices);
#endif
        }

        private void InitMonitor() {
            foreach (var monitor in _monitors) {
                if (monitor.TryGetComponent(out Renderer renderer)) {
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetTexture("_EmissionMap", _bufferPost);
                }
            }
        }

        internal void Update() {
            Dispatch();
        }

        private void Dispatch() {
            DispatchClear();
            DispatchRaster();
            DispatchFill();
            DispatchRender();
            DispatchSimulate();
            SampleBias();
        }

        private void DispatchRaster() {
            BeginSample("Voxel Cull Init");
            _voxelize.Dispatch(_kernelInitCull.i, 1, 1, 1);
            EndSample();

            Matrix4x4 view = _controller._projection * _controller._view;

            foreach (UltrasoundTarget target in _targets) {
                Mesh mesh;
                GraphicsBuffer vertices = null;
                Matrix4x4 model;

                if (target.TryGetComponent(out SkinnedMeshRenderer skin)) {
                    mesh = skin.sharedMesh;
                    vertices = skin.GetVertexBuffer();
                    model = (skin.rootBone ?? skin.transform).localToWorldMatrix;
                } else if (target.TryGetComponent(out MeshFilter filter)) {
                    mesh = filter.sharedMesh;
                    vertices = mesh.GetVertexBuffer(0);
                    model = filter.transform.localToWorldMatrix;
                } else {
                    continue;
                }

                GraphicsBuffer indices = mesh.GetIndexBuffer();

                if (vertices is null || indices is null) {
                    vertices?.Release();
                    indices?.Release();
                    continue;
                }

                _voxelize.SetMatrix(ID_Matrix, view * model);
                _voxelize.SetInt(ID_VertexStride, vertices.stride);
                _voxelize.SetInt(ID_IndexStride, indices.stride);

                for (int i = 0; i < mesh.subMeshCount; i++) {
                    if (!target._materials[i].enabled)
                        continue;

                    var subMesh = mesh.GetSubMesh(i);
                    _voxelize.SetInt(ID_InStart, subMesh.indexStart);
                    _voxelize.SetInt(ID_InCount, subMesh.indexCount);

                    _voxelize.SetFloat(ID_InLayer, target._materials[i].layer);

                    _voxelize.SetBuffer(_kernelMainCull.i, ID_InVertices, vertices);
                    _voxelize.SetBuffer(_kernelMainCull.i, ID_InIndices, indices);

                    BeginSample("Voxel Cull Main");
                    _voxelize.Dispatch(_kernelMainCull.i, 1, 1, 1);
                    EndSample();
                }

                vertices.Release();
                indices.Release();
            }

            BeginSample("Voxel Raster");
            _voxelize.Dispatch(_kernelMainRaster.i, 1, 1, 1);
            EndSample();
        }

        private void DispatchFill() {
            BeginSample("Voxel Fill");
            _voxelize.Dispatch(_kernelFill.i, _kernelFill.x, _kernelFill.y, 1);
            EndSample();
        }

        private void DispatchClear() {
            BeginSample("Voxel Clear");
            _voxelize.Dispatch(_kernelClear.i, _kernelClear.x, _kernelClear.y, _kernelClear.z);
            EndSample();
        }

        private void DispatchSimulate() {
            _simulate.SetInt(ID_Frame, Time.frameCount);
            Graphics.Blit(Texture2D.blackTexture, _bufferPost);
            Graphics.Blit(Texture2D.blackTexture, _bufferSimulate);

            _simulate.Dispatch(_kernelSimulateNormal.i, _kernelSimulateNormal.x, _kernelSimulateNormal.y, _kernelSimulateNormal.z);
            _simulate.Dispatch(_kernelSimulateMain.i, _kernelSimulateMain.x, _kernelSimulateMain.y, _kernelSimulateMain.z);
            _simulate.Dispatch(_kernelSimulatePost.i, _kernelSimulatePost.x, _kernelSimulatePost.y, _kernelSimulatePost.z);
        }

        private void DispatchRender() {
#if VISUALIZE_VOXELS
            Matrix4x4 matrix = (_controller._projection * _controller._view).inverse;
            _voxelize.SetMatrix(ID_Matrix, matrix);
            BeginSample("Voxel Render");
            _voxelize.Dispatch(_kernelRender.i, _kernelRender.x, _kernelRender.y, _kernelRender.z);
            EndSample();

            var bounds = _mesh.bounds;
            bounds.center = matrix.MultiplyPoint(new Vector3(0.0f, 0.0f, 0.0f));
            bounds.extents = matrix.MultiplyPoint(new Vector3(1.0f, 1.0f, 1.0f));
            _mesh.bounds = bounds;
#endif
        }

        private void OnDestroy() {
            _outVoxels?.Release();
            _outVoxelMasks?.Release();
            _varTriangles?.Release();
            _varPointers?.Release();
            _vertices?.Release();
            _bufferMaterial?.Release();
            _bufferNormal?.Release();
            _bufferSimulate?.Release();
            _bufferPost?.Release();
        }

        private void SampleBias() {
#if PROFILE_SHADERS
            for (int i = 0; i < 10; i++) {
                BeginSample("Sampling Bias");
                EndSample();
            }
#endif
        }

        private void BeginSample(string name) {
#if PROFILE_SHADERS
            Profiler.BeginSample(name);
#endif
        }

        private void EndSample() {
#if PROFILE_SHADERS
            var data = new int[1];
            _profilerFlag.GetData(data);
            _profilerAcc += data[0];
            Profiler.EndSample();
#endif
        }

        internal Kernel<FX, FY, FZ> init<FX, FY, FZ>(string name, ComputeShader _shader, KernelParam<FX> tx, KernelParam<FY> ty, KernelParam<FZ> tz) {
            int i = _shader.FindKernel(name);
            _shader.GetKernelThreadGroupSizes(i, out uint gx, out uint gy, out uint gz);
            Kernel<FX, FY, FZ> k = new() {
                i = i,
                x = tx.create(gx),
                y = ty.create(gy),
                z = tz.create(gz)
            };

            return k;
        }

        public enum KernelParamType { CONST, DEFERRED }
        public class KernelParam<T> {

            public KernelParamType type;

            public int size;

            private KernelParam(){}

            public T create(uint g) {
                if (typeof(T) == typeof(int))
                    return (T)(object)(int)((size + g - 1) / g);
                if (typeof(T) == typeof(Func<int, int>))
                    return (T)(object)(Func<int, int>)(size => (int)((size + g - 1) / g));
                throw new Exception("Invalid Type");
            }

            public static KernelParam<int> Const(int size) => new KernelParam<int>() { type = KernelParamType.CONST, size = size };
            public static KernelParam<Func<int, int>> Deferred() => new KernelParam<Func<int, int>>() { type = KernelParamType.DEFERRED };
        }

        internal struct Kernel<FX, FY, FZ> {
            internal int i;
            internal FX x;
            internal FY y;
            internal FZ z;
        }

        private static IEnumerable<(int x, int y, int z)> Range((int, int, int) start, (int, int, int) count) {
            (int, int, int) tuple = (0, 0, 0);
            while (true) {
                if (++tuple.Item1 < count.Item1)
                    yield return tuple;
                tuple.Item1 = 0;
                if (++tuple.Item2 < count.Item2)
                    yield return tuple;
                tuple.Item2 = 0;
                if (++tuple.Item3 < count.Item3)
                    yield return tuple;
                tuple.Item3 = 0;
                break;
            }
        }
    }

    struct Voxel {
        public float density;
        public float attenuation;
    }

    struct VoxelMask {
        public int fill;
        public int back;
    }

    struct Triangle {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
        public int layer;
    }
}
