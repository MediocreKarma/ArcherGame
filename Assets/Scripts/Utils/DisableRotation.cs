using UnityEngine;

public class DisableRotation : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = Vector3.zero;

    private void LateUpdate()
    {
        transform.position = target.position + offset;
    }
}
