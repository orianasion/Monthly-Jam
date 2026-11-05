using UnityEngine;

public class Cameracontroller : MonoBehaviour
{
    [SerializeField] private Transform FairyTransform;

    void Update()
    {
        // if (FairyTransform == null) return;
        // Vector3 cameraDirection = (FairyTransform.position - gameObject.transform.position).normalized;
        // cameraDirection.x = 0f;
        // cameraDirection.z = 0f;
        // float angle = Mathf.Atan2(cameraDirection.y, cameraDirection.x) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.Euler(0f, angle, 0f);

        // if (FairyTransform == null) return;
        // transform.LookAt(FairyTransform);
        
        if (FairyTransform == null) return;
        
        Vector3 direction = FairyTransform.position - transform.position;
        direction.y = 0f;
        
        if (direction.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(direction);
     
    }
}
