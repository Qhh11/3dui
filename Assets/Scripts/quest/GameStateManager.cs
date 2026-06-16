using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private List<string> startingFlags = new();

    private readonly HashSet<string> flags = new();

    public UnityEvent<string, bool> OnFlagChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (string flag in startingFlags)
        {
            if (!string.IsNullOrWhiteSpace(flag))
                flags.Add(flag);
        }
    }

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && flags.Contains(flag);
    }

    public void SetFlag(string flag)
    {
        SetFlag(flag, true);
    }

    public void ClearFlag(string flag)
    {
        SetFlag(flag, false);
    }

    public void SetFlag(string flag, bool value)
    {
        if (string.IsNullOrWhiteSpace(flag)) return;

        bool changed;

        if (value)
            changed = flags.Add(flag);
        else
            changed = flags.Remove(flag);

        if (changed)
        {
            OnFlagChanged?.Invoke(flag, value);
            Debug.Log($"Game flag changed: {flag} = {value}");
        }
    }
}
