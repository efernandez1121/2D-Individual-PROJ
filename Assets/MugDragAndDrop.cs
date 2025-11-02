using UnityEngine;
using UnityEngine.SceneManagement;

public class MugDragAndDrop : MonoBehaviour
{
    public Collider2D grabHandleZone;  
    public Collider2D grabRimZone; 
    public Collider2D makerSlot;
    public Transform mugSnapPoint; 
    public Collider2D counterZone; 
    public Transform mugStartPoint; 

    public SpriteRenderer mugRenderer;
    public Sprite mugEmptySprite;
    public Sprite mugFilledSprite;
    public Sprite mugSpilledSprite;
    public GameObject coffeeStream; 

    public float dragSmooth = 18f;
    public float pourTime = 1.2f;       // grow stream
    public float fillHold = 0.4f;       // short beat before switching scenes

    public string nextSceneName = "DinoScene";
    public string gameOverSceneName = "SceneLose";

    Camera cam;
    bool dragging;
    bool safeGrab;          // true if grabbed by handle 
    Vector3 grabOffset;

    void Awake()
    {
        cam = Camera.main;
        if (mugRenderer == null) mugRenderer = GetComponent<SpriteRenderer>();
        if (mugRenderer && mugEmptySprite) mugRenderer.sprite = mugEmptySprite;

        if (coffeeStream)
        {
            coffeeStream.SetActive(false);
            var s = coffeeStream.transform.localScale; s.y = 0f; coffeeStream.transform.localScale = s;
        }

        if (mugStartPoint) transform.position = mugStartPoint.position;
    }

    void OnMouseDown()
    {
        // must have a collider on this Mug root for OnMouseDown to fire
        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition); mouse.z = 0;

        // Determine how it was grabbed: handle = safe, rim = unsafe, body = safe
        bool onHandle = grabHandleZone && grabHandleZone.OverlapPoint(mouse);
        bool onRim    = grabRimZone    && grabRimZone.OverlapPoint(mouse);
        safeGrab = onHandle || (!onHandle && !onRim);

        grabOffset = transform.position - mouse;
        dragging = true;
    }

    void OnMouseDrag()
    {
        if (!dragging) return;
        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition); mouse.z = 0;
        transform.position = Vector3.Lerp(transform.position, mouse + grabOffset, Time.deltaTime * dragSmooth);
    }

    void OnMouseUp()
    {
        if (!dragging) return;
        dragging = false;

        // Success: dropped in coffee maker slot
        if (makerSlot && makerSlot.OverlapPoint(transform.position))
        {
            transform.position = mugSnapPoint.position;
            transform.localRotation = Quaternion.identity;
            StartCoroutine(PourThenAdvance());
            return;
        }

        // Fail: unsafe grab + dropped on counter
        if (!safeGrab && counterZone && counterZone.OverlapPoint(transform.position))
        {
            if (mugRenderer && mugSpilledSprite) mugRenderer.sprite = mugSpilledSprite;
            if (!string.IsNullOrEmpty(gameOverSceneName))
                SceneManager.LoadScene(gameOverSceneName);
            return;
        }

        // Otherwise, return to cabinet
        if (mugStartPoint)
        {
            transform.position = mugStartPoint.position;
            transform.localRotation = Quaternion.identity;
            if (mugRenderer && mugEmptySprite) mugRenderer.sprite = mugEmptySprite;
        }
    }

    System.Collections.IEnumerator PourThenAdvance()
    {
        // show & grow the coffee stream
        if (coffeeStream)
        {
            coffeeStream.SetActive(true);
            float t = 0f;
            while (t < pourTime)
            {
                t += Time.deltaTime;
                var s = coffeeStream.transform.localScale;
                s.y = Mathf.Clamp01(t / pourTime);
                coffeeStream.transform.localScale = s;
                yield return null;
            }
        }

        // swap to filled mug and continue
        if (mugRenderer && mugFilledSprite) mugRenderer.sprite = mugFilledSprite;
        yield return new WaitForSeconds(fillHold);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}

