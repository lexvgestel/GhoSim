using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class LookAtRobot : MonoBehaviour
{
    [SerializeField] private new Transform camera;
    [HideInInspector] public int playerIndex = 0;
    private LoadMatch loadMatch;

    private Transform target;
    

    private bool lookTo;
    // Start is called before the first frame update
    void Start()
    {
        lookTo = false;
        loadMatch = FindFirstObjectByType<LoadMatch>();
        if (loadMatch != null)
        {
            var robot = loadMatch.GetRobotLoaded(playerIndex);
            if (robot != null)
            {
                target = robot.transform;
                lookTo = loadMatch.GetTrackingType(playerIndex) == TrackingType.TrackRobot;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (lookTo && target != null)
        {
          camera.LookAt(target);
        }
    }
}