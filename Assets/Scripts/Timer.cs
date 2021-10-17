using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Michsky.UI.ModernUIPack;

public class Timer : MonoBehaviour
{
    public float timerMax;
    public ModalWindowManager popup;
    public UnityEvent timerFinished = new UnityEvent();

    private float timer;
    private bool isRunning;

    void Start()
    {
        timer = timerMax;
    }

    void Update()
    {
        if (isRunning)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                timer = 0;
                isRunning = false;
                timerFinished.Invoke();
            }
        }

        int minutes = Mathf.FloorToInt(timer / 60F);
        int seconds = Mathf.FloorToInt(timer - minutes * 60);
        string niceTime = string.Format("{0:0}:{1:00}", minutes, seconds);

        popup.descriptionText = niceTime;
        popup.UpdateUI();
    }

    public void StartTimer()
    {
        timer = timerMax;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }
}
