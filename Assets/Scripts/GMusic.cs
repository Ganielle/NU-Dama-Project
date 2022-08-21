using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GMusic : MonoBehaviour
{
    public static GMusic GMusicInstance;
    private void Awake(){
        if(GMusicInstance != null && GMusicInstance != this){
            Destroy(this.gameObject);
            return;
        }
        GMusicInstance = this;
        DontDestroyOnLoad(this);
    }
}
