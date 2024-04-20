using UnityEngine;

public class CollisionController : MonoBehaviour
{
    [SerializeField] private Collider targetCollider;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Needle"))
        {
            //Ignorerer kollisjonen
            Physics.IgnoreCollision(collision.collider, targetCollider, true);



        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Needle"))
        {
            //Slutter å ignorere kollisjonen
            Physics.IgnoreCollision(collision.collider, targetCollider, false);

        }
    }
}
