using UnityEngine;
using UnityEngine.SceneManagement;

public class CoffeeSceneHandler : MonoBehaviour
{
    public Transform mug;
    // The mug object in the scene
    public SpriteRenderer mugRenderer;
    // The part that shows the mug’s sprite
    public Collider2D mugCollider;
    // Used to detect clicks and collisions
    public Sprite mugEmptySprite;
    // What the mug looks like when empty
    public Sprite mugFilledSprite;
    // What it looks like after being filled
    public Sprite mugSpilledSprite;
    // What it looks like if spilled
    public Transform mugStartPoint;
    // Where the mug starts at the beginning
    public Collider2D grabHandleZone;
    public Collider2D grabRimZone;
    public Collider2D makerSlot;
    // Area under the coffee machine
    public Transform mugSnapPoint;
    // Where the mug snaps during pouring
    public Collider2D counterZone;
    // Area representing the counter
    public GameObject coffeeStream;
    // The pouring coffee effect
    public AudioSource narratorSource;
    // Audio source that plays the narration
    public AudioClip narrationClip;
    // The narration clip itself
    public bool allowSkipNarration = true;
    // Can the player skip the narration?
    public string nextSceneName = "NextScene";
    // Scene after a successful pour
    public string gameOverSceneName = "GameOverScene";
    // Scene to load if you spill coffee
    public float dragSmooth = 18f;
    // How smoothly the mug follows the mouse
    public float pourTime = 1.2f;
    // How long the coffee pours
    public float afterFillDelay = 0.5f;
    // Wait time before moving to the next scene
    public float minClickGap = 0.12f;
    // Prevents multiple clicks too close together
    Camera cam;
    bool dragging = false;
    // True if the player is currently dragging the mug
    bool safeGrab = true;
    // True if the player grabbed it by the handle
    Vector3 grabOffset;
    // Distance between mouse and mug when dragging
    bool canInteract = false;
    // True when narration is done and player can play
    float lastInputTime = -999f;
    // Keeps track of when the last input happened

    void Awake()
    {
        // Get the main camera so we can convert mouse positions later
        cam = Camera.main;

        // Set mug to empty and move it to the start point
        if (mugRenderer && mugEmptySprite) mugRenderer.sprite = mugEmptySprite;
        if (mugStartPoint) mug.position = mugStartPoint.position;

        // Hide the coffee stream and set its scale to 0 (so it “grows” when pouring)
        if (coffeeStream)
        {
            coffeeStream.SetActive(false);
            var s = coffeeStream.transform.localScale;
            s.y = 0f;
            coffeeStream.transform.localScale = s;
        }
    }

    void Start()
    {
        // Play narration first if available, otherwise let player interact immediately
        if (narratorSource != null && narrationClip != null)
            StartCoroutine(PlayNarrationThenUnlock());
        else
            canInteract = true;
    }

    // Plays narration and waits for it to finish (or allows skipping)
    System.Collections.IEnumerator PlayNarrationThenUnlock()
    {
        narratorSource.clip = narrationClip;
        narratorSource.Play();

        float t = 0f;
        while (t < narrationClip.length)
        {
            t += Time.deltaTime;

            // If skipping is allowed and the player clicks or presses space
            if (allowSkipNarration && Time.time - lastInputTime > minClickGap &&
                (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                lastInputTime = Time.time;
                narratorSource.Stop();
                break;
            }
            yield return null;
        }

        // Player can now interact after narration ends or is skipped
        canInteract = true;
    }

    void Update()
    {
        // Do nothing if we’re not allowed to interact yet
        if (!canInteract) return;

        // Get the mouse position in world space 
        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0; // Make sure it’s at the same depth as the mug

        // Start dragging when clicking the mug
        if (Input.GetMouseButtonDown(0) && Time.time - lastInputTime > minClickGap)
        {
            lastInputTime = Time.time;
            TryBeginDrag(mouse);
        }

        // If dragging, smoothly move the mug toward the mouse
        if (dragging)
            mug.position = Vector3.Lerp(mug.position, mouse + grabOffset, Time.deltaTime * dragSmooth);

        // When releasing the mouse, stop dragging and check mug position
        if (Input.GetMouseButtonUp(0) && dragging && Time.time - lastInputTime > minClickGap)
        {
            lastInputTime = Time.time;
            EndDrag();
        }
    }

    // Checks if the player clicked on the mug and starts dragging if so
    void TryBeginDrag(Vector3 mouseWorld)
    {
        if (mugCollider == null) return;
        if (!mugCollider.OverlapPoint(mouseWorld)) return; // Not clicking on mug

        // Check if click is on handle 
        bool onHandle = grabHandleZone && grabHandleZone.OverlapPoint(mouseWorld);
        bool onRim = grabRimZone && grabRimZone.OverlapPoint(mouseWorld);

        // If grabbing the rim, mark it as unsafe
        safeGrab = onHandle || (!onHandle && !onRim);

        // Store how far the mouse is from the mug so it drags naturally
        grabOffset = mug.position - mouseWorld;
        dragging = true;
    }

    // Called when the player lets go of the mouse
    void EndDrag()
    {
        dragging = false;

        // Get the mug’s center point for checking overlaps
        Vector2 mugCenter = mugCollider.bounds.center;

        // If the mug is placed correctly under the coffee machine
        if (makerSlot && makerSlot.OverlapPoint(mugCenter))
        {
            // Snap mug to the correct spot and start pouring
            mug.position = mugSnapPoint.position;
            mug.localRotation = Quaternion.identity;
            StartCoroutine(PourThenAdvance());
            return;
        }

        // If the player grabbed the mug by the rim it then drops it on counter
        if (!safeGrab && counterZone && counterZone.OverlapPoint(mugCenter))
        {
            if (mugRenderer && mugSpilledSprite) mugRenderer.sprite = mugSpilledSprite;
            if (!string.IsNullOrEmpty(gameOverSceneName))
                SceneManager.LoadScene(gameOverSceneName);
            return;
        }

        // Otherwise, just reset the mug back to the start
        if (mugStartPoint)
        {
            mug.position = mugStartPoint.position;
            mug.localRotation = Quaternion.identity;
            if (mugRenderer && mugEmptySprite) mugRenderer.sprite = mugEmptySprite;
        }
    }

    // Handles coffee pouring and then goes to the next scene
    System.Collections.IEnumerator PourThenAdvance()
    {
        // Show and animate the coffee stream
        if (coffeeStream)
        {
            coffeeStream.SetActive(true);
            float t = 0f;
            while (t < pourTime)
            {
                t += Time.deltaTime;
                var s = coffeeStream.transform.localScale;
                s.y = Mathf.Clamp01(t / pourTime); // Grows the stream over time
                coffeeStream.transform.localScale = s;
                yield return null;
            }
        }

        // Change mug sprite to filled
        if (mugRenderer && mugFilledSprite) mugRenderer.sprite = mugFilledSprite;

        // Wait a bit after pouring before moving on
        yield return new WaitForSeconds(afterFillDelay);

        // Load the next scene 
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
// Note: This did not work as I intended and I wasn't able to find my error
// As long as we proceeded to the next scene was fine by me
