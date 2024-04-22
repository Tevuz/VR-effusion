using UnityEngine;
using UnityEngine.InputSystem;

public class NeedleController : MonoBehaviour
{
    private bool isColliding = false;
    public InputActionProperty pinchAction;
    public Animator handAnimator;

    private void Update()
    {
        if (isColliding)
        {
            // Freeze position in X and Z directions
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            float triggerValue = pinchAction.action.ReadValue<float>();
            handAnimator.SetFloat("Trigger", triggerValue);

            //Trigger is down -> freeze positions/rotations -> extract liquid 
            if (triggerValue > 0.5f)
            {
                rb.constraints = RigidbodyConstraints.FreezeAll;
                Debug.Log("Høyre trigger er presset ned!");
            }
        }
        else
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.None;
        }


    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Body"))
        {
            isColliding = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Body"))
        {
            isColliding = false;
        }
    }
}


