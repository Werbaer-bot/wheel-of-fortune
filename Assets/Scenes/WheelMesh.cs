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

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    void LateUpdate()
    {
        meshFilter.sharedMesh = GenerateCircleMesh();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
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
        circle.SetVertices(vertices);
        circle.SetIndices(indices, MeshTopology.Triangles, 0);
        circle.RecalculateBounds();
        circle.RecalculateNormals();
        return circle;
    }
}
