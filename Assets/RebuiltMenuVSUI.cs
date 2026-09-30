using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class RebuiltMenuVSUI : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private Button startRebuiltButton;
    [SerializeField] private Button switchReefscapeButton;
    [SerializeField] private Button switchMultiplayerButton;
    [SerializeField] private Button switchSingleplayerButton;

    
        void Start() {

        quitButton.onClick.AddListener(quitButtonOnClick);
        startRebuiltButton.onClick.AddListener(startRebuiltButtonOnClick);
        switchReefscapeButton.onClick.AddListener(switchReefscapeButtonOnClick);
        switchMultiplayerButton.onClick.AddListener(switchMultiplayerButtonOnClick);
        switchSingleplayerButton.onClick.AddListener(switchSingleplayerButtonOnClick);
    
    }

    private void quitButtonOnClick(){

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
   
    private void startRebuiltButtonOnClick(){

        SceneManager.LoadScene("RebuiltSceneVS");
    }

    private void switchReefscapeButtonOnClick(){

        SceneManager.LoadScene("ReefscapeMenu");
    }

    private void switchMultiplayerButtonOnClick(){

        SceneManager.LoadScene("RebuiltMenuMulti");
    }

    private void switchSingleplayerButtonOnClick(){

        SceneManager.LoadScene("RebuiltMenuSingle");
    }

}
