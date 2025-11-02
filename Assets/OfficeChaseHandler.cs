using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
// I think this is the part I struggled with the most, especially because of the cutscenes
public class OfficeChaseHandler : MonoBehaviour
{
    public VideoPlayer videoPlayer;     // Plays cutscenes
    public VideoClip cutsceneA;         // Opening cutscene
    public VideoClip cutsceneB;         // Ending cutscene
    public CanvasGroup fadeOverlay;
    public GameObject videoImageLayer;

    public GameObject chaseBackgroundRoot;
    public AudioSource narratorSource;
    public AudioSource sfxSource;
    public AudioSource breathingSource;
    public AudioSource ambientSource;   // background ambience
    public AudioClip[] narratorLines;   // lines to play at moments in the chase
    public AudioClip footstepsLoop;     // loop while running
    public AudioClip breathingLoop;     // loop that changes intensity
    public AudioClip ambientLoop;
    public AudioClip trexRoar;
    public float requiredMash = 40f;    // I changed it to 100 in unity
    public float mashPerPress = 2.0f;       // how much each key press adds
    public float mashDecayPerSecond = 5f;   // how fast it decays over time
    public float totalChaseTime = 10f;      // time limit to reach the goal
    public ForwardRunnerCamera runnerCam;   // camera that zooms/bobs with speed
    public TinyCameraShake shaker;          // small camera shake component
    public float shakeMax = 0.12f;          // max shake at full speed
    public string nextSceneName = "Credits";
    public string gameOverSceneName = "SceneLose";
    private bool inChase;    // true while the chase is active
    private bool finished;   // true once win/lose has triggered
    private float mash;      // current effort value from mashing
    private float chaseTimer;// counts down the chase time

    // Which key the player presses to run
    private readonly KeyCode runKey = KeyCode.Space;

    void Start()
    {
        // Start with video layer visible and gameplay background hidden
        if (videoImageLayer)      videoImageLayer.SetActive(true);
        if (chaseBackgroundRoot)  chaseBackgroundRoot.SetActive(false);
        if (fadeOverlay)          fadeOverlay.alpha = 1f; // start faded in 

        // Start ambient audio early so it feels continuous
        if (ambientSource && ambientLoop)
        {
            ambientSource.loop = true;
            ambientSource.clip = ambientLoop;
            ambientSource.Play();
        }

        // If we have an opening cutscene, play it first
        if (videoPlayer && cutsceneA)
            StartCoroutine(PlayPrepared(cutsceneA));
        else
            StartCoroutine(BeginChaseAfter(0.25f));

        // Set initial feedback values to idle
        if (runnerCam) runnerCam.SetSpeed01(0f);
        if (shaker)    shaker.amplitude = 0f;

        // Fade from black into the first thing we show
        StartCoroutine(FadeIn(0.75f));
    }

    // Prepares a video then plays it
    System.Collections.IEnumerator PlayPrepared(VideoClip clip)
    {
        videoPlayer.clip = clip;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null; // wait until ready

        // When the clip ends, start the chase
        videoPlayer.loopPointReached += OnCutsceneAEnded;
        videoPlayer.Play();
    }

    void OnCutsceneAEnded(VideoPlayer vp)
    {
        // Clean up event and move into gameplay
        videoPlayer.loopPointReached -= OnCutsceneAEnded;
        StartCoroutine(BeginChaseAfter(0.25f));
    }

    // Small delay, then swap UI layers and boot the chase systems
    System.Collections.IEnumerator BeginChaseAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Hide video, show gameplay background
        if (videoImageLayer)     videoImageLayer.SetActive(false);
        if (chaseBackgroundRoot) chaseBackgroundRoot.SetActive(true);
        PlayNarrationSafe(0);
        yield return new WaitForSeconds(0.5f);

        // Start looping SFX for footsteps and breathing
        if (sfxSource && footstepsLoop)
        {
            sfxSource.clip = footstepsLoop;
            sfxSource.loop = true;
            sfxSource.Play();
        }
        if (breathingSource && breathingLoop)
        {
            breathingSource.clip = breathingLoop;
            breathingSource.loop = true;
            breathingSource.volume = 0.35f; // start lighter
            breathingSource.Play();
        }

        // Reset chase state
        inChase    = true;
        finished   = false;
        mash       = 0f;
        chaseTimer = totalChaseTime;

