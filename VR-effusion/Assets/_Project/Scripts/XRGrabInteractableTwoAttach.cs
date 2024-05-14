using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VREffusion {
    public class XRGrabInteractableTwoAttach : XRGrabInteractable {

        public Transform leftAttachTransform;
        public Transform rightAttachTransform;

        private void Start() {
        }

        public override Transform GetAttachTransform(IXRInteractor interactor) {
            if (interactor.transform.CompareTag("Left Hand")) {
                attachTransform = leftAttachTransform;
            } else if (interactor.transform.CompareTag("Right Hand")) {
                attachTransform = rightAttachTransform;
            }

            return attachTransform;
        }
    }
}