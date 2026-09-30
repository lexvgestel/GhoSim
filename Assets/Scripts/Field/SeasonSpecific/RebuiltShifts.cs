using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using Random = System.Random;

public class RebuiltShifts : ScoreOnlyOnce
{
    public static CurrentShift currentShift;
    private bool blueWonAuto;
    private float shiftTimer;
    private float teleopStartMatchTimer;
    private MatchState previousMatchState;

    [SerializeField] private GameObject shiftOnLight;

    // Season-specific match timing
    [Header("Season Match Timing")]
    [SerializeField] private int seasonMatchTime = 160;
    [SerializeField] private int seasonAutoTime = 20;
    [SerializeField] private int seasonEndgameTime = 30;

    // Scoring grace period after the shift light turns off
    [Header("Scoring Grace Period")]
    [SerializeField] private float scoreGracePeriod = 3f;
    private bool wasOnShift;
    private float shiftOffTime = -1f;

    private void Awake()
    {
        var fms = FindObjectOfType<FMS>();
        if (fms != null)
        {
            fms.matchTime = seasonMatchTime;
            fms.autoTime = seasonAutoTime;
            fms.endgameTime = seasonEndgameTime;
        }
    }

    // Update is called once per frame
    private void Start()
    {
        blueWonAuto = false;
        shiftTimer = 0;
        teleopStartMatchTimer = 0f;
        currentShift = CurrentShift.Auto;
        previousMatchState = MatchState.auto;
        FMS.MatchTimer = seasonMatchTime;
        wasOnShift = false;
        shiftOffTime = -1f;
    }

    private new void FixedUpdate()
    {
        bool onShift = isOnShift();

        // Light reflects the real shift state, no delay
        shiftOnLight.SetActive(onShift);

        // Track the moment the light turns off, so we can keep scoring briefly after
        if (onShift)
        {
            wasOnShift = true;
            shiftOffTime = -1f;
        }
        else if (wasOnShift)
        {
            shiftOffTime = Time.time;
            wasOnShift = false;
        }

        bool scoringActive = onShift ||
            (shiftOffTime >= 0f && Time.time - shiftOffTime <= scoreGracePeriod);

        poolOccupyObjects();
        handleShiftState();
        compareObjects(scoringActive);
        ScorePoints(totalScore);
        ShiftOverlay.ShiftTimer = shiftTimer;
        switch (currentShift)
        {
            case CurrentShift.Auto:       ShiftOverlay.ShiftName = "Auto"; break;
            case CurrentShift.Transition: ShiftOverlay.ShiftName = "1/6"; break;
            case CurrentShift.Shift1:     ShiftOverlay.ShiftName = "2/6"; break;
            case CurrentShift.Shift2:     ShiftOverlay.ShiftName = "3/6"; break;
            case CurrentShift.Shift3:     ShiftOverlay.ShiftName = "4/6"; break;
            case CurrentShift.Shift4:     ShiftOverlay.ShiftName = "5/6"; break;
            case CurrentShift.EndGame:    ShiftOverlay.ShiftName = "6/6"; break;
            default:                      ShiftOverlay.ShiftName = ""; break;
        }
    }
    private const float TransitionEnd = 10f;
    private const float Shift1End     = 35f;
    private const float Shift2End     = 60f;
    private const float Shift3End     = 85f;
    private const float Shift4End     = 110f;

    private void handleShiftState()
    {
        if (FMS.MatchState != MatchState.auto && previousMatchState == MatchState.auto)
        {
            blueWonAuto = ScoreHolder.BlueScore > ScoreHolder.RedScore;
            teleopStartMatchTimer = FMS.MatchTimer;
            currentShift = CurrentShift.Transition;
            shiftTimer = TransitionEnd;
        }

        if (FMS.MatchState is not MatchState.auto)
        {
            float teleopElapsed = teleopStartMatchTimer - FMS.MatchTimer;
            if (teleopElapsed < 0f) teleopElapsed = 0f;
            
            CurrentShift resolved;
            float remaining;
            if (teleopElapsed < TransitionEnd)
            {
                resolved = CurrentShift.Transition;
                remaining = TransitionEnd - teleopElapsed;
            }
            else if (teleopElapsed < Shift1End)
            {
                resolved = CurrentShift.Shift1;
                remaining = Shift1End - teleopElapsed;
            }
            else if (teleopElapsed < Shift2End)
            {
                resolved = CurrentShift.Shift2;
                remaining = Shift2End - teleopElapsed;
            }
            else if (teleopElapsed < Shift3End)
            {
                resolved = CurrentShift.Shift3;
                remaining = Shift3End - teleopElapsed;
            }
            else if (teleopElapsed < Shift4End)
            {
                resolved = CurrentShift.Shift4;
                remaining = Shift4End - teleopElapsed;
            }
            else
            {
                resolved = CurrentShift.EndGame;
                // End game runs until the match ends
                remaining = Mathf.Max(FMS.MatchTimer, 0f);
            }

            currentShift = resolved;
            shiftTimer = remaining;
        }

        previousMatchState = FMS.MatchState;
    }

    private bool isOnShift()
    {
        var isBlue = GetIsBlue();
        if (blueWonAuto)
        {
            if (isBlue)
            {
                return currentShift is CurrentShift.Auto or CurrentShift.Transition or CurrentShift.Shift2 or CurrentShift.Shift4 or CurrentShift.EndGame;
            }
            else
            {
                return currentShift is CurrentShift.Auto or CurrentShift.Transition or CurrentShift.Shift1 or CurrentShift.Shift3 or CurrentShift.EndGame;
            }
        }
        else
        {
            if (isBlue)
            {
                return currentShift is CurrentShift.Auto or CurrentShift.Transition or CurrentShift.Shift1 or CurrentShift.Shift3 or CurrentShift.EndGame;
            }
            else
            {
                return currentShift is CurrentShift.Auto or CurrentShift.Transition or CurrentShift.Shift2 or CurrentShift.Shift4 or CurrentShift.EndGame;
            }
        }
    }

    [Serializable]
    public enum CurrentShift
    {
        Auto,
        Transition,
        Shift1,
        Shift2,
        Shift3,
        Shift4,
        EndGame,
    }
}