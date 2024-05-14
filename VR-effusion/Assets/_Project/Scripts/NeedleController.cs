using System;
using System.Collections;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VREffusion {
    public class NeedleController : MonoBehaviour {
        private Rigidbody body;
        private bool isColliding = false;

        private void Start() {
            body = GetComponent<Rigidbody>();
        }

        private void LateUpdate() {
            FixedUpdate();
        }

        private void FixedUpdate() {
            if (isColliding) {
                Vector3 delta = body.velocity;
                delta = transform.InverseTransformDirection(delta);
                delta = delta.Multiply(Vector3.forward);
                delta = transform.TransformDirection(delta);
                body.velocity = delta;
            }
        }

        private void OnTriggerEnter(Collider collider) {
            if (collider.CompareTag("Body")) {
                isColliding = true;
                body.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }

        private void OnTriggerExit(Collider collider) {
            if (collider.CompareTag("Body")) {
                body.constraints = RigidbodyConstraints.None;
                isColliding = false;
            }
        }
    }
}
