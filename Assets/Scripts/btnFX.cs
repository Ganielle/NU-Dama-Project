using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class btnFX : MonoBehaviour
{
    public AudioSource myFX;
    public AudioClip hoverFX;
    public AudioClip clickFX;
    public Slider volumeSlider;
    private float musicVolume = 1f;

    public void HoverSound(){
        myFX.PlayOneShot(hoverFX);
    }
    public void ClickSound(){
        myFX.PlayOneShot (clickFX);
    }
    void Start()
    {
        musicVolume = PlayerPrefs.GetFloat("volume");
        myFX.volume = musicVolume;
        volumeSlider.value = musicVolume;
    }
    void Update()
    {
        myFX.volume = musicVolume;
        PlayerPrefs.SetFloat("volume", musicVolume);
    }

    public void updateVolume(float volume){
        musicVolume = volume;
    }
}
