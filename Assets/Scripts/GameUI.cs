using System;
using TMPro;
using UnityEngine;

    public enum CameraAngle{
        menu = 0,
        whiteTeam = 1,
        blackTeam = 2
    }

public class GameUI : MonoBehaviour
{
    public static GameUI Instance{set; get;}

    public Server server;
    public Client client;

    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private GameObject[] cameraAngles;

    public Action<bool> SetLocalGame;

    private void Awake(){
        Instance = this;
        RegisterEvents();
    }

    //Cameras
    public void ChangeCamera(CameraAngle index){
        for (int i = 0; i < cameraAngles.Length; i++)
            cameraAngles[i].SetActive(false);

        cameraAngles[(int)index].SetActive(true);
    }

    //Button
    public void OnLocalGameButton(){
        menuAnimator.SetTrigger("InGameMenu");
        SetLocalGame?.Invoke(true);
        server.Init(6321);
        client.Init("127.0.0.1", 6321);
    }

    public void OnOnlineGameButton(){
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnOnlineHostButton(){
        SetLocalGame?.Invoke(false);
        server.Init(6321);
        client.Init("127.0.0.1", 6321);
        menuAnimator.SetTrigger("HostMenu");
    }

    public void OnOnlineConnectButton(){
        SetLocalGame?.Invoke(false);
        client.Init(addressInput.text, 6321);
    }

    public void OnOnlineBackButton(){
        menuAnimator.SetTrigger("StartMenu");
    }

    public void OnHostBackButton(){
        server.Shutdown();
        client.Shutdown();
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnLeaveFromGameMenu(){
        ChangeCamera(CameraAngle.menu);
        menuAnimator.SetTrigger("StartMenu");
    }

    #region 
    private void RegisterEvents(){
        NetUtility.C_START_GAME += OnStartGameClient;
    }

    private void UnregisterToEvents(){
        NetUtility.C_START_GAME -= OnStartGameClient;
    }
    private void OnStartGameClient(NetMessage obj){
        menuAnimator.SetTrigger("InGameMenu");
    }

    #endregion
}