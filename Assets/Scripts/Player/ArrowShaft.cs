using UnityEngine;

public class ArrowShaft : MonoBehaviour
{
    private Rigidbody2D arrowRb;
    private Arrow arrow;

    void Start()
    {
        arrowRb = GetComponentInParent<Rigidbody2D>();
        arrow = GetComponentInParent<Arrow>();
    }
}
