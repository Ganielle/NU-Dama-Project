using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManagers : MonoBehaviour
{
    public AudioSource Audio;
    public AudioClip Click;
    public static SFXManagers SFXInstance;
    private void Awake(){
        if(SFXInstance != null && SFXInstance != this){
            Destroy(this.gameObject);
            return;
        }
        SFXInstance = this;
        DontDestroyOnLoad(this);
    }
}
