using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelMesh : MonoBehaviour
{
    public float visibilityRange;
    public int circleSegmentCount = 64;
    public int circleVertexSize;
    public WheelOfFortune.WheelSegment segment;

    private int circleIndexCount;
    private int circleVertexCount;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private TextMesh textMesh;
    private Vector3 center;
    private Vector3 centerOuterPoints;
    private GameObject centerObject;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        textMesh = GetComponentInChildren<TextMesh>();

        centerObject = new GameObject("Center");
        centerObject.transform.SetParent(transform);
        UpdateMeshes();
    }

    void LateUpdate()
    {
        UpdateMeshes();
    }

    private void UpdateMeshes()
    {
        meshFilter.sharedMesh = GenerateCircleMesh();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        textMesh.transform.position = center;
        textMesh.transform.LookAt(centerOuterPoints, textMesh.transform.up);
        textMesh.transform.localEulerAngles = new Vector3(90, textMesh.transform.localEulerAngles.y, textMesh.transform.localEulerAngles.z);

        centerObject.transform.position = center;
        centerObject.transform.LookAt(centerOuterPoints, centerObject.transform.up);
        centerObject.transform.localEulerAngles = new Vector3(90, centerObject.transform.localEulerAngles.y, centerObject.transform.localEulerAngles.z);
    }

    private Mesh GenerateCircleMesh()
    {
        circleVertexCount = circleSegmentCount + 2;
        circleIndexCount = circleSegmentCount * 3;

        var circle = new Mesh();
        var vertices = new List<Vector3>(circleVertexCount);
        var indices = new int[circleIndexCount];
        var segmentWidth = Mathf.PI * 2f / circleSegmentCount;
        var angle = 0f;
        vertices.Add(Vector3.zero);
        for (int i = 1; i < circleVertexSize + 2; ++i)
        {
            Vector3 nextCirclePoint = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices.Add(nextCirclePoint * visibilityRange);

            angle -= segmentWidth;
            if (i > 1)
            {
                var j = (i - 2) * 3;
                indices[j + 0] = 0;
                indices[j + 1] = i - 1;
                indices[j + 2] = i;
            }
        }

        Vector3 totalOffset = new Vector3();
        totalOffset += transform.TransformPoint(vertices[0]);
        totalOffset += transform.TransformPoint(vertices[1]);
        totalOffset += transform.TransformPoint(vertices[vertices.Count - 1]);

        center = totalOffset / 3;

        totalOffset = new Vector3();
        totalOffset += transform.TransformPoint(vertices[1]);
        totalOffset += transform.TransformPoint(vertices[vertices.Count - 1]);
        centerOuterPoints = totalOffset / 2;

        circle.SetVertices(vertices);
        circle.SetIndices(indices, MeshTopology.Triangles, 0);
        circle.RecalculateBounds();
        circle.RecalculateNormals();
        return circle;
    }
}
