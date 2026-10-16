using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class StatsWindow : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerShooter playerShooter;
    public TextMeshProUGUI statsText;

    void OnEnable()
    {
        if (gameManager != null)
        {
            float time = Mathf.Max(0, gameManager.TimeLeft);
            
            int stageEnemiesKilled = gameManager.EnemiesKilledInStage;
            int totalEnemiesKilled = GameSessionManager.Instance != null ? GameSessionManager.Instance.TotalEnemiesKilled : stageEnemiesKilled;
            int totalShotsFired = GameSessionManager.Instance != null ? GameSessionManager.Instance.TotalShotsFired : 0;
            int totalAmmoPickedUp = GameSessionManager.Instance != null ? GameSessionManager.Instance.TotalAmmoPickedUp : 0;

            if (statsText != null)
            {
                statsText.text = $"Stage Complete!\n\n" +
                                 $"Stage Enemies Killed: {stageEnemiesKilled}\n" +
                                 $"Total Enemies Killed: {totalEnemiesKilled}\n" +
                                 $"Total Shots Fired: {totalShotsFired}\n" +
                                 $"Total Ammo Picked Up: {totalAmmoPickedUp}\n" +
                                 $"Remaining Time: {time:F1}s\n\n" +
                                 $"Press [ENTER] to continue";
            }
        }
    }

    private bool isLoading = false;

    void Update()
    {
        if (!isLoading && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            NextLevel();
        }
    }

    public void NextLevel()
    {
        if (isLoading) return;
        isLoading = true;
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.NextStage();
            int nextStage = GameSessionManager.Instance.CurrentStage;
            if (nextStage <= 6)
            {
                SceneManager.LoadScene("Level" + nextStage);
            }
            else
            {
                // Game finished
                SceneManager.LoadScene("MainMenu");
            }
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
