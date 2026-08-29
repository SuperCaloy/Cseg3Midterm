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

    [Header("Ammo & Color")]
    public int totalAmmo = 240;
    public int clipSize = 30;
    public int currentAmmo;
    public bool isReloading = false;
    public Color[] availableColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };
    public int selectedColorIndex = 0;

    private bool showCrosshair = false;

    void Start()
    {
        currentAmmo = clipSize;
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // Prevent any interaction when the game is over/paused

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

        // Color Selection Input
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedColorIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedColorIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedColorIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedColorIndex = 3;

        // Ensure index is valid
        if (selectedColorIndex >= availableColors.Length) selectedColorIndex = availableColors.Length - 1;

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
            
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetColor(availableColors[selectedColorIndex]);
            }

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