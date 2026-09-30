using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class GamePiece : MonoBehaviour
{
    public PieceNames pieceType;
    public Transform owner;
    public Rigidbody rb;
    public GamePieceState state;
    public GameObject colliderParent;
    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public Transform originalParent;
    [HideInInspector] public float startingDistance;
    private bool hasId;

    private void Start()
    {
        hasId = false;

        // Probeer originalParent zo vroeg mogelijk te zetten, zodat andere
        // systemen (intake/outake) hier nooit op moeten wachten en de piece
        // nooit zonder geldige parent kan komen te zitten.
        TryAssignOriginalParent();
    }

    private void Update()
    {
        if (hasId) return;

        // Fallback: als Start() de LoadMatch-parent nog niet kon vinden
        // (bijv. door script execution order), blijf het elke frame proberen.
        TryAssignOriginalParent();
    }

    private void TryAssignOriginalParent()
    {
        if (!rb) rb = GetComponent<Rigidbody>();

        var core = Utils.FindParentObjectComponent<LoadMatch>(gameObject);
        if (!core || !core.getFieldHolder()) return;

        var returnTo = core.getFieldHolder().transform.GetChild(0);
        originalParent = returnTo;
        hasId = true;
    }
}