using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ReefscapeMenuUI : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private Button startReefscapeButton;
    [SerializeField] private Button switchRebuiltButton;
    
        void Start() {

        quitButton.onClick.AddListener(quitButtonOnClick);
        startReefscapeButton.onClick.AddListener(startReefscapeButtonOnClick);
        switchRebuiltButton.onClick.AddListener(switchRebuiltButtonOnClick);
    }

    private void quitButtonOnClick(){

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
   
    private void startReefscapeButtonOnClick(){

        SceneManager.LoadScene("ReefscapeScene");
    }
    private void switchRebuiltButtonOnClick(){

        SceneManager.LoadScene("RebuiltMenuSingle");
    }
}