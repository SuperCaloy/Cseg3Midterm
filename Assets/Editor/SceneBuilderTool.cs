using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class SceneBuilderTool : EditorWindow
{
    [MenuItem("Tools/Build Game Scenes")]
    public static void BuildScenes()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Cannot build scenes while in Play Mode! Please stop playing the game first.");
            return;
        }

        string gameScenePath = "Assets/Scenes/Game.unity";
        string level1Path = "Assets/Scenes/Level1.unity";
        string level2Path = "Assets/Scenes/Level2.unity";
        
        if (!System.IO.File.Exists(gameScenePath))
        {
            Debug.LogError($"Could not find base scene at {gameScenePath}");
            return;
        }

        // Copy Game to Level 1
        AssetDatabase.CopyAsset(gameScenePath, level1Path);
        
        // Copy Game to Level 2
        AssetDatabase.CopyAsset(gameScenePath, level2Path);
        
        AssetDatabase.Refresh();

        // Process Level 1
        Scene level1 = EditorSceneManager.OpenScene(level1Path);
        SetupLevel(level1, false);
        EditorSceneManager.SaveScene(level1);

        // Process Level 2
        Scene level2 = EditorSceneManager.OpenScene(level2Path);
        SetupLevel(level2, true);
        EditorSceneManager.SaveScene(level2);

        // Update Build Settings
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        
        // Find main menu
        string[] menuGuids = AssetDatabase.FindAssets("MainMenu t:Scene");
        if (menuGuids.Length > 0)
        {
            buildScenes.Add(new EditorBuildSettingsScene(AssetDatabase.GUIDToAssetPath(menuGuids[0]), true));
        }

        buildScenes.Add(new EditorBuildSettingsScene(level1Path, true));
        buildScenes.Add(new EditorBuildSettingsScene(level2Path, true));
        
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Debug.Log("Successfully built Level1 and Level2 scenes, and updated Build Settings.");
    }

    private static void SetupLevel(Scene scene, bool isLevel2)
    {
        // 1. Create Managers
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.maxTime = isLevel2 ? 45f : 60f; // 45 secs for Level 2 as requested
        
        GameObject omObj = new GameObject("ObjectiveManager");
        ObjectiveManager om = omObj.AddComponent<ObjectiveManager>();

        // 2. Add more obstacles and randomize their positions/shapes in Level 2
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        
        // Dynamically calculate room size based on the Plane
        float roomBoundX = 35f; // Default fallback
        float roomBoundZ = 45f; // Default fallback
        
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.name.Equals("Plane", System.StringComparison.OrdinalIgnoreCase) || go.name.Contains("Floor"))
            {
                roomBoundX = (go.transform.localScale.x * 10f / 2f) - 3f;
                roomBoundZ = (go.transform.localScale.z * 10f / 2f) - 3f;
                break;
            }
        }

        Vector3[] furniturePositions = new Vector3[4];
        List<Vector3> occupiedPositions = new List<Vector3>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) occupiedPositions.Add(playerObj.transform.position);

        for (int i = 0; i < 4; i++)
        {
            Vector3 randomPos = Vector3.zero;
            bool validPos = false;
            for (int attempt = 0; attempt < 50; attempt++)
            {
                // Keep them slightly away from the walls
                randomPos = new Vector3(Random.Range(-roomBoundX + 4f, roomBoundX - 4f), 0, Random.Range(-roomBoundZ + 4f, roomBoundZ - 4f));
                validPos = true;
                foreach (var p in occupiedPositions)
                {
                    if (Vector3.Distance(new Vector3(randomPos.x, 0, randomPos.z), new Vector3(p.x, 0, p.z)) < 8f) // Good distance between objectives
                    {
                        validPos = false;
                        break;
                    }
                }
                if (validPos) break;
            }
            furniturePositions[i] = randomPos;
            occupiedPositions.Add(randomPos);
        }

        if (obstacles.Length > 0)
        {
            // Level 2 has many more objects, Level 1 has a few
            int clonesToAdd = isLevel2 ? 15 : 5;
            for (int i = 0; i < clonesToAdd; i++)
            {
                // Try to find a valid non-wall obstacle to clone
                GameObject templateObstacle = null;
                for (int j = 0; j < 10; j++)
                {
                    templateObstacle = obstacles[Random.Range(0, obstacles.Length)];
                    string tName = templateObstacle.name.ToLower();
                    if (!tName.Contains("wall") && !tName.Contains("plane") && !tName.Contains("floor") && !tName.Contains("objectivemanager"))
                        break;
                }
                
                if (templateObstacle != null)
                {
                    GameObject clone = Object.Instantiate(templateObstacle);
                    clone.name = templateObstacle.name + "_Clone" + i;
                }
            }
            
            // Refetch all obstacles (including the new ones)
            obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
            
            // Now, completely randomize the position and shape of EVERY obstacle
            foreach (var obs in obstacles)
            {
                // Ignore structural objects like walls and planes
                string objName = obs.name.ToLower();
                if (objName.Contains("wall") || objName.Contains("plane") || objName.Contains("floor") || objName.Contains("objectivemanager"))
                {
                    continue;
                }

                float scaleX, scaleY, scaleZ;
                if (isLevel2)
                {
                    // Level 2: Wild, non-uniform scaling
                    scaleX = Random.Range(0.5f, 4f);
                    scaleY = Random.Range(0.5f, 4f);
                    scaleZ = Random.Range(0.5f, 4f);
                }
                else
                {
                    // Level 1: Normal uniform scaling
                    float uniformScale = Random.Range(0.8f, 2.5f);
                    scaleX = scaleY = scaleZ = uniformScale;
                }
                obs.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                // Different Position (Spread across the entire room dynamically, avoiding overlaps)
                Vector3 randomPos = Vector3.zero;
                bool validPos = false;
                float requiredDistance = Mathf.Max(scaleX, scaleZ) + 1f; // Buffer distance based on scale

                for (int attempt = 0; attempt < 50; attempt++) // Increased attempts to guarantee a spot
                {
                    randomPos = new Vector3(Random.Range(-roomBoundX, roomBoundX), obs.transform.position.y, Random.Range(-roomBoundZ, roomBoundZ));
                    validPos = true;
                    foreach (var p in occupiedPositions)
                    {
                        if (Vector3.Distance(new Vector3(randomPos.x, 0, randomPos.z), new Vector3(p.x, 0, p.z)) < requiredDistance)
                        {
                            validPos = false;
                            break;
                        }
                    }
                    if (validPos) break;
                }
                
                obs.transform.position = randomPos;
                occupiedPositions.Add(randomPos);
                
                // Random Rotation
                obs.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }
        string[] level1Names = { "Chair", "Table", "Cabinet", "Lamp" };
        Color[] level1Colors = { Color.red, Color.blue, Color.green, Color.yellow };
        
        // Fully different objects for Level 2 to satisfy rubric exactly
        string[] level2Names = { "Bed", "Desk", "Sofa", "Bookshelf" };
        Color[] level2Colors = { Color.yellow, Color.red, Color.blue, Color.green };

        string[] targetNames = isLevel2 ? level2Names : level1Names;
        Color[] targetColors = isLevel2 ? level2Colors : level1Colors;

        List<GameObject> validObstacles = new List<GameObject>();
        foreach (var obs in obstacles)
        {
            string n = obs.name.ToLower();
            if (!n.Contains("wall") && !n.Contains("plane") && !n.Contains("floor") && !n.Contains("objectivemanager"))
            {
                validObstacles.Add(obs);
            }
        }
        for (int i = 0; i < targetNames.Length; i++)
        {
            if (i >= validObstacles.Count) break;
            
            GameObject target = validObstacles[i];
            target.name = targetNames[i];
            
            Material targetMat = target.GetComponent<Renderer>() != null ? target.GetComponent<Renderer>().sharedMaterial : new Material(Shader.Find("Standard"));
            
            // Shape the primitive to actually resemble the furniture piece using a compound structure
            TransformToFurniture(target, targetNames[i], targetMat, furniturePositions[i]);
            
            om.objectives.Add(new Objective { targetObject = target, requiredColor = targetColors[i] });
        }
        
        // 3. Enemy processing
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int targetEnemyCount = isLevel2 ? 10 : 5;
        
        if (enemies.Length > targetEnemyCount)
        {
            // Remove excess enemies
            for (int i = targetEnemyCount; i < enemies.Length; i++)
            {
                Object.DestroyImmediate(enemies[i]);
            }
        }
        else if (enemies.Length < targetEnemyCount && enemies.Length > 0)
        {
            // Spawn more to reach target
            int toSpawn = targetEnemyCount - enemies.Length;
            GameObject templateEnemy = enemies[0];
            for (int i = 0; i < toSpawn; i++)
            {
                GameObject newEnemy = Object.Instantiate(templateEnemy);
                newEnemy.name = "Enemy_AutoSpawned_" + i;
            }
        }

        // Refetch remaining/new enemies and randomize locations far from player
        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject pShooterForPos = Object.FindObjectOfType<PlayerShooter>()?.gameObject;
        Vector3 playerPos = pShooterForPos != null ? pShooterForPos.transform.position : Vector3.zero;

        foreach (var e in enemies)
        {
            if (e == null) continue;

            // Find a random position at least 10 units away from the player, and avoid occupied positions
            Vector3 randomPos = Vector3.zero;
            bool validPos = false;
            for (int attempt = 0; attempt < 50; attempt++)
            {
                randomPos = new Vector3(Random.Range(-roomBoundX, roomBoundX), e.transform.position.y, Random.Range(-roomBoundZ, roomBoundZ));
                validPos = Vector3.Distance(randomPos, playerPos) > 10f;
                
                if (validPos)
                {
                    foreach (var p in occupiedPositions)
                    {
                        if (Vector3.Distance(new Vector3(randomPos.x, 0, randomPos.z), new Vector3(p.x, 0, p.z)) < 3f)
                        {
                            validPos = false;
                            break;
                        }
                    }
                }
                if (validPos) break;
            }
            e.transform.position = randomPos;
            occupiedPositions.Add(randomPos);
            
            if (e.GetComponent<EnemyAI>() == null)
            {
                e.AddComponent<EnemyAI>();
            }
            EnemyAI ai = e.GetComponent<EnemyAI>();
            
            // Apply requested difficulty scaling for Level 2
            ai.bulletDamage = isLevel2 ? 5f : 2f;
            ai.canShoot = true;
            
            if (isLevel2)
            {
                ai.speed = 3f; // Faster enemy movement
                ai.bulletSpeed = 40f; // Faster enemy bullets
                ai.fireRate = Random.Range(0.5f, 1f); // Spam 1-2 bullets per second
            }
            else
            {
                ai.speed = 1.5f; // Slow enemy movement
                ai.bulletSpeed = 20f; // Normal bullet speed
                ai.fireRate = 2f; // Slow fire rate
            }
        }
        
        // 4. Update UI
        GameObject canvasObj = new GameObject("HUD Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        GameUIManager uiManager = canvasObj.AddComponent<GameUIManager>();
        uiManager.gameManager = gm;
        uiManager.objectiveManager = om;
        
        GameObject pShooter = Object.FindObjectOfType<PlayerShooter>()?.gameObject;
        if (pShooter != null) 
        {
            pShooter.tag = "Player"; // Ensure the tag is correct for collision detection
            uiManager.playerShooter = pShooter.GetComponent<PlayerShooter>();
            
            // Add PlayerHealth if missing
            PlayerHealth pHealth = pShooter.GetComponent<PlayerHealth>();
            if (pHealth == null) pHealth = pShooter.AddComponent<PlayerHealth>();
            uiManager.playerHealth = pHealth;
        }

        // Generate HUD Texts
        uiManager.ammoText = CreateUIText(canvasObj.transform, "AmmoText", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), "Ammo: 30 / \u221E", 40, TextAlignmentOptions.Right);
        uiManager.healthText = CreateUIText(canvasObj.transform, "HealthText", new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), "Health: 100", 40, TextAlignmentOptions.Left);
        uiManager.timerText = CreateUIText(canvasObj.transform, "TimerText", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), "Time: 60s", 40, TextAlignmentOptions.Center);
        
        uiManager.reloadingText = CreateUIText(canvasObj.transform, "ReloadingText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, "RELOADING...", 60, TextAlignmentOptions.Center);
        uiManager.reloadingText.gameObject.SetActive(false);

        // Objectives List (Right Side)
        uiManager.objectivesText = CreateUIText(canvasObj.transform, "ObjectivesText", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), "<b>OBJECTIVES:</b>\nLoading...", 28, TextAlignmentOptions.Right);
        uiManager.objectivesText.rectTransform.sizeDelta = new Vector2(500, 300);

        // Color Legend (Left Side)
        string colorLegendStr = "<b>COLORS:</b>\n1: <color=red>Red</color>\n2: <color=blue>Blue</color>\n3: <color=green>Green</color>\n4: <color=yellow>Yellow</color>";
        TextMeshProUGUI colorLegend = CreateUIText(canvasObj.transform, "ColorLegendText", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), colorLegendStr, 28, TextAlignmentOptions.Left);
        colorLegend.rectTransform.sizeDelta = new Vector2(300, 300);

        // Generate Lose Window
        GameObject loseWin = new GameObject("LoseWindow");
        loseWin.transform.SetParent(canvasObj.transform, false);
        RectTransform loseRt = loseWin.AddComponent<RectTransform>();
        loseRt.anchorMin = Vector2.zero; loseRt.anchorMax = Vector2.one; // Fill screen
        loseRt.offsetMin = Vector2.zero; loseRt.offsetMax = Vector2.zero;
        UnityEngine.UI.Image loseImg = loseWin.AddComponent<UnityEngine.UI.Image>();
        loseImg.color = new Color(0, 0, 0, 0.8f);
        CreateUIText(loseWin.transform, "LoseText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, "GAME OVER\n\nPress [ENTER] to Retry", 60, TextAlignmentOptions.Center);
        loseWin.SetActive(false);
        uiManager.loseWindow = loseWin;

        // Generate Win Window (Stats)
        GameObject winWin = new GameObject("WinWindow");
        winWin.transform.SetParent(canvasObj.transform, false);
        RectTransform winRt = winWin.AddComponent<RectTransform>();
        winRt.anchorMin = Vector2.zero; winRt.anchorMax = Vector2.one; // Fill screen
        winRt.offsetMin = Vector2.zero; winRt.offsetMax = Vector2.zero;
        UnityEngine.UI.Image winImg = winWin.AddComponent<UnityEngine.UI.Image>();
        winImg.color = new Color(0, 0, 0, 0.9f);
        
        TextMeshProUGUI statsTxt = CreateUIText(winWin.transform, "StatsText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, "STATS...", 40, TextAlignmentOptions.Center);
        statsTxt.rectTransform.sizeDelta = new Vector2(800, 600);
        
        StatsWindow statsScript = winWin.AddComponent<StatsWindow>();
        statsScript.objectiveManager = om;
        statsScript.gameManager = gm;
        if (pShooter != null) statsScript.playerShooter = pShooter.GetComponent<PlayerShooter>();
        statsScript.statsText = statsTxt;
        statsScript.nextSceneName = isLevel2 ? "MainMenu" : "Level2";
        winWin.SetActive(false);
        uiManager.winWindow = winWin;
    }

    private static void TransformToFurniture(GameObject target, string type, Material baseMaterial, Vector3 fixedPos)
    {
        // Remove existing mesh and collider from the root to make it a pure container
        if (target.GetComponent<MeshFilter>() != null) Object.DestroyImmediate(target.GetComponent<MeshFilter>());
        if (target.GetComponent<MeshRenderer>() != null) Object.DestroyImmediate(target.GetComponent<MeshRenderer>());
        if (target.GetComponent<Collider>() != null) Object.DestroyImmediate(target.GetComponent<Collider>());
        
        target.transform.localScale = Vector3.one * 1.5f; // Scaled up to make them a little big
        target.transform.rotation = Quaternion.identity;
        // Position firmly in the requested spot
        target.transform.position = fixedPos;
        
        switch (type)
        {
            case "Table":
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(2f, 0.1f, 1.5f), new Vector3(0, 0.95f, 0), baseMaterial);
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 0.9f, 0.1f), new Vector3(-0.9f, 0.45f, -0.65f), baseMaterial);
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 0.9f, 0.1f), new Vector3(0.9f, 0.45f, -0.65f), baseMaterial);
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 0.9f, 0.1f), new Vector3(-0.9f, 0.45f, 0.65f), baseMaterial);
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 0.9f, 0.1f), new Vector3(0.9f, 0.45f, 0.65f), baseMaterial);
                break;
            case "Chair":
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.6f, 0.1f, 0.6f), new Vector3(0, 0.55f, 0), baseMaterial); // Seat
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.6f, 0.6f, 0.1f), new Vector3(0, 0.9f, -0.25f), baseMaterial); // Back
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 0.5f, 0.1f), new Vector3(-0.25f, 0.25f, -0.25f), baseMaterial);
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 0.5f, 0.1f), new Vector3(0.25f, 0.25f, -0.25f), baseMaterial);
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 0.5f, 0.1f), new Vector3(-0.25f, 0.25f, 0.25f), baseMaterial);
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 0.5f, 0.1f), new Vector3(0.25f, 0.25f, 0.25f), baseMaterial);
                break;
            case "Cabinet":
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(1.2f, 2.4f, 0.8f), new Vector3(0, 1.2f, 0), baseMaterial); // Body
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.05f, 1f, 0.05f), new Vector3(-0.1f, 1.2f, 0.42f), baseMaterial); // Handle L
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.05f, 1f, 0.05f), new Vector3(0.1f, 1.2f, 0.42f), baseMaterial); // Handle R
                break;
            case "Lamp":
                CreatePrimitiveChild(target, PrimitiveType.Cylinder, new Vector3(0.4f, 0.05f, 0.4f), new Vector3(0, 0.05f, 0), baseMaterial); // Base
                CreatePrimitiveChild(target, PrimitiveType.Cylinder, new Vector3(0.1f, 1f, 0.1f), new Vector3(0, 1f, 0), baseMaterial); // Pole
                CreatePrimitiveChild(target, PrimitiveType.Sphere, new Vector3(0.6f, 0.5f, 0.6f), new Vector3(0, 2f, 0), baseMaterial); // Shade
                break;
            case "Bed":
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(2f, 0.4f, 3f), new Vector3(0, 0.2f, 0), baseMaterial); // Mattress
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(2f, 1f, 0.2f), new Vector3(0, 0.5f, -1.4f), baseMaterial); // Headboard
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(1.8f, 0.2f, 0.8f), new Vector3(0, 0.5f, 1f), baseMaterial); // Blanket
                break;
            case "Desk":
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(2.5f, 0.1f, 1.2f), new Vector3(0, 1f, 0), baseMaterial); // Top
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 1f, 1.1f), new Vector3(-1.2f, 0.5f, 0), baseMaterial); // Leg L
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.1f, 1f, 1.1f), new Vector3(1.2f, 0.5f, 0), baseMaterial); // Leg R
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.6f, 0.8f, 1.1f), new Vector3(0.8f, 0.4f, 0), baseMaterial); // Drawers
                break;
            case "Sofa":
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(2.4f, 0.4f, 1f), new Vector3(0, 0.2f, 0), baseMaterial); // Seat
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(2.4f, 1f, 0.3f), new Vector3(0, 0.7f, -0.35f), baseMaterial); // Backrest
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.3f, 0.6f, 1f), new Vector3(-1.25f, 0.5f, 0), baseMaterial); // Arm L
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(0.3f, 0.6f, 1f), new Vector3(1.25f, 0.5f, 0), baseMaterial); // Arm R
                break;
            case "Bookshelf":
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(1.5f, 2.5f, 0.6f), new Vector3(0, 1.25f, 0), baseMaterial); // Back/Body
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(1.4f, 0.05f, 0.5f), new Vector3(0, 0.5f, -0.05f), baseMaterial); // Shelf 1
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(1.4f, 0.05f, 0.5f), new Vector3(0, 1.25f, -0.05f), baseMaterial); // Shelf 2
                CreatePrimitiveChild(target, PrimitiveType.Cube, new Vector3(1.4f, 0.05f, 0.5f), new Vector3(0, 2f, -0.05f), baseMaterial); // Shelf 3
                break;
        }
    }
    
    private static void CreatePrimitiveChild(GameObject parent, PrimitiveType type, Vector3 scale, Vector3 localPos, Material mat)
    {
        GameObject child = GameObject.CreatePrimitive(type);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = localPos;
        child.transform.localScale = scale;
        child.tag = "Obstacle"; // Crucial for Bullet script
        if (mat != null) child.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static TextMeshProUGUI CreateUIText(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, string defaultText, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent, false);
        
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(400, 100); // Made slightly larger for spacing
        
        TextMeshProUGUI text = txtObj.AddComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        
        // Add a small outline for visibility against bright backgrounds
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(0, 0, 0, 255);
        
        return text;
    }
}
