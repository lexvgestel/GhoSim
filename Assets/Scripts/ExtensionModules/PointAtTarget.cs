using System;
using System.Collections;
using System.Collections.Generic;
using MyBox;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

public class PointAtTarget : MonoBehaviour
{
    [SerializeField] private TargetType targetType;
    
    [SerializeField] private TargetWhen targetWhen;
    
    [SerializeField] private TargetingMethod targetingMethod;

    [Tooltip("Optioneel: gebruik het actieve setpoint van een ander mechanisme (bijv. een bestaande Mode Toggle) in plaats van dit object zelf")]
    [SerializeField] private BuildMechanism drivingMechanism;
    
    [Header("Targeting Settings")]
    [ConditionalField(true, nameof(IsPreset))]
    [SerializeField] private Vector3 targetPosition;

    [ConditionalField(true, nameof(WhenAtSetpoint))] [SerializeField]
    private string SetpointName;
    
    [ConditionalField(true, nameof(IsPreset), true)]
    [SerializeField] private Vector3[] extraTargets;

    [Header("Tuning Settings")]
    [ConditionalField(true, nameof(IsInterpolating), true)] 
    [SerializeField] private float heightOffset;
    [ConditionalField(true, nameof(IsInterpolating), true)] 
    [SerializeField] private float angleOffset;

    [Tooltip("Begrens de berekende hoek, zodat de hood niet doorschiet als je over de middellijn heen gaat")]
    [SerializeField] private bool clampAngle;
    [ConditionalField(nameof(clampAngle))]
    [SerializeField] private float minAngle;
    [ConditionalField(nameof(clampAngle))]
    [SerializeField] private float maxAngle;

    [ConditionalField(true, nameof(IsInterpolating))] 
    [SerializeField] private DistanceValue[] interpolationTable;
    private bool IsPreset() => targetType == TargetType.Preset;
    private bool WhenAtSetpoint() => targetWhen == TargetWhen.AtSetpoint;
    private bool IsInterpolating() => targetingMethod == TargetingMethod.Interpolation;
    
    private List<Vector3> _allTargets;
    
    private DistanceValue[] _sortedCache;
    
    private JointController _controller;

    private bool _lateStartup;
    // Start is called before the first frame update
    void Start()
    {
        InitializeCache();
        var foundTargets = Utils.FindGameObjectsOnLayer("AutoAngleNodes");

        _allTargets = new List<Vector3>();
        
        foreach (var target in foundTargets)
        {
            _allTargets.Add(target.transform.position); 
        }

        foreach (var target in extraTargets)
        {
            _allTargets.Add(target);
        }
        
        _lateStartup = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (_lateStartup)
        {
            _controller = GetComponent<BuildMechanism>().GetController();
            _lateStartup = false;
        }

        var checkController = drivingMechanism ? drivingMechanism.GetController() : _controller;

        bool shouldTarget = targetWhen == TargetWhen.Always || 
                            (targetWhen == TargetWhen.AtSetpoint && checkController &&
                             String.Equals(
                                 (checkController.GetActiveSetpoint() ?? "").ToLower().Trim(), 
                                 SetpointName.ToLower().Trim(), 
                                 StringComparison.OrdinalIgnoreCase));

        if (!shouldTarget) return;

        Vector3 target = GetTargetValue();
    
        float setpointValue;
    
        switch (targetingMethod)
        {
            case TargetingMethod.PointAtOffset:
                setpointValue = CalculateTargetAngle(target) + angleOffset;
                break;
            
            case TargetingMethod.Interpolation:
                Vector3 originPos = transform.position;
                float currentDistance = Vector3.Distance(originPos, target);
                setpointValue = Interpolate(currentDistance) + angleOffset;
                break;
            
            default:
                setpointValue = 0f;
                break;
        }
    
        if (clampAngle)
        {
            setpointValue = Mathf.Clamp(setpointValue, minAngle, maxAngle);
        }

        _controller.OverridePosition(setpointValue);
    }
    
