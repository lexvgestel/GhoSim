using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LeaveRebuilt : MonoBehaviour
{
    public InputActionProperty escape;
    void Start()
    {
        escape.action.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (escape.action.WasPressedThisFrame())
        {
            SceneManager.LoadScene("RebuiltMenuSingle");
        }
    }
}
