using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FindMatchController : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject cancelBtn;
    [SerializeField] private GameObject findBtn;
    [SerializeField] private GameObject matchFoundPanelObj;
    [SerializeField] private Button findMatchBtn;
    [SerializeField] private Button cancelFindBtn;
    [SerializeField] private TextMeshProUGUI timer;
    [SerializeField] private TextMeshProUGUI status;
    [SerializeField] private TextMeshProUGUI matchFoundTimerTMP;

    //  ================================

    float currentTimer;
    string minutesFindMatch, secondsFindMatch;

    //  ================================

    private void OnEnable()
    {
        MultiplayerController.instance.OnLobbyStateChange += LobbyStatusChange;
        MultiplayerController.instance.OnClientConnectedChange += ClientConnectedChange;
        PhotonNetwork.NetworkingClient.EventReceived += MatchmakingEvents;
    }

    private void OnDisable()
    {
        MultiplayerController.instance.OnLobbyStateChange -= LobbyStatusChange;
        MultiplayerController.instance.OnClientConnectedChange -= ClientConnectedChange;
        PhotonNetwork.NetworkingClient.EventReceived -= MatchmakingEvents;
    }

    private void Start()
    {
        status.text = "CONNECTING TO SERVER...";
        timer.text = "";

        PhotonNetwork.ConnectUsingSettings();
        MultiplayerController.instance.ConnectingToServer = true;
        MultiplayerController.instance.ClientConnectedToServer = false;
        findBtn.SetActive(false);
        cancelBtn.SetActive(false);
    }

    private void LobbyStatusChange(object sender, EventArgs e)
    {
        LobbyStateObjectCheckers();
    }

    private void ClientConnectedChange(object sender, EventArgs e)
    {
        if (MultiplayerController.instance.ClientConnectedToServer)
        {
            MultiplayerController.instance.CurrentLobbyState = MultiplayerController.LobbyState.NONE;
            LobbyStateObjectCheckers();
        }
    }

    private void Update()
    {
        FindMatchTimer();
    }

    #region NON-PHOTON

    private void FindMatchTimer()
    {
        if (MultiplayerController.instance.CurrentLobbyState != MultiplayerController.LobbyState.FINDINGMATCH)
            return;

        currentTimer += Time.deltaTime;

        minutesFindMatch = Mathf.Floor(currentTimer / 60).ToString("00");

        secondsFindMatch = (currentTimer % 60).ToString("00");

        status.text = "FINDING MATCH...";
        timer.text = string.Format("{0}:{1}", minutesFindMatch, secondsFindMatch);
    }

    private void LobbyStateObjectCheckers()
    {
        if (MultiplayerController.instance.CurrentLobbyState == MultiplayerController.LobbyState.NONE)
        {
            findMatchBtn.interactable = true;
            cancelBtn.SetActive(false);
            findBtn.SetActive(true);
            timer.text = "";
            status.text = "";
            currentTimer = 0;
        }
        else if (MultiplayerController.instance.CurrentLobbyState == MultiplayerController.LobbyState.FINDINGMATCH)
        {
            cancelFindBtn.interactable = true;
            findBtn.SetActive(false);
            cancelBtn.SetActive(true);
        }
        else if (MultiplayerController.instance.CurrentLobbyState == MultiplayerController.LobbyState.MATCHFOUND)
        {
            CheckIfReadyToStartMatch();
        }
    }

    private IEnumerator CancelFindMatch()
    {
        PhotonNetwork.Disconnect();

        MultiplayerController.instance.ClientConnectedToServer = false;

        findMatchBtn.interactable = false;

        while (PhotonNetwork.IsConnected)
            yield return null;

        PhotonNetwork.ConnectUsingSettings();
        MultiplayerController.instance.ConnectingToServer = true;
        MultiplayerController.instance.ClientConnectedToServer = false;
    }

    private void CheckIfReadyToStartMatch()
    {
        matchFoundPanelObj.SetActive(true);

        matchFoundTimerTMP.text = "MATCH FOUND \n 3";

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            {
                StartCoroutine(MatchFoundTimer());
            }
        }
    }

    IEnumerator MatchFoundTimer()
    {

        int currentMatchFoundTime = 3;

        matchFoundTimerTMP.text = "MATCH FOUND \n" + currentMatchFoundTime.ToString();

        while (currentMatchFoundTime > -1)
        {
            matchFoundTimerTMP.text = "MATCH FOUND \n" + currentMatchFoundTime.ToString();

            yield return new WaitForSeconds(1f);

            currentMatchFoundTime -= 1;

            object[] data = new object[]
           {
            currentMatchFoundTime
           };

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others
            };
            SendOptions sendOptions = new SendOptions
            {
                Reliability = true
            };

            PhotonNetwork.RaiseEvent(18, data, raiseEventOptions, sendOptions);

            yield return null;
        }
    }

    #endregion

    #region PHOTON

    private void MatchmakingEvents(EventData obj)
    {

        if (obj.Code == 18)
        {
            object[] data = (object[])obj.CustomData;

            int timer = Convert.ToInt32(data[0]);

            matchFoundTimerTMP.text = "MATCH FOUND\n" + timer.ToString();
        }

        if (obj.Code == 19)
        {
            object[] dataState = (object[])obj.CustomData;

            int state = Convert.ToInt32(dataState[0]);

            MultiplayerController.instance.CurrentLobbyState = (MultiplayerController.LobbyState)state;
        }
    }

    #endregion

    #region BUTTON

    public void FindMatchButton()
    {
        findMatchBtn.interactable = false;
        ExitGames.Client.Photon.Hashtable customFilterMap = new ExitGames.Client.Photon.Hashtable { { "MAP", "001"} };

        PhotonNetwork.JoinRandomRoom(customFilterMap, 2);
    }

    public void CancelFindMatchButton() 
    {
        cancelFindBtn.interactable = false;
        StartCoroutine(CancelFindMatch());
    }

    #endregion
}
