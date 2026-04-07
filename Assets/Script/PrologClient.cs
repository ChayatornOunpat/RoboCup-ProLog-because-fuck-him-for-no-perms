using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;



[System.Serializable]
public class PlayerData
{
    public string name;
    public string team;
    public float x;
    public float y;
}

[System.Serializable]
public class GameState
{
    public float[] ball;
    public PlayerData[] players;
}

public class PrologClient : MonoBehaviour
{

    public GameObject ballObject;
    public GameObject playerPrefab;
    private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
    string url = "http://localhost:5000/action";



    void Start()
    {
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while(true)
        {
            yield return SendStep();
            yield return new WaitForSeconds(1f);
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

        GameState state = JsonUtility.FromJson<GameState>(request.downloadHandler.text);
        Debug.Log("Ball X: " + state.ball[0]);

        foreach (var p in state.players)
        {
            if (!playerObjects.ContainsKey(p.name))
            {
                GameObject obj = Instantiate(playerPrefab);

                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

                if (p.team == "teamA")
                    sr.color = Color.blue;
                else
                    sr.color = Color.red;

                playerObjects[p.name] = obj;
            }

            playerObjects[p.name].transform.position =
                new Vector3(p.x / 20f, p.y / 20f, 0);
        }

        Debug.Log("Ball: " + state.ball[0] + ", " + state.ball[1]);

        ballObject.transform.position = new Vector3(state.ball[0] / 20f, state.ball[1] / 20f, 0);
    }

    public void ResetGame()
    {
        foreach (var obj in playerObjects.Values)
        {
            Destroy(obj);
        }
        playerObjects.Clear();

        GameUIManager.Instance.ToggleSettingsPanel();
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

        Debug.Log("Game Reset Sent");
    }
}
