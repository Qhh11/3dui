using System.Collections.Generic;
using UnityEngine;

public class ControlLockGroup : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("Options")]
    [SerializeField] private bool includeChildren = false;

    private readonly Dictionary<Behaviour, bool> previousStates = new Dictionary<Behaviour, bool>();

    public void SetControlsEnabled(bool enabled)
    {
        if (enabled)
        {
            RestorePreviousStates();
        }
        else
        {
            DisableTargetBehaviours();
        }
    }

    private void DisableTargetBehaviours()
    {
        previousStates.Clear();

        foreach (GameObject targetObject in targetObjects)
        {
            if (targetObject == null)
            {
                continue;
            }

            Behaviour[] behaviours = includeChildren
                ? targetObject.GetComponentsInChildren<Behaviour>(true)
                : targetObject.GetComponents<Behaviour>();

            foreach (Behaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour == this)
                {
                    continue;
                }

                previousStates[behaviour] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }
    }

    private void RestorePreviousStates()
    {
        foreach (KeyValuePair<Behaviour, bool> state in previousStates)
        {
            if (state.Key != null)
            {
                state.Key.enabled = state.Value;
            }
        }

        previousStates.Clear();
    }
}