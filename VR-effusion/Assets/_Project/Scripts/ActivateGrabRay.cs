using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VREffusion {
    public class ActivateGrabRay : MonoBehaviour {

        public GameObject rightGrabRay;
        public XRDirectInteractor rightDirectGrab;

        //public GameObject leftGrabRay;
        //public XRDirectInteractor leftDirectGrab;



        // Start is called before the first frame update
        void Start() { }

        // Update is called once per frame
        void Update() {
            rightGrabRay.SetActive(rightDirectGrab.interactablesSelected.Count == 0);
            //  leftGrabRay.SetActive(leftDirectGrab.interactablesSelected.Count == 0);

        }
    }
}
