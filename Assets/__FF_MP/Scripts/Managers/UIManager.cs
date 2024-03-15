using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button joinRoomByArgBtn;
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Button joinRandomRoomBtn;
    [SerializeField] private TMP_InputField roomInputField;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject roomJoinUI;
    [SerializeField] private GameObject lobbyUI;
    // Start is called before the first frame update
    
    private void Awake()
    {
        joinRoomByArgBtn.onClick.AddListener(() => CreateRoom(GameMode.Shared, roomInputField.text));
        createRoomBtn.onClick.AddListener(() => CreateRoom(GameMode.Shared, roomInputField.text));
        joinRandomRoomBtn.onClick.AddListener( JoinRandomRoom);
        loadingScreen.SetActive(false);
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GlobalManager.Instance.UIManager = this;
        
        lobbyUI.SetActive(false);
    }

    private void CreateRoom(GameMode gameMode, string roomName)
    {

        Debug.Log($"------------{gameMode}------------");
        loadingScreen.SetActive(true);
        loadingText.text = "Joining as "+gameMode;
        roomJoinUI.SetActive(false);
        GlobalManager.Instance.GameLauncher.JoinOrCreateLobby(gameMode, roomName);
        
    }

    private void JoinRandomRoom()
    {
        loadingScreen.SetActive(true);
        loadingText.text = "Joining Random Room";
        roomJoinUI.SetActive(false);
        GlobalManager.Instance.GameLauncher.JoinRandomRoom(GameMode.AutoHostOrClient);
    }
    public void DisableLoadingScreen()
    {
        loadingScreen.SetActive(false);
    }

    public void OpenLobbyUI()
    {
     lobbyUI.SetActive(true);   
    }
    
    private string[] adjectives = { "Good", "Fast", "Hot", "Cute", "Wild" };
    private string[] nouns = { "Panda", "Lion", "Tiger", "Wolf", "Eagle" };

    // Method to generate a random nickname
    public string GenerateRandomNickname()
    {
        // Choose a random adjective and noun
        string randomAdjective = adjectives[Random.Range(0, adjectives.Length)];
        string randomNoun = nouns[Random.Range(0, nouns.Length)];

        // Combine them to create the nickname
        string nickname = randomAdjective + randomNoun;

        return nickname;
    }
}
