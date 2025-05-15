using UnityEngine;

public class ArrowSticking : MonoBehaviour
{
    private Rigidbody2D rb;
    private GameObject stuckTo = null;
    // FixedJoint2D joint = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void StickTo(Rigidbody2D targetRb, Collision2D collision)
    {
        //Debug.Log("Sticking to " + targetRb.gameObject.name + " at " + Time.frameCount);
        //if (isStuck) return;
        //isStuck = true;
        //stuckTo = targetRb.gameObject;

        //// Create the joint
        //joint = gameObject.AddComponent<FixedJoint2D>();
        //joint.connectedBody = targetRb;

        //// Set the anchor point at the exact hit location
        //joint.anchor = transform.InverseTransformPoint(collision.contacts[0].point);

        //// Configure joint for realistic behavior
        //joint.dampingRatio = 0.8f;
        //joint.frequency = 5f;

        //// Optional: Disable collision between arrow and enemy if needed
        //// Physics2D.IgnoreCollision(GetComponent<Collider2D>(), 
        ////     targetRb.GetComponent<Collider2D>(), true);
       
        //Debug.Log("Stick to called at " + Time.frameCount);
        stuckTo = targetRb.gameObject;
        transform.parent = targetRb.transform;
        rb.simulated = false;
        Debug.Log("Stick to called at " + Time.frameCount);
    }

    public void Unstick()
    {
        //if (isStuck && TryGetComponent<FixedJoint2D>(out var joint))
        //{
        //    Destroy(joint);
        //}
        //Debug.Log("Unstick called at " + Time.frameCount);
        transform.parent = null;
        rb.simulated = true;
        stuckTo = null;
        Debug.Log("Unstick " + Time.frameCount);
    }

    public GameObject StuckTo()
    {
        return stuckTo;
    }
}