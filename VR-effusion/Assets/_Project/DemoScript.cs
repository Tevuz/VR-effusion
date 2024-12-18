using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VREffusion;

public class DemoScript : MonoBehaviour {

    private GameObject right;
    private GameObject left;
    private GameObject probe;
    private GameObject syringe;

    // Start is called before the first frame update
    void Start() {
        right = GameObject.FindWithTag("Right Hand");
        left = GameObject.FindWithTag("Left Hand");
        probe = GameObject.FindWithTag("Ultrasoundprobe");
        syringe = GameObject.FindWithTag("Syringe");

        Rigidbody body = probe.GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
