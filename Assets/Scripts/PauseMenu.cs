using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
   
    public void Pause(){
        Time.timeScale = 0f;
        AudioListener.pause = true;
        pauseMenuUI.SetActive(true);
    }
    public void Resume(){
        AudioListener.pause = false;
        Time.timeScale = 1f;
        pauseMenuUI.SetActive(false);
    }
}
