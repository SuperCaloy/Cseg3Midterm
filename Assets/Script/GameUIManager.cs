using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerShooter playerShooter;
    public PlayerHealth playerHealth;
    public ObjectiveManager objectiveManager;

    [Header("HUD")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI reloadingText;
    public TextMeshProUGUI objectivesText;
    public UnityEngine.UI.Image colorIndicator;
    public GameObject loseWindow;
    public GameObject winWindow;

    void Update()
    {
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
        }

        if (playerHealth != null && healthText != null)
        {
            healthText.text = "Health: " + Mathf.CeilToInt(playerHealth.currentHealth);
        }

        if (objectiveManager != null && objectivesText != null)
        {
            objectivesText.text = objectiveManager.GetObjectivesText();
        }

        if (playerShooter != null)
        {
            if (ammoText != null)
            {
                Color c = Color.white;
                if (playerShooter.availableColors != null && playerShooter.availableColors.Length > playerShooter.selectedColorIndex)
                {
                    c = playerShooter.availableColors[playerShooter.selectedColorIndex];
                }
                string colorHex = ColorUtility.ToHtmlStringRGB(c);
                ammoText.text = $"<color=#{colorHex}>[■]</color> Ammo: {playerShooter.currentAmmo} / {playerShooter.totalAmmo}";
            }

            if (reloadingText != null)
                reloadingText.gameObject.SetActive(playerShooter.isReloading);

            if (colorIndicator != null)
            {
                if (playerShooter.availableColors != null && playerShooter.availableColors.Length > playerShooter.selectedColorIndex)
                {
                    colorIndicator.color = playerShooter.availableColors[playerShooter.selectedColorIndex];
                }
            }
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
}
