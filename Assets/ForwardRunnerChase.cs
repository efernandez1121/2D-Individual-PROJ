using UnityEngine;

// Makes sure there is always a Camera attached to this GameObject
[RequireComponent(typeof(Camera))]
public class ForwardRunnerCamera : MonoBehaviour
{
    // If true, use the camera's current size as the starting zoom level
    public bool useCameraSizeAsStart = true;

    // Used if the above option is false — sets the starting camera size manually
    public float startSize = 5f;

    // How much the camera zooms in when moving fast (smaller size = more zoom)
    public float zoomInAmount = 20f;

    // How quickly the zoom changes from normal to zoomed in
    public float zoomLerpSpeed = 3f;

    // How much the camera moves up and down while running 
    public float bobAmplitude = 0.08f;

    // How fast the bobbing happens 
    public float bobFrequency = 7.5f;

    // The target zoom level, between 0 and 1 
    private float targetT = 0f;     

    // The current zoom level 
    private float currentT = 0f;

    // The camera component we’re controlling
    private Camera cam;

    // The original local position of the camera 
    private Vector3 baseLocalPos;

    // Stores the starting size of the camera during gameplay
    private float startSizeRuntime; 

    // The smallest zoom size we can reach
    private float minSizeRuntime; 

    // Runs once when the script starts
    void Awake()
    {
        // Get the Camera component from the same GameObject
        cam = GetComponent<Camera>();

        // Make sure the camera is orthographic 
        cam.orthographic = true;

        // Remember where the camera started 
        baseLocalPos = transform.localPosition;

        // Decide what the starting zoom size should be
        if (useCameraSizeAsStart)
            startSizeRuntime = cam.orthographicSize;  // Use whatever size is already on the camera
        else
            startSizeRuntime = startSize;             // Use the value set in the Inspector

        // Calculate the smallest zoom size (so it never goes below zero)
        minSizeRuntime = Mathf.Max(0.01f, startSizeRuntime - Mathf.Abs(zoomInAmount));
    }

    // This function is called by other scripts to tell the camera how fast the player is going 
    public void SetSpeed01(float t)
    {
        targetT = Mathf.Clamp01(t); // Make sure it stays between 0 and 1
    }

    // LateUpdate runs after everything else — good for camera movement
    void LateUpdate()
    {
        // Smoothly move currentT toward targetT (makes zoom smooth)
        currentT = Mathf.Lerp(currentT, targetT, Time.deltaTime * zoomLerpSpeed);

        // Adjust the camera zoom based on speed
        cam.orthographicSize = Mathf.Lerp(startSizeRuntime, minSizeRuntime, currentT);

        // Calculate the up-and-down bobbing motion
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude * currentT;

        // Apply the bobbing to the camera's local position
        transform.localPosition = baseLocalPos + new Vector3(0f, bob, 0f);
    }
}
