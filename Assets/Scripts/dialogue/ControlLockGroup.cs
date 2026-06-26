using System.Collections.Generic;
using UnityEngine;

public class ControlLockGroup : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("Options")]
    [SerializeField] private bool includeChildren = false;

    private sealed class SharedState
    {
        public bool PreviousEnabled;
        public int LockCount;
    }

    private static readonly Dictionary<Behaviour, SharedState>
        sharedStates = new Dictionary<Behaviour, SharedState>();

    private readonly HashSet<Behaviour> locallyLocked =
        new HashSet<Behaviour>();

    public void SetControlsEnabled(bool enabled)
    {
        if (enabled)
            RestorePreviousStates();
        else
            DisableTargetBehaviours();
    }

    private void DisableTargetBehaviours()
    {
        foreach (Behaviour behaviour in EnumerateTargets())
        {
            if (behaviour == null)
                continue;

            if (behaviour is ControlLockGroup)
                continue;

            if (!sharedStates.TryGetValue(
                    behaviour,
                    out SharedState state))
            {
                state = new SharedState
                {
                    PreviousEnabled = behaviour.enabled,
                    LockCount = 0
                };

                sharedStates.Add(behaviour, state);
            }

            if (locallyLocked.Add(behaviour))
                state.LockCount++;

            behaviour.enabled = false;
        }
    }

    private void RestorePreviousStates()
    {
        foreach (Behaviour behaviour in locallyLocked)
        {
            if (behaviour == null)
                continue;

            if (!sharedStates.TryGetValue(
                    behaviour,
                    out SharedState state))
            {
                continue;
            }

            state.LockCount--;

            if (state.LockCount <= 0)
            {
                behaviour.enabled = state.PreviousEnabled;
                sharedStates.Remove(behaviour);
            }
        }

        locallyLocked.Clear();
    }

    private IEnumerable<Behaviour> EnumerateTargets()
    {
        if (targetObjects == null)
            yield break;

        foreach (GameObject targetObject in targetObjects)
        {
            if (targetObject == null)
                continue;

            Behaviour[] behaviours =
                includeChildren
                    ? targetObject.GetComponentsInChildren<Behaviour>(true)
                    : targetObject.GetComponents<Behaviour>();

            foreach (Behaviour behaviour in behaviours)
                yield return behaviour;
        }
    }
}
