using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultrasound {
    public class UltrasoundSystem : MonoBehaviour {

        internal static int MAX_VERTICES_OUT = 10000;

        [SerializeField] private ComputeShader _shaderSimulate;
        [SerializeField] private ComputeShader _shaderIntersect;

        [SerializeField] private UltrasoundMonitor[] _monitor;
        [SerializeField] private UltrasoundController _controller;
        [SerializeField] private UltrasoundTarget[] _targets;

        private const int WIDTH = 256, HEIGHT = 256;

        private Simulate _simulate;
        private Intersect _intersect;

        private Mesh _mesh;

        internal void Start() {
            MeshFilter filter = GetComponent<MeshFilter>();
            _mesh = filter.mesh;
            _mesh.name = "Intersection";

            _simulate = new Simulate(_shaderSimulate, WIDTH, HEIGHT);
            _intersect = new Intersect(_shaderIntersect, _mesh, WIDTH, HEIGHT);

            _intersect._targets = _targets;
            _intersect._controller = _controller;

            foreach (var monitor in _monitor) {
                if (monitor.TryGetComponent(out Renderer renderer)) {
                    print(monitor);
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetTexture("_EmissionMap", _simulate.result);
                }
            }
        }

        internal void Update() {
            _intersect.Dispatch();
            _simulate.Dispatch(_intersect._outBuffer);
        }

        private void OnDestroy() {
            _intersect.Dispose();
        }
    }
}
