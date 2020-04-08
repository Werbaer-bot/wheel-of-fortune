using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WheelOfFortune : MonoBehaviour
{
    public class WheelValueEvent : UnityEvent<string> { }

    [Serializable]
    public class WheelSegment
    {
        public string value;
    }

    public enum Mode
    {
        Stopped,
        Running
    }

    [Header("Wheel")]
    public WheelSegment[] wheelSegments;
    public Material[] wheelMaterials;
    public WheelMesh[] wheelMeshes;
    public int wheelParts;
    public float wheelSize;
    public int circleSegmentCount;
    public int circleVertexSize;
    public int circleResolution;
    public Mode wheelMode;
    public WheelValueEvent onValueChanged = new WheelValueEvent();
    [Header("Divider")]
    public GameObject dividerPrefab;
    public GameObject[] dividers;
    [Header("Pointer")]
    public Pointer pointer;
    [Header("UI")]
    public Text partCountText;
    public Text segmentCountText;
    public Text segmentResolutionText;
    public Slider partCountSlider;
    public Slider segmentSlider;
    public InputField segmentValueInput;
    [Header("Effect")]
    public ParticleSystem winParticle;

    private HingeJoint joint;
    private float timeRunning;

    private void Start()
    {
        joint = GetComponent<HingeJoint>();
        UpdatePartCount(wheelSegments.Length);
        onValueChanged.AddListener(OnWheelStopped);
    }



    private void Update()
    {
        switch (wheelMode)
        {
            case Mode.Stopped:
                break;
            case Mode.Running:
                timeRunning += Time.deltaTime;

                if (timeRunning > 1 && joint.velocity >= -1)
                {
                    timeRunning = 0;
                    wheelMode = Mode.Stopped;
                    onValueChanged.Invoke(pointer.currentValue);
                }
                break;
        }
    }

    public void StartMotor()
    {
        joint.useMotor = true;
        wheelMode = Mode.Running;
    }

    public void StopMotor()
    {
        joint.useMotor = false;
    }
    private void OnWheelStopped(string value)
    {
        Debug.Log(value);
        winParticle.Play();
    }

    public void CreateNewSegment(int position)
    {
        //Segment
        GameObject newSegment = new GameObject("WheelMesh");
        WheelMesh wheelMesh = newSegment.AddComponent<WheelMesh>();
        wheelMesh.visibilityRange = wheelSize;
        wheelMesh.circleSegmentCount = circleSegmentCount * circleResolution;
        wheelMesh.circleVertexSize = circleVertexSize * circleResolution;
        wheelMesh.segment = wheelSegments[position];
        wheelMeshes[position] = wheelMesh;
        newSegment.AddComponent<MeshFilter>();
        MeshRenderer wheelRenderer = newSegment.AddComponent<MeshRenderer>();
        wheelRenderer.material = wheelMaterials[position % wheelMaterials.Length];
        MeshCollider wheelCollider = newSegment.AddComponent<MeshCollider>();
        wheelCollider.convex = true;
        wheelCollider.isTrigger = true;

        newSegment.transform.SetParent(transform);
        newSegment.transform.localPosition = Vector3.zero;
        newSegment.transform.localEulerAngles = new Vector3(-180, position * (360 / (circleSegmentCount / circleVertexSize)), 0);
        newSegment.transform.localScale = Vector3.one;

        //Text
        GameObject newTextObject = new GameObject("SegmentText: " + position);
        TextMesh textMesh = newTextObject.AddComponent<TextMesh>();
        textMesh.text = wheelSegments[position].value != "" ? wheelSegments[position].value : (position + 1).ToString();
        textMesh.color = Color.black;
        textMesh.fontSize = 200;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        newTextObject.transform.eulerAngles = new Vector3(0, (360 / (wheelParts)), 0);
        newTextObject.transform.SetParent(newSegment.transform);
        newTextObject.transform.localScale = Vector3.one * 0.01f;
        newTextObject.transform.localPosition = -Vector3.forward * (wheelSize / 2);

        //Divider
        GameObject newDivider = Instantiate(dividerPrefab, transform);
        newDivider.transform.localEulerAngles = new Vector3(0, position * (360 / (circleSegmentCount / circleVertexSize)), 0);
        dividers[position] = newDivider;
    }

    public void UpdatePartCount(float count)
    {
        partCountText.text = "Part Count: " + count;
        wheelParts = (int)count;

        partCountSlider.maxValue = (circleSegmentCount / circleVertexSize);
        partCountSlider.SetValueWithoutNotify(count);
        segmentSlider.minValue = circleVertexSize;

        RebuildWheel();
    }

    public void UpdateSegmentCount(float count)
    {
        segmentCountText.text = "Segment Count: " + count;
        circleSegmentCount = (int)count;
        segmentSlider.SetValueWithoutNotify(count);
        RebuildWheel();
    }

    public void UpdateSegmentResolution(float resolution)
    {
        segmentResolutionText.text = "Segment Resolution: " + resolution;
        circleResolution = (int)resolution;
        RebuildWheel();
    }

    public void CreateWheelSegment()
    {
        Array.Resize(ref wheelSegments, wheelSegments.Length + 1);
        wheelSegments[wheelSegments.Length - 1] = new WheelSegment
        {
            value = !string.IsNullOrEmpty(segmentValueInput.text) ? segmentValueInput.text : (wheelSegments.Length).ToString()
        };

        UpdatePartCount(wheelSegments.Length);
        UpdateSegmentCount(wheelSegments.Length);
    }

    public void DeleteAllSegments()
    {
        foreach (var wheelMesh in wheelMeshes)
        {
            Destroy(wheelMesh.gameObject);
        }

        foreach (var divider in dividers)
        {
            Destroy(divider);
        }

        dividers = Array.Empty<GameObject>();
        wheelMeshes = Array.Empty<WheelMesh>();
        wheelSegments = Array.Empty<WheelSegment>();

        UpdatePartCount(0);
        UpdateSegmentCount(0);
    }

    private void RebuildWheel()
    {
        foreach (var wheelMesh in wheelMeshes)
        {
            Destroy(wheelMesh.gameObject);
        }

        foreach (var divider in dividers)
        {
            Destroy(divider);
        }

        dividers = new GameObject[wheelParts];
        wheelMeshes = new WheelMesh[wheelParts];
        for (int i = 0; i < wheelParts; i++)
        {
            CreateNewSegment(i);
        }
    }
}
