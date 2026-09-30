using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class GameUI : MonoBehaviour
{
    [SerializeField] protected GameObject gameUI;
    protected FMS fms;
    protected GameObject UI;
    protected GameObject spawnedUI;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitAndInit());
    }

    private IEnumerator WaitAndInit()
    {
        GameObject fieldHolder = null;
        FMS foundFms = null;

        // Wacht tot LoadMatch klaar is met het aanmaken van FieldHolder + FMS
        while (foundFms == null)
        {
            fieldHolder = GameObject.Find("FieldHolder");
            if (fieldHolder != null)
            {
                foundFms = fieldHolder.GetComponentInChildren<FMS>();
            }
            if (foundFms == null) yield return null;
        }

        UI = GameObject.Find("GameUi");
        fms = foundFms;
        spawnedUI = Instantiate(gameUI, UI.transform.GetChild(0));
        Init();
    }

    protected virtual void Init()
    {
        
    }
}
