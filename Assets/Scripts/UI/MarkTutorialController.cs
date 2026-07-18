using System.Collections;
using UnityEngine;

public sealed class MarkTutorialController : MonoBehaviour
{
    [Header("Mark References")]
    [Tooltip("The three existing MarkInstance objects: Blue, Green and Red.")]
    [SerializeField]
    private MarkInstance[] marks;

    [Tooltip("The existing MarkerButton GameObject.")]
    [SerializeField]
    private GameObject markButtonRoot;

    [Tooltip("Optional highlight object placed behind the MarkerButton.")]
    [SerializeField]
    private GameObject markButtonHighlight;

    [Header("Tutorial Target")]
    [Tooltip(
        "Colliders defining the valid tutorial target area. " +
        "A trigger BoxCollider around the strange tent is recommended."
    )]
    [SerializeField]
    private Collider[] validTargetColliders;

    [Tooltip(
        "Optional fallback target. Used only when no valid target collider is assigned."
    )]
    [SerializeField]
    private Transform fallbackTarget;

    [Min(0.1f)]
    [Tooltip(
        "Valid distance from fallbackTarget when no target collider is assigned."
    )]
    [SerializeField]
    private float fallbackTargetRadius = 8f;

    [Min(0f)]
    [Tooltip(
        "Allows the Mark to sit slightly outside the target collider " +
        "because MarkController applies a surface offset."
    )]
    [SerializeField]
    private float targetSurfaceTolerance = 0.5f;

    [Header("Tutorial Feedback")]
    [Tooltip("Optional visual highlight or outline for the strange tent.")]
    [SerializeField]
    private GameObject tentHighlightObject;

    [Tooltip("Optional existing hint target belonging to the strange tent.")]
    [SerializeField]
    private ObjectiveHintTarget tentHintTarget;

    [Header("Objective")]
    [SerializeField]
    private string tutorialObjectiveText =
        "Aim at the strange tent and place a marker";

    [Header("Completion Dialogue")]
    [SerializeField]
    private string completionSequenceId =
        "level1_tent_marked";

    [Min(0f)]
    [SerializeField]
    private float completionDialogueDelay = 0.25f;

    [Header("Start State")]
    [Tooltip(
        "Hides the Marker button until the tutorial begins. " +
        "The Mark system itself is not disabled."
    )]
    [SerializeField]
    private bool hideMarkButtonUntilTutorial = true;

    private bool tutorialActive;
    private bool tutorialCompleted;
    private Coroutine completionRoutine;

    public bool TutorialActive => tutorialActive;
    public bool TutorialCompleted => tutorialCompleted;

    private void Awake()
    {
        if (hideMarkButtonUntilTutorial &&
            markButtonRoot != null)
        {
            markButtonRoot.SetActive(false);
        }

        SetTutorialHighlights(false);
    }

    private void Update()
    {
        if (!tutorialActive ||
            tutorialCompleted)
        {
            return;
        }

        // Do not complete from a previously placed Mark while
        // the tutorial dialogue is still covering the screen.
        if (DialogueRunner.Instance != null &&
            DialogueRunner.Instance.IsDialogueOpen)
        {
            return;
        }

        if (HasValidDeployedMark())
        {
            CompleteTutorial();
        }
    }

    /// <summary>
    /// Called by DialogueEventRouter when the first UFO dialogue
    /// finishes introducing the Mark feature.
    /// </summary>
    public void BeginTutorial()
    {
        if (tutorialCompleted ||
            tutorialActive)
        {
            return;
        }

        tutorialActive = true;

        if (markButtonRoot != null)
        {
            markButtonRoot.SetActive(true);
        }

        SetTutorialHighlights(true);

        if (ObjectivePanelController.Instance != null)
        {
            ObjectivePanelController.Instance
                .CompleteAndSetNextObjective(
                    tutorialObjectiveText,
                    tentHintTarget
                );
        }

        if (marks == null ||
            marks.Length == 0)
        {
            Debug.LogWarning(
                "Mark tutorial has no MarkInstance references.",
                this
            );
        }

        if (!HasConfiguredTarget())
        {
            Debug.LogWarning(
                "Mark tutorial has no target collider or fallback target.",
                this
            );
        }
    }

