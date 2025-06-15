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

    private AudioSource chargeAudio;
    private AudioSource effectSource;
    [SerializeField] private AudioClip launchAudio;

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
        var sources = GetComponents<AudioSource>();
        chargeAudio = sources[0];
        effectSource = sources[1];
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
        arrow.transform.localPosition = arrowTransformChargeOrigin + localOffset;
    }

    public void StartCharging()
    {
        if (arrow.IsLaunched())
        {
            return;
        }
        effectSource.Play();
        chargeTime = 0;
        arrowTransformChargeOrigin = arrow.OriginPosition();
        charging = true;
        chargeAudio.Play();
    }

    public void ReleaseCharge()
    {
        if (!charging)
        {
            return;
        }
        charging = false;
        chargeAudio.Stop();
        Shoot();
    }

    public void RetrieveArrow()
    {
        arrow.Return();
    }

    void Shoot()
    {
        effectSource.PlayOneShot(launchAudio);
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
