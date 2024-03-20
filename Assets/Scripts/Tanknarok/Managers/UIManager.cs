using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; set; }
    public EndRaceUI endRaceUI { get; private set; }

    public EndRaceUI endRaceUIPrefab;
    // Start is called before the first frame update
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        DontDestroyOnLoad(this);
    }

    public void ShowResult()
    {
        endRaceUI = Instantiate(endRaceUIPrefab);
        endRaceUI.Init();
        endRaceUI.RedrawResultsList();
    }
    
    private string[] adjectives = { "Good", "Fast", "Hot", "Cute", "Wild", "Cool", "Brave", "Loyal" };

    private string[] nouns = { "Panda", "Lion", "Tiger", "Wolf", "Eagle", "Bear", "Dog", "Koala", "Jaguar", "Sloth" };


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
