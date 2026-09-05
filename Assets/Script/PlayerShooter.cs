using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 40f;
    public float fireRate = 0.05f;
    private float nextFireTime = 0f;

    [Header("Zooming")]
    public Camera playerCamera;
    public float normalFOV = 60f;
    public float zoomedFOV = 20f;
    public float zoomSpeed = 10f;

    [Header("UI")]
    [Tooltip("Drag your Crosshair texture asset directly here.")]
    public Texture2D crosshairTexture;
    public float crosshairSize = 32f;

    [Header("Ammo")]
    public int totalAmmo = 60; // Reduced so pickups matter
    public int clipSize = 30;
    public int currentAmmo;
    public bool isReloading = false;

    private bool showCrosshair = false;

    void Start()
    {
        // The user requested ammo to explicitly reset per level instead of carrying over
        totalAmmo = 60;
        currentAmmo = clipSize;
        
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.PlayerTotalAmmo = totalAmmo;
            GameSessionManager.Instance.PlayerCurrentAmmo = currentAmmo;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        ApplyGunMaterial();
    }

    private void ApplyGunMaterial()
    {
        Material gunMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (gunMat == null || gunMat.shader.name == "Hidden/InternalErrorShader") 
            gunMat = new Material(Shader.Find("Standard"));
            
        gunMat.color = new Color(0.15f, 0.15f, 0.15f);
        if (gunMat.HasProperty("_Metallic")) gunMat.SetFloat("_Metallic", 0.9f);
        if (gunMat.HasProperty("_Smoothness")) gunMat.SetFloat("_Smoothness", 0.5f);

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in allRenderers)
        {
            string n = r.gameObject.name.ToLower();
            if (n.Contains("assault") || n.Contains("rifle") || n.Contains("gun") || n.Contains("weapon") || n.Contains("smg") || n.Contains("silencer") || n.Contains("suppressor") || n.Contains("muzzle"))
            {
                Material[] newMats = new Material[r.materials.Length];
                for (int i = 0; i < newMats.Length; i++)
                {
                    newMats[i] = gunMat;
                }
                r.materials = newMats;
            }
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // Prevent any interaction when the game is over/paused

        // Keep session manager synced
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.PlayerTotalAmmo = totalAmmo;
            GameSessionManager.Instance.PlayerCurrentAmmo = currentAmmo;
        }

        if (Input.GetMouseButton(1))
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomedFOV, zoomSpeed * Time.deltaTime);
            }
            showCrosshair = true;
        }
        else
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, normalFOV, zoomSpeed * Time.deltaTime);
            }
            showCrosshair = false;
        }

        if (isReloading) return; // Cannot shoot or reload while reloading

        // Reload Input
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < clipSize)
        {
            StartCoroutine(ReloadCoroutine());
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime && currentAmmo > 0)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
        else if (Input.GetMouseButton(0) && currentAmmo <= 0 && !isReloading)
        {
            // Auto-reload if clicking while empty
            StartCoroutine(ReloadCoroutine());
        }
    }

    System.Collections.IEnumerator ReloadCoroutine()
    {
        if (totalAmmo <= 0 || currentAmmo == clipSize) yield break;

        isReloading = true;
        yield return new WaitForSeconds(1f);
        
        int ammoNeeded = clipSize - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, totalAmmo);

        currentAmmo += ammoToReload;
        totalAmmo -= ammoToReload;
        
        isReloading = false;
    }

    void OnGUI()
    {
        if (showCrosshair && crosshairTexture != null)
        {
            float x = (Screen.width - crosshairSize) / 2f;
            float y = (Screen.height - crosshairSize) / 2f;
            GUI.DrawTexture(new Rect(x, y, crosshairSize, crosshairSize), crosshairTexture);
        }
    }

    void Shoot()
    {
        currentAmmo--;
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.AddShotFired();
        }

        if (bulletPrefab != null && firePoint != null)
        {
            Vector3 shootDirection = firePoint.forward;
            if (playerCamera != null)
            {
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100f))
                {
                    shootDirection = (hit.point - firePoint.position).normalized;
                }
                else
                {
                    shootDirection = playerCamera.transform.forward;
                }
            }

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = shootDirection * bulletSpeed;
            }
        }
        else
        {
            Debug.LogWarning("Missing Bullet Prefab or Fire Point in the Inspector!");
        }
    }
}