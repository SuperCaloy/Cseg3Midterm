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

    private bool showCrosshair = false;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
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

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
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