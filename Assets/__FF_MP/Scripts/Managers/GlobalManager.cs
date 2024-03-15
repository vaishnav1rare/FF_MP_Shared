using UnityEngine;

public class GlobalManager : MonoBehaviour
{
    public static GlobalManager Instance { get; private set; }
    public GameManager GameManager { get; set; } 
    public GameLauncher GameLauncher { get; set; }
    public UIManager UIManager { get; set; }
    public PowerUpSpawnManager PowerUpSpawnManager { get; set; }
    
    public ChallengeManager ChallengeManager { get; set;}

    public bool IsGamePaused = false;
    void Awake()
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

    
}
