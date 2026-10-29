using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Unity.VisualScripting;

public class InteractiveShadows : MonoBehaviour
{   
    [SerializeField] private Transform shadowTransform;

    [SerializeField] private Transform lightTransform;
    private LightType lightType;

    [SerializeField] private LayerMask targetLayerMask;

    [SerializeField] private Vector3 extrusionDirection = Vector3.zero;

    private Vector3[] objectVertices;

    [SerializeField] private GameObject shadowObject;
    private Mesh shadowColliderMesh;
    private Vector3[] shadowColliderVertices; // same as intersection points
    private MeshCollider shadowCollider;
    private MeshRenderer shadowRenderer; //
    [SerializeField] Material shadowMaterial;


    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private Vector3 previousScale;

    [SerializeField] private float maxRayDistance = 100f;

    private Vector3[] rayStartPoints; // world space
    private Vector3[] rayEndPoints;

    private bool canUpdateCollider = true;

    [SerializeField][Range(0.02f, 1f)] private float shadowColliderUpdateTime = 0.00f; // may not want a delay

    private void Awake()
    {
        InitializeShadowCollider();

        lightType = lightTransform.GetComponent<Light>().type;

        //objectVertices = transform.GetComponent<MeshFilter>().mesh.vertices.Distinct().ToArray(); // distinct points avoids unecessary raycasts
        Mesh sourceMesh = GetComponent<MeshFilter>().sharedMesh;
        objectVertices = sourceMesh.vertices;

        //shadowColliderMesh = new Mesh();
        shadowColliderMesh = shadowObject.GetComponent<MeshFilter>().mesh;
    }

    private void Update()
    {
        shadowTransform.position = transform.position;
    }

    private void FixedUpdate()
    {
        
        // collision detection only occurs during FixedUpdate, so no reason to update the collider every frame

        if (TransformHasChanged() && canUpdateCollider)
        {
            //shadowColliderMesh.vertices = ComputeShadowColliderMeshVertices();
            //shadowCollider.sharedMesh = shadowColliderMesh;
            Invoke("UpdateShadowCollider", shadowColliderUpdateTime);
            canUpdateCollider = false;
        }

        previousPosition = transform.position;
        previousRotation = transform.rotation;
        previousScale = transform.localScale;
    }

    private void InitializeShadowCollider()
    {
        GameObject shadowGameObject = shadowTransform.gameObject;
        shadowGameObject.hideFlags = HideFlags.HideInHierarchy; // hides it from the inspector, doesn’t change functionality
        shadowCollider = shadowGameObject.AddComponent<MeshCollider>();
        shadowCollider.convex = true; // why is this important?
        shadowCollider.isTrigger = true; // depends on what you want the collider to do
    }

    private void UpdateShadowCollider()
    {
        shadowColliderMesh.vertices = ComputeShadowColliderMeshVertices();
        shadowColliderMesh.RecalculateNormals();

        shadowCollider.sharedMesh = shadowColliderMesh;
        canUpdateCollider = true;
    }

    private Vector3[] ComputeShadowColliderMeshVertices()
    {
        int n = objectVertices.Length;

        Vector3[] points = new Vector3[2 * n];
        Vector3[] shadowColliderVertices = new Vector3[n];

        rayStartPoints = new Vector3[n];
        rayEndPoints = new Vector3[n];

        Vector3 raycastDirection = lightTransform.forward; // assuming directional light, but checks later

        for (int i = 0; i < n; i++)
        {
            Vector3 objectPoint = transform.TransformPoint(objectVertices[i]);

            if (lightType != LightType.Directional)
            {
                raycastDirection = objectPoint - lightTransform.position;
            }

            rayStartPoints[i] = objectPoint;
            rayEndPoints[i] = ComputeIntersectionPoint(objectPoint, raycastDirection); // surface points

            shadowColliderVertices[i] = rayStartPoints[i]; // don't have to keep using shadowCollider vertices

            points[i] = rayEndPoints[i];
            points[n + i] = ComputeExtrusionPoint(objectPoint, points[i]);
            //points[i] = shadowTransform.InverseTransformPoint(rayEndPoints[i]);
            //points[n + i] = shadowTransform.InverseTransformPoint(ComputeExtrusionPoint(objectPoint, points[i]));
        }

        return points;
    }

    private Vector3 ComputeIntersectionPoint(Vector3 objectPoint, Vector3 direction)
    {
        RaycastHit hit;

        if (Physics.Raycast(objectPoint, direction, out hit, maxRayDistance, targetLayerMask, QueryTriggerInteraction.Ignore)) 
        {
            return hit.point;
        }

        return objectPoint + direction * maxRayDistance;
    }

    private Vector3 ComputeExtrusionPoint(Vector3 objectPoint, Vector3 intersectionPoints)
    {

        if (extrusionDirection.sqrMagnitude == 0) // square magnitude to save memory since exact magnitude is unnecessary
        {
            return objectPoint;
        }

        return intersectionPoints + extrusionDirection;
    }

    private bool TransformHasChanged()
    {
        return previousPosition != transform.position || previousRotation != transform.rotation || previousScale != transform.localScale;
    }

    private void OnDrawGizmos()
    {
        if (shadowColliderVertices == null || shadowColliderMesh == null || shadowTransform == null)
        {
            //Debug.Log(shadowColliderVertices);
            //Debug.Log(shadowColliderMesh);
            //Debug.Log(shadowTransform);
            return;
        }

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color preiousColor = Gizmos.color;

        Gizmos.matrix = Matrix4x4.identity;

        Vector3[] vertices = shadowColliderMesh.vertices;

        if (vertices.Length >= shadowColliderVertices.Length)
        {
            for (int i = 0; i < shadowColliderVertices.Length; i++)
            {
                Vector3 hit = shadowTransform.TransformPoint(vertices[i]);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(shadowColliderVertices[i], hit);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(hit, 0.05f);
            }
        }

        Gizmos.matrix = shadowTransform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireMesh(shadowColliderMesh);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = preiousColor;
    }
}
