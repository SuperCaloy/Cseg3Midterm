using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    public int CurrentStage { get; private set; } = 1;
    public int TotalEnemiesKilled { get; private set; } = 0;
    public int TotalShotsFired { get; private set; } = 0;
    public int TotalAmmoPickedUp { get; private set; } = 0;

    public int PlayerTotalAmmo { get; set; } = -1; // -1 means start with default
    public int PlayerCurrentAmmo { get; set; } = -1;

    public int BaseEnemyRequirement = 5;

    public int TargetEnemiesForCurrentStage
    {
        get
        {
            return Mathf.CeilToInt(BaseEnemyRequirement * Mathf.Pow(1.5f, CurrentStage - 1));
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddEnemyKilled()
    {
        TotalEnemiesKilled++;
    }

    public void AddShotFired()
    {
        TotalShotsFired++;
    }

    public void AddAmmoPickedUp(int amount)
    {
        TotalAmmoPickedUp += amount;
    }

    public void NextStage()
    {
        CurrentStage++;
    }

    public void ResetSession()
    {
        CurrentStage = 1;
        TotalEnemiesKilled = 0;
        TotalShotsFired = 0;
        TotalAmmoPickedUp = 0;
        PlayerTotalAmmo = -1;
        PlayerCurrentAmmo = -1;
    }
}
