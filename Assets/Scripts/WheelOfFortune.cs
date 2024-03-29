﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.ModernUIPack;

public class WheelOfFortune : MonoBehaviour
{
    public class WheelValueEvent : UnityEvent<WheelSegment> { }

    [Serializable]
    public class WheelSegment
    {
        public int id;
        public string value;
        public GameObject gameObject;
        public Canvas canvas;
    }

    public enum Mode
    {
        Stopped,
        Running
    }

    [Header("Wheel")]
    public List<User> users = new List<User>();
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
    public float autoStopTimer;
    [Header("Divider")]
    public GameObject dividerPrefab;
    public GameObject[] dividers;
    [Header("Pointer")]
    public Pointer pointer;
    [Header("Timer")]
    public Timer timer;
    [Header("UI")]
    public Text partCountText;
    public Slider segmentSlider;
    public TMP_InputField segmentValueInput;
    public ModalWindowManager winnerWindow;
    public Toggle removeWinnerToggle;
    public Toggle autoStopToggle;
    public Slider chargeSlider;
    public Button startButton;
    public Button stopButton;
    public GameObject wheelSegmentCanvasPrefab;
    public GameObject wheelUserListPrefab;
    public Transform wheelUserListParent;
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
    private WheelSegment lastWinner;
    private JointMotor motor;
    private float motorVelocity;
    private float motorForce;
    private bool mouseWasNotOnUI;

    public int wheelParts { get => wheelSegments.Count; }

    #region ---MONO---
    private void Start()
    {
        joint = GetComponent<HingeJoint>();
        motor = joint.motor;
        lastJointAngle = joint.angle;
        UpdatePartCount(wheelSegments.Count);
        UpdateSegmentCount(wheelSegments.Count);
        UpdateSegmentResolution(circleResolution);
        LoadUsers();
        RebuildUsers();
        onValueChanged.AddListener(OnWheelStopped);
    }

    private void Update()
    {
        UpdateForceInput();

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
    #endregion

    #region ---INPUT---
    private void UpdateForceInput()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            chargeSlider.gameObject.SetActive(true);
            chargeSlider.value = 0;
            mouseWasNotOnUI = true;
        }

        if (Input.GetMouseButton(0))
        {
            float chargeMlp = 10;
            chargeSlider.value += Time.deltaTime * chargeMlp;

            chargeSlider.GetComponent<RectTransform>().anchoredPosition = Input.mousePosition;
            chargeSlider.fillRect.GetComponent<Image>().color = Color.Lerp(Color.yellow, Color.red, chargeSlider.value / chargeSlider.maxValue);
        }

        if (Input.GetMouseButtonUp(0))
        {
            chargeSlider.gameObject.SetActive(false);

            if (!EventSystem.current.IsPointerOverGameObject() && mouseWasNotOnUI)
            {
                StartMotor();
                motorForce += chargeSlider.value;
                motorVelocity -= chargeSlider.value * 15;
                mouseWasNotOnUI = false;
            }
        }

