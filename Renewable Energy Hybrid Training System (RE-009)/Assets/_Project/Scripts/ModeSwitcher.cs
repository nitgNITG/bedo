using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ModeSwitcher : MonoBehaviour
{
    [Header("Prefab Options")]
    [SerializeField] private GameObject option1Prefab;
    [SerializeField] private GameObject option2Prefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;

    [Header("Black Screen")]
    [SerializeField] private Image blackScreen;

    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Startup")]
    [SerializeField] private bool startWithOption1 = true;

    private GameObject currentInstance;

    private bool isSwitching = false;

    private void Start()
    {
        if (startWithOption1)
        {
            SpawnOption1();
        }
        else
        {
            SpawnOption2();
        }
    }

    // =========================================================
    // OPTION 1
    // =========================================================

    public void SpawnOption1()
    {
        if (isSwitching)
            return;

        StartCoroutine(SwitchPrefab(option1Prefab));
    }

    // =========================================================
    // OPTION 2
    // =========================================================

    public void SpawnOption2()
    {
        if (isSwitching)
            return;

        StartCoroutine(SwitchPrefab(option2Prefab));
    }

    // =========================================================
    // SWITCH PREFAB
    // =========================================================

    private IEnumerator SwitchPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is not assigned!");
            yield break;
        }

        isSwitching = true;

        // Don't fade the very first spawn
        if (currentInstance != null)
        {
            yield return StartCoroutine(FadeToBlack());

            Destroy(currentInstance);

            // Wait one frame to ensure destruction
            yield return null;
        }

        // Create new prefab
        currentInstance = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Fade back in
        if (blackScreen != null)
        {
            yield return StartCoroutine(FadeFromBlack());
        }

        isSwitching = false;
    }

    // =========================================================
    // FADE TO BLACK
    // =========================================================

    private IEnumerator FadeToBlack()
    {
        if (blackScreen == null)
            yield break;

        float timer = 0f;

        Color color = blackScreen.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            color.a =
                Mathf.Lerp(
                    0f,
                    1f,
                    timer / fadeDuration
                );

            blackScreen.color = color;

            yield return null;
        }

        color.a = 1f;

        blackScreen.color = color;
    }

    // =========================================================
    // FADE FROM BLACK
    // =========================================================

    private IEnumerator FadeFromBlack()
    {
        if (blackScreen == null)
            yield break;

        float timer = 0f;

        Color color = blackScreen.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            color.a =
                Mathf.Lerp(
                    1f,
                    0f,
                    timer / fadeDuration
                );

            blackScreen.color = color;

            yield return null;
        }

        color.a = 0f;

        blackScreen.color = color;
    }
}