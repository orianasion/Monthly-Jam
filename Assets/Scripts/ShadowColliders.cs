using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Unity.VisualScripting;

public class ShadowColliders : MonoBehaviour
{   
    //[SerializeField] private Transform shadowTransform;

    [SerializeField] private Transform lightTransform;
    private LightType lightType;

    [SerializeField] private LayerMask targetLayerMask;

    [SerializeField] private Vector3 extrusionDirection = Vector3.zero;

    private Vector3[] objectVertices;

    [SerializeField] private GameObject shadowObject;
    private Mesh shadowColliderMesh;
    private Vector3[] shadowColliderVertices; // same as intersection points
    private MeshCollider shadowCollider;
    private MeshRenderer shadowRenderer;


    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private Vector3 previousScale;

    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private float extendColliderBy = 20f;

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

        //MeshRenderer renderer = shadowObject.GetComponent<MeshRenderer>();
        //renderer.enabled = true;
        //renderer.sharedMaterial = shadowMaterial;
    }

    private void Update()
    {
        //shadowTransform.position = transform.position;
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
        


        //UpdateShadowCollider();
    }

    private void InitializeShadowCollider()
    {
        //GameObject shadowGameObject = shadowTransform.gameObject;
        //shadowObject.hideFlags = HideFlags.HideInHierarchy; // hides it from the inspector, doesn’t change functionality
        shadowObject = new GameObject();
        shadowObject.AddComponent<MeshFilter>();
        shadowColliderMesh = shadowObject.GetComponent<MeshFilter>().mesh;
        shadowCollider = shadowObject.AddComponent<MeshCollider>();
        //shadowCollider = shadowObject.GetComponent<MeshCollider>();
        shadowCollider.convex = true; // why is this important?
        shadowCollider.isTrigger = false;
        shadowCollider.providesContacts = true;
    }

    private void UpdateShadowCollider()
    {
        shadowColliderMesh.vertices = ComputeShadowColliderMeshVertices();
        shadowColliderMesh.RecalculateBounds();
        shadowColliderMesh.RecalculateNormals();

        shadowCollider.sharedMesh = null;
        shadowCollider.sharedMesh = shadowColliderMesh;
        
        canUpdateCollider = true;
    }

    private Vector3[] ComputeShadowColliderMeshVertices()
    {
        int n = objectVertices.Length;

        Vector3[] points = new Vector3[2 * n];
        shadowColliderVertices = new Vector3[n]; /////////////////

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

            //Debug.Log(rayEndPoints[i]);

            shadowColliderVertices[i] = rayStartPoints[i]; // don't have to keep using shadowCollider vertices

            // for level surface colliders --> needs debugging still:(
            //points[i] = shadowObject.transform.InverseTransformPoint(rayEndPoints[i]);
            //points[n + i] = shadowObject.transform.InverseTransformPoint(ComputeExtrusionPoint(objectPoint, points[i]));

            // for colliders which follow the light rays
            points[i] = shadowObject.transform.InverseTransformPoint(rayStartPoints[i]);
            points[n + i] = shadowObject.transform.InverseTransformPoint(rayEndPoints[i]);
        }

        return points;
    }

    private Vector3 ComputeIntersectionPoint(Vector3 objectPoint, Vector3 direction)
    {
        direction.Normalize();
        
        RaycastHit hit;

        if (Physics.Raycast(objectPoint, direction, out hit, maxRayDistance, Physics.AllLayers, QueryTriggerInteraction.Collide)) 
        {
            //return hit.point; // correct intersection point

            return hit.point + direction * extendColliderBy;
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
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color preiousColor = Gizmos.color;

        Gizmos.matrix = Matrix4x4.identity;

        if (rayStartPoints != null && rayEndPoints != null)
        {
            for (int i = 0; i < rayStartPoints.Length; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rayStartPoints[i], rayEndPoints[i]);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(rayEndPoints[i], 0.05f);
            }
        }
        /*
        if (shadowColliderMesh != null && shadowTransform != null)
        {
            Gizmos.matrix = shadowTransform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireMesh(shadowColliderMesh);
        }*/

        Gizmos.matrix = previousMatrix;
        Gizmos.color = preiousColor;
    }
}
