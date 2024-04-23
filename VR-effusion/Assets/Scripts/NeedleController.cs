using UnityEngine;
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
    }
    private void Update()
    {
        if (isColliding)
        {


            // Freeze position in Y and X directions, allow movement in Z
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

            float triggerValue = pinchAction.action.ReadValue<float>();
            handAnimator.SetFloat("Trigger", triggerValue);

            // If trigger is down, freeze all positions/rotations and simulate liquid extraction
            if (triggerValue > 0.5f)
            {
                rb.constraints = RigidbodyConstraints.FreezeAll;
            } else
            {
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

            }
        }
        else
        {
            rb.constraints = RigidbodyConstraints.None;
        }


    }
    private void OnCollisionEnter(Collision collision)
    {
        // Når nålen treffer målet, ignorer kollisjoner mellom nålen og målet
        if (collision.collider.CompareTag("Body"))
        {
            isColliding = true;
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Når nålen forlater målet, ikke lenger ignorer kollisjoner mellom nålen og målet
        if (collision.collider.CompareTag("Body"))
        {
            isColliding = false;
        }
    }
}
