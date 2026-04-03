using UnityEngine;

[ExecuteAlways]
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // rotate to face camera while preserving upright orientation
        Vector3 direction = cam.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}
