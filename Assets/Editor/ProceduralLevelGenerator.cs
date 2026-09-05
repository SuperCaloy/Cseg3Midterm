using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ProceduralLevelGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Campaign Levels")]
    public static void GenerateLevels()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Cannot generate levels while in Play Mode! Please exit Play Mode first.");
            return;
        }

        Debug.Log("Starting Procedural Level Generation based on Active Scene...");

        // Save current scene state first
        EditorSceneManager.SaveOpenScenes();
        string baseScenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(baseScenePath))
        {
            Debug.LogError("Please save your current scene first before generating levels!");
            return;
        }

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // New Realistic Colored Textures for specific obstacles
        Texture2D woodTex = TextureGenerator.GenerateFractalNoise("WoodTex", 256, 256, 40f, 3, 0.4f, 2f, new Color(0.4f, 0.25f, 0.1f), new Color(0.55f, 0.35f, 0.15f)); // Brown Wood
        Texture2D canvasTex = TextureGenerator.GenerateFractalNoise("CanvasTex", 256, 256, 50f, 2, 0.3f, 2f, new Color(0.7f, 0.65f, 0.5f), new Color(0.8f, 0.75f, 0.6f)); // Khaki Sandbag
        Texture2D redMetalTex = TextureGenerator.GenerateFractalNoise("RedMetalTex", 256, 256, 15f, 3, 0.5f, 2f, new Color(0.6f, 0.1f, 0.1f), new Color(0.8f, 0.15f, 0.15f)); // Red Painted Metal
        Texture2D yellowMetalTex = TextureGenerator.GenerateFractalNoise("YellowMetalTex", 256, 256, 15f, 3, 0.5f, 2f, new Color(0.7f, 0.6f, 0.1f), new Color(0.9f, 0.8f, 0.15f)); // Yellow Painted Metal
        Texture2D darkMetalTex = TextureGenerator.GenerateFractalNoise("DarkMetalTex", 256, 256, 15f, 3, 0.5f, 2f, new Color(0.1f, 0.1f, 0.1f), new Color(0.2f, 0.2f, 0.2f));

        Material woodMat = CreateRealisticMaterial("WoodMat", Color.white, 0.0f, 0.1f, woodTex);
        Material canvasMat = CreateRealisticMaterial("CanvasMat", Color.white, 0.0f, 0.05f, canvasTex);
        Material redMetalMat = CreateRealisticMaterial("RedMetalMat", Color.white, 0.7f, 0.6f, redMetalTex);
        Material yellowMetalMat = CreateRealisticMaterial("YellowMetalMat", Color.white, 0.7f, 0.6f, yellowMetalTex);
        Material darkMetalMat = CreateRealisticMaterial("DarkMetalMat", Color.white, 0.9f, 0.5f, darkMetalTex);
        GameObject[] enemyPrefabs = CreateProceduralHumanoidEnemies(darkMetalMat, redMetalMat, yellowMetalMat);

        GameObject eventTilePrefab = CreateBasicPrefab("EventTilePrefab", new Color(0.15f, 0.15f, 0.15f), 0.1f, 0.8f);
        if (eventTilePrefab != null)
        {
            if (eventTilePrefab.GetComponent<EventTile>() == null) eventTilePrefab.AddComponent<EventTile>();
            if (eventTilePrefab.GetComponent<Collider>() == null) eventTilePrefab.AddComponent<BoxCollider>();
            eventTilePrefab.GetComponent<Collider>().isTrigger = true;
        }

        for (int level = 1; level <= 4; level++)
        {
            // Organic Earthy / Muddy Colors for realistic terrain
            Color floorBase = new Color(0.25f, 0.18f, 0.11f) * UnityEngine.Random.Range(0.8f, 1.2f);
            Color floorHighlight = new Color(0.35f, 0.28f, 0.18f) * UnityEngine.Random.Range(0.8f, 1.2f);
            
            // Stone with moss growth for walls
            Color wallBase = new Color(0.2f, 0.22f, 0.2f);
            Color wallHighlight = new Color(0.1f, 0.35f, 0.1f); // Vibrant moss highlight
            
            // Load a High-Resolution Wall Texture from the newly imported package
            int randomWallIdx = UnityEngine.Random.Range(1, 19); // 1 to 18
            string wallStr = randomWallIdx.ToString("D2"); // "01", "02", etc.
            string wallMatPath = $"Assets/ADG_Textures/walls_vol1/wall{wallStr}/wall{wallStr}.mat";
            Material wallMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(wallMatPath);
            
            // Upgrade legacy Standard shader to URP Lit if necessary to prevent pink/magenta textures
            if (wallMat != null && (wallMat.shader.name == "Standard" || wallMat.shader.name == "Hidden/InternalErrorShader"))
            {
                Texture mainTex = wallMat.HasProperty("_MainTex") ? wallMat.GetTexture("_MainTex") : null;
                wallMat.shader = Shader.Find("Universal Render Pipeline/Lit");
                if (mainTex != null && wallMat.HasProperty("_BaseMap"))
                {
                    wallMat.SetTexture("_BaseMap", mainTex);
                }
                UnityEditor.EditorUtility.SetDirty(wallMat);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            
            // Fallback in case it wasn't found
            if (wallMat == null)
            {
                Texture2D wallTex = TextureGenerator.GenerateFractalNoise($"WallTex_Level{level}", 512, 512, 12f, 5, 0.6f, 2f, wallBase, wallHighlight);
                wallMat = CreateRealisticMaterial($"WallMat_Level{level}", Color.white, 0.8f, 0.4f, wallTex);
            }
            
            Texture2D grassTex = TextureGenerator.GenerateFractalNoise($"GrassTex_Level{level}", 128, 128, 20f, 3, 0.5f, 2f, new Color(0.1f, 0.4f, 0.1f), new Color(0.3f, 0.7f, 0.2f));
            Material grassMat = CreateRealisticMaterial($"GrassMat_Level{level}", Color.white, 0.1f, 0.9f, grassTex);
            grassMat.enableInstancing = true;
            
            // Generate Puddle Material (High smoothness, dark muddy water)
            Texture2D puddleTex = TextureGenerator.GenerateFractalNoise($"PuddleTex_Level{level}", 128, 128, 5f, 2, 0.5f, 2f, new Color(0.1f, 0.08f, 0.05f, 0.9f), new Color(0.2f, 0.15f, 0.1f, 0.95f));
            Material puddleMat = CreateRealisticMaterial($"PuddleMat_Level{level}", Color.white, 0.9f, 1.0f, puddleTex);
            // Setup Transparency for Puddle
            puddleMat.SetFloat("_Mode", 3); // Transparent
            puddleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            puddleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            puddleMat.SetInt("_ZWrite", 0);
            puddleMat.DisableKeyword("_ALPHATEST_ON");
            puddleMat.EnableKeyword("_ALPHABLEND_ON");
            puddleMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            puddleMat.renderQueue = 3000;
            
            // Generate Vine Material
            Texture2D vineTex = TextureGenerator.GenerateFractalNoise($"VineTex_Level{level}", 64, 128, 10f, 2, 0.5f, 2f, new Color(0.05f, 0.2f, 0.05f), new Color(0.1f, 0.4f, 0.1f));
            Material vineMat = CreateRealisticMaterial($"VineMat_Level{level}", Color.white, 0.0f, 0.8f, vineTex);

            // Generate Higher Resolution 512x512 Procedural Textures Per Level
            Texture2D floorTex = TextureGenerator.GenerateFractalNoise($"FloorTex_Level{level}", 512, 512, 25f, 4, 0.5f, 2f, floorBase, floorHighlight);
            Texture2D coverTex = TextureGenerator.GenerateFractalNoise($"CoverTex_Level{level}", 512, 512, 35f, 3, 0.4f, 2f, new Color(0.4f, 0.4f, 0.4f), new Color(0.6f, 0.6f, 0.6f));

            Material floorMat = CreateRealisticMaterial($"FloorMat_Level{level}", Color.white, 0.0f, 0.7f, floorTex);
            Material coverMat = CreateRealisticMaterial($"CoverMat_Level{level}", Color.white, 0.0f, 0.2f, coverTex);

            GameObject[] obstaclePrefabs = new GameObject[] {
                CreateConcreteBarricade(coverMat),
                CreateSteelPillar(wallMat),
                CreateCrateStack(woodMat),
                CreateSandbagWall(canvasMat),
                CreateOilDrum(redMetalMat),
                CreateIndustrialPipes(yellowMetalMat),
                CreateServerRack(darkMetalMat),
                CreateComputerDesk(woodMat),
                CreateSupplyShelves(wallMat)
            };

            GameObject ammoPrefab = CreateAmmoPrefab();

            // Open base scene
            Scene currentScene = EditorSceneManager.OpenScene(baseScenePath, OpenSceneMode.Single);
            
            // Clean up old environment and UI
            CleanupOldObjects();

            // Generate Dungeon Geometry
            GameObject environment = new GameObject("Environment");
            
            // Generate Grass Mesh dynamically
            Mesh grassMesh = CreateGrassMesh();
            List<Matrix4x4> grassMatrices = new List<Matrix4x4>();

            int gridSize = 15 + (level * 10); // L1: 25, L2: 35, L3: 45, L4: 55
            BSPDungeonGenerator bsp = new BSPDungeonGenerator(level * 100);
            Grid2D<TileType> grid = bsp.Generate(gridSize, gridSize, 6);

            List<BoxCollider> floorColliders = new List<BoxCollider>();

            float tileSize = 2f;
            Vector3 centerOffset = new Vector3(-gridSize * tileSize / 2f, 0, -gridSize * tileSize / 2f);

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize) + centerOffset;
                    TileType type = grid.Get(x, y);

                    if (type == TileType.Floor)
                    {
                        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        floor.transform.SetParent(environment.transform);
                        floor.transform.position = pos;
                        floor.transform.localScale = new Vector3(tileSize, 1f, tileSize);
                        floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                        floorColliders.Add(floor.GetComponent<BoxCollider>());

                        // Grass Spawning
                        int grassCount = UnityEngine.Random.Range(5, 15);
                        for(int g = 0; g < grassCount; g++)
                        {
                            Vector3 gPos = pos + new Vector3(UnityEngine.Random.Range(-0.9f, 0.9f), 0.5f, UnityEngine.Random.Range(-0.9f, 0.9f));
                            Quaternion gRot = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);
                            Vector3 gScale = Vector3.one * UnityEngine.Random.Range(0.7f, 1.3f);
                            grassMatrices.Add(Matrix4x4.TRS(gPos, gRot, gScale));
                        }

                        // Puddle Spawning
                        if (UnityEngine.Random.value < 0.1f)
                        {
                            GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            puddle.transform.SetParent(environment.transform);
                            puddle.transform.position = pos + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.51f, UnityEngine.Random.Range(-0.5f, 0.5f)); // Slightly above floor
                            puddle.transform.localScale = new Vector3(UnityEngine.Random.Range(0.5f, 1.5f), 0.01f, UnityEngine.Random.Range(0.5f, 1.5f));
                            puddle.GetComponent<Renderer>().sharedMaterial = puddleMat;
                            DestroyImmediate(puddle.GetComponent<Collider>());
                        }
                        else if (UnityEngine.Random.value < 0.1f)
                        {
                            GameObject obsPrefab = obstaclePrefabs[UnityEngine.Random.Range(0, obstaclePrefabs.Length)];
                            if (obsPrefab != null)
                            {
                                GameObject cover = PrefabUtility.InstantiatePrefab(obsPrefab) as GameObject;
                                if (cover != null)
                                {
                                    cover.transform.SetParent(environment.transform);
                                    cover.transform.position = pos;
                                }
                            }
                        }
                        else if (UnityEngine.Random.value < 0.05f) // 5% Event Tile
                        {
                            GameObject eTile = PrefabUtility.InstantiatePrefab(eventTilePrefab) as GameObject;
                            eTile.transform.SetParent(environment.transform);
                            eTile.transform.position = pos + new Vector3(0, 0.55f, 0);
                            eTile.transform.localScale = new Vector3(tileSize * 0.8f, 0.1f, tileSize * 0.8f);
                            EventTile etComp = eTile.GetComponent<EventTile>();
                            if (etComp != null)
                            {
                                etComp.ammoPrefab = ammoPrefab;
                                etComp.enemyPrefabs = enemyPrefabs;
                            }
                        }
                    }
                    else if (type == TileType.Wall)
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.transform.SetParent(environment.transform);
                        wall.transform.position = pos + new Vector3(0, 6f, 0); // Center is half of 12f
                        wall.transform.localScale = new Vector3(tileSize, 12f, tileSize); // Tripled the height
                        wall.GetComponent<Renderer>().sharedMaterial = wallMat;
                        
                        // Vine Spawning
                        if (UnityEngine.Random.value < 0.2f)
                        {
                            GameObject vine = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            vine.transform.SetParent(wall.transform);
                            vine.transform.localPosition = new Vector3(UnityEngine.Random.value > 0.5f ? 0.51f : -0.51f, 0.2f, 0); // Attach to face
                            vine.transform.localRotation = Quaternion.Euler(0, vine.transform.localPosition.x > 0 ? 90 : -90, 0);
                            vine.transform.localScale = new Vector3(1f, 0.5f, 1f);
                            vine.GetComponent<Renderer>().sharedMaterial = vineMat;
                            DestroyImmediate(vine.GetComponent<Collider>());
                        }
                    }
                }
            }

            // Setup Grass Instancer
            ProceduralGrassInstancer grassInstancer = environment.AddComponent<ProceduralGrassInstancer>();
            grassInstancer.grassMesh = grassMesh;
            grassInstancer.grassMaterial = grassMat;
            grassInstancer.Initialize(grassMatrices);

            // Setup Spawners
            GameObject spawners = new GameObject("Spawners");
            
            RandomSpawner enemySpawner = spawners.AddComponent<RandomSpawner>();
            enemySpawner.spawnInterval = Mathf.Max(0.5f, 3f - (level * 0.5f)); // Spawn enemies much faster
            enemySpawner.maxConcurrent = 10 + (level * 5); // Allow a larger horde
            enemySpawner.prefabsToSpawn = enemyPrefabs;
            enemySpawner.spawnAreas = floorColliders;
            enemySpawner.spawnNearPlayer = true;
            enemySpawner.minSpawnRadius = 15f;
            enemySpawner.maxSpawnRadius = 30f; // Far enough to not spawn on top of player, close enough to keep action going

            RandomSpawner ammoSpawner = spawners.AddComponent<RandomSpawner>();
            ammoSpawner.spawnInterval = Mathf.Max(5f, 25f - (level * 2f)); // More frequent ammo on harder levels
            ammoSpawner.maxConcurrent = 3 + level; // Allow multiple ammo boxes to exist at once
            ammoSpawner.prefabsToSpawn = new GameObject[] { ammoPrefab };
            ammoSpawner.spawnAreas = floorColliders;
            ammoSpawner.spawnNearPlayer = true;
            ammoSpawner.minSpawnRadius = 5f;
            ammoSpawner.maxSpawnRadius = 20f; // Close to player so they can easily find it

            // Move Player to start position (0, 1.5, 0) if player exists
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0, 1.5f, 0);
                
                // Spawn a free ammo box right in front of the player at spawn
                GameObject startingAmmo = (GameObject)PrefabUtility.InstantiatePrefab(ammoPrefab);
                if (startingAmmo != null)
                {
                    startingAmmo.transform.position = new Vector3(0, 1.5f, 3f);
                    startingAmmo.transform.SetParent(spawners.transform);
                }
            }

            // Setup UI (Brand New)
            CreateUI();

            // Core Managers
            if (FindObjectOfType<GameSessionManager>() == null && level == 1)
            {
                GameObject gsm = new GameObject("GameSessionManager");
                gsm.AddComponent<GameSessionManager>();
            }

            if (FindObjectOfType<GameManager>() == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // Save Scene as Level X
            string scenePath = $"Assets/Scenes/Level{level}.unity";
            EditorSceneManager.SaveScene(currentScene, scenePath);
            
            // Add to Build Settings
            bool found = false;
            foreach (var bs in buildScenes)
            {
                if (bs.path == scenePath) found = true;
            }
            if (!found)
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();

        // Finally, load Level 1 for the user to see
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);

        Debug.Log("Procedural Campaign Generation Complete! Scenes Level1 to Level4 have been created and added to Build Settings.");
    }

    private static void CleanupOldObjects()
    {
        string[] oldNames = { "Environment", "Spawners", "HUD Canvas", "ObjectiveManager", "Wall", "Floor", "Plane", "Cube", "Table", "Cabinet", "Lamp", "Chair", "Desk", "Sofa", "Bookshelf", "Bed", "Invisible Box", "Ball", "Enemy", "EventSystem" };
        foreach (string n in oldNames)
        {
            GameObject[] objs = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in objs)
            {
                if (obj == null) continue;

                if (obj.name.Contains(n) || obj.name.StartsWith("Room_"))
                {
                    DestroyImmediate(obj);
                }
            }
        }
        
        // Also explicitly delete exact matches for "Canvas" to remove accidental MainMenu canvases
        GameObject[] exactObjs = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in exactObjs)
        {
            if (obj != null && obj.name == "Canvas")
            {
                DestroyImmediate(obj);
            }
        }
    }

    private static void CreateUI()
    {
        GameObject canvasObj = new GameObject("HUD Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Objectives Text
        GameObject objTextObj = new GameObject("ObjectivesText");
        objTextObj.transform.SetParent(canvasObj.transform, false);
        var objText = objTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        objText.text = "Objectives";
        objText.fontSize = 24;
        objText.alignment = TMPro.TextAlignmentOptions.TopRight;
        RectTransform rt = objText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(300, 100);

        // Timer Text
        GameObject timerTextObj = new GameObject("TimerText");
        timerTextObj.transform.SetParent(canvasObj.transform, false);
        var timerText = timerTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        timerText.text = "Time: 60s";
        timerText.fontSize = 32;
        timerText.alignment = TMPro.TextAlignmentOptions.Top;
        RectTransform trt = timerText.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1);
        trt.anchorMax = new Vector2(0.5f, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.anchoredPosition = new Vector2(0, -20);

        // Ammo Text
        GameObject ammoTextObj = new GameObject("AmmoText");
        ammoTextObj.transform.SetParent(canvasObj.transform, false);
        var ammoText = ammoTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        ammoText.text = "Ammo: 30 / 60";
        ammoText.fontSize = 24;
        ammoText.alignment = TMPro.TextAlignmentOptions.BottomRight;
        RectTransform art = ammoText.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(1, 0);
        art.anchorMax = new Vector2(1, 0);
        art.pivot = new Vector2(1, 0);
        art.anchoredPosition = new Vector2(-20, 20);

        // Reloading Text
        GameObject reloadingTextObj = new GameObject("ReloadingText");
        reloadingTextObj.transform.SetParent(canvasObj.transform, false);
        var reloadingText = reloadingTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        reloadingText.text = "RELOADING...";
        reloadingText.fontSize = 36;
        reloadingText.color = Color.yellow;
        reloadingText.alignment = TMPro.TextAlignmentOptions.Center;
        reloadingText.enableWordWrapping = false; // Prevent it from splitting into two lines
        RectTransform rtReload = reloadingText.GetComponent<RectTransform>();
        rtReload.anchorMin = new Vector2(0.5f, 0.5f);
        rtReload.anchorMax = new Vector2(0.5f, 0.5f);
        rtReload.pivot = new Vector2(0.5f, 0.5f);
        rtReload.anchoredPosition = new Vector2(0, -50);
        rtReload.sizeDelta = new Vector2(400, 100); // Make the box wide enough for the text
        reloadingTextObj.SetActive(false);

        // GameUIManager Link
        GameUIManager uiManager = canvasObj.AddComponent<GameUIManager>();
        uiManager.objectivesText = objText;
        uiManager.timerText = timerText;
        uiManager.ammoText = ammoText;
        uiManager.reloadingText = reloadingText;
        uiManager.gameManager = FindObjectOfType<GameManager>();

        // Stats Panel
        GameObject statsPanelObj = new GameObject("StatsPanel");
        statsPanelObj.transform.SetParent(canvasObj.transform, false);
        statsPanelObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.8f);
        RectTransform srt = statsPanelObj.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.sizeDelta = Vector2.zero;
        statsPanelObj.SetActive(false);

        GameObject statsTextObj = new GameObject("StatsText");
        statsTextObj.transform.SetParent(statsPanelObj.transform, false);
        var statsTextStr = statsTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        statsTextStr.alignment = TMPro.TextAlignmentOptions.Center;
        statsTextStr.fontSize = 32;
        RectTransform strt = statsTextStr.GetComponent<RectTransform>();
        strt.anchorMin = Vector2.zero;
        strt.anchorMax = Vector2.one;
        strt.sizeDelta = Vector2.zero;

        StatsWindow statsWindow = statsPanelObj.AddComponent<StatsWindow>();
        statsWindow.statsText = statsTextStr;
        statsWindow.gameManager = uiManager.gameManager;
        uiManager.winWindow = statsPanelObj; 
        
        // Game Over Panel (Lose Window)
        GameObject losePanelObj = new GameObject("LosePanel");
        losePanelObj.transform.SetParent(canvasObj.transform, false);
        losePanelObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0, 0, 0.9f); // Dark red tint
        RectTransform lrt = losePanelObj.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        losePanelObj.SetActive(false);
        uiManager.loseWindow = losePanelObj;
        
        GameObject loseTextObj = new GameObject("LoseText");
        loseTextObj.transform.SetParent(losePanelObj.transform, false);
        var loseText = loseTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        loseText.text = "YOU DIED\n\n<size=20>Press ENTER to Try Again</size>";
        loseText.alignment = TMPro.TextAlignmentOptions.Center;
        loseText.fontSize = 48;
        loseText.color = Color.red;
        RectTransform ltrt = loseText.GetComponent<RectTransform>();
        ltrt.anchorMin = Vector2.zero;
        ltrt.anchorMax = Vector2.one;
        ltrt.sizeDelta = Vector2.zero;

        // Pause Panel
        GameObject pausePanelObj = new GameObject("PausePanel");
        pausePanelObj.transform.SetParent(canvasObj.transform, false);
        pausePanelObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.85f);
        RectTransform prt = pausePanelObj.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.sizeDelta = Vector2.zero;
        pausePanelObj.SetActive(false);
        uiManager.pauseWindow = pausePanelObj;
        
        GameObject pauseTextObj = new GameObject("PauseText");
        pauseTextObj.transform.SetParent(pausePanelObj.transform, false);
        var pauseText = pauseTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        pauseText.text = "PAUSED\n\n<size=24>Press ESC to Continue</size>\n<size=24>Press Q to Quit</size>";
        pauseText.alignment = TMPro.TextAlignmentOptions.Center;
        pauseText.fontSize = 48;
        pauseText.color = Color.white;
        RectTransform ptrt = pauseText.GetComponent<RectTransform>();
        ptrt.anchorMin = Vector2.zero;
        ptrt.anchorMax = Vector2.one;
        ptrt.sizeDelta = Vector2.zero;

        // Find PlayerShooter and PlayerHealth on the player tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            uiManager.playerShooter = player.GetComponentInChildren<PlayerShooter>();
            if (uiManager.playerShooter == null) uiManager.playerShooter = FindObjectOfType<PlayerShooter>();
            
            uiManager.playerHealth = player.GetComponentInChildren<PlayerHealth>();
            if (uiManager.playerHealth == null) uiManager.playerHealth = FindObjectOfType<PlayerHealth>();
        }

        // Health Text
        GameObject healthTextObj = new GameObject("HealthText");
        healthTextObj.transform.SetParent(canvasObj.transform, false);
        var healthText = healthTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        healthText.text = "Health: 100";
        healthText.fontSize = 24;
        healthText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
        RectTransform hrt = healthText.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 0);
        hrt.anchorMax = new Vector2(0, 0);
        hrt.pivot = new Vector2(0, 0);
        hrt.anchoredPosition = new Vector2(20, 20);
        uiManager.healthText = healthText;

        // Notification Text
        GameObject notifObj = new GameObject("NotificationText");
        notifObj.transform.SetParent(canvasObj.transform, false);
        var notifText = notifObj.AddComponent<TMPro.TextMeshProUGUI>();
        notifText.text = "";
        notifText.fontSize = 28;
        notifText.alignment = TMPro.TextAlignmentOptions.Center;
        notifText.color = Color.yellow;
        RectTransform nrt = notifText.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.5f, 0.7f);
        nrt.anchorMax = new Vector2(0.5f, 0.7f);
        nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.sizeDelta = new Vector2(600, 100);
        nrt.anchoredPosition = Vector2.zero;
        uiManager.notificationText = notifText;
        notifObj.SetActive(false); // GameUIManager will enable it
    }

    private static Material CreateRealisticMaterial(string name, Color color, float metallic, float smoothness, Texture2D tex = null)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string matPath = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        mat.color = color;
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject CreateAmmoPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string path = "Assets/Prefabs/RealisticAmmoBox.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("RealisticAmmoBox");
        
        // Base army-green ammo crate
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(root.transform);
        box.transform.localPosition = Vector3.zero;
        box.transform.localScale = new Vector3(0.6f, 0.35f, 0.4f);
        Material boxMat = CreateRealisticMaterial("AmmoBoxMat", new Color(0.2f, 0.3f, 0.1f), 0.1f, 0.5f);
        box.GetComponent<Renderer>().sharedMaterial = boxMat;
        DestroyImmediate(box.GetComponent<Collider>()); // Remove internal collider

        // Golden bullet icon on top
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bullet.transform.SetParent(root.transform);
        bullet.transform.localPosition = new Vector3(0, 0.25f, 0);
        bullet.transform.localScale = new Vector3(0.12f, 0.15f, 0.12f);
        bullet.transform.localRotation = Quaternion.Euler(0, 0, 90);
        Material bulletMat = CreateRealisticMaterial("AmmoBulletMat", new Color(0.9f, 0.7f, 0.1f), 0.9f, 0.9f);
        bullet.GetComponent<Renderer>().sharedMaterial = bulletMat;
        DestroyImmediate(bullet.GetComponent<Collider>());

        // Main trigger collider
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(0.7f, 0.6f, 0.5f);
        col.isTrigger = true;
        
        // Float/Bob script can be added, or just AmmoPickup
        root.AddComponent<AmmoPickup>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateBasicPrefab(string name, Color color, float metallic, float smoothness)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string path = $"Assets/Prefabs/{name}.prefab";

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = name;
        Material mat = CreateRealisticMaterial(name + "Mat", color, metallic, smoothness);
        temp.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        DestroyImmediate(temp);
        return prefab;
    }

    private static GameObject CreateConcreteBarricade(Material mat)
    {
        string path = "Assets/Prefabs/ConcreteBarricade.prefab";

        GameObject root = new GameObject("ConcreteBarricade");
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.transform.SetParent(root.transform);
        block.transform.localScale = new Vector3(2f, 1f, 0.5f);
        block.transform.localPosition = new Vector3(0, 0.5f, 0);
        block.GetComponent<Renderer>().sharedMaterial = mat;
        
        GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftLeg.transform.SetParent(root.transform);
        leftLeg.transform.localScale = new Vector3(0.5f, 1f, 1f);
        leftLeg.transform.localPosition = new Vector3(-0.75f, 0.5f, 0);
        leftLeg.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightLeg.transform.SetParent(root.transform);
        rightLeg.transform.localScale = new Vector3(0.5f, 1f, 1f);
        rightLeg.transform.localPosition = new Vector3(0.75f, 0.5f, 0);
        rightLeg.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateSteelPillar(Material mat)
    {
        string path = "Assets/Prefabs/SteelPillar.prefab";

        GameObject root = new GameObject("SteelPillar");
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.transform.SetParent(root.transform);
        cyl.transform.localScale = new Vector3(1f, 1.5f, 1f);
        cyl.transform.localPosition = new Vector3(0, 1.5f, 0);
        cyl.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject baseCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseCyl.transform.SetParent(root.transform);
        baseCyl.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
        baseCyl.transform.localPosition = new Vector3(0, 0.1f, 0);
        baseCyl.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateCrateStack(Material mat)
    {
        string path = "Assets/Prefabs/CrateStack.prefab";

        GameObject root = new GameObject("CrateStack");
        Vector3[] pos = { new Vector3(-0.4f, 0.5f, 0), new Vector3(0.4f, 0.5f, 0.2f), new Vector3(0, 1.5f, 0.1f) };
        foreach (Vector3 p in pos)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.transform.SetParent(root.transform);
            crate.transform.localPosition = p;
            crate.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 45), 0);
            crate.GetComponent<Renderer>().sharedMaterial = mat;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }
    private static GameObject CreateSandbagWall(Material mat)
    {
        string path = "Assets/Prefabs/SandbagWall.prefab";

        GameObject root = new GameObject("SandbagWall");
        for (int i = 0; i < 4; i++)
        {
            GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bag.transform.SetParent(root.transform);
            bag.transform.localScale = new Vector3(0.5f, 0.4f, 1.2f);
            bag.transform.localPosition = new Vector3(0, 0.2f + (i * 0.35f), 0);
            bag.transform.localRotation = Quaternion.Euler(90, UnityEngine.Random.Range(-5f, 5f), 0);
            bag.GetComponent<Renderer>().sharedMaterial = mat;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateOilDrum(Material mat)
    {
        string path = "Assets/Prefabs/OilDrum.prefab";

        GameObject root = new GameObject("OilDrum");
        
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.SetParent(root.transform);
        body.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);
        body.transform.localPosition = new Vector3(0, 0.7f, 0);
        body.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject rim1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim1.transform.SetParent(root.transform);
        rim1.transform.localScale = new Vector3(0.82f, 0.05f, 0.82f);
        rim1.transform.localPosition = new Vector3(0, 0.4f, 0);
        rim1.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject rim2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim2.transform.SetParent(root.transform);
        rim2.transform.localScale = new Vector3(0.82f, 0.05f, 0.82f);
        rim2.transform.localPosition = new Vector3(0, 1.0f, 0);
        rim2.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateIndustrialPipes(Material mat)
    {
        string path = "Assets/Prefabs/IndustrialPipes.prefab";

        GameObject root = new GameObject("IndustrialPipes");
        
        GameObject pipe1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipe1.transform.SetParent(root.transform);
        pipe1.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);
        pipe1.transform.localPosition = new Vector3(-0.4f, 1.5f, 0);
        pipe1.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject pipe2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipe2.transform.SetParent(root.transform);
        pipe2.transform.localScale = new Vector3(0.3f, 1.0f, 0.3f);
        pipe2.transform.localPosition = new Vector3(0.4f, 1.0f, 0);
        pipe2.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject crossPipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crossPipe.transform.SetParent(root.transform);
        crossPipe.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        crossPipe.transform.localPosition = new Vector3(0, 1.5f, 0);
        crossPipe.transform.localRotation = Quaternion.Euler(0, 0, 90);
        crossPipe.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject joint1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        joint1.transform.SetParent(root.transform);
        joint1.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        joint1.transform.localPosition = new Vector3(-0.4f, 1.5f, 0);
        joint1.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject joint2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        joint2.transform.SetParent(root.transform);
        joint2.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        joint2.transform.localPosition = new Vector3(0.4f, 1.5f, 0);
        joint2.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateServerRack(Material mat)
    {
        string path = "Assets/Prefabs/ServerRack.prefab";

        GameObject root = new GameObject("ServerRack");
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.transform.SetParent(root.transform);
        frame.transform.localScale = new Vector3(0.8f, 2.2f, 0.6f);
        frame.transform.localPosition = new Vector3(0, 1.1f, 0);
        frame.GetComponent<Renderer>().sharedMaterial = mat;

        Material lightMat = CreateRealisticMaterial("ServerLightMat", Color.green, 0f, 0f);
        if (lightMat.IsKeywordEnabled("_EMISSION") == false)
        {
            lightMat.EnableKeyword("_EMISSION");
            lightMat.SetColor("_EmissionColor", Color.green * 2f);
        }

        for (int i = 0; i < 5; i++)
        {
            GameObject light = GameObject.CreatePrimitive(PrimitiveType.Cube);
            light.transform.SetParent(root.transform);
            light.transform.localScale = new Vector3(0.1f, 0.05f, 0.65f);
            light.transform.localPosition = new Vector3(-0.2f, 0.5f + (i * 0.35f), 0);
            light.GetComponent<Renderer>().sharedMaterial = lightMat;
            DestroyImmediate(light.GetComponent<Collider>());
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateComputerDesk(Material mat)
    {
        string path = "Assets/Prefabs/ComputerDesk.prefab";

        GameObject root = new GameObject("ComputerDesk");
        GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        desk.transform.SetParent(root.transform);
        desk.transform.localScale = new Vector3(2f, 0.1f, 1f);
        desk.transform.localPosition = new Vector3(0, 0.9f, 0);
        desk.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject leg1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leg1.transform.SetParent(root.transform);
        leg1.transform.localScale = new Vector3(0.1f, 0.9f, 0.8f);
        leg1.transform.localPosition = new Vector3(-0.9f, 0.45f, 0);
        leg1.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject leg2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leg2.transform.SetParent(root.transform);
        leg2.transform.localScale = new Vector3(0.1f, 0.9f, 0.8f);
        leg2.transform.localPosition = new Vector3(0.9f, 0.45f, 0);
        leg2.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.transform.SetParent(root.transform);
        screen.transform.localScale = new Vector3(0.8f, 0.5f, 0.05f);
        screen.transform.localPosition = new Vector3(0, 1.25f, 0.3f);
        screen.transform.localRotation = Quaternion.Euler(-10, 0, 0);
        
        Material screenMat = CreateRealisticMaterial("ScreenMat", Color.black, 0.9f, 0.9f);
        screen.GetComponent<Renderer>().sharedMaterial = screenMat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateSupplyShelves(Material mat)
    {
        string path = "Assets/Prefabs/SupplyShelves.prefab";

        GameObject root = new GameObject("SupplyShelves");
        for (int i = 0; i < 4; i++)
        {
            GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.transform.SetParent(root.transform);
            shelf.transform.localScale = new Vector3(1.8f, 0.05f, 0.8f);
            shelf.transform.localPosition = new Vector3(0, 0.3f + (i * 0.6f), 0);
            shelf.GetComponent<Renderer>().sharedMaterial = mat;
        }

        GameObject leg1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leg1.transform.SetParent(root.transform);
        leg1.transform.localScale = new Vector3(0.05f, 2.2f, 0.05f);
        leg1.transform.localPosition = new Vector3(-0.85f, 1.1f, -0.35f);
        leg1.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject leg2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leg2.transform.SetParent(root.transform);
        leg2.transform.localScale = new Vector3(0.05f, 2.2f, 0.05f);
        leg2.transform.localPosition = new Vector3(0.85f, 1.1f, -0.35f);
        leg2.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
        return prefab;
    }

    private static GameObject[] CreateWeaponPrefabs(Material[] gunMats)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Weapons"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Weapons");
        }

        GameObject[] weapons = new GameObject[3];

        // 0: Pistol (Short barrel, small grip)
        GameObject p = new GameObject("Pistol");
        
        GameObject pBarrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pBarrel.transform.SetParent(p.transform);
        pBarrel.transform.localScale = new Vector3(0.15f, 0.15f, 0.4f);
        pBarrel.transform.localPosition = new Vector3(0, 0.1f, 0.2f);
        pBarrel.GetComponent<Renderer>().sharedMaterial = gunMats[0]; // Dark Metal
        DestroyImmediate(pBarrel.GetComponent<Collider>());
        
        GameObject pGrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pGrip.transform.SetParent(p.transform);
        pGrip.transform.localScale = new Vector3(0.12f, 0.2f, 0.12f);
        pGrip.transform.localPosition = new Vector3(0, -0.05f, 0.05f);
        pGrip.transform.localRotation = Quaternion.Euler(15, 0, 0); // Angled slightly
        pGrip.GetComponent<Renderer>().sharedMaterial = gunMats[0];
        DestroyImmediate(pGrip.GetComponent<Collider>());
        
        weapons[0] = PrefabUtility.SaveAsPrefabAsset(p, "Assets/Prefabs/Weapons/Pistol.prefab");
        DestroyImmediate(p);

        // 1: Rifle (Long barrel, stock, magazine)
        GameObject r = new GameObject("Rifle");
        
        GameObject rBarrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rBarrel.transform.SetParent(r.transform);
        rBarrel.transform.localScale = new Vector3(0.12f, 0.15f, 0.8f);
        rBarrel.transform.localPosition = new Vector3(0, 0.1f, 0.3f);
        rBarrel.GetComponent<Renderer>().sharedMaterial = gunMats[1]; // Red Metal
        DestroyImmediate(rBarrel.GetComponent<Collider>());

        GameObject rStock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rStock.transform.SetParent(r.transform);
        rStock.transform.localScale = new Vector3(0.12f, 0.2f, 0.3f);
        rStock.transform.localPosition = new Vector3(0, 0f, -0.1f);
        rStock.GetComponent<Renderer>().sharedMaterial = gunMats[0]; // Dark accent
        DestroyImmediate(rStock.GetComponent<Collider>());
        
        GameObject rMag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rMag.transform.SetParent(r.transform);
        rMag.transform.localScale = new Vector3(0.1f, 0.25f, 0.15f);
        rMag.transform.localPosition = new Vector3(0, -0.1f, 0.2f);
        rMag.GetComponent<Renderer>().sharedMaterial = gunMats[0];
        DestroyImmediate(rMag.GetComponent<Collider>());

        weapons[1] = PrefabUtility.SaveAsPrefabAsset(r, "Assets/Prefabs/Weapons/Rifle.prefab");
        DestroyImmediate(r);

        // 2: Shotgun (Double barrel, wood stock)
        GameObject s = new GameObject("Shotgun");
        
        GameObject sBarrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sBarrel.transform.SetParent(s.transform);
        sBarrel.transform.localScale = new Vector3(0.2f, 0.15f, 0.7f);
        sBarrel.transform.localPosition = new Vector3(0, 0.1f, 0.35f);
        sBarrel.GetComponent<Renderer>().sharedMaterial = gunMats[2]; // Yellow Metal
        DestroyImmediate(sBarrel.GetComponent<Collider>());
        
        GameObject sPump = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sPump.transform.SetParent(s.transform);
        sPump.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);
        sPump.transform.localPosition = new Vector3(0, 0.05f, 0.4f);
        sPump.GetComponent<Renderer>().sharedMaterial = gunMats[0];
        DestroyImmediate(sPump.GetComponent<Collider>());

        GameObject sGrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sGrip.transform.SetParent(s.transform);
        sGrip.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);
        sGrip.transform.localPosition = new Vector3(0, -0.05f, -0.05f);
        sGrip.transform.localRotation = Quaternion.Euler(30, 0, 0);
        sGrip.GetComponent<Renderer>().sharedMaterial = gunMats[0];
        DestroyImmediate(sGrip.GetComponent<Collider>());

        weapons[2] = PrefabUtility.SaveAsPrefabAsset(s, "Assets/Prefabs/Weapons/Shotgun.prefab");
        DestroyImmediate(s);

        return weapons;
    }

    private static GameObject[] CreateProceduralHumanoidEnemies(Material darkMetalMat, Material redMetalMat, Material yellowMetalMat)
    {
        Material[] gunMats = new Material[] { darkMetalMat, redMetalMat, yellowMetalMat };
        GameObject[] gunPrefabs = CreateWeaponPrefabs(gunMats);

        Material skinMat = CreateRealisticMaterial("SkinMat", new Color(0.8f, 0.6f, 0.5f), 0.1f, 0.3f);
        Material shirtMat = CreateRealisticMaterial("ShirtMat", new Color(0.6f, 0.1f, 0.1f), 0.1f, 0.1f);
        Material pantsMat = CreateRealisticMaterial("PantsMat", new Color(0.1f, 0.1f, 0.3f), 0.1f, 0.1f);
        Material eyeMat = CreateRealisticMaterial("EyeMat", Color.black, 0f, 0.8f);

        string[] names = { "ProceduralEnemy_Pistol", "ProceduralEnemy_Rifle", "ProceduralEnemy_Shotgun", "ProceduralEnemy_Brawler" };
        GameObject[] finalPrefabs = new GameObject[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject root = new GameObject(names[i]);

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.transform.SetParent(root.transform);
            head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            head.transform.localPosition = new Vector3(0, 1.6f, 0);
            head.GetComponent<Renderer>().sharedMaterial = skinMat;

            GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eyeL.transform.SetParent(head.transform);
            eyeL.transform.localScale = new Vector3(0.2f, 0.2f, 0.05f);
            eyeL.transform.localPosition = new Vector3(-0.2f, 0.2f, 0.5f);
            eyeL.GetComponent<Renderer>().sharedMaterial = eyeMat;

            GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eyeR.transform.SetParent(head.transform);
            eyeR.transform.localScale = new Vector3(0.2f, 0.2f, 0.05f);
            eyeR.transform.localPosition = new Vector3(0.2f, 0.2f, 0.5f);
            eyeR.GetComponent<Renderer>().sharedMaterial = eyeMat;

            // Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.transform.SetParent(root.transform);
            torso.transform.localScale = new Vector3(0.6f, 0.7f, 0.3f);
            torso.transform.localPosition = new Vector3(0, 1.05f, 0);
            torso.GetComponent<Renderer>().sharedMaterial = shirtMat;

            // Arms
            GameObject armLPivot = new GameObject("LeftArmPivot");
            armLPivot.transform.SetParent(root.transform);
            armLPivot.transform.localPosition = new Vector3(-0.45f, 1.3f, 0);
            GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armL.transform.SetParent(armLPivot.transform);
            armL.transform.localScale = new Vector3(0.2f, 0.7f, 0.2f);
            armL.transform.localPosition = new Vector3(0, -0.3f, 0);
            armL.GetComponent<Renderer>().sharedMaterial = skinMat;

            GameObject armRPivot = new GameObject("RightArmPivot");
            armRPivot.transform.SetParent(root.transform);
            armRPivot.transform.localPosition = new Vector3(0.45f, 1.3f, 0);
            GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armR.transform.SetParent(armRPivot.transform);
            armR.transform.localScale = new Vector3(0.2f, 0.7f, 0.2f);
            armR.transform.localPosition = new Vector3(0, -0.3f, 0);
            armR.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Legs
            GameObject legLPivot = new GameObject("LeftLegPivot");
            legLPivot.transform.SetParent(root.transform);
            legLPivot.transform.localPosition = new Vector3(-0.15f, 0.7f, 0);
            GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legL.transform.SetParent(legLPivot.transform);
            legL.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f);
            legL.transform.localPosition = new Vector3(0, -0.35f, 0);
            legL.GetComponent<Renderer>().sharedMaterial = pantsMat;

            GameObject legRPivot = new GameObject("RightLegPivot");
            legRPivot.transform.SetParent(root.transform);
            legRPivot.transform.localPosition = new Vector3(0.15f, 0.7f, 0);
            GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legR.transform.SetParent(legRPivot.transform);
            legR.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f);
            legR.transform.localPosition = new Vector3(0, -0.35f, 0);
            legR.GetComponent<Renderer>().sharedMaterial = pantsMat;

            foreach (Collider c in root.GetComponentsInChildren<Collider>())
                DestroyImmediate(c);

            // Add Rigidbody and Colliders
            Rigidbody rootRb = root.AddComponent<Rigidbody>();
            rootRb.freezeRotation = true;
            CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0, 0.9f, 0);
            cc.height = 1.8f;
            cc.radius = 0.35f;

            // Procedural Animator
            ProceduralAnimator anim = root.AddComponent<ProceduralAnimator>();
            anim.leftArm = armLPivot.transform;
            anim.rightArm = armRPivot.transform;
            anim.leftLeg = legLPivot.transform;
            anim.rightLeg = legRPivot.transform;

            // Health UI
            GameObject canvasObj = new GameObject("HealthCanvas");
            canvasObj.transform.SetParent(root.transform);
            canvasObj.transform.localPosition = new Vector3(0, 2.1f, 0);
            canvasObj.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100f, 15f);
            canvasRect.localScale = new Vector3(0.015f, 0.015f, 0.015f);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            bgObj.layer = LayerMask.NameToLayer("UI");
            UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.sizeDelta = Vector2.zero;

            GameObject fillAreaObj = new GameObject("FillArea");
            fillAreaObj.transform.SetParent(canvasObj.transform, false);
            fillAreaObj.layer = LayerMask.NameToLayer("UI");
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero; fillAreaRect.anchorMax = Vector2.one; fillAreaRect.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            fillObj.layer = LayerMask.NameToLayer("UI");
            UnityEngine.UI.Image fillImg = fillObj.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = Color.red;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;

            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(canvasObj.transform, false);
            sliderObj.layer = LayerMask.NameToLayer("UI");
            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero; sliderRect.anchorMax = Vector2.one; sliderRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillRect;
            slider.interactable = false;
            slider.transition = UnityEngine.UI.Selectable.Transition.None;

            root.tag = "Enemy";
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            eh.healthBar = slider;
            EnemyAI ai = root.AddComponent<EnemyAI>();

            // Setup variant specific logic
            if (i < 3)
            {
                // Has Gun
                GameObject gunInst = PrefabUtility.InstantiatePrefab(gunPrefabs[i]) as GameObject;
                gunInst.transform.SetParent(armR.transform, false);
                // Counteract the arm's squished local scale (0.2, 0.7, 0.2) so the gun looks correct
                gunInst.transform.localScale = new Vector3(5f, 1.42f, 5f); 
                gunInst.transform.localPosition = new Vector3(0, -0.3f, 1.0f);
                
                ai.firePoint = gunInst.transform;
                ai.combatType = EnemyAI.CombatType.Ranged;
                
                if (i == 0) // Pistol
                {
                    ai.fireRate = 1.0f;
                    ai.bulletDamage = 10f;
                }
                else if (i == 1) // Rifle
                {
                    ai.fireRate = 0.3f;
                    ai.bulletDamage = 5f;
                }
                else if (i == 2) // Shotgun
                {
                    ai.fireRate = 2.5f;
                    ai.bulletDamage = 25f;
                }
            }
            else
            {
                // Brawler
                ai.combatType = EnemyAI.CombatType.Melee;
                ai.bulletDamage = 15f;
                ai.fireRate = 1.5f; // Punch interval
                ai.speed *= 1.2f; // Faster to reach player
            }

            finalPrefabs[i] = PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Prefabs/{names[i]}.prefab");
            DestroyImmediate(root);
        }

        return finalPrefabs;
    }

    private static Mesh CreateGrassMesh()
    {
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[] {
            new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 1, 0), new Vector3(0.5f, 1, 0),
            new Vector3(0, 0, -0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, -0.5f), new Vector3(0, 1, 0.5f)
        };
        
        int[] triangles = new int[] {
            0, 2, 1, 2, 3, 1, // Front quad 1
            1, 3, 2, 1, 2, 0, // Back quad 1
            4, 6, 5, 6, 7, 5, // Front quad 2
            5, 7, 6, 5, 6, 4  // Back quad 2
        };
        
        Vector2[] uv = new Vector2[] {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        
        return mesh;
    }
}
