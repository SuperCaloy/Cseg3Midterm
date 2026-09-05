using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerShooter playerShooter;
    public PlayerHealth playerHealth;

    [Header("HUD")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI reloadingText;
    public TextMeshProUGUI objectivesText; // We'll repurpose this for "Enemies Killed"
    
    public GameObject loseWindow;
    public GameObject winWindow;

    public TextMeshProUGUI notificationText;
    private float notificationTimer = 0f;

    void Update()
    {
        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0 && notificationText != null)
            {
                notificationText.gameObject.SetActive(false);
            }
        }

        if (gameManager != null)
        {
            if (timerText != null)
                timerText.text = "Time: " + Mathf.CeilToInt(gameManager.TimeLeft).ToString() + "s";

            if (gameManager.CurrentState == GameState.Lost && loseWindow != null && !loseWindow.activeSelf)
            {
                loseWindow.SetActive(true);
            }
            if (gameManager.CurrentState == GameState.Won && winWindow != null && !winWindow.activeSelf)
            {
                winWindow.SetActive(true);
            }

            // Input handling for restarting / next level
            if (gameManager.CurrentState == GameState.Lost && Input.GetKeyDown(KeyCode.Return))
            {
                RetryLevel();
            }
            if (gameManager.CurrentState == GameState.Won && Input.GetKeyDown(KeyCode.Return))
            {
                StatsWindow sw = winWindow.GetComponent<StatsWindow>();
                if (sw != null) sw.NextLevel();
            }
            
            if (objectivesText != null)
            {
                int target = GameSessionManager.Instance != null ? GameSessionManager.Instance.TargetEnemiesForCurrentStage : 5;
                objectivesText.text = $"<b>OBJECTIVE:</b>\nKill Enemies: {gameManager.EnemiesKilledInStage} / {target}";
            }
        }

        if (playerHealth != null && healthText != null)
        {
            healthText.text = "Health: " + Mathf.CeilToInt(playerHealth.currentHealth);
        }

        if (playerShooter != null)
        {
            if (ammoText != null)
            {
                ammoText.text = $"Ammo: {playerShooter.currentAmmo} / {playerShooter.totalAmmo}";
            }

            if (reloadingText != null)
                reloadingText.gameObject.SetActive(playerShooter.isReloading);
        }
    }

    public void RetryLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu"); // Adjust name if different
    }

    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            notificationTimer = 3f;
        }
    }
}
