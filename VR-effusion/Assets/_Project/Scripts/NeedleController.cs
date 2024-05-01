using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class NeedleController : MonoBehaviour
{
    private bool isColliding = false;
    public InputActionProperty pinchAction;
    public Animator handAnimator;
    private Rigidbody rb;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        MeshCollider c;
    }

    private void FixedUpdate()
    {
        if (isColliding)
        {
            Vector3 vel = rb.velocity;
            vel = transform.InverseTransformDirection(vel);
            vel = vel.Multiply(Vector3.forward);
            vel = transform.TransformDirection(vel);
            rb.velocity = vel;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // N�r n�len treffer m�let, ignorer kollisjoner mellom n�len og m�let
        if (collision.collider.CompareTag("Body"))
        {
            isColliding = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // N�r n�len forlater m�let, ikke lenger ignorer kollisjoner mellom n�len og m�let
        if (collision.collider.CompareTag("Body"))
        {
            rb.constraints = RigidbodyConstraints.None;
            isColliding = false;
        }
    }
}
