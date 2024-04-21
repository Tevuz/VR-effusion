using UnityEngine;

public class NeedleController : MonoBehaviour
{
    private bool isColliding = false;

    private void Update()
    {
        if (isColliding)
        {
            // Freeze position in X and Z directions
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY;

            float rightTrigger = Input.GetAxis("PrimaryTrigger");

            //Trigger is down -> freeze positions -> extract liquid 
            if (rightTrigger > 0.5f)
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


