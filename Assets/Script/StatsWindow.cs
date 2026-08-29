using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class StatsWindow : MonoBehaviour
{
    public ObjectiveManager objectiveManager;
    public GameManager gameManager;
    public PlayerShooter playerShooter;

    public TextMeshProUGUI statsText;
    public string nextSceneName = "Level2";

    void OnEnable()
    {
        if (objectiveManager != null && gameManager != null && playerShooter != null)
        {
            int correct = objectiveManager.CorrectColors;
            int incorrect = objectiveManager.IncorrectColors;
            float time = Mathf.Max(0, gameManager.TimeLeft);
            int ammo = playerShooter.currentAmmo + playerShooter.totalAmmo;

            statsText.text = $"Level Complete!\n\n" +
                             $"Correct Objects: {correct}\n" +
                             $"Incorrect Objects: {incorrect}\n" +
                             $"Remaining Time: {time:F1}s\n" +
                             $"Remaining Ammo: {ammo}\n\n" +
                             $"Final Score: {(correct * 100) - (incorrect * 50) + (ammo * 10) + Mathf.CeilToInt(time * 5)}\n\n" +
                             $"Press [ENTER] to continue";
        }
    }

    public void NextLevel()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
