using UnityEngine;

public class ScreenFadeAction : MonoBehaviour
{
    public void PlayWakeUpFade()
    {
        ScreenFadeController.Instance?.PlayWakeUpFade();
    }

    public void FadeToBlack()
    {
        ScreenFadeController.Instance?.FadeToBlack(0.5f);
    }

    public void FadeFromBlack()
    {
        ScreenFadeController.Instance?.FadeFromBlack(0.5f);
    }
}