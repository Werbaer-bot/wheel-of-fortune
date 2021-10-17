using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class User : MonoBehaviour
{
    public TMP_InputField userNameInput;
    public int Id { get => mySegment.id; }
    public string UserName { get => mySegment.value; }

    private WheelOfFortune.WheelSegment mySegment;
    private WheelOfFortune myWheel;

    void Start()
    {
        userNameInput.onValueChanged.AddListener(UpdateSegmentValue);
    }

    void Update()
    {
        
    }

    private void UpdateSegmentValue(string value)
    {
        mySegment.value = value;
        myWheel.UpdateUser(this);
    }

    public void ShowSegment(WheelOfFortune.WheelSegment segment, WheelOfFortune wheel)
    {
        mySegment = segment;
        myWheel = wheel;
        userNameInput.text = mySegment.value;
    }

    public void RemoveSegment()
    {
        myWheel.RemoveUser(this);
    }
}
