using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity;
using UnityEngine;
using UnityEngine.Rendering;
using static Ultrasound.Util;

namespace Ultrasound{
    public class Intersect {

        private readonly int WIDTH, HEIGHT;

        private ComputeShader _shader;
        private (int i, int x) _kernelInit;
        private (int i, int dx) _kernelMain;
        private (int i, int x) _kernelRaster;

        private ComputeBuffer _counter;
        private GraphicsBuffer _outVertices;
        private RenderTexture _outBuffer;
        private int _outStride = Marshal.SizeOf<Vector3>();

        internal RenderTexture result => _outBuffer;

        private Mesh _mesh;

        internal UltrasoundController _controller { get; set; }
        internal GameObject[] _targets { get; set; }


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

            _kernelRaster.i = _shader.FindKernel("Raster");
            _shader.GetKernelThreadGroupSizes(_kernelRaster.i, out tx, out _, out _);
            _kernelRaster.x = NumGroups(UltrasoundSystem.MAX_VERTICES_OUT, (int)tx);

            _shader.SetInt("MAX_VERTICES_OUT", UltrasoundSystem.MAX_VERTICES_OUT);
            _shader.SetInt("WIDTH", WIDTH);
            _shader.SetInt("HEIGHT", HEIGHT);
        }

        private void InitOutput() {
            _counter = new ComputeBuffer(1, 12, ComputeBufferType.Counter);

            _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            _mesh.SetVertexBufferParams(UltrasoundSystem.MAX_VERTICES_OUT, new VertexAttributeDescriptor(VertexAttribute.Position));
            _mesh.SetIndexBufferParams(UltrasoundSystem.MAX_VERTICES_OUT, IndexFormat.UInt32);

            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, UltrasoundSystem.MAX_VERTICES_OUT, MeshTopology.Lines), MeshUpdateFlags.DontRecalculateBounds);

            _outVertices = _mesh.GetVertexBuffer(0);
            _mesh.SetIndexBufferData(Enumerable.Range(0, UltrasoundSystem.MAX_VERTICES_OUT).ToList(), 0, 0, UltrasoundSystem.MAX_VERTICES_OUT, MeshUpdateFlags.DontRecalculateBounds);

            Debug.Log(string.Join(", ", Enumerable.Range(0, UltrasoundSystem.MAX_VERTICES_OUT)));

            //_outVertices = new GraphicsBuffer(GraphicsBuffer.Target.Vertex | GraphicsBuffer.Target.Raw, UltrasoundSystem.MAX_VERTICES_OUT, 12);

            _outBuffer = new RenderTexture(WIDTH, HEIGHT, 0, RenderTextureFormat.ARGBHalf) {
                enableRandomWrite = true };
            _outBuffer.Create();
        }

        internal void Dispatch() {
            DispatchInit();
            DispatchMain();
            //DispatchRaster();
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
            _shader.SetMatrix("u_matrix", matrix);

            foreach (var target in _targets) {
                if (target.TryGetComponent(out SkinnedMeshRenderer skin)) {
                    DispatchMain(skin);
                } else if (target.TryGetComponent(out MeshFilter filter)) {
                    DispatchMain(filter);
                }
            }
        }

        private void DispatchMain(SkinnedMeshRenderer skin) {
            skin.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            skin.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            skin.sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            GraphicsBuffer vertices = skin.GetVertexBuffer();
            GraphicsBuffer indices = skin.sharedMesh.GetIndexBuffer();

            DispatchMain(vertices, indices);
        }

        private void DispatchMain(MeshFilter filter) {
            filter.mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            filter.mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            GraphicsBuffer vertices = filter.mesh.GetVertexBuffer(0);
            GraphicsBuffer indices = filter.mesh.GetIndexBuffer();

            DispatchMain(vertices, indices);

        }

        private void DispatchMain(GraphicsBuffer vertices, GraphicsBuffer indices) {
            _shader.SetBuffer(_kernelMain.i, "_inVertices", vertices);
            _shader.SetBuffer(_kernelMain.i, "_inIndices", indices);
            _shader.SetInt("_inStride", vertices.stride);

            _shader.SetInt("_inOffset", 0);
            _shader.SetInt("_inCount", indices.count);

            _shader.Dispatch(_kernelMain.i, NumGroups(indices.count, _kernelMain.dx), 1, 1);
        }

        private void DispatchRaster() {
            _shader.SetBuffer(_kernelRaster.i, "_outVertices", _outVertices);
            _shader.SetTexture(_kernelRaster.i, "_outBuffer", _outBuffer);

            _shader.Dispatch(_kernelRaster.i, _kernelRaster.x, 1, 1);
        }
    }
}