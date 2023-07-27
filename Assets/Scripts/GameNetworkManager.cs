using System.Collections;
using System.Threading.Tasks;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance  { get; private set; }
    public Lobby? CurrentLobby { get; private set; }

    private FacepunchTransport _transport;

    public event UnityAction<Result, Lobby> onLobbyCreated;
    public event UnityAction<Lobby> onLobbyJoined;
    public event UnityAction<Lobby> onLobbyLeft;

    public event UnityAction<Lobby, Friend> onMemberJoined;
    public event UnityAction<Lobby, Friend> onMemberLeft;

    #region Unity Defaults

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
    }
    
    IEnumerator Load()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        SceneManager.LoadScene(1);
    }

    private void Start()
    {
        _transport = GetComponent<FacepunchTransport>();
        StartCoroutine(Load());
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        
        if (!NetworkManager.Singleton) return;
        
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    #endregion

    private void OnApplicationQuit() => Disconnect();
    
    public void StartClient(SteamId id)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        
        _transport.targetSteamId = id;
        
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client Started", this);
        }
        else
        {
            Debug.Log("Client did not start!", this);
        }
    }

    public async Task<bool> StartHost(int maxMembers)
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host Started");
            CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
            return true;
        }
        
        Debug.Log("Host did not start!");
        return false;
    }

    public async Task<RoomEnter?> JoinLobbyWithId(string id)
    {
        ulong Id;
        if (!ulong.TryParse(id, out Id))
        {
            Debug.LogError($"Invalid Lobby ID: {id}", this);
            return null;
        }

        Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();

        foreach (var lobby in lobbies)
        {
            if (lobby.Id == Id)
            {
                RoomEnter roomEnter = await lobby.Join();
                return roomEnter;
            }
        }
        
        Debug.LogError($"Lobby not found: {id}", this);
        return null;
    }
    
    public void Disconnect()
    {
        if (NetworkManager.Singleton == null) return;
        CurrentLobby?.Leave();
        NetworkManager.Singleton.Shutdown();
    }

    public void StartGameServer()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsHost) NetworkManager.Singleton.SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);
    }

    #region Unitry Network Callbacks

    private void OnServerStarted()
    {
        Debug.Log("Server Started", this);
    }
    
    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log($"Client Connected: {clientId}", this);
    }
    
    private void OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log($"Client Disconnected: {clientId}", this);
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    #endregion

     #region Steam Callbacks


    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        try
        {
            onMemberLeft?.Invoke(lobby, friend);
        }
        catch { /*ignore*/ }
    }

    private void OnLobbyInvite(Friend friend, Lobby lobby)
    {
        Debug.Log($"You got an invite from {friend.Name}", this);
    }

    private void OnLobbyGameCreated(Lobby lobby, uint arg2, ushort arg3, SteamId steamId)
    {
        
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        try
        {
            onMemberJoined?.Invoke(lobby, friend);
        }
        catch { /*ignore*/ }
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsHost) return;
        Debug.Log($"Lobby Joined! {lobby.Id}", this);
        
        StartClient(lobby.Id);
        try
        {
            onLobbyJoined?.Invoke(lobby);
        }
        catch { /*ignore*/ }
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.LogError($"Lobby Creation Failed! {result}", this);
            return;
        }
        
        // 
        // lobby.SetFriendsOnly();
        // lobby.SetPublic();
        // lobby.SetPrivate();
        lobby.SetData("name", "LobbyName" + Random.Range(0, 100));
        lobby.SetJoinable(true);
        
        Debug.Log($"Lobby Created! {lobby.Id}", this);

        try
        {
            onLobbyCreated?.Invoke(result, lobby);
        }
        catch { /*ignore*/ }
    }
    
    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        // When the lobby invite is accepted!
        lobby.Join();
    }
    
    #endregion
}
