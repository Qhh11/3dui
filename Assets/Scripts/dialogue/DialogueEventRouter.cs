using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogueEventRouter : MonoBehaviour
{
    public static DialogueEventRouter Instance { get; private set; }

    [System.Serializable]
    public class DialogueEventBinding
    {
        public string key;
        public UnityEvent onTriggered;
    }

    [SerializeField] private List<DialogueEventBinding> bindings = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Trigger(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        bool found = false;

        foreach (DialogueEventBinding binding in bindings)
        {
            if (binding.key == key)
            {
                found = true;
                binding.onTriggered?.Invoke();
            }
        }

        if (!found)
        {
            Debug.LogWarning($"Dialogue event key not found: {key}");
        }
    }
}
