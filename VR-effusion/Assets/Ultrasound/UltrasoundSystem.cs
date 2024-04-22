using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultrasound {
    public class UltrasoundSystem : MonoBehaviour {

        internal static int MAX_VERTICES_OUT = 20000;

        [SerializeField] private ComputeShader _shaderSimulate;
        [SerializeField] private ComputeShader _shaderIntersect;

        private Simulate _simulate;
        private Intersect _intersect;

        private UltrasoundTarget[] _targets;
        private UltrasoundMonitor[] _monitor;
        private UltrasoundController[] _controllers;

        internal void Start() {
            _simulate = new Simulate(_shaderSimulate);
            _intersect = new Intersect(_shaderIntersect);

            _targets = FindObjectsByType<UltrasoundTarget>(FindObjectsSortMode.None);
            _monitor = FindObjectsByType<UltrasoundMonitor>(FindObjectsSortMode.None);
            _controllers = FindObjectsByType<UltrasoundController>(FindObjectsSortMode.None);

            foreach (var monitor in _monitor) {
                Renderer renderer = monitor.gameObject.GetComponent<Renderer>();
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetTexture("_EmissionMap", _intersect.result);
            }
        }

        internal void Update() {
            _intersect.Dispatch(_controllers.First(), _targets);
            //_simulate.Dispatch(_intersect.result, _intersect.result);
        }
    }
}
