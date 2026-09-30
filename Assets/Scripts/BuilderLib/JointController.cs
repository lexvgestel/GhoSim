using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Util;

public class JointController : MonoBehaviour
{
    /// <summary>
    /// The position for the controller to base its targets off of
    /// </summary>
    public float currentPosition;

    /// <summary>
    /// The joint for the controller to use
    /// </summary>
    public ConfigurableJoint joint;

    /// <summary>
    /// Whether the joint should move in an angular or linear axis (true is angular)
    /// </summary>
    public bool isAngularJoint;

    /// <summary>
    /// Whether the joint should have an angular limit that it must go around
    /// </summary>
    public bool useNoWrap;

    /// <summary>
    /// The angle that the joint cannot cross
    /// </summary>
    public float noWrapAngle;

    /// <summary>
    /// Specifies the Euler axis to control. Must be (1,0,0) (0,1,0) or (0,0,1)
    /// </summary>
    public Vector3 driveAxis;

    /// <summary>
    /// The home position
    /// </summary>
    public float home;

    /// <summary>
    /// Used when another script needs to control the target instead of the passed through setpoints.
    /// </summary>
    public bool follower = false;

    private PlayerInput _playerInput;
    public InputActionMap _inputMap;
    public float _targetPosition;

    private PIDController _pidController;

    private Dictionary<SetPoint, float> originalPositions = new Dictionary<SetPoint, float>();

    private bool _sequenceInterrupted, _isSequenceUsingDelay;
    private float _sequenceTime;
    private string _activeSequenceName;
    private SetPoint _nextSequencePoint;
    private bool _overrideActive;
    private string _activeSetpointName;

    private float _delayedTogglePoint;
    private float _toggleDelayTime;
    private bool _isWaitingForToggle;

    [HideInInspector] public float p;
    [HideInInspector] public float i;
    [HideInInspector] public float d;
    [HideInInspector] public float iSat;
    [HideInInspector] public float max;
    [HideInInspector] public float offset = 0;

    /// <summary>
    /// The setpoint structs that the joint should use.
    /// </summary>
    [HideInInspector] public SetPoint[] setPoints;

    private float _overridePosition;

    //private float _lastUpdateTimestamp;

    // Start is called before the first frame update
    void Start()
    {
        noWrapAngle = Utils.FlipAngle(noWrapAngle);
        noWrapAngle = Mathf.Repeat(noWrapAngle, 360);

        _playerInput = Utils.FindParentObjectComponent<PlayerInput>(gameObject);
        _inputMap = _playerInput.actions.FindActionMap("Robot");
        _targetPosition = 0;

        _inputMap.Enable();

        _sequenceInterrupted = false;
        _isSequenceUsingDelay = false;
        _sequenceTime = 0;
        _activeSequenceName = null;
        _nextSequencePoint = null;
        _overrideActive = false;
        _activeSetpointName = "";

        _delayedTogglePoint = 0;
        _toggleDelayTime = 0;

        _pidController = new PIDController
        {
            proportionalGain = p,
            derivativeGain = d,
            integralGain = i,
            outputMax = max,
            outputMin = -max,
            integralSaturation = iSat
        };

        //_lastUpdateTimestamp = Time.time;
    }

    public string GetActiveSetpoint()
    {
        return _activeSetpointName;
    }

    /// <summary>
    /// Sets the target position for the joint to reach.
    /// </summary>
    /// <param name="targetPosition"></param> The position to go to
    public void FollowPosition(float targetPosition)
    {
        _targetPosition = targetPosition;
    }

    /// <summary>
    /// Overrides the target position of the joint.
    /// Used for keeping the joint at an externally calculated position.
    /// Must be called periodically.
    /// </summary>
    /// <param name="targetPosition"></param> The position to override with
    public void OverridePosition(float targetPosition)
    {
        _targetPosition = targetPosition;
        _overrideActive = true;
        _overridePosition = targetPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_playerInput)
        {
            _playerInput = Utils.FindParentObjectComponent<PlayerInput>(gameObject);
            return;
        }

        if (FMS.RobotState == RobotState.disabled)
        {
            _targetPosition = isAngularJoint ? -currentPosition : currentPosition;
            return;
        }

        if (follower) return;

