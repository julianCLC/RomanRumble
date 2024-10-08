using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField] private int _targetFPS = 60;
    [SerializeField] RoundHandler roundHandler;
    [SerializeField] GameObject roundStartButton;

    Dictionary<ulong, GameObject> playerObjectsDict = new Dictionary<ulong, GameObject>();
    public List<GameObject> playerObjects {get; private set;}
    public List<ulong> connectedPlayers {get; private set;}
    public Dictionary<ulong, int> playerScore = new Dictionary<ulong, int>(); // TODO: make this live on server, and clients either synchronize values, or only request values

    public static Action<ulong> onPlayerDeath;
    public static Action<ulong> onPlayerRevive;

    // connect/disconnect events coming from NetworkHelperFuncs.cs
    // all other scripts should subscribe to these events
    // for any functionality they need
    public static Action<ulong> onManualClientConnected;
    public static Action<ulong> onManualClientDisconnected;
    public static Action onJoinSession;
    public static Action onLeaveSession;

    public static Action onGameStart;

    public static GameManager Instance { get; private set; }

    Color[] colorList = {Color.blue, Color.red, Color.green, Color.yellow, Color.black, Color.white};

    void Awake(){
        if (Instance != null && Instance != this){ 
            Destroy(this); 
        } 
        else{ 
            Instance = this; 
        } 
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = _targetFPS;
    }

    void OnEnable(){
        RoundHandler.onRoundEnd += OnRoundEnd;

        PlayerControllerServer.onPlayerDeath += OnPlayerDeath;
        PlayerControllerServer.onPlayerRevive += OnPlayerRevive;

        NetworkHelperFuncs.onJoin += OnJoin;
        NetworkHelperFuncs.onLeave += OnLeave;
        NetworkHelperFuncs.onClientJoin += OnClientConnect;
        NetworkHelperFuncs.onClientLeave += OnClientDisconnect;

        NetworkHelperFuncs.onGameStart += OnGameStart;
    }

    void OnDisable(){
        RoundHandler.onRoundEnd -= OnRoundEnd;

        PlayerControllerServer.onPlayerDeath -= OnPlayerDeath;
        PlayerControllerServer.onPlayerRevive -= OnPlayerRevive;

        NetworkHelperFuncs.onJoin -= OnJoin;
        NetworkHelperFuncs.onLeave -= OnLeave;
        NetworkHelperFuncs.onClientJoin -= OnClientConnect;
        NetworkHelperFuncs.onClientLeave -= OnClientDisconnect;
        
        NetworkHelperFuncs.onGameStart -= OnGameStart;
    }

    public void StartRound(){
        roundHandler.StartRound();
    }

    void OnRoundEnd(){
        roundStartButton.SetActive(true);
    }

    void OnGameStart(){
        onGameStart?.Invoke();
    }

    /// <summary>
    /// Adds 1 score to player
    /// returns score after adding, to be used by server to send to others
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public int PlayerAddScore(ulong playerId){
        playerScore.TryGetValue(playerId, out var currScore);
        int newScore = currScore + 1;
        playerScore[playerId] = newScore;
        PlayerUIManager.Instance.UpdateScoreUI(playerId, newScore);

        return newScore;
    }

    public void PlayerSetScore(ulong playerId, int _score){
        playerScore[playerId] = _score;
        PlayerUIManager.Instance.UpdateScoreUI(playerId, _score);
    }

    void OnPlayerDeath(Transform player){
        onPlayerDeath?.Invoke(player.GetComponent<NetworkObject>().OwnerClientId);
    }

    void OnPlayerRevive(Transform player){
        onPlayerRevive?.Invoke(player.GetComponent<NetworkObject>().OwnerClientId);
    }

    public static Vector3 GetRandomPositionArena(){
        return new Vector3(UnityEngine.Random.Range(-6, 3), 5f, UnityEngine.Random.Range(-5, 4));
    }

    public Color GetColour(ulong clientId){
        return colorList[clientId];
    }

    public void OnClientConnect(ulong clientId){
        UpdatePlayerObjects();
        onManualClientConnected?.Invoke(clientId);
    }

    public void OnClientDisconnect(ulong clientId){
        UpdatePlayerObjects();
        onManualClientDisconnected?.Invoke(clientId);
    }

    void OnJoin(ulong clientId){
        if(playerObjects == null){ playerObjects = new List<GameObject>(); }
        if(connectedPlayers == null){ connectedPlayers = new List<ulong>(); }
        
        UpdatePlayerObjects();
        onJoinSession?.Invoke();
    }

    void OnLeave(ulong clientId){
        if(playerObjects != null){ playerObjects.Clear(); }
        if(connectedPlayers != null){ connectedPlayers.Clear(); }

        onLeaveSession?.Invoke();
    }

    void UpdatePlayerObjects(){
        // Get all player objects
        GameObject[] tempPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
        
        // Clear lists
        connectedPlayers.Clear();
        playerObjects.Clear();
        playerObjectsDict.Clear();

        // Populate lists and dictionaries
        foreach(GameObject playerObject in tempPlayerObjects){
            if(playerObject.TryGetComponent(out NetworkObject playerNO)){
                connectedPlayers.Add(playerNO.OwnerClientId);
                playerObjects.Add(playerObject);
                playerObjectsDict.Add(playerNO.OwnerClientId, playerObject);
            }
        }
    }

    public GameObject GetPlayerObjectByID(ulong clientId){
        if(playerObjectsDict.TryGetValue(clientId, out GameObject playerObject)){
            return playerObject;
        }
        return null;
    }

}

public struct PlayerInfo {
    ulong clientId;
    string playerName;
    GameObject playerObj;
    int score;
    float health;
}
