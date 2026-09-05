using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public int ammoAmount = 30;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerShooter shooter = other.GetComponent<PlayerShooter>();
            if (shooter == null) shooter = other.GetComponentInChildren<PlayerShooter>();
            if (shooter == null) shooter = other.GetComponentInParent<PlayerShooter>();
            
            if (shooter != null)
            {
                int randomAmount = Random.Range(15, 45); // Random ammo
                shooter.totalAmmo += randomAmount;
                if (GameSessionManager.Instance != null)
                {
                    GameSessionManager.Instance.AddAmmoPickedUp(randomAmount);
                }

                GameUIManager ui = FindObjectOfType<GameUIManager>();
                if (ui != null) ui.ShowNotification($"Picked up {randomAmount} Ammo!");

                Destroy(gameObject);
            }
        }
    }
}
