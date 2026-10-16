using UnityEngine;

public enum GameState
{
    NotStarted,
    Playing,
    Won,
    Lost
}

public class GameManager : MonoBehaviour
{
    public float maxTime = 60f;
    public float TimeLeft { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.NotStarted;
    private PlayerShooter playerShooter;
    
    public int EnemiesKilledInStage { get; private set; } = 0;

    public void StartGame()
    {
        Time.timeScale = 1f;
        Application.runInBackground = true;
        
        // Calculate maxTime based on target enemies and stage scaling
        if (GameSessionManager.Instance != null)
        {
            float stageMultiplier = Mathf.Pow(1.15f, GameSessionManager.Instance.CurrentStage - 1);
            maxTime = 30f + (GameSessionManager.Instance.TargetEnemiesForCurrentStage * 15f * stageMultiplier);
        }
        else
        {
            maxTime = 60f;
        }

        TimeLeft = maxTime;
        EnemiesKilledInStage = 0;
        CurrentState = GameState.Playing;
        playerShooter = FindObjectOfType<PlayerShooter>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            ForceUpdate(Time.deltaTime);
        }
    }

    public void ForceUpdate(float deltaTime)
    {
        if (CurrentState != GameState.Playing) return;

        TimeLeft -= deltaTime;

        if (TimeLeft <= 0)
        {
            TimeLeft = 0;
            LoseGame();
        }
    }
    
    public void AddKill()
    {
        if (CurrentState != GameState.Playing) return;
        
        EnemiesKilledInStage++;
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.AddEnemyKilled();
            if (EnemiesKilledInStage >= GameSessionManager.Instance.TargetEnemiesForCurrentStage)
            {
                WinGame();
            }
        }
        else
        {
            // Fallback for testing without session manager
            if (EnemiesKilledInStage >= 5)
            {
                WinGame();
            }
        }
    }

    public void LoseGame()
    {
        CurrentState = GameState.Lost;
        Time.timeScale = 0f; // Freeze game
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void WinGame()
    {
        CurrentState = GameState.Won;
        Time.timeScale = 0f; // Freeze game
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
