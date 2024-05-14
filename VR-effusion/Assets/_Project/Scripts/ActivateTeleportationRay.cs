using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

namespace VREffusion {
    public class ActivateTeleportationRay : MonoBehaviour {

        public GameObject rightTeleportation;
        public GameObject leftTeleportation;

        public InputActionProperty rightActivate;
        public InputActionProperty rightCancel;
        public InputActionProperty leftActivate;
        public InputActionProperty leftCancel;



        // Update is called once per frame
        void Update() {
            rightTeleportation.SetActive(rightCancel.action.ReadValue<float>() == 0 &&
                                         rightActivate.action.ReadValue<float>() > 0.1f);
            leftTeleportation.SetActive(leftCancel.action.ReadValue<float>() == 0 &&
                                        leftActivate.action.ReadValue<float>() > 0.1f);

        }

    }
}
