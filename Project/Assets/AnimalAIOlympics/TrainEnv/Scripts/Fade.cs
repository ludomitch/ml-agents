using System.Collections;
using UnityEngine;

public class Fade : MonoBehaviour
{
    public int fadeSpeed = 4;

    private int _fadeDirection = -1;

    public void ResetFade()
    {
        _fadeDirection = -1;
        CanvasGroup canvasGroup = gameObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
    }

    public void StartFade()
    {
        _fadeDirection *= -1;
        StartCoroutine(FadeOutEnum());        
    }


    IEnumerator FadeOutEnum()
    {
        int localFadeDirection = _fadeDirection;
        CanvasGroup canvasGroup = gameObject.GetComponent<CanvasGroup>();
        while (localFadeDirection ==_fadeDirection && canvasGroup.alpha<=1 && canvasGroup.alpha>=0)
        {
            canvasGroup.alpha += _fadeDirection * Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }

    void Awake()
    {
        ResetFade();
    }

}
