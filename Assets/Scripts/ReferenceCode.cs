using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
public class ReferenceCode : MonoBehaviour
{
    [SerializeField]
    Sprite[] sprite;
    [SerializeField]
    Image btnImage;
    [SerializeField]
    ARPlaneManager planeManager;

     void Awake()
        {
            planeManager = GetComponent<ARPlaneManager>();
        }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
     public void TogglePlaneDetection(){
         
            planeManager.enabled = !planeManager.enabled;
            foreach(ARPlane planes in planeManager.trackables){
                planes.gameObject.SetActive(planeManager.enabled);
            }
            if(planeManager.enabled){
             btnImage.sprite = sprite[0];
         }
         else{
             btnImage.sprite = sprite[1];
         }
        }
}
