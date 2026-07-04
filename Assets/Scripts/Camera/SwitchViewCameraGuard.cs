using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 切换视角前先检查摄像头是否被遮挡：被挡就弹提示(默认2秒)、并【不切换】；未挡才真正切。
///
/// 用法：把【切换按钮】的 OnClick 从"直接切换"改成指向本组件的 TrySwitch()。
/// 再把真正的切换调用(你原来的 Toggle/SwitchToUfo 等)接到 onSwitchAllowed 上。
/// </summary>
public class SwitchViewCameraGuard : MonoBehaviour
{
    [Header("检测")]
    public CameraBlockCheck cameraBlockCheck;
    [Tooltip("只在'按下将进入 UFO(AR)视角'时检查；切回人物不检查。需要场景里有 SwitchPerspectiveButtonController")]
    public bool onlyCheckWhenEnteringUfo = true;

    [Header("提示 UI")]
    [Tooltip("提示面板的 CanvasGroup（用它淡入淡出）")]
    public CanvasGroup warningGroup;
    [Tooltip("提示文本")]
    public TMP_Text warningText;
    public string message = "请确认摄像头没有被遮挡";
    [Tooltip("提示持续时间(秒)")]
    public float showSeconds = 2f;
    public float fadeDuration = 0.15f;

    [Header("允许切换时执行")]
    [Tooltip("接你真正的切换调用，如 ViewToggleButton.Toggle 或 SwitchPerspectiveButtonController 的切换方法")]
    public UnityEvent onSwitchAllowed;

    private Coroutine warnRoutine;

    void Awake()
    {
        if (warningGroup != null)
        {
            warningGroup.alpha = 0f;
            warningGroup.interactable = false;
            warningGroup.blocksRaycasts = false;
        }
        if (warningText != null && !string.IsNullOrEmpty(message)) warningText.text = message;
    }

    /// <summary>切换按钮的 OnClick 指到这里。</summary>
    public void TrySwitch()
    {
        if (ShouldCheckNow() && cameraBlockCheck != null && cameraBlockCheck.IsBlocked())
        {
            ShowWarning();     // 弹提示，不切
            return;
        }

        onSwitchAllowed?.Invoke();   // 未挡才真正切
    }

    private bool ShouldCheckNow()
    {
        if (!onlyCheckWhenEnteringUfo) return true;

        // 当前不是 UFO 视角 → 按下将进入 UFO → 需要检查
        var sw = SwitchPerspectiveButtonController.Instance;
        if (sw == null) return true;         // 拿不到状态就一律检查，保守
        return !sw.IsUfoView;
    }

    private void ShowWarning()
    {
        if (warningText != null) warningText.text = message;
        if (warnRoutine != null) StopCoroutine(warnRoutine);
        warnRoutine = StartCoroutine(WarnRoutine());
    }

    private IEnumerator WarnRoutine()
    {
        yield return Fade(1f);
        yield return new WaitForSeconds(showSeconds);
        yield return Fade(0f);
        warnRoutine = null;
    }

    private IEnumerator Fade(float target)
    {
        if (warningGroup == null) yield break;

        float start = warningGroup.alpha;
        float t = 0f;

        if (fadeDuration <= 0f)
        {
            warningGroup.alpha = target;
            yield break;
        }

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;   // 用 unscaled，暂停时也能显示
            warningGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        warningGroup.alpha = target;
    }
}
