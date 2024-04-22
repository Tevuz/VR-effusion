using System;
using System.Collections.Generic;
using Unity;
using UnityEngine;
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

        internal RenderTexture result => _outBuffer;


        internal Intersect(ComputeShader shader, int width = 512, int height = 512) {
            _shader = shader;
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
            _outVertices = new GraphicsBuffer(GraphicsBuffer.Target.Vertex | GraphicsBuffer.Target.Raw, UltrasoundSystem.MAX_VERTICES_OUT, 12);
            _outBuffer = new RenderTexture(WIDTH, HEIGHT, 0, RenderTextureFormat.ARGBHalf) {
                enableRandomWrite = true };
            _outBuffer.Create();
        }

        internal void Dispatch(UltrasoundController controller, UltrasoundTarget[] targets) {
            DispatchInit();
            DispatchMain(controller, targets);
            DispatchRaster();
        }

        private void DispatchInit() {
            _counter.SetCounterValue(8);
            _shader.SetBuffer(_kernelInit.i, "_counter", _counter);
            _shader.SetBuffer(_kernelInit.i, "_outVertices", _outVertices);
            _shader.Dispatch(_kernelInit.i, 1, 1, 1);
        }

        private void DispatchMain(UltrasoundController controller, UltrasoundTarget[] targets) {
            _counter.SetCounterValue(8);
            _shader.SetBuffer(_kernelMain.i, "_counter", _counter);
            _shader.SetBuffer(_kernelMain.i, "_outVertices", _outVertices);

            foreach (var target in targets) {
                //var meshTarget = target._target;
                //if (meshTarget is null)
                //    continue;
                //Debug.Log(meshTarget);
                //_shader.SetBuffer(_kernelMain.i, "_inVertices", meshTarget?.vertices);
                //_shader.SetBuffer(_kernelMain.i, "_inIndices", meshTarget?.indices);
                //foreach (var subMesh in meshTarget?.subMeshes) {
                //    _shader.SetInt("_inOffset", subMesh.start);
                //    _shader.SetInt("_inCount", subMesh.count);
                //    _shader.SetFloats("_inMaterial", 1.0f, 1.0f, 1.0f);
                //    _shader.Dispatch(_kernelMain.i, NumGroups(subMesh.count, _kernelMain.dx), 1, 1);
                //}

                var skin = target.gameObject.GetComponents<SkinnedMeshRenderer>();
                if (skin.Length == 0)
                    continue;

                Matrix4x4 matrix = controller._projection * controller._view;
                _shader.SetMatrix("u_matrix", matrix);
                _shader.SetMatrix("u_inverse", matrix.inverse);

                var vertices = skin[0].GetVertexBuffer();
                if (vertices is null)
                    continue;

                var indices = skin[0].sharedMesh.GetIndexBuffer();

                _shader.SetBuffer(_kernelMain.i, "_inVertices", vertices);
                _shader.SetBuffer(_kernelMain.i, "_inIndices", indices);

                _shader.SetInt("_inOffset", 0);
                _shader.SetInt("_inCount", indices.count);
                _shader.SetFloats("_inMaterial", 1.0f, 1.0f, 1.0f);

                _shader.Dispatch(_kernelMain.i, NumGroups(indices.count, _kernelMain.dx), 1, 1);

                vertices.Dispose();
                indices.Dispose();
            }
        }

        private void DispatchRaster() {
            _shader.SetBuffer(_kernelRaster.i, "_outVertices", _outVertices);
            _shader.SetTexture(_kernelRaster.i, "_outBuffer", _outBuffer);

            _shader.Dispatch(_kernelRaster.i, _kernelRaster.x, 1, 1);
        }
    }
}