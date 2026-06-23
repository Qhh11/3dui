using UnityEngine;

[DisallowMultipleComponent]
public sealed class BridgePlacementGoal : MonoBehaviour
{
    [Header("Ghost")]
    [SerializeField] private GhostAlphaPulse ghostPulse;

    [Header("Initial state")]
    [SerializeField] private bool startGoalActive;

    private bool goalActive;

    private void Awake()
    {
        if (ghostPulse == null)
            ghostPulse = GetComponent<GhostAlphaPulse>();

        goalActive = startGoalActive;

        if (goalActive)
            ghostPulse?.Show();
        else
            ghostPulse?.Hide();
    }

    public void ActivateGoal()
    {
        goalActive = true;
        ghostPulse?.Show();

        Debug.Log("Bridge goal hint activated.", this);
    }

    public void DeactivateGoal()
    {
        goalActive = false;
        ghostPulse?.Hide();

        Debug.Log("Bridge goal hint deactivated.", this);
    }
}