using UnityEngine;

public class DoorToggleTwoSprites : MonoBehaviour
{
    public GameObject DoorClosed;
    public GameObject DoorOpen;
    public float clickCooldown = 0.15f;
    float lastClick;

    void OnMouseDown()
    {
        if (Time.time - lastClick < clickCooldown) return;
        lastClick = Time.time;

        bool opening = DoorClosed.activeSelf;
        DoorClosed.SetActive(!opening);
        DoorOpen.SetActive(opening);
    }
}
