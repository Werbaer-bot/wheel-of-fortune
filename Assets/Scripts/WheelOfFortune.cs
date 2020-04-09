using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WheelOfFortune : MonoBehaviour
{
    public class WheelValueEvent : UnityEvent<WheelSegment> { }

    [Serializable]
    public class WheelSegment
    {
        public int id;
        public string value;
    }

    public enum Mode
    {
        Stopped,
        Running
    }

    [Header("Wheel")]
    public List<WheelSegment> wheelSegments = new List<WheelSegment>();
    public Material[] wheelMaterials;
    public WheelMesh[] wheelMeshes;
    public float wheelSize;
    public int circleSegmentCount;
    public int circleVertexSize;
    public int circleResolution;
    public Mode wheelMode;
    public WheelValueEvent onValueChanged = new WheelValueEvent();
    public bool removeWinners;
    [Header("Divider")]
    public GameObject dividerPrefab;
    public GameObject[] dividers;
    [Header("Pointer")]
    public Pointer pointer;
    [Header("UI")]
    public int fontSize;
    public Text partCountText;
    public Text segmentCountText;
    public Text segmentResolutionText;
    public Text WinnerText;
    public Slider partCountSlider;
    public Slider segmentSlider;
    public InputField segmentValueInput;
    public Toggle removeWinnerToggle;
    [Header("Effect")]
    public ParticleSystem winParticle;
    [Header("Audio")]
    public GameObject audioSourcePrefab;
    public AudioClip hitDividerClip;
    public AudioClip wheelStepClip;
    public float wheelStepSize;
    public AudioClip winClip;

    private HingeJoint joint;
    private float lastJointAngle;
    private float timeRunning;

    public int wheelParts { get => wheelSegments.Count; }

    private void Start()
    {
        joint = GetComponent<HingeJoint>();
        lastJointAngle = joint.angle;
        UpdatePartCount(wheelSegments.Count);
        onValueChanged.AddListener(OnWheelStopped);
    }

    private void Update()
    {
        switch (wheelMode)
        {
            case Mode.Stopped:
                break;
            case Mode.Running:
                if (Mathf.Abs(joint.angle - lastJointAngle) > wheelStepSize)
                {
                    lastJointAngle = joint.angle;
                    PlayWheelStep();
                }

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
        WinnerText.text = "";
    }

    public void StopMotor()
    {
        joint.useMotor = false;
    }

    private void OnWheelStopped(WheelSegment segment)
    {
        Debug.Log(segment.value);

        PlayWin();
        WinnerText.text = segment.value;
        winParticle.Play();

        if (removeWinners)
        {
            wheelSegments.Remove(segment);
            UpdateSegmentCount(wheelSegments.Count);
            RebuildWheel();
        }
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

        newSegment.AddComponent<MeshCollider>();
        newSegment.AddComponent<Rigidbody>().isKinematic = true;

        newSegment.transform.SetParent(transform);
        newSegment.transform.localPosition = Vector3.zero;
        newSegment.transform.localEulerAngles = new Vector3(-180, position * (360 / (circleSegmentCount / circleVertexSize)), 0);
        newSegment.transform.localScale = Vector3.one;

        //Text
        GameObject newTextObject = new GameObject("SegmentText: " + position);

        TextMesh textMesh = newTextObject.AddComponent<TextMesh>();
        textMesh.text = !string.IsNullOrEmpty(wheelSegments[position].value) ? wheelSegments[position].value : (position + 1).ToString();
        textMesh.color = Color.black;
        textMesh.fontSize = fontSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        newTextObject.transform.SetParent(newSegment.transform);
        newTextObject.transform.localScale = Vector3.one * 0.01f;

        //Divider
        GameObject newDivider = Instantiate(dividerPrefab, transform);
        newDivider.transform.localEulerAngles = new Vector3(0, position * (360 / (circleSegmentCount / circleVertexSize)), 0);
        dividers[position] = newDivider;
    }

    public void UpdatePartCount(float count)
    {
        partCountText.text = "Part Count: " + count;
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

    public void UpdateRemoveWinners(bool toggle)
    {
        removeWinners = toggle;
    }

    public void CreateWheelSegment()
    {
        wheelSegments.Add(new WheelSegment
        {
            id = wheelSegments.Count - 1,
            value = !string.IsNullOrEmpty(segmentValueInput.text) ? segmentValueInput.text : (wheelSegments.Count).ToString()
        });

        UpdatePartCount(wheelSegments.Count);
        UpdateSegmentCount(wheelSegments.Count);
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
        wheelSegments = new List<WheelSegment>();

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

    public void PlayHitDivider(Transform divider)
    {
        PlayOneShot(hitDividerClip, divider, 1);
    }

    public void PlayWin()
    {
        PlayOneShot(winClip, transform, 1);
    }

    public void PlayWheelStep()
    {
        PlayOneShot(wheelStepClip, transform, 1);
    }

    public void PlayOneShot(AudioClip clip, Transform point, float volume)
    {
        if (clip)
        {
            GameObject tmpAudioSource = Instantiate(audioSourcePrefab, point.position, point.rotation);
            tmpAudioSource.GetComponent<AudioSource>().PlayOneShot(clip, volume);
            Destroy(tmpAudioSource, clip.length);
        }
    }
}
