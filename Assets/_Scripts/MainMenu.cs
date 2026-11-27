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

    public void FullScreen(bool isFull)
    {
        Screen.fullScreen = isFull;
    }

    // sound and music not implimented yet,, just stores them atm.
    public void SetVolume(float v)
    {
        PlayerPrefs.SetFloat("MasterVolume", v);
    }
    public void SetVolumeMusic(float v)
    {
        PlayerPrefs.SetFloat("MusicVolume", v);
    }
    public void SetVolumeSFX(float v)
    {
        PlayerPrefs.SetFloat("SFXVolume", v);
    }

}
