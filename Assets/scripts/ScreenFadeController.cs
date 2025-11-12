using UnityEngine;
using System.Collections;
using System;

public class ScreenFadeController : MonoBehaviour
{
    [Tooltip("CanvasGroup on a full-screen black UI Image")]
    public CanvasGroup fadeCanvasGroup;

    // Public method to start the fade-out process
    public void FadeOut(float duration, Action onFadeComplete = null)
    {
        StartCoroutine(StartFade(0f, 1f, duration, onFadeComplete));
    }

    // Public method to start the fade-in process
    public void FadeIn(float duration, Action onFadeComplete = null)
    {
        StartCoroutine(StartFade(1f, 0f, duration, onFadeComplete));
    }

    // Coroutine to handle the fade animation
    private IEnumerator StartFade(float startAlpha, float endAlpha, float duration, Action onFadeComplete)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("Fade Canvas Group not assigned on ScreenFadeController!");
            onFadeComplete?.Invoke();
            yield break;
        }

        float timer = 0f;
        fadeCanvasGroup.alpha = startAlpha;

        // Ensure the Canvas is visible and blocks interaction
        fadeCanvasGroup.blocksRaycasts = true;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            fadeCanvasGroup.alpha = newAlpha;
            yield return null;
        }

        fadeCanvasGroup.alpha = endAlpha;

        // If fading in (endAlpha = 0), unblock raycasts
        if (endAlpha < 0.01f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }

        // Execute the callback function after the fade
        onFadeComplete?.Invoke();
    }
}