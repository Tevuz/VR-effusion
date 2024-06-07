using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static Ultrasound.Util;

namespace Ultrasound{
    public class Intersect {

        private readonly int WIDTH, HEIGHT;

        private ComputeShader _shader;
        private (int i, int x) _kernelInit;
        private (int i, int dx) _kernelMain;

        private (int i, int x) _kernelClear;
        private (int i, int x) _kernelRaster;
        private (int i, int x) _kernelFill;
        private (int i, int x) _kernelBlit;

        private ComputeBuffer _counter;
        private GraphicsBuffer _outVertices;
        internal ComputeBuffer _outBuffer;
        private int _outStride = Marshal.SizeOf<Vertex>();

        internal RenderTexture result;

        private Mesh _mesh;

        internal UltrasoundController _controller { get; set; }
        internal UltrasoundTarget[] _targets { get; set; }

        internal Intersect(ComputeShader shader, Mesh mesh, int width = 512, int height = 512) {
            _shader = shader;
            _mesh = mesh;

            WIDTH = width;
            HEIGHT = height;

            InitShader();
            InitOutput();
        }

        private void InitShader() {
            uint tx;
            _kernelInit.i = _shader.FindKernel("Init");
            _kernelInit.x = 1;

            _kernelMain.i = _shader.FindKernel("Main");
            _shader.GetKernelThreadGroupSizes(_kernelMain.i, out tx, out _, out _);
            _kernelMain.dx = (int)tx;

            _kernelClear.i = _shader.FindKernel("Clear");
            _shader.GetKernelThreadGroupSizes(_kernelClear.i, out tx, out _, out _);
            _kernelClear.x = NumGroups(HEIGHT, (int)tx);

            _kernelRaster.i = _shader.FindKernel("Raster");
            _shader.GetKernelThreadGroupSizes(_kernelRaster.i, out tx, out _, out _);
            _kernelRaster.x = NumGroups(UltrasoundSystem.MAX_VERTICES_OUT, (int)tx);

            _kernelFill.i = _shader.FindKernel("Fill");
            _shader.GetKernelThreadGroupSizes(_kernelFill.i, out tx, out _, out _);
            _kernelFill.x = NumGroups(WIDTH, (int)tx);

            _kernelBlit.i = _shader.FindKernel("Blit");
            _shader.GetKernelThreadGroupSizes(_kernelBlit.i, out tx, out _, out _);
            _kernelBlit.x = NumGroups(WIDTH, (int)tx);

            _shader.SetInt("MAX_VERTICES_OUT", UltrasoundSystem.MAX_VERTICES_OUT);
            _shader.SetInt("WIDTH", WIDTH);
            _shader.SetInt("HEIGHT", HEIGHT);
        }

        private void InitOutput() {
            _counter = new ComputeBuffer(1, 12, ComputeBufferType.Counter);

            _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            var a_material = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UInt32, 4);

            _mesh.SetVertexBufferParams(UltrasoundSystem.MAX_VERTICES_OUT, new VertexAttributeDescriptor(VertexAttribute.Position), a_material);
            _mesh.SetIndexBufferParams(UltrasoundSystem.MAX_VERTICES_OUT, IndexFormat.UInt32);

            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, UltrasoundSystem.MAX_VERTICES_OUT, MeshTopology.Lines), MeshUpdateFlags.DontRecalculateBounds);

            _outVertices = _mesh.GetVertexBuffer(0);
            _mesh.SetIndexBufferData(Enumerable.Range(0, UltrasoundSystem.MAX_VERTICES_OUT).ToList(), 0, 0, UltrasoundSystem.MAX_VERTICES_OUT, MeshUpdateFlags.DontRecalculateBounds);

            //_outVertices = new GraphicsBuffer(GraphicsBuffer.Target.Vertex | GraphicsBuffer.Target.Raw, UltrasoundSystem.MAX_VERTICES_OUT, 12);

            _outBuffer = new ComputeBuffer(WIDTH * HEIGHT, Marshal.SizeOf<Material>(), ComputeBufferType.Structured);

            result = new RenderTexture(WIDTH, HEIGHT, 0, RenderTextureFormat.ARGB32) {
                enableRandomWrite = true };
            result.Create();
        }

        internal void Dispose() {
            _counter.Release();
            _outVertices.Release();
            _outBuffer.Release();
            result.Release();
        }

        internal void Dispatch() {
            DispatchInit();
            DispatchMain();

            DispatchClear();
            DispatchRaster();
            DispatchFill();
            DispatchBlit();
        }

        private void DispatchInit() {
            _counter.SetCounterValue(0);
            _shader.SetBuffer(_kernelInit.i, "_counter", _counter);
            _shader.SetBuffer(_kernelInit.i, "_outVertices", _outVertices);
            _shader.Dispatch(_kernelInit.i, 1, 1, 1);
        }

        private void DispatchMain() {
            _counter.SetCounterValue(0);
            _shader.SetBuffer(_kernelMain.i, "_counter", _counter);

            _shader.SetBuffer(_kernelMain.i, "_outVertices", _outVertices);
            _shader.SetInt("_outStride", _outStride);

            _shader.SetFloats("u_plane", 0.0f, 1.0f, 0.0f);
            _shader.SetFloat("u_offset", 0.0f);

            Matrix4x4 matrix = _controller._projection * _controller._view;
            _shader.SetMatrix("matrix_view", matrix);

            foreach (var target in _targets) {
                if (target.TryGetComponent(out SkinnedMeshRenderer skin)) {
                    DispatchMain(target, skin);
                } else if (target.TryGetComponent(out MeshFilter filter)) {
                    DispatchMain(target, filter);
                }
            }
        }

        private void DispatchMain(UltrasoundTarget target, SkinnedMeshRenderer skin) {
            skin.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            skin.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            skin.sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            GraphicsBuffer vertices = skin.GetVertexBuffer();
            GraphicsBuffer indices = skin.sharedMesh.GetIndexBuffer();
            if (vertices is null || indices is null) {
                vertices?.Release();
                indices?.Release();
                return;
            }

            _shader.SetBuffer(_kernelMain.i, "_inVertices", vertices);
            _shader.SetBuffer(_kernelMain.i, "_inIndices", indices);
            _shader.SetInt("_vertexStride", vertices.stride);
            _shader.SetInt("_indexStride", indices.stride);

            _shader.SetMatrix("matrix_model", (skin.rootBone ?? skin.transform).localToWorldMatrix);

            DispatchMain(target, skin.sharedMesh);

            vertices?.Release();
            indices?.Release();
        }

        private void DispatchMain(UltrasoundTarget target, MeshFilter filter) {
            filter.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            filter.sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            GraphicsBuffer vertices = filter.sharedMesh.GetVertexBuffer(0);
            GraphicsBuffer indices = filter.sharedMesh.GetIndexBuffer();
            if (vertices is null || indices is null)
                return;

            _shader.SetBuffer(_kernelMain.i, "_inVertices", vertices);
            _shader.SetBuffer(_kernelMain.i, "_inIndices", indices);
            _shader.SetInt("_vertexStride", vertices.stride);
            _shader.SetInt("_indexStride", indices.stride);

            _shader.SetMatrix("matrix_model", filter.transform.localToWorldMatrix);

            //Debug.Log($"{target}, {string.Join(", ", filter.sharedMesh.GetVertexAttributes())}");
            //Debug.Log($"{target}, {indices.stride}");

            DispatchMain(target, filter.sharedMesh);

            vertices?.Release();
            indices?.Release();
        }

        private void DispatchMain(UltrasoundTarget target, Mesh mesh) {
            for (int i = 0; i < mesh.subMeshCount; i++) {
                if (!target._materials[i].enabled)
                    continue;

                var subMesh = mesh.GetSubMesh(i);
                _shader.SetInt("_inOffset", subMesh.indexStart);
                _shader.SetInt("_inCount", subMesh.indexCount);

                _shader.SetFloat("material_layer", target._materials[i].layer);
                _shader.SetFloat("material_density", target._materials[i].density);
                _shader.SetFloat("material_attenuation", target._materials[i].attenuation);

                _shader.Dispatch(_kernelMain.i, NumGroups(subMesh.indexCount, _kernelMain.dx), 1, 1);
            }
        }

        private void DispatchClear() {
            _shader.SetBuffer(_kernelClear.i, "_outBuffer", _outBuffer);
            _shader.Dispatch(_kernelClear.i, _kernelClear.x, 1, 1);
        }

        private void DispatchRaster() {
            _shader.SetBuffer(_kernelRaster.i, "_outVertices", _outVertices);
            _shader.SetBuffer(_kernelRaster.i, "_outBuffer", _outBuffer);
            _shader.Dispatch(_kernelRaster.i, _kernelRaster.x, 1, 1);
        }

        private void DispatchFill() {
            _shader.SetBuffer(_kernelFill.i, "_outBuffer", _outBuffer);
            _shader.Dispatch(_kernelFill.i, _kernelFill.x, 1, 1);
        }

        private void DispatchBlit() {
            _shader.SetBuffer(_kernelBlit.i, "_outBuffer", _outBuffer);
            _shader.SetTexture(_kernelBlit.i, "_outTexture", result);
            _shader.Dispatch(_kernelBlit.i, _kernelBlit.x, 1, 1);
        }

        private struct Vertex {
            internal Vector3 pos;
            internal Material material;
        }

        private struct Material {
            internal int layer;
            internal int back;
            internal float density;
            internal float attenuation;
        }
    }
}