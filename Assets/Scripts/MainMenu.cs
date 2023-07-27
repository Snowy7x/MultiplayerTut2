using System.Threading.Tasks;
using Steamworks;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject loadingMenu;
    [SerializeField] private GameObject inLobbyMenu;

    [SerializeField] private TMP_Text roomId;
    [SerializeField] private TMP_InputField roomIdInputField;

    private void Start()
    {
        GameNetworkManager.Instance.onLobbyJoined += (lobby) =>
        {
            OpenInLobbyMenu();
            roomId.text = lobby.Id.ToString();
        };

        GameNetworkManager.Instance.onLobbyLeft += (lobby) =>
        {
            OpenMainMenu();
        };

        GameNetworkManager.Instance.onLobbyCreated += (result, lobby) =>
        {
            OpenInLobbyMenu();
            roomId.text = lobby.Id.ToString();
        };
    }

    #region Menu Stuff

    public void OpenMainMenu()
    {
        mainMenu.SetActive(true);
        loadingMenu.SetActive(false);
        inLobbyMenu.SetActive(false);
    }
    
    public void OpenInLobbyMenu()
    {
        mainMenu.SetActive(false);
        loadingMenu.SetActive(false);
        inLobbyMenu.SetActive(true);
    }

    public void OpenLoadingMenu()
    {
        mainMenu.SetActive(false);
        loadingMenu.SetActive(true);
        inLobbyMenu.SetActive(false);
    }
    

    #endregion

    public async void CreateRoom()
    {
        OpenLoadingMenu();
        bool didStart = await GameNetworkManager.Instance.StartHost(8);
        if (didStart)
        {
            Debug.Log("Created Room!");
        }
        else
        {
            Debug.Log("Failed to create room!");
            OpenMainMenu();
        }
    }

    public void CopyID()
    {
        TextEditor te = new TextEditor();
        te.text = roomId.text;
        te.SelectAll();
        te.Copy();
    }

    public async void JoinRoomWithId()
    {
        OpenLoadingMenu();
        RoomEnter? roomEnter = await GameNetworkManager.Instance.JoinLobbyWithId(roomIdInputField.text);
        if (roomEnter == null) return;
        if (roomEnter == RoomEnter.Success)
        {
            Debug.Log("Joined Lobby!");
            OpenInLobbyMenu();
        }
        else
        {
            Debug.Log("Could not join!!");
        }
    }

    public void StartGame()
    {
        GameNetworkManager.Instance.StartGameServer();
    }
}
