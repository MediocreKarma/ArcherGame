using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Bow : MonoBehaviour
{

    [SerializeField] private Arrow arrow;
    public float arrowMaxChargingTravel = 0.5f;
    public float maxLaunchForce;
    public float minLaunchForce;
    public float maxChargeTime;
    private float chargeTime = 0;
    private bool charging = false;
    private Vector3 arrowTransformChargeOrigin;
    private Player player;

    void Update()
    {
        //Vector2 bowPosition = transform.position;
        //Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Vector2 direction = (mousePosition - bowPosition).normalized;
        //transform.right = direction;
        ChargingUpdate();
    }

    public void AimTowards(Vector2 position)
    {
        Vector2 bowPosition = transform.position;
        Vector2 direction = (position - bowPosition).normalized;
        transform.right = direction;
    }

    private void Start()
    {
        arrowTransformChargeOrigin = arrow.OriginPosition();
        player = GetComponentInParent<Player>();
    }

    void ChargingUpdate()
    {
        if (!charging)
        {
            return;
        }
        chargeTime = Mathf.Min(chargeTime + Time.deltaTime, maxChargeTime);
        float chargeRatio = chargeTime / maxChargeTime;
        float pullDistance = Mathf.Lerp(0, arrowMaxChargingTravel, chargeRatio);
        Vector3 localOffset = transform.InverseTransformDirection(-transform.right) * pullDistance;
        if (!player.IsFacingRight())
        {
            //localOffset.x *= -1;
        }
        arrow.transform.localPosition = arrowTransformChargeOrigin + localOffset;
    }

    public void StartCharging()
    {
        if (arrow.IsLaunched())
        {
            return;
        }
        chargeTime = 0;
        arrowTransformChargeOrigin = arrow.OriginPosition();
        charging = true;
    }

    public void ReleaseCharge()
    {
        if (!charging)
        {
            return;
        }
        charging = false;
        Shoot();
    }

    public void RetrieveArrow()
    {
        arrow.Return();
    }

    void Shoot()
    {
        arrow.Launch(Mathf.Lerp(minLaunchForce, maxLaunchForce, chargeTime / maxChargeTime));
    }

    public void RotateSprite()
    {
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
    }
}
