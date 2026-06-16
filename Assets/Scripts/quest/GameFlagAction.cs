using UnityEngine;

public class GameFlagAction : MonoBehaviour
{
    [SerializeField] private string flagName;
    [SerializeField] private bool value = true;

    public void Apply()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("GameStateManager is missing from the scene.");
            return;
        }

        GameStateManager.Instance.SetFlag(flagName, value);
    }
}