        if (wheelMode == Mode.Running)
        {
            motor.targetVelocity = motorVelocity;
            motor.force = motorForce;
            joint.motor = motor;
        }
    }
    #endregion

    #region ---MOTOR---
    public void StartMotor()
    {
        timer.StopTimer();
        startButton.interactable = false;
        stopButton.interactable = true;
        joint.useMotor = true;
        motorVelocity -= 100;
        motorForce += 5;

        wheelMode = Mode.Running;
        UpdateSegmentCount(wheelSegments.Count);

        if (autoStopToggle.isOn)
        {
            LeanTween.cancel(gameObject);

            LeanTween.delayedCall(gameObject, autoStopTimer, () =>
           {
               StopMotor();
           });
        }
    }

    public void StopMotor()
    {
        timer.StopTimer();
        LeanTween.cancel(gameObject);
        startButton.interactable = true;
        stopButton.interactable = false;
        motorVelocity = 0;
        motorForce = 0;
        joint.useMotor = false;
    }
    #endregion

    #region ---EVENTS---
    private void OnWheelStopped(WheelSegment segment)
    {
        Debug.Log(segment.value);

        timer.StartTimer();

        PlayWin();
        winnerWindow.titleText = segment.value;
        //winnerWindow.descriptionText = "is a winner!";
        winnerWindow.UpdateUI();
        winnerWindow.OpenWindow();
        winParticle.Play();
        lastWinner = segment;

        if (removeWinners)
        {
            lastWinner.gameObject.GetComponent<WheelMesh>().updateMeshes = false;

            //LeanTween.move(lastWinner.canvas.gameObject, new Vector3(0, 2, -3), 1)
            //    .setOnStart(() =>
            //    {
            //        LeanTween.rotate(lastWinner.canvas.gameObject, new Vector3(0, 0, 0), 1);
            //    });

            wheelSegments.Remove(lastWinner);
            UpdateSegmentCount(wheelSegments.Count);
        }
    }
    #endregion

    #region ---UI---
    public void UpdatePartCount(float count)
    {
        partCountText.text = "Part Count " + count;
        segmentSlider.minValue = circleVertexSize;

        RebuildWheel();
    }

    public void UpdateSegmentCount(float count)
    {
        circleSegmentCount = (int)count;
        segmentSlider.SetValueWithoutNotify(count);
        RebuildWheel();
    }

    public void UpdateSegmentResolution(float resolution)
    {
        circleResolution = (int)resolution;
        RebuildWheel();
    }

    public void UpdateAutoStop(bool toggle)
    {
        LeanTween.cancel(gameObject);

        if (toggle && wheelMode == Mode.Running)
        {
            LeanTween.delayedCall(gameObject, autoStopTimer, () =>
            {
                StopMotor();
            });
        }
    }

    public void UpdateRemoveWinners(bool toggle)
    {
        removeWinners = toggle;
    }
    #endregion

    #region ---SEGMENTS---
    public void CreateNewSegment(int position)
    {
        //Segment
        GameObject newSegment = new GameObject("WheelMesh");
        WheelSegment newWheelSegment = wheelSegments[position];

        WheelMesh wheelMesh = newSegment.AddComponent<WheelMesh>();
        wheelMesh.visibilityRange = wheelSize;
        wheelMesh.circleSegmentCount = circleSegmentCount * circleResolution;
        wheelMesh.circleVertexSize = circleVertexSize * circleResolution;
        newWheelSegment.gameObject = newSegment;
        wheelMeshes[position] = wheelMesh;

        newSegment.AddComponent<MeshFilter>();
        MeshRenderer wheelRenderer = newSegment.AddComponent<MeshRenderer>();
        wheelRenderer.material = wheelMaterials[position % wheelMaterials.Length];

        newSegment.AddComponent<MeshCollider>();
        newSegment.AddComponent<Rigidbody>().isKinematic = true;

        newSegment.transform.SetParent(transform);
        newSegment.transform.localPosition = Vector3.zero;
        newSegment.transform.localEulerAngles = new Vector3(-180, position * (360f / (circleSegmentCount / circleVertexSize)), 0);
        newSegment.transform.localScale = Vector3.one;

        //Text
        GameObject wheelSegmentCanvas = Instantiate(wheelSegmentCanvasPrefab, newSegment.transform);
        newWheelSegment.canvas = wheelSegmentCanvas.GetComponent<Canvas>();
        wheelSegmentCanvas.name = "SegmentCanvas: " + position;
        Text segmentText = wheelSegmentCanvas.GetComponentInChildren<Text>();
        segmentText.text = !string.IsNullOrEmpty(wheelSegments[position].value) ? wheelSegments[position].value : (position + 1).ToString();
        wheelSegmentCanvas.transform.localScale = Vector3.one * 0.01f;

        //Divider
        GameObject newDivider = Instantiate(dividerPrefab, transform);
        newDivider.transform.localEulerAngles = new Vector3(0, position * (360f / (circleSegmentCount / circleVertexSize)), 0);
        dividers[position] = newDivider;

        wheelMesh.segment = newWheelSegment;
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
        RebuildUsers();
        segmentValueInput.text = "";
    }

    public void RestartUserSegments()
    {
        RebuildWheel();
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
        RebuildUsers();
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
    #endregion


    #region ---USERS---
    private void RebuildUsers()
    {
        foreach (Transform item in wheelUserListParent)
        {
            Destroy(item.gameObject);
        }

        users.Clear();
        foreach (var segment in wheelSegments)
        {
            User tmpUser = Instantiate(wheelUserListPrefab, wheelUserListParent).GetComponent<User>();
            tmpUser.ShowSegment(segment, this);
            users.Add(tmpUser);
        }
    }

    public void UpdateUser(User user)
    {
        wheelSegments[user.Id].value = user.UserName;
        RebuildWheel();
    }

    public void RemoveUser(User user)
    {
        users.Remove(user);
        wheelSegments.Remove(wheelSegments.Single((WheelSegment s) => s.value == user.UserName));
        UpdateSegmentCount(wheelSegments.Count);
        RebuildWheel();
        RebuildUsers();
    }

    private void SaveUsers()
    {
        PlayerPrefs.SetInt("UserCount", users.Count);

        for (int i = 0; i < users.Count; i++)
        {
            PlayerPrefs.SetString($"User{i}", users[i].UserName);

        }
    }

    private void LoadUsers()
    {
        for (int i = 0; i < PlayerPrefs.GetInt("UserCount", 0); i++)
        {
            segmentValueInput.text = PlayerPrefs.GetString($"User{i}", "invalid");
            CreateWheelSegment();
        }
        segmentValueInput.text = "";
    }
    #endregion

    #region ---AUDIO---
    public void PlayHitDivider(Transform divider)
    {
        PlayOneShot(hitDividerClip, divider, 0.5f);
    }

    public void PlayWin()
    {
        PlayOneShot(winClip, transform, 1);
    }

    public void PlayWheelStep()
    {
        PlayOneShot(wheelStepClip, transform, 0.1f);
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
    #endregion
}
