using UnityEngine;

public class TinyCameraShake : MonoBehaviour
{
    public float amplitude = 0f;
    public float frequency = 24f;

    Vector3 basePos;

    void Awake() { basePos = transform.localPosition; }

    void LateUpdate()
    {
        if (amplitude <= 0f)
        {
            transform.localPosition = basePos;
            return;
        }
        float x = (Mathf.PerlinNoise(Time.time * frequency, 0.3f) - 0.5f) * 2f * amplitude;
        float y = (Mathf.PerlinNoise(0.8f, Time.time * frequency) - 0.5f) * 2f * amplitude;
        transform.localPosition = basePos + new Vector3(x, y, 0f);
    }
}
