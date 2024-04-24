using UnityEngine;
using static Ultrasound.Util;

namespace Ultrasound {
    internal class Simulate {

        private ComputeShader _shader;
        private (int i, int x, int y) _kernelNormal;
        private (int i, int x) _kernelSimulate;
        private (int i, int x, int y) _kernelPost;

        private RenderTexture _bufferNormal;
        private RenderTexture _bufferSimulate;
        private RenderTexture _bufferPost;

        internal RenderTexture result => _bufferPost;

        private readonly int WIDTH, HEIGHT;
        private int frame = 0;

        internal Simulate(ComputeShader shader, int width = 512, int height = 512) {
            _shader = shader;
            WIDTH = width;
            HEIGHT = height;

            InitBuffers();
            InitShader();
        }

        private void InitBuffers() {
            _bufferNormal = new RenderTexture(WIDTH, HEIGHT, 0, RenderTextureFormat.ARGBHalf) {
                enableRandomWrite = true };
            _bufferNormal.Create();

            _bufferSimulate = new RenderTexture(WIDTH, HEIGHT, 0, RenderTextureFormat.RFloat) {
                enableRandomWrite = true };
            _bufferSimulate.Create();

            _bufferPost = new RenderTexture(WIDTH, HEIGHT, 0, RenderTextureFormat.Default) {
                enableRandomWrite = true };
            _bufferPost.Create();
        }

        private void InitShader() {
            uint tx, ty;

            _kernelNormal.i = _shader.FindKernel("MainNormal");
            _shader.GetKernelThreadGroupSizes(_kernelNormal.i, out tx, out ty, out _);
            _kernelNormal.x = NumGroups(WIDTH, (int)tx);
            _kernelNormal.y = NumGroups(HEIGHT, (int)ty);

            _kernelSimulate.i = _shader.FindKernel("MainSimulate");
            _shader.GetKernelThreadGroupSizes(_kernelSimulate.i, out tx, out _, out _);
            _kernelSimulate.x = NumGroups(WIDTH, (int)tx);

            _kernelPost.i = _shader.FindKernel("MainPost");
            _shader.GetKernelThreadGroupSizes(_kernelPost.i, out tx, out ty, out _);
            _kernelPost.x = NumGroups(WIDTH, (int)tx);
            _kernelPost.y = NumGroups(HEIGHT, (int)ty);

            _shader.SetInt("WIDTH", WIDTH);
            _shader.SetInt("HEIGHT", HEIGHT);
        }

        internal void Dispatch(RenderTexture source) {
            SetUniforms();

            DispatchNormal(source);
            DispatchSimulate(source);
            DispatchPost();

            //Graphics.Blit(_bufferPost, destination);

            Clear();
        }

        private void Clear() {
            Graphics.Blit(Texture2D.blackTexture, _bufferSimulate);
            Graphics.Blit(Texture2D.blackTexture, _bufferPost);
        }

        private void SetUniforms() {
            _shader.SetInt("FRAME", frame++);
        }

        private void DispatchNormal(RenderTexture source) {
            _shader.SetTexture(_kernelNormal.i, "buffer_source", source);
            _shader.SetTexture(_kernelNormal.i, "buffer_normal", _bufferNormal);
            _shader.Dispatch(_kernelNormal.i, _kernelNormal.x, _kernelNormal.y, 1);
        }

        private void DispatchSimulate(RenderTexture source) {
            _shader.SetTexture(_kernelSimulate.i, "buffer_source", source);
            _shader.SetTexture(_kernelSimulate.i, "buffer_normal", _bufferNormal);
            _shader.SetTexture(_kernelSimulate.i, "buffer_simulate", _bufferSimulate);
            _shader.SetTexture(_kernelSimulate.i, "buffer_post", _bufferPost);
            _shader.Dispatch(_kernelSimulate.i, _kernelSimulate.x, 1, 1);
        }

        private void DispatchPost() {
            _shader.SetTexture(_kernelPost.i, "buffer_simulate", _bufferSimulate);
            _shader.SetTexture(_kernelPost.i, "buffer_post", _bufferPost);
            _shader.Dispatch(_kernelPost.i, _kernelPost.x, _kernelPost.y, 1);
        }
    }
}