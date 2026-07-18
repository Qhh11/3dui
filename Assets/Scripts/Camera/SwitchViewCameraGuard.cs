using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SwitchViewCameraGuard :
    MonoBehaviour
{
    [Header("检测")]
    public CameraBlockCheck cameraBlockCheck;

    public bool onlyCheckWhenEnteringUfo = true;

    [Header("提示 UI")]
    public CanvasGroup warningGroup;
    public TMP_Text warningText;

    public string message =
        "Please make sure the camera is not blocked";

    public float showSeconds = 2f;
    public float fadeDuration = 0.15f;

    [Header("允许切换时执行")]
    public UnityEvent onSwitchAllowed;

    private Coroutine warnRoutine;

    private void Awake()
    {
        if (warningGroup != null)
        {
            warningGroup.alpha = 0f;
            warningGroup.interactable = false;
            warningGroup.blocksRaycasts = false;
        }

        if (warningText != null &&
            !string.IsNullOrEmpty(message))
        {
            warningText.text = message;
        }
    }

    public void TrySwitch()
    {
        if (ShouldCheckNow() &&
            !TryPassCameraCheck())
        {
            return;
        }

        onSwitchAllowed?.Invoke();
    }

    /// <summary>
    /// Hint 从 User View 进入 UFO 时调用。
    /// 被遮挡会显示现有 warning，并返回 false。
    /// </summary>
    public bool TryAllowEnteringUfo()
    {
        return TryPassCameraCheck();
    }

    private bool TryPassCameraCheck()
    {
        if (cameraBlockCheck == null)
            return true;

        if (!cameraBlockCheck.IsBlocked())
            return true;

        ShowWarning();
        return false;
    }

    private bool ShouldCheckNow()
    {
        if (!onlyCheckWhenEnteringUfo)
            return true;

        SwitchPerspectiveButtonController
            controller =
                SwitchPerspectiveButtonController
                    .Instance;

        if (controller == null)
            return true;

        return !controller.IsUfoView;
    }

    private void ShowWarning()
    {
        if (warningText != null)
            warningText.text = message;

        if (warnRoutine != null)
            StopCoroutine(warnRoutine);

        warnRoutine =
            StartCoroutine(WarnRoutine());
    }

    private IEnumerator WarnRoutine()
    {
        yield return Fade(1f);

        yield return new WaitForSecondsRealtime(
            showSeconds
        );

        yield return Fade(0f);
        warnRoutine = null;
    }

    private IEnumerator Fade(float target)
    {
        if (warningGroup == null)
            yield break;

        float start = warningGroup.alpha;
        float time = 0f;

        if (fadeDuration <= 0f)
        {
            warningGroup.alpha = target;
            yield break;
        }

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;

            warningGroup.alpha =
                Mathf.Lerp(
                    start,
                    target,
                    time / fadeDuration
                );

            yield return null;
        }

        warningGroup.alpha = target;
    }
}