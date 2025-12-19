using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Не забудь для доступа к Image

public class BlurController : MonoBehaviour
{
    public Material blurMaterial;
    public float fadeDuration = 1.0f;
    public float defaultRadius = 32f;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        // Делаем копию материала и назначаем её на Image
        blurMaterial = new Material(blurMaterial);
        GetComponent<Image>().material = blurMaterial;

        // Устанавливаем блюр при старте
        blurMaterial.SetFloat("_Radius", defaultRadius);
    }

    public void StartFadeOut()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeBlur(blurMaterial.GetFloat("_Radius"), 0f));
    }

    public void StartFadeIn(float targetRadius = -1f)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Якщо targetRadius не заданий, використовуємо дефолт
        if (targetRadius <= 0f)
            targetRadius = defaultRadius;

        // Завжди починаємо з нуля
        blurMaterial.SetFloat("_Radius", 0f);
        fadeCoroutine = StartCoroutine(FadeBlur(0f, targetRadius));
    }

    private IEnumerator FadeBlur(float fromRadius, float toRadius)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            float newRadius = Mathf.Lerp(fromRadius, toRadius, t);
            blurMaterial.SetFloat("_Radius", newRadius);

            yield return null;
        }

        blurMaterial.SetFloat("_Radius", toRadius);
    }
}