    //runs on editor change
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateTable(interpolationTable);
        }
    }
    
    //Get target
    private Vector3 GetTargetValue()
    {
        switch (targetType)
        {
            case TargetType.Preset:
                return targetPosition;
            case TargetType.Closest:
                return getClosestTarget();
            case TargetType.Furthest:
                return getFurthestTarget();
            case TargetType.Custom:
                return GetClosestCustomTarget();
        }
        
        return Vector3.zero;
    }
    
    private Vector3 GetClosestCustomTarget()
    {
        if (extraTargets.Length == 0) return Vector3.zero;

        float closestDistance = float.MaxValue;
        Vector3 closestTarget = Vector3.zero;
        Vector3 originPos = transform.position;
    
        foreach (var target in extraTargets)
        {
            var distance = Vector3.Distance(originPos, target); 
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }
    
        return closestTarget;
    }

    private Vector3 getClosestTarget()
    {
        float closestDistance = float.MaxValue;
        Vector3 closestTarget = Vector3.zero;
        Vector3 originPos = transform.position;
    
        foreach (var target in _allTargets)
        {
            var distance = Vector3.Distance(originPos, target); 
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }
    
        return closestTarget;
    }

    private Vector3 getFurthestTarget()
    {
        float furthestDistance = float.MinValue;
        Vector3 furthestTarget = Vector3.zero;
        Vector3 originPos = transform.position;
    
        foreach (var target in _allTargets)
        {
            var distance = Vector3.Distance(originPos, target);
            if (distance > furthestDistance)
            {
                furthestDistance = distance;
                furthestTarget = target;
            }
        }
    
        return furthestTarget;
    }

    //direct calculation stuff
    private float CalculateTargetAngle(Vector3 targetPos)
    {
        Transform refPoint = transform;
    
        targetPos -= heightOffset * Vector3.up;

        Vector3 localTarget = refPoint.parent.InverseTransformPoint(targetPos);
      
        float angleRad = Mathf.Atan2(localTarget.y, localTarget.z);
        float angleDeg = angleRad * Mathf.Rad2Deg;

        return angleDeg + angleOffset;
    }
    
    //Interpolation stuff
    private void InitializeCache()
    {
        if (interpolationTable == null || interpolationTable.Length == 0)
        {
            _sortedCache = Array.Empty<DistanceValue>();
            return;
        }

        _sortedCache = new DistanceValue[interpolationTable.Length];
    
        Array.Copy(interpolationTable, _sortedCache, interpolationTable.Length);

        Array.Sort(_sortedCache, new DistanceComparer());
    }
    
    private void UpdateTable(DistanceValue[] newData)
    {
        if (_sortedCache == null || _sortedCache.Length != newData.Length)
        {
            _sortedCache = new DistanceValue[newData.Length];
        }
        
        Array.Copy(newData, _sortedCache, newData.Length);
        Array.Sort(_sortedCache, (a, b) => a.distance.CompareTo(b.distance));
    }

    private float Interpolate(float currentDistance)
    {
        if (_sortedCache == null || _sortedCache.Length == 0) return 0f;

        int index = Array.BinarySearch(_sortedCache, new DistanceValue { distance = currentDistance }, new DistanceComparer());

        if (index >= 0) return _sortedCache[index].value;

        int nextIndex = ~index;

        if (nextIndex == 0) return _sortedCache[0].value;
        if (nextIndex >= _sortedCache.Length) return _sortedCache[_sortedCache.Length - 1].value;
        
        var lower = _sortedCache[nextIndex - 1];
        var upper = _sortedCache[nextIndex];
        float t = (currentDistance - lower.distance) / (upper.distance - lower.distance);
        return Mathf.Lerp(lower.value, upper.value, t);
    }

    [Serializable]
    public struct DistanceValue
    {
        public float distance;
        public float value;
    }
    
    public struct DistanceComparer : System.Collections.Generic.IComparer<DistanceValue>
    {
        public int Compare(DistanceValue x, DistanceValue y) => x.distance.CompareTo(y.distance);
    }
}