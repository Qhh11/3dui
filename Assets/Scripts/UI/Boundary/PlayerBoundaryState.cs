using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerBoundaryState : MonoBehaviour
{
    [SerializeField] private BoundaryWarningUI warningUi;

    [Header("Push")]
    [Tooltip("离开所有回推 trigger 后，边界回推速度回到 0 的速度。")]
    [SerializeField] private float defaultPushDecay = 500f;

    private readonly Dictionary<BoundaryDangerTrigger, int> dangerTriggerCounts =
        new Dictionary<BoundaryDangerTrigger, int>();

    private Vector3 pushVelocity;

    public float SpeedMultiplier
    {
        get
        {
            CleanupTriggers();

            if (ShouldIgnoreBoundaryEffects())
                return 1f;

            float multiplier = 1f;

            foreach (BoundaryDangerTrigger trigger in dangerTriggerCounts.Keys)
            {
                if (trigger != null)
                    multiplier = Mathf.Min(multiplier, trigger.SpeedMultiplier);
            }

            return multiplier;
        }
    }

    public Vector3 PushVelocity => pushVelocity;

    public void EnterDanger(BoundaryDangerTrigger trigger)
    {
        if (trigger == null)
            return;

        if (dangerTriggerCounts.TryGetValue(trigger, out int count))
            dangerTriggerCounts[trigger] = count + 1;
        else
            dangerTriggerCounts.Add(trigger, 1);

        RefreshWarning();
    }

    public void ExitDanger(BoundaryDangerTrigger trigger)
    {
        if (trigger == null)
            return;

        if (!dangerTriggerCounts.TryGetValue(trigger, out int count))
            return;

        count--;

        if (count <= 0)
            dangerTriggerCounts.Remove(trigger);
        else
            dangerTriggerCounts[trigger] = count;

        RefreshWarning();
    }

    public Vector3 UpdateAndGetPushVelocity(float deltaTime)
    {
        CleanupTriggers();

        if (ShouldIgnoreBoundaryEffects())
        {
            HideWarning();
            DecayPush(deltaTime);
            return pushVelocity;
        }

        RefreshWarning();

        BoundaryDangerTrigger bestTrigger = GetBestPushTrigger();

        if (bestTrigger == null)
        {
            DecayPush(deltaTime);
            return pushVelocity;
        }

        Vector3 safeDirection = bestTrigger.SafeDirection;

        if (safeDirection.sqrMagnitude <= 0.0001f)
        {
            DecayPush(deltaTime);
            return pushVelocity;
        }

        Vector3 targetVelocity =
            safeDirection * bestTrigger.MaxPushSpeed;

        pushVelocity =
            Vector3.MoveTowards(
                pushVelocity,
                targetVelocity,
                bestTrigger.PushAcceleration * deltaTime
            );

        pushVelocity.y = 0f;
        return pushVelocity;
    }

    public void ResetBoundaryState()
    {
        dangerTriggerCounts.Clear();
        pushVelocity = Vector3.zero;
        HideWarning();
    }

    private void DecayPush(float deltaTime)
    {
        pushVelocity =
            Vector3.MoveTowards(
                pushVelocity,
                Vector3.zero,
                defaultPushDecay * deltaTime
            );

        pushVelocity.y = 0f;
    }

    private BoundaryDangerTrigger GetBestPushTrigger()
    {
        BoundaryDangerTrigger best = null;

        foreach (BoundaryDangerTrigger trigger in dangerTriggerCounts.Keys)
        {
            if (trigger == null ||
                !trigger.PushBack ||
                trigger.MaxPushSpeed <= 0f ||
                trigger.PushAcceleration <= 0f ||
                trigger.SafeDirection.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            if (best == null ||
                trigger.PushPriority > best.PushPriority ||
                trigger.PushPriority == best.PushPriority &&
                trigger.MaxPushSpeed > best.MaxPushSpeed ||
                trigger.PushPriority == best.PushPriority &&
                Mathf.Approximately(trigger.MaxPushSpeed, best.MaxPushSpeed) &&
                trigger.PushAcceleration > best.PushAcceleration)
            {
                best = trigger;
            }
        }

        return best;
    }

    private void RefreshWarning()
    {
        if (warningUi == null)
            return;

        if (ShouldIgnoreBoundaryEffects())
        {
            HideWarning();
            return;
        }

        BoundaryDangerTrigger best = null;

        foreach (BoundaryDangerTrigger trigger in dangerTriggerCounts.Keys)
        {
            if (trigger == null || !trigger.ShowWarning)
                continue;

            if (best == null ||
                trigger.WarningPriority > best.WarningPriority ||
                trigger.WarningPriority == best.WarningPriority &&
                trigger.MaxPushSpeed > best.MaxPushSpeed)
            {
                best = trigger;
            }
        }

        if (best == null)
        {
            HideWarning();
            return;
        }

        warningUi.SetWarningVisible(true, best.WarningText);
    }

    private void HideWarning()
    {
        if (warningUi != null)
            warningUi.SetWarningVisible(false, string.Empty);
    }

    private void CleanupTriggers()
    {
        List<BoundaryDangerTrigger> toRemove = null;

        foreach (BoundaryDangerTrigger trigger in dangerTriggerCounts.Keys)
        {
            if (trigger != null && trigger.isActiveAndEnabled)
                continue;

            if (toRemove == null)
                toRemove = new List<BoundaryDangerTrigger>();

            toRemove.Add(trigger);
        }

        if (toRemove == null)
            return;

        foreach (BoundaryDangerTrigger trigger in toRemove)
            dangerTriggerCounts.Remove(trigger);
    }

    private bool ShouldIgnoreBoundaryEffects()
    {
        return GameplayInputLockService.IsBlocked || IsUfoView();
    }

    private bool IsUfoView()
    {
        return
            SwitchPerspectiveButtonController.Instance != null &&
            SwitchPerspectiveButtonController.Instance.IsUfoView;
    }
}