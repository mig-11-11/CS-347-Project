using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // to start the game via switch scenes
    public void PlayGame()
    {
        SceneManager.LoadScene("_Scene_0");
    }

    // to exit the game
    public void QuitGame()
    {
        Debug.Log("I QUIT I SWEAR");
        Application.Quit();
    }

    public void Options()
    {
        SceneManager.LoadScene("Options");
    }

    public void CloseOptions()
    {
        SceneManager.LoadScene("TitleScreen");
    }
}
