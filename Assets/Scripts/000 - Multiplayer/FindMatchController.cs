using Photon.Pun;
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
    [SerializeField] private Button findMatchBtn;
    [SerializeField] private Button cancelFindBtn;
    [SerializeField] private TextMeshProUGUI timer;
    [SerializeField] private TextMeshProUGUI status;

    //  ================================

    float currentTimer;
    string minutesFindMatch, secondsFindMatch;

    //  ================================

    private void OnEnable()
    {
        MultiplayerController.instance.OnLobbyStateChange += LobbyStatusChange;
        MultiplayerController.instance.OnClientConnectedChange += ClientConnectedChange;
    }

    private void OnDisable()
    {
        MultiplayerController.instance.OnLobbyStateChange -= LobbyStatusChange;
        MultiplayerController.instance.OnClientConnectedChange -= ClientConnectedChange;
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
