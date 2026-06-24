using UnityEngine;

[DisallowMultipleComponent]
public sealed class BridgePlacementGoal :
    MonoBehaviour
{
    [Header("Ghost")]
    [SerializeField]
    private GhostAlphaPulse ghostPulse;

    [Header("View")]
    [SerializeField]
    private ViewToggleButton viewToggleButton;

    [Header("Initial State")]
    [SerializeField]
    private bool startGoalActive;

    private bool goalActive;
    private bool ghostVisible;
    private bool hasAppliedVisibility;

    private void Awake()
    {
        if (ghostPulse == null)
        {
            ghostPulse =
                GetComponent<GhostAlphaPulse>();
        }

        ResolveViewToggleButton();

        goalActive = startGoalActive;
    }

    private void Start()
    {
        RefreshGhostVisibility(true);
    }

    private void Update()
    {
        if (viewToggleButton == null)
        {
            ResolveViewToggleButton();
        }

        RefreshGhostVisibility(false);
    }

    private void OnDisable()
    {
        if (ghostPulse != null)
        {
            ghostPulse.Hide();
        }

        ghostVisible = false;
        hasAppliedVisibility = true;
    }

    public void ActivateGoal()
    {
        goalActive = true;

        RefreshGhostVisibility(true);

        Debug.Log(
            "Bridge goal hint activated.",
            this
        );
    }

    public void DeactivateGoal()
    {
        goalActive = false;

        RefreshGhostVisibility(true);

        Debug.Log(
            "Bridge goal hint deactivated.",
            this
        );
    }

    private void RefreshGhostVisibility(
        bool force)
    {
        bool shouldShow =
            goalActive &&
            viewToggleButton != null &&
            viewToggleButton.IsUfoView();

        if (!force &&
            hasAppliedVisibility &&
            ghostVisible == shouldShow)
        {
            return;
        }

        hasAppliedVisibility = true;
        ghostVisible = shouldShow;

        if (ghostPulse == null)
        {
            return;
        }

        if (shouldShow)
        {
            ghostPulse.Show();
        }
        else
        {
            ghostPulse.Hide();
        }
    }

    private void ResolveViewToggleButton()
    {
        if (viewToggleButton != null)
        {
            return;
        }

        viewToggleButton =
            FindObjectOfType<ViewToggleButton>();
    }
}