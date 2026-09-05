using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SceneBuilderTool : EditorWindow
{
    [MenuItem("Tools/Scene Builder")]
    public static void ShowWindow()
    {
        GetWindow<SceneBuilderTool>("Scene Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Scene Builder Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("1. Generate Character Selection Scene"))
        {
            GenerateCharacterSelectionScene();
        }

        GUILayout.Space(10);
        GUILayout.Label("Main Menu Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("2. Add Exit Button to Current Scene"))
        {
            AddExitButtonToMainMenu();
        }
    }

    private void AddExitButtonToMainMenu()
    {
        // Find existing Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the current scene. Please open the MainMenu scene first.");
            return;
        }

        // Find MainMenu script in the scene
        MainMenu mainMenuScript = FindObjectOfType<MainMenu>();
        if (mainMenuScript == null)
        {
            Debug.LogError("No MainMenu script found in the scene. Please open the MainMenu scene.");
            return;
        }

        // Create the button
        GameObject buttonObj = new GameObject("ExitButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        Button btn = buttonObj.AddComponent<Button>();
        buttonObj.AddComponent<Image>();

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 50);
        // Position it somewhere visible (e.g., below other buttons)
        rect.anchoredPosition = new Vector2(0, -100); 

        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = "Exit Game";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.black;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // Hook up the Exit event
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, 
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), mainMenuScript, "Exit") as UnityEngine.Events.UnityAction);

        // Mark scene as dirty so it saves
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Exit Button successfully added to the canvas and wired up to MainMenu.Exit()!");
    }

    private void GenerateCharacterSelectionScene()
    {
        // 1. Create a new empty scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Setup Camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f); // Dark gray background
        cameraObj.AddComponent<AudioListener>();

        // 3. Setup Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 4. Setup EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        // 5. Setup CharacterSelectionManager
        GameObject managerObj = new GameObject("CharacterSelectionManager");
        CharacterSelectionManager manager = managerObj.AddComponent<CharacterSelectionManager>();
        
        // 6. UI Text for Skill Description
        GameObject textObj = new GameObject("SkillDescriptionText");
        textObj.transform.SetParent(canvasObj.transform, false);
        Text descText = textObj.AddComponent<Text>();
        descText.text = "Skill Description Here";
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 24;
        descText.alignment = TextAnchor.MiddleCenter;
        descText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(600, 100);
        textRect.anchoredPosition = new Vector2(0, 150);

        manager.skillDescriptionText = descText;

        // 7. Buttons
        CreateButton(canvasObj.transform, "CharAButton", "Select Character A", new Vector2(-200, 0), manager, "SelectCharacterA");
        CreateButton(canvasObj.transform, "CharBButton", "Select Character B", new Vector2(0, 0), manager, "SelectCharacterB");
        CreateButton(canvasObj.transform, "CharCButton", "Select Character C", new Vector2(200, 0), manager, "SelectCharacterC");

        CreateButton(canvasObj.transform, "StartButton", "Start Game", new Vector2(0, -150), manager, "StartGame");

        // 8. Save the Scene
        string scenePath = "Assets/Scenes/CharacterSelection.unity";
        
        // Ensure Scenes folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log("Character Selection Scene generated and saved to: " + scenePath);
    }

    private void CreateButton(Transform parent, string objName, string buttonText, Vector2 position, CharacterSelectionManager manager, string methodName)
    {
        GameObject buttonObj = new GameObject(objName);
        buttonObj.transform.SetParent(parent, false);
        Button btn = buttonObj.AddComponent<Button>();
        buttonObj.AddComponent<Image>(); // Required for button visuals

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 50);
        rect.anchoredPosition = position;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = buttonText;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 14;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.black;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // Hook up the event
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, 
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), manager, methodName) as UnityEngine.Events.UnityAction);
    }
}
