using System.Collections.Generic;
using UnityEngine;

public sealed class GameplayInputLockService : MonoBehaviour
{
    public readonly struct LockHandle
    {
        internal readonly int Token;

        internal LockHandle(int token)
        {
            Token = token;
        }

        public bool IsValid => Token != 0;
    }

    private static GameplayInputLockService instance;

    private readonly Dictionary<int, string> activeLocks =
        new Dictionary<int, string>();

    private int nextToken = 1;

    public static bool IsBlocked =>
        InstanceOrCreate.activeLocks.Count > 0;

    private static GameplayInputLockService InstanceOrCreate
    {
        get
        {
            if (instance != null)
                return instance;

            instance =
                FindObjectOfType<GameplayInputLockService>();

            if (instance != null)
                return instance;

            GameObject go =
                new GameObject(nameof(GameplayInputLockService));

            instance =
                go.AddComponent<GameplayInputLockService>();

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public static LockHandle Acquire(string reason)
    {
        GameplayInputLockService service =
            InstanceOrCreate;

        int token = service.nextToken++;

        if (service.nextToken == int.MaxValue)
            service.nextToken = 1;

        service.activeLocks[token] =
            string.IsNullOrWhiteSpace(reason)
                ? "Gameplay lock"
                : reason;

        return new LockHandle(token);
    }

    public static void Release(LockHandle handle)
    {
        if (!handle.IsValid || instance == null)
            return;

        instance.activeLocks.Remove(handle.Token);
    }

    public static bool Owns(LockHandle handle)
    {
        return
            handle.IsValid &&
            instance != null &&
            instance.activeLocks.ContainsKey(handle.Token);
    }
}