        if (_sequenceTime > 0)
            _sequenceTime -= Time.deltaTime;
        if (_toggleDelayTime > 0)
            _toggleDelayTime -= Time.deltaTime;


        foreach (var setPoint in setPoints)
        {
            var controllerAction = _inputMap.FindAction(setPoint.controllerButton.ToString());
            var keyboardAction = _inputMap.FindAction(setPoint.keyboardButton.ToString());

            var buttonPressed = false;
            if (controllerAction.triggered)
                if (controllerAction.activeControl?.device is Gamepad)
                    buttonPressed = true;
            if (keyboardAction.triggered)
                if (keyboardAction.activeControl?.device is Keyboard)
                    buttonPressed = true;

            var controllerHeld = controllerAction.IsPressed() && controllerAction.activeControl?.device is Gamepad;
            var keyboardHeld = keyboardAction.IsPressed() && keyboardAction.activeControl?.device is Keyboard;
            var buttonHeld = controllerHeld || keyboardHeld;

            switch (setPoint.controlType)
            {
                case ControlType.Sequence when _isSequenceUsingDelay ? _sequenceTime <= 0 : buttonPressed:

                    if (_sequenceInterrupted)
                    {
                        ResetSequence(false);
                        continue;
                    }

                    if (_nextSequencePoint == null)
                    {
                        if (_activeSequenceName != null)
                            ResetSequence(true);
                        continue;
                    }

                    _activeSetpointName = setPoint.setpointName;

                    if (_nextSequencePoint.sequenceType == SequenceType.end)
                    {
                        if (!_nextSequencePoint.getPersist())
                            _targetPosition = _nextSequencePoint.getPoint();

                        originalPositions.Clear();
                        originalPositions[_nextSequencePoint] = _nextSequencePoint.getPoint();
                        ResetSequence(false);
                        continue;
                    }

                    _targetPosition = _nextSequencePoint.getPoint();

                    switch (setPoint.sequenceType)
                    {
                        case SequenceType.delay:
                            _sequenceTime = setPoint.delay;
                            _isSequenceUsingDelay = true;
                            break;
                        case SequenceType.nextPress:
                            _sequenceTime = 0;
                            _isSequenceUsingDelay = false;
                            break;
                    }

                    foreach (var t in setPoints)
                    {
                        if (t.setpointName != _nextSequencePoint.sequenceTo)
                            continue;
                        _nextSequencePoint = t;
                        return;
                    }

                    _nextSequencePoint = null;

                    break;

                case ControlType.Hold:
                    if (buttonPressed)
                    {
                        _sequenceInterrupted = true;
                        if (!originalPositions.ContainsKey(setPoint))
                        {
                            // Store original position
                            originalPositions.Clear();
                            originalPositions[setPoint] = home;
                            // Apply new position
                            _targetPosition = setPoint.getPoint();
                            _activeSetpointName = setPoint.setpointName;
                        }
                    }
                    else if (originalPositions.ContainsKey(setPoint) && !buttonHeld)
                    {
                        // Restore original position
                        _targetPosition = originalPositions[setPoint];
                        originalPositions.Remove(setPoint);
                        _activeSetpointName = null;
                    }

                    break;

                case ControlType.SequenceStart when buttonPressed:
                    if (_nextSequencePoint == null && _activeSequenceName == null)
                    {
                        _sequenceInterrupted = false;
                        _activeSequenceName = setPoint.setpointName;
                        _activeSetpointName = setPoint.setpointName;

                        switch (setPoint.sequenceType)
                        {
                            case SequenceType.delay:
                                _sequenceTime = setPoint.delay;
                                _isSequenceUsingDelay = true;
                                break;
                            case SequenceType.nextPress:
                                _sequenceTime = 0;
                                _isSequenceUsingDelay = false;
                                break;
                        }

                        if (!setPoint.getPersist())
                            _targetPosition = setPoint.getPoint();

                        foreach (var t in setPoints)
                        {
                            if (t.setpointName != setPoint.sequenceTo) continue;
                            _nextSequencePoint = t;
                            return;
                        }

                        _nextSequencePoint = null;
                        return;
                    }

                    if (_activeSequenceName == setPoint.setpointName)
                    {
                        if (_nextSequencePoint != null &&
                            (_nextSequencePoint.keyboardButton == setPoint.keyboardButton ||
                             _nextSequencePoint.controllerButton == setPoint.controllerButton)
                           ) continue;

                        ResetSequence(true);
                        return;
                    }

                    break;

                case ControlType.Toggle:
                    if (buttonPressed)
                    {
                        _sequenceInterrupted = true;
                        if (!originalPositions.ContainsKey(setPoint))
                        {
                            _toggleDelayTime = setPoint.delay;
                            _delayedTogglePoint = setPoint.getPoint();
                            _isWaitingForToggle = true;
                            originalPositions[setPoint] = setPoint.getPoint();
                            _activeSetpointName = setPoint.setpointName;
                        }
                        else
                        {
                            _targetPosition = home;
                            _delayedTogglePoint = 0;
                            _toggleDelayTime = 0;
                            _isWaitingForToggle = false;
                            originalPositions.Remove(setPoint);
                            _activeSetpointName = null;
                        }
                    }

                    if (_isWaitingForToggle && _toggleDelayTime <= 0)
                    {
                        _isWaitingForToggle = false;
                        _targetPosition = _delayedTogglePoint;
                    }

                    break;
                case ControlType.LastPressed when buttonPressed:
                    _sequenceInterrupted = true;

                    originalPositions.Clear();
                    originalPositions[setPoint] = setPoint.getPoint();

                    _activeSetpointName = setPoint.setpointName;

                    if (!setPoint.getPersist())
                        _targetPosition = setPoint.getPoint();

                    break;
            }
        }

