using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogueEventRouter : MonoBehaviour
{
    public static DialogueEventRouter Instance { get; private set; }

    [Serializable]
    public class DialogueEventBinding
    {
        public string key;
        public UnityEvent onTriggered;
    }

    [Header("Dialogue Event Bindings")]
    [SerializeField] private List<DialogueEventBinding> bindings = new List<DialogueEventBinding>();

    private Dictionary<string, UnityEvent> eventMap;

    private void Awake()
    {
        Instance = this;
        BuildEventMap();
    }

    private void BuildEventMap()
    {
        eventMap = new Dictionary<string, UnityEvent>();

        foreach (DialogueEventBinding binding in bindings)
        {
            if (binding == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(binding.key))
            {
                continue;
            }

            if (eventMap.ContainsKey(binding.key))
            {
                Debug.LogWarning($"Duplicate dialogue event key: {binding.key}");
                continue;
            }

            eventMap.Add(binding.key, binding.onTriggered);
        }
    }

    public void Trigger(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (eventMap == null)
        {
            BuildEventMap();
        }

        if (eventMap.TryGetValue(key, out UnityEvent unityEvent))
        {
            Debug.Log($"Dialogue event triggered: {key}");
            unityEvent?.Invoke();
            return;
        }

        Debug.LogWarning($"No dialogue event binding found for key: {key}");
    }
}