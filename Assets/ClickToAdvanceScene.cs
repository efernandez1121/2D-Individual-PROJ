using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// this script is for the opening scene
public class ClickToAdvanceScene : MonoBehaviour
{
    // These are the character sprites that will appear one after another when clicked
    public GameObject CharSprite;
    public GameObject CharSprite1;
    public GameObject CharSprite2;

    // Audio stuff for narration
    public AudioSource narratorSource;
    public AudioClip narrationClip;
    public bool allowSkipNarration = true;

    // Scene transition settings
    public string nextSceneName = "NextSceneNameHere"; 
    public float fadeDuration = 1.0f;                  // How long the fade to black lasts
    public float minClickGap = 0.15f;                  // Time between allowed clicks

    private GameObject[] order;    // Array holding the characters in the order they’ll appear
    private int index = 0;
    private bool transitioning = false;
    private float lastClickTime = -999f; 
    private bool canClick = false;

    void Start()
    {
        // Store characters in an array so we can easily switch between them
        order = new[] { CharSprite, CharSprite1, CharSprite2 };

        // Show only the first character at the start
        SetVisibleOnly(0);

        // If we have narration, play it before allowing clicks
        if (narratorSource != null && narrationClip != null)
        {
            StartCoroutine(PlayNarrationThenEnableClicks());
        }
        else
        {
            canClick = true;
        }
    }

    // This coroutine plays the narration and waits for it to finish
    System.Collections.IEnumerator PlayNarrationThenEnableClicks()
    {
        narratorSource.clip = narrationClip;
        narratorSource.Play();

        float t = 0f;
        while (t < narrationClip.length)
        {
            t += Time.deltaTime;

            // If skipping is allowed, stop the audio on click or spacebar
            if (allowSkipNarration && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                narratorSource.Stop();
                break;
            }
            yield return null;
        }

        // Once narration is done, we can start clicking to advance
        canClick = true;
    }

    void Update()
    {
        // If we’re not allowed to click yet or we’re in transition, skip update
        if (!canClick || transitioning) return;

        // If the player clicks and enough time has passed since the last click
        if (Input.GetMouseButtonDown(0) && Time.time - lastClickTime > minClickGap)
        {
            lastClickTime = Time.time;
            index++;
            // If there are still sprites left, show the next one
            if (index < order.Length)
            {
                SetVisibleOnly(index);
            }
            else
            {
                // time to fade out and load the next scene
                transitioning = true;
                StartCoroutine(FadeAndLoad());
            }
        }
    }

    // Turns on only one sprite at a time 
    void SetVisibleOnly(int which)
    {
        for (int i = 0; i < order.Length; i++)
        {
            if (order[i] == null) continue;

            var sr = order[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = (i == which); 
        }
    }

    // Handles the fade-to-black and then loads the next scene
    private System.Collections.IEnumerator FadeAndLoad()
    {
        GameObject fadeCanvas = new GameObject("FadeCanvas");
        var canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var img = fadeCanvas.AddComponent<Image>();
        img.color = Color.black; 

        var cg = fadeCanvas.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Gradually fade in 
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        // Once fade is done, load the next scene
        SceneManager.LoadScene(nextSceneName);
    }
}
