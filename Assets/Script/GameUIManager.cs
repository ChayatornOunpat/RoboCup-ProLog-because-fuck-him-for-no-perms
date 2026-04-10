using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{

    // Singleton code
    public static GameUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }

        Instance = this;
        
        // Optional: Keep this object alive across scene loads
        DontDestroyOnLoad(gameObject); 
    }

    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject scorePanel;
    [SerializeField] TextMeshProUGUI errorMessageText;

    void Start()
    {
        settingsPanel.SetActive(false);
    }
    public void ToggleSettingsPanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void ToggleScorePanel()
    {
        scorePanel.SetActive(!scorePanel.activeSelf);
    }

    public void DisplayErrorMessage(string errorText)
    {
        errorMessageText.text = errorText;
    }
}
