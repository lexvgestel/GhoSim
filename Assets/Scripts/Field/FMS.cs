using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Util;

public class FMS : MonoBehaviour
{
    //THIS SCRIPT SHOULD BE SEASON GENERIC AND FEATURE NO SEASON SPECIFIC CODE
    public Transform defaultSpawn;
    public int matchTime = 150;
    public int autoTime = 15;
    public float autoDisableTime = 0.5f;
    public int endgameTime = 20;
    public float matchDisabledTime = 3;
    public GameObject[] blueStationCams;
    public GameObject[] redStationCams;
    public static float MatchTimer;
    public static RobotState RobotState;
    public static MatchState MatchState;
    public MatchState state;

    private MatchState previousMatchState;

    private LoadMatch matchLoader;
    private TextMeshProUGUI timer;

    public RobotState robotState;
    


    // Start is called before the first frame update
    void OnEnable()
    {
        if (_activeInstance != null && _activeInstance != this)
        {
            Debug.LogWarning($"Er bestaat al een actief FMS-object ({_activeInstance.gameObject.name}). " +
                              $"Dit tweede object ({gameObject.name}) wordt uitgeschakeld om de klok niet dubbel te laten aftellen.");
            enabled = false;
            return;
        }
        _activeInstance = this;
        Restart();
    }

    private void OnDisable()
    {
        if (_activeInstance == this) _activeInstance = null;
    }

    private static FMS _activeInstance;

    // Update is called once per frame
    void Update()
    {
        state = MatchState;
        robotState = RobotState;
        if (robotState == RobotState.enabled) MatchTimer -= Time.deltaTime;

        MatchState newState;
        if (MatchTimer < 0)
        {
            newState = MatchState.finished;
        }
        else if (MatchTimer <= endgameTime)
        {
            newState = MatchState.endgame;
        }
        else if (MatchTimer >= matchTime - autoTime)
        {
            newState = MatchState.auto;
        }
        else
        {
            newState = MatchState.teleop;
        }

        if (newState != previousMatchState)
        {
            switch (newState)
            {
                case MatchState.teleop:
                    StartCoroutine(wait(autoDisableTime));
                    break;
                case MatchState.finished:
                    StartCoroutine(wait(matchDisabledTime));
                    break;
            }
        }

        MatchState = newState;
        previousMatchState = newState;

        int totalSeconds = Mathf.CeilToInt(Mathf.Max(MatchTimer, 0f));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timer != null)
        {
            timer.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private IEnumerator wait(float time)
    {
        RobotState = RobotState.disabled;
        yield return new WaitForSeconds(time);
        RobotState = RobotState.enabled;
    }

    public void Restart()
    {
        var dispT = GameObject.Find("TimerDisplay");
        if (dispT != null)
        {
            timer = dispT.GetComponent<TextMeshProUGUI>();
        }
        
        matchLoader = Utils.FindParentObjectComponent<LoadMatch>(gameObject);
        matchLoader.setFMS(this);
        MatchTimer = matchTime;
        previousMatchState = MatchState.auto;
        MatchState = MatchState.auto;
        RobotState = RobotState.enabled;
       
    }
}

[Serializable]
public enum RobotState
{
    enabled,
    disabled,
}

[Serializable]
public enum MatchState
{
    auto,
    teleop,
    endgame,
    finished
}