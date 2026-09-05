using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text skillDescriptionText;
    
    [Header("Settings")]
    public string gameSceneName = "Level1";

    void Start()
    {
        // Initialize with default
        SelectCharacterA();
    }

    public void SelectCharacterA()
    {
        CharacterSelectionData.SelectedCharacter = CharacterType.CharacterA;
        UpdateSkillDescription();
    }

    public void SelectCharacterB()
    {
        CharacterSelectionData.SelectedCharacter = CharacterType.CharacterB;
        UpdateSkillDescription();
    }

    public void SelectCharacterC()
    {
        CharacterSelectionData.SelectedCharacter = CharacterType.CharacterC;
        UpdateSkillDescription();
    }

    private void UpdateSkillDescription()
    {
        if (skillDescriptionText == null) return;

        switch (CharacterSelectionData.SelectedCharacter)
        {
            case CharacterType.CharacterA:
                skillDescriptionText.text = "Skill: Roll (2 charges, with cooldown)";
                break;
            case CharacterType.CharacterB:
                skillDescriptionText.text = "Skill: Double Jump (no cooldown)";
                break;
            case CharacterType.CharacterC:
                skillDescriptionText.text = "Skill: Short-distance Teleport (with cooldown)";
                break;
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
