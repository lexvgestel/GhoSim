using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class RebuiltMenuSingleUI : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private Button startRebuiltButton;
    [SerializeField] private Button switchReefscapeButton;
    [SerializeField] private Button switchVSButton;
    [SerializeField] private Button switchMultiplayerButton;

    
        void Start() {

        quitButton.onClick.AddListener(quitButtonOnClick);
        startRebuiltButton.onClick.AddListener(startRebuiltButtonOnClick);
        switchReefscapeButton.onClick.AddListener(switchReefscapeButtonOnClick);
        switchVSButton.onClick.AddListener(switchVSButtonOnClick);
        switchMultiplayerButton.onClick.AddListener(switchMultiplayerButtonOnClick);
    
    }

    private void quitButtonOnClick(){

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
   
    private void startRebuiltButtonOnClick(){

        SceneManager.LoadScene("RebuiltSceneSingle");
    }

    private void switchReefscapeButtonOnClick(){

        SceneManager.LoadScene("ReefscapeMenu");
    }

    private void switchVSButtonOnClick(){

        SceneManager.LoadScene("RebuiltMenuVS");
    }

    private void switchMultiplayerButtonOnClick(){

        SceneManager.LoadScene("RebuiltMenuMulti");
    }

}
