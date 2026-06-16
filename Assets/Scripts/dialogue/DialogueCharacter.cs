using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string displayName;
    public Sprite portrait;
    public Color nameColor = Color.white;
}