    private bool HasValidDeployedMark()
    {
        if (marks == null)
        {
            return false;
        }

        foreach (MarkInstance mark in marks)
        {
            if (mark == null ||
                mark.State !=
                    MarkInstance.MarkState.Deployed)
            {
                continue;
            }

            if (IsInsideTutorialTarget(
                    mark.transform.position))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInsideTutorialTarget(
        Vector3 markPosition)
    {
        bool hasColliderTarget = false;

        if (validTargetColliders != null)
        {
            float tolerance =
                Mathf.Max(
                    0f,
                    targetSurfaceTolerance
                );

            float toleranceSquared =
                tolerance * tolerance;

            foreach (Collider targetCollider
                     in validTargetColliders)
            {
                if (targetCollider == null)
                {
                    continue;
                }

                hasColliderTarget = true;

                Vector3 nearestPoint =
                    targetCollider.ClosestPoint(
                        markPosition
                    );

                float distanceSquared =
                    (
                        nearestPoint -
                        markPosition
                    ).sqrMagnitude;

                if (distanceSquared <=
                    toleranceSquared)
                {
                    return true;
                }
            }
        }

        // When colliders are configured, do not also accept
        // the less precise radius check.
        if (hasColliderTarget)
        {
            return false;
        }

        if (fallbackTarget == null)
        {
            return false;
        }

        float radius =
            Mathf.Max(
                0.1f,
                fallbackTargetRadius
            );

        return
            (
                fallbackTarget.position -
                markPosition
            ).sqrMagnitude
            <= radius * radius;
    }

    private bool HasConfiguredTarget()
    {
        if (validTargetColliders != null)
        {
            foreach (Collider targetCollider
                     in validTargetColliders)
            {
                if (targetCollider != null)
                {
                    return true;
                }
            }
        }

        return fallbackTarget != null;
    }

    private void CompleteTutorial()
    {
        if (tutorialCompleted)
        {
            return;
        }

        tutorialCompleted = true;
        tutorialActive = false;

        SetTutorialHighlights(false);

        if (ObjectivePanelController.Instance != null)
        {
            ObjectivePanelController.Instance
                .CompleteCurrentObjective();
        }

        if (completionRoutine != null)
        {
            StopCoroutine(completionRoutine);
        }

        completionRoutine =
            StartCoroutine(
                PlayCompletionDialogueRoutine()
            );
    }

    private IEnumerator
        PlayCompletionDialogueRoutine()
    {
        if (completionDialogueDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    completionDialogueDelay
                );
        }

        while (DialogueRunner.Instance != null &&
               DialogueRunner.Instance.IsDialogueOpen)
        {
            yield return null;
        }

        if (Level1DialogueDatabase.Instance == null)
        {
            Debug.LogWarning(
                "Level1DialogueDatabase is missing.",
                this
            );

            completionRoutine = null;
            yield break;
        }

        DialogueSequence sequence =
            Level1DialogueDatabase.Instance
                .GetSequence(
                    completionSequenceId
                );

        if (sequence == null)
        {
            Debug.LogWarning(
                $"Dialogue sequence not found: " +
                $"{completionSequenceId}",
                this
            );

            completionRoutine = null;
            yield break;
        }

        if (DialogueRunner.Instance == null)
        {
            Debug.LogWarning(
                "DialogueRunner is missing.",
                this
            );

            completionRoutine = null;
            yield break;
        }

        DialogueRunner.Instance
            .StartDialogue(sequence);

        completionRoutine = null;
    }

    private void SetTutorialHighlights(
        bool visible)
    {
        if (markButtonHighlight != null)
        {
            markButtonHighlight.SetActive(
                visible
            );
        }

        if (tentHighlightObject != null)
        {
            tentHighlightObject.SetActive(
                visible
            );
        }
    }

    private void OnDisable()
    {
        SetTutorialHighlights(false);
    }
}