        if (_overrideActive)
        {
            _targetPosition = _overridePosition;
            _overrideActive = false;
        }
    }

    private void FixedUpdate()
    {
        float rawPID;

        currentPosition -= offset;
        if (isAngularJoint)
        {
            float targetForPid = -_targetPosition;

            if (useNoWrap)
            {
                if (PassesThroughWrapAngle(currentPosition, targetForPid, noWrapAngle))
                {
                    float currentAngularOffset = Utils.AngleDifference(_targetPosition, currentPosition);
                    targetForPid = noWrapAngle + (currentAngularOffset > 0 ? 180 : -180);
                }
            }

            rawPID = _pidController.UpdateAngle(Time.fixedDeltaTime, currentPosition, targetForPid);
            joint.targetAngularVelocity = rawPID * driveAxis;
        }
        else
        {
            rawPID = _pidController.UpdateLinear(Time.fixedDeltaTime, currentPosition, _targetPosition);
            joint.targetVelocity = -rawPID * driveAxis;
        }

        //_lastUpdateTimestamp = Time.time;
    }

    private bool PassesThroughWrapAngle(float currentAngle, float targetAngle, float wrapAngle)
    {
        // Normalize all angles to [0, 360)
        currentAngle = Mathf.Repeat(currentAngle, 360);
        targetAngle = Mathf.Repeat(targetAngle, 360);
        wrapAngle = Mathf.Repeat(wrapAngle, 360);

        // Calculate the shortest angular difference
        float angularDifference = targetAngle - currentAngle;
        if (angularDifference > 180) angularDifference -= 360;
        if (angularDifference < -180) angularDifference += 360;

        // Determine the end angle of the motion
        float endAngle = currentAngle + angularDifference;
        endAngle = Mathf.Repeat(endAngle, 360);

        // Check if wrapAngle is between start and end on the shortest path
        if (angularDifference > 0) // Moving counter-clockwise
        {
            if (currentAngle <= endAngle)
                return wrapAngle > currentAngle && wrapAngle < endAngle;
            return wrapAngle > currentAngle || wrapAngle < endAngle;
        }

        // Moving clockwise  
        if (currentAngle >= endAngle)
            return wrapAngle < currentAngle && wrapAngle > endAngle;
        return wrapAngle < currentAngle || wrapAngle > endAngle;
    }

    private void ResetSequence(bool shouldHome)
    {
        Debug.Log("Reset sequence");
        _sequenceInterrupted = false;
        _nextSequencePoint = null;
        _activeSequenceName = null;
        _isSequenceUsingDelay = false;
        _sequenceTime = 0;

        if (shouldHome)
            _targetPosition = home;
    }
}