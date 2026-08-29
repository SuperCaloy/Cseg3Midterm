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

    public void StartGame()
    {
        Time.timeScale = 1f;
        TimeLeft = maxTime;
        CurrentState = GameState.Playing;
        playerShooter = FindObjectOfType<PlayerShooter>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        ObjectiveManager om = FindObjectOfType<ObjectiveManager>();
        if (om != null) om.OnAllObjectivesCompleted += WinGame;
        
        StartGame();
    }

    void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            ForceUpdate(Time.deltaTime);

            // Check if completely out of ammo (game over)
            if (playerShooter != null)
            {
                if (playerShooter.currentAmmo <= 0 && playerShooter.totalAmmo <= 0 && !playerShooter.isReloading)
                {
                    // Allow 1 second grace period for last bullets to land
                    StartCoroutine(CheckAmmoLoseCoroutine());
                }
            }
        }
    }

    System.Collections.IEnumerator CheckAmmoLoseCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        if (CurrentState == GameState.Playing && playerShooter.currentAmmo <= 0 && playerShooter.totalAmmo <= 0)
        {
            LoseGame();
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
