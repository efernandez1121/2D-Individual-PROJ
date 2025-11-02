using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameHandler : MonoBehaviour
{
        public static int playerStat1;

    void Update()
    {
    }

    // Called when the "Start Game" button is pressed
    public void StartGame()
    {
        // Loads the first gameplay scene
        SceneManager.LoadScene("Scene1");
    }

    // Called when the Credits button is pressed
    public void OpenCredits()
    {
        // Loads the Credits scene
        SceneManager.LoadScene("Credits");
    }

    // Called when the Restart or Main Menu button is pressed
    public void RestartGame()
    {
        // Make sure time is running normally again 
        Time.timeScale = 1f;

        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    // Called when the Quit Game button is pressed
    public void QuitGame()
    {
        // Quits differently depending on if we're in the editor or a built game
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // stop play mode in Unity
        #else
        Application.Quit(); // close the game when built
        #endif
    }
}
