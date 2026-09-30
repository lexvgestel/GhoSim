using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuUI : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private Button quitButton;

    [Header("Rebuilt buttons")]
    [SerializeField] private Button switchReefscapeButton;  
    

    [SerializeField] private Button startRebuiltSingleButton;
    [SerializeField] private Button startRebuiltVSButton;
    [SerializeField] private Button startRebuiltMultiButton;

    [SerializeField] private Button switchSingleplayerButton;
    [SerializeField] private Button switchVSButton;         
    [SerializeField] private Button switchMultiplayerButton; 
    
    [Header("Reefscape buttons")]
    [SerializeField] private Button switchRebuiltButton;

    [SerializeField] private Button startReefscapeBlinkyButton;  
    [SerializeField] private Button startReefscapeBlinkyEVO1Button; 
    [SerializeField] private Button startReefscapeBlinkyEVO2Button; 

    [SerializeField] private Button switchBlinkyButton;
    [SerializeField] private Button switchBlinkyEVO1Button;         
    [SerializeField] private Button switchBlinkyEVO2Button; 

    void Start()
    {
        
        quitButton.onClick.AddListener(QuitButtonOnClick);

        if (switchRebuiltButton != null)
            switchRebuiltButton.onClick.AddListener(() => LoadScene("RebuiltMenuSingle"));

        if (startRebuiltSingleButton != null)
            startRebuiltSingleButton.onClick.AddListener(() => LoadScene("RebuiltSceneSingle"));

        if (startRebuiltVSButton != null)
            startRebuiltVSButton.onClick.AddListener(() => LoadScene("RebuiltSceneVS"));

        if (startRebuiltMultiButton != null)
            startRebuiltMultiButton.onClick.AddListener(() => LoadScene("RebuiltSceneMulti"));

        if (switchSingleplayerButton != null)
            switchSingleplayerButton.onClick.AddListener(() => LoadScene("RebuiltMenuSingle"));

        if (switchVSButton != null)
            switchVSButton.onClick.AddListener(() => LoadScene("RebuiltMenuVS"));

        if (switchMultiplayerButton != null)
            switchMultiplayerButton.onClick.AddListener(() => LoadScene("RebuiltMenuMulti"));
        
        if (switchReefscapeButton != null)
            switchReefscapeButton.onClick.AddListener(() => LoadScene("ReefscapeMenuBlinky"));
        
        if (startReefscapeBlinkyButton != null)
            startReefscapeBlinkyButton.onClick.AddListener(() => LoadScene("ReefscapeSceneBlinky"));

        if (startReefscapeBlinkyEVO1Button != null)
            startReefscapeBlinkyEVO1Button.onClick.AddListener(() => LoadScene("ReefscapeSceneBlinkyEVO1"));

        if (startReefscapeBlinkyEVO2Button != null)
            startReefscapeBlinkyEVO2Button.onClick.AddListener(() => LoadScene("ReefscapeSceneBlinkyEVO2"));
        
        if (switchBlinkyButton != null)
            switchBlinkyButton.onClick.AddListener(() => LoadScene("ReefscapeMenuBlinky"));

        if (switchBlinkyEVO1Button != null)
            switchBlinkyEVO1Button.onClick.AddListener(() => LoadScene("ReefscapeMenuBlinkyEVO1"));

        if (switchBlinkyEVO2Button != null)
            switchBlinkyEVO2Button.onClick.AddListener(() => LoadScene("ReefscapeMenuBlinkyEVO2"));

    }

    private void QuitButtonOnClick()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"MenuUI on '{gameObject.name}': a button was clicked but has no scene name set.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

}