using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;



[System.Serializable]
public class PlayerData
{
    public string name;
    public string team;
    public float x;
    public float y;
}

[System.Serializable]
public class BallData
{
    public float x;
    public float y;
    public float vx;
    public float vy;
}

[System.Serializable]
public class ScoreData
{
    public int teamA;
    public int teamB;
}

[System.Serializable]
public class EventData
{
    public string type;
    public string team;
}

[System.Serializable]
public class GameState
{
    public BallData ball;
    public PlayerData[] players;
    public EventData[] events;
    public ScoreData score;
}

public class PrologClient : MonoBehaviour
{

    [SerializeField] GameObject ballPrefab;
    private GameObject ballObject;
    public GameObject playerPrefab;
    private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
    Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();
    Vector3 ballTarget; // for smooth unity rendering
    [SerializeField] float secondsToWaitBeforeNextFrame = 0.4f;
    [SerializeField] float kickoffHoldSeconds = 1.5f; // extra pause so turn 1 / turn 16 layouts are visible
    bool holdNextFrame = false; // set when the current step is kickoff (game start or half-time)
    [SerializeField] Toggle interpolationToggle; // frames/positions of players/ball from prolog will be used to render WITHOUT unity smoothing movement rendering

    [SerializeField] Sprite teamACharacter;
    [SerializeField] Sprite teamBCharacter;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] GameObject LogText;
    [SerializeField] GameObject gameOverPanel;

    [Header("Settings UI")]
    [SerializeField] Button modeToggleButton;
    [SerializeField] TextMeshProUGUI modeLabelText;
    [SerializeField] TMP_Dropdown strategyDropdown;

    string url = "http://localhost:5000/action";
    bool gameOver = false;

    // Applied on next reset; see ApplyPendingModeAndReset.
    string pendingMode = "ai_vs_ai";

    // Dropdown labels and aggression values sent to Prolog.
    static readonly string[] StrategyLabels = { "Defensive", "Balanced", "Attacking" };
    static readonly int[] StrategyAggressions = { 15, 50, 85 };



    void Start()
    {
        SetupSettingsUI();
        ResetGame();
        ShowLogText("Game has started!");
        interpolationToggle.isOn = true;
        // StartCoroutine(GameLoop());
    }

    void SetupSettingsUI()
    {
        if (modeToggleButton != null)
        {
            modeToggleButton.onClick.RemoveAllListeners();
            modeToggleButton.onClick.AddListener(OnToggleMode);
        }
        UpdateModeLabel();

        if (strategyDropdown != null)
        {
            strategyDropdown.ClearOptions();
            strategyDropdown.AddOptions(new List<string>(StrategyLabels));
            strategyDropdown.value = 1; // Balanced
            strategyDropdown.RefreshShownValue();
            strategyDropdown.onValueChanged.RemoveAllListeners();
            strategyDropdown.onValueChanged.AddListener(OnStrategyChanged);
        }
    }

    public void OnToggleMode()
    {
        pendingMode = pendingMode == "ai_vs_ai" ? "slider_vs_ai" : "ai_vs_ai";
        UpdateModeLabel();
        ShowLogText("Mode: " + ModeDisplayName(pendingMode) + " (applies on restart)");
    }

    void UpdateModeLabel()
    {
        if (modeLabelText != null) modeLabelText.text = ModeDisplayName(pendingMode);
    }

    string ModeDisplayName(string mode)
    {
        return mode == "ai_vs_ai" ? "AI vs AI" : "AI vs Priority";
    }

    public void OnStrategyChanged(int index)
    {
        int i = Mathf.Clamp(index, 0, StrategyAggressions.Length - 1);
        int agg = StrategyAggressions[i];
        // Strategy drives Team B's priority agent in slider_vs_ai mode.
        StartCoroutine(SendSimplePost(
            "{\"action\":\"set_strategy\",\"team\":\"teamB\",\"aggression\":" + agg + "}"));
        ShowLogText("Strategy: " + StrategyLabels[i]);
    }

    IEnumerator SendSimplePost(string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
    }

    IEnumerator GameLoop()
    {
        holdNextFrame = true; // first frame after reset shows the kickoff layout
        while(!gameOver)
        {
            yield return SendStep();
            float wait = secondsToWaitBeforeNextFrame;
            if (holdNextFrame)
            {
                wait += kickoffHoldSeconds;
                holdNextFrame = false;
            }
            yield return new WaitForSeconds(wait);
        }
    }

    IEnumerator SendStep()
    {
        string json = "{\"action\":\"step\"}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        string responseText = request.downloadHandler.text;


        GameState state = JsonUtility.FromJson<GameState>(responseText);

        if (state == null) {
            GameUIManager.Instance.DisplayErrorMessage("Error: Prolog Server Not Initialized!");
        } else
        {
            GameUIManager.Instance.DisplayErrorMessage("");
        }

        if (state.events != null)
        {
            foreach (var ev in state.events)
            {
                switch (ev.type)
                {
                    case "goal":
                        //Debug.Log("GOAL by Team " + ev.team);
                        ShowLogText("Team " + ev.team +  " scored!");
                        scoreText.text = state.score.teamA + " - " + state.score.teamB;
                        break;
                    case "half_time":
                        ShowLogText("Half-time!");
                        holdNextFrame = true; // linger on the turn-16 kickoff layout
                        break;
                    case "full_time":
                        ShowLogText("Full-time! Game Over!");
                        gameOver = true;

                        if (gameLoopCoroutine != null)
                        {
                            StopCoroutine(gameLoopCoroutine);
                        }

                        ShowGameOverUI(state.score.teamA, state.score.teamB);
                        break;
                }
                
        
            }
        }


        if (state == null)
        {
            //Debug.LogError("Error: Prolog Server Not Initiailized!");
            GameUIManager.Instance.DisplayErrorMessage("Error: Prolog Server Not Initiailized!");
        }


        foreach (var p in state.players)
        {
            Vector3 gridPos = new Vector3(p.x / 20f, p.y / 20f, 0);

            if (!playerObjects.ContainsKey(p.name))
            {
                GameObject obj = Instantiate(playerPrefab, gridPos, Quaternion.identity);

                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

                if (p.team == "teamA")
                    sr.sprite = teamACharacter;
                else
                    sr.sprite = teamBCharacter;

                playerObjects[p.name] = obj;
                targetPositions[p.name] = gridPos;
            }

            if (!interpolationToggle.isOn) // frames/positions of players/ball from prolog will be used to render WITHOUT unity smoothing rendering
            {
                playerObjects[p.name].transform.position = gridPos;
            } else  // frames/positions of players/ball from prolog will be used to render WITH unity smoothing rendering
            {
                targetPositions[p.name] = gridPos;
            }

        }

        Vector3 ballPos = new Vector3(state.ball.x / 20f, state.ball.y / 20f, 0);

        if (ballObject == null)
        {
            ballObject = Instantiate(ballPrefab, ballPos, Quaternion.identity);
            ballTarget = ballPos;
        }


        //Debug.Log("Ball: " + state.ball.x + ", " + state.ball.y);

        if (!interpolationToggle.isOn)
        {
            ballObject.transform.position = ballPos;
        } else
        {
            ballTarget = ballPos;
        }
        
    }

    private void ShowGameOverUI(int scoreA, int scoreB)
    {
        gameOverPanel.SetActive(true);
        FulltimePanelManager manager = gameOverPanel.GetComponent<FulltimePanelManager>();
        manager.setTeamAScore(scoreA);
        manager.setTeamBScore(scoreB);
        
    }

    private void ShowLogText(string log)
    {
        LogText.GetComponent<Animator>().Play("Logtext", 0, 0f);
        LogText.GetComponentInChildren<TextMeshProUGUI>().text = log;
    }


    public float moveSpeed = 5f;

    void Update()
    {
        foreach (var kvp in playerObjects)
        {
            string name = kvp.Key;
            GameObject obj = kvp.Value;

            if (interpolationToggle.isOn)
            {
                if (targetPositions.ContainsKey(name))
                {
                    obj.transform.position = Vector3.MoveTowards(
                        obj.transform.position,
                        targetPositions[name],
                        moveSpeed * Time.deltaTime
                    );
                }
            }

            
        }

        // Move ball with unity help
        if (interpolationToggle.isOn)
        {
            if (ballObject == null) return;
            ballObject.transform.position = Vector3.MoveTowards(
                ballObject.transform.position,
                ballTarget,
                moveSpeed * Time.deltaTime
            );
        }

        
    }

    Coroutine gameLoopCoroutine;

    public void ResetGame()
    {
        //if (gameOver) return;
        scoreText.text = "0 - 0";
        gameOver = false;
        gameOverPanel.SetActive(false);
        foreach (var obj in playerObjects.Values)
        {
            Destroy(obj);
        }
        Destroy(ballObject);
        playerObjects.Clear();


        ShowLogText("Game has been reset!");

        if (gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine);
        }


        //GameUIManager.Instance.ToggleSettingsPanel();
        StartCoroutine(SendReset());
    }

    IEnumerator SendReset()
    {
        string json = "{\"action\":\"reset\"}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        // reset re-initializes game_mode/strategy on the server, so push the
        // UI selections AFTER reset but BEFORE the first step.
        yield return SendSimplePost(
            "{\"action\":\"set_mode\",\"mode\":\"" + pendingMode + "\"}");

        if (strategyDropdown != null)
        {
            int i = Mathf.Clamp(strategyDropdown.value, 0, StrategyAggressions.Length - 1);
            yield return SendSimplePost(
                "{\"action\":\"set_strategy\",\"team\":\"teamB\",\"aggression\":"
                + StrategyAggressions[i] + "}");
        }

        gameLoopCoroutine = StartCoroutine(GameLoop());
    }

    public void speedUpGame()
    {
        if (secondsToWaitBeforeNextFrame <= 0.21f)
        {
            return;
        }
        secondsToWaitBeforeNextFrame-=0.2f;
    }

    public void slowDownGame()
    {
        if (secondsToWaitBeforeNextFrame >= 0.59f)
        {
            return;
        }
        secondsToWaitBeforeNextFrame+=0.2f;
    }
}