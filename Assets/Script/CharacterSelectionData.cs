public enum CharacterType
{
    CharacterA, // Roll (2 charges)
    CharacterB, // Double Jump
    CharacterC  // Short Teleport
}

public static class CharacterSelectionData
{
    public static CharacterType SelectedCharacter = CharacterType.CharacterA;
}