        // Kick off timed narrator prompts during the chase
        StartCoroutine(ChasePrompts());
    }

    // Plays mid-chase narrator lines at specific times
    System.Collections.IEnumerator ChasePrompts()
    {
        float[] times = { 2.5f, 5f, 7.5f };
        int clipIndex = 1;
        foreach (var t in times)
        {
            yield return new WaitForSeconds(t);
            if (!inChase || finished) yield break; // stop if chase ended
            PlayNarrationSafe(clipIndex);
            // Clamp index to the second-to-last clip (last clip reserved for win)
            clipIndex = Mathf.Min(clipIndex + 1, Mathf.Max(0, narratorLines.Length - 2));
        }
    }

    void Update()
    {
        // Only process input/logic while chase is active
        if (!inChase || finished) return;

        // Pressing the run key adds to mash
        if (Input.GetKeyDown(runKey))
            mash += mashPerPress;

        // Mash value decays over time so you must keep tapping
        mash -= mashDecayPerSecond * Time.deltaTime;
        mash = Mathf.Clamp(mash, 0f, requiredMash);

        // Timer counts down to zero
        chaseTimer -= Time.deltaTime;
        float pct = (requiredMash <= 0f) ? 0f : (mash / requiredMash);

        // Feed pct into camera speed and camera shake for feedback
        if (runnerCam) runnerCam.SetSpeed01(pct);
        if (shaker)    shaker.amplitude = shakeMax * pct;

        // Breathing gets heavier when you're going slower
        if (breathingSource)
        {
            breathingSource.volume = Mathf.Lerp(0.2f, 0.85f, 1f - pct);
            breathingSource.pitch = Mathf.Lerp(1.05f, 0.90f, 1f - pct);
        }

        // Win: reached required mash
        // Lose: time ran out
        if (mash >= requiredMash)
            StartCoroutine(WinSequence());
        else if (chaseTimer <= 0f)
            StartCoroutine(LoseSequence());
    }

    // What happens when you win the chase
    System.Collections.IEnumerator WinSequence()
    {
        finished = true;
        inChase  = false;

        // Play the final narrator line 
        PlayNarrationSafe(narratorLines.Length - 1);

        // Stop chase loops
        if (sfxSource)       sfxSource.Stop();
        if (breathingSource) breathingSource.Stop();

        // Fade to black before showing the ending cutscene
        yield return FadeOut(0.6f);

        // Optional ending cutscene B
        if (videoPlayer && cutsceneB)
        {
            if (videoImageLayer)     videoImageLayer.SetActive(true);
            if (chaseBackgroundRoot) chaseBackgroundRoot.SetActive(false);

            bool done = false;
            videoPlayer.clip = cutsceneB;
            videoPlayer.loopPointReached += (vp) => done = true;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared) yield return null;
            videoPlayer.Play();

            // Fade back in for the cutscene, then out again when it ends
            yield return FadeIn(0.6f);
            while (!done) yield return null;
            yield return FadeOut(0.6f);
        }

        // Move to the next scene, this would be victory for the player
        SceneManager.LoadScene(nextSceneName);
    }

    // What happens when you lose the chase
    System.Collections.IEnumerator LoseSequence()
    {
        finished = true;
        inChase  = false;
        if (sfxSource)       sfxSource.Stop();
        if (breathingSource) breathingSource.Stop();

        // Play a scare/roar if provided
        if (narratorSource && trexRoar)
        {
            narratorSource.Stop();
            narratorSource.clip = trexRoar;
            narratorSource.Play();
        }

        // Small pause, fade, then load game-over scene
        yield return new WaitForSeconds(1.0f);
        yield return FadeOut(0.6f);
        SceneManager.LoadScene(gameOverSceneName);
    }

    // --- Helpers ---

    // Safely plays a narrator line by index
    void PlayNarrationSafe(int index)
    {
        if (!narratorSource || narratorLines == null || narratorLines.Length == 0) return;
        if (index < 0 || index >= narratorLines.Length) return;
        narratorSource.Stop();
        narratorSource.clip = narratorLines[index];
        narratorSource.Play();
    }

    // Fade from current alpha to 0 
    System.Collections.IEnumerator FadeIn(float dur)
    {
        if (!fadeOverlay) yield break;
        float t = 0f; float a0 = fadeOverlay.alpha;
        while (t < dur)
        {
            t += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(a0, 0f, t / dur);
            yield return null;
        }
        fadeOverlay.alpha = 0f;
    }

    // Fade from current alpha to 1
    System.Collections.IEnumerator FadeOut(float dur)
    {
        if (!fadeOverlay) yield break;
        float t = 0f; float a0 = fadeOverlay.alpha;
        while (t < dur)
        {
            t += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(a0, 1f, t / dur);
            yield return null;
        }
        fadeOverlay.alpha = 1f;
    }
}