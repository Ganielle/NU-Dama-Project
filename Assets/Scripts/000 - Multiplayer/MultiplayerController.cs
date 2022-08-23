using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MultiplayerController : MonoBehaviourPunCallbacks
{
    private static MultiplayerController _instance;

    public static MultiplayerController instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MultiplayerController>();

                if (_instance == null)
                    _instance = new GameObject().AddComponent<MultiplayerController>();
            }

            return _instance;
        }
    }

    //  ====================================

    public enum LobbyState
    {
        NONE,
        FINDINGMATCH,
        MATCHFOUND
    }
    private event EventHandler LobbyStateChange;
    public event EventHandler OnLobbyStateChange
    {
        add
        {
            if (LobbyStateChange == null || !LobbyStateChange.GetInvocationList().Contains(value))
                LobbyStateChange += value;
        }
        remove { LobbyStateChange -= value; }
    }
    public LobbyState CurrentLobbyState
    {
        get => currentLobbyState;
        set
        {
            currentLobbyState = value;
            LobbyStateChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler discconectCauseChange;
    public event EventHandler onDisconnectCauseChange
    {
        add
        {
            if (discconectCauseChange == null || !discconectCauseChange.GetInvocationList().
                Contains(value))
                discconectCauseChange += value;
        }
        remove { discconectCauseChange -= value; }
    }
    public DisconnectCause DisconnectCauseStatus
    {
        get => disconnectCause;
        set
        {
            disconnectCause = value;
            discconectCauseChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool ConnectingToServer
    {
        get => connectingToServer;
        set => connectingToServer = value;
    }

    private event EventHandler ClientConnectedChange;
    public event EventHandler OnClientConnectedChange
    {
        add
        {
            if (ClientConnectedChange == null || ClientConnectedChange.GetInvocationList().Contains(value))
                ClientConnectedChange += value;
        }
        remove { ClientConnectedChange -= value; }
    }
    public bool ClientConnectedToServer
    {
        get => clientConnectedToServer;
        set
        {
            clientConnectedToServer = value;
            ClientConnectedChange?.Invoke(this, EventArgs.Empty);
        }
    }

    //  ====================================



    //  ====================================

    private LobbyState currentLobbyState;
    private DisconnectCause disconnectCause;

    private bool connectingToServer;
    private bool clientConnectedToServer;

    //  ====================================

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
    }

    private void OnDisable()
    {
        _instance = null;
        Destroy(this);
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        ClientConnectedToServer = true;

        ConnectingToServer = false;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);

        DisconnectCauseStatus = cause;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);

        //  Create room when joining room failed
        CreateRoom("001", 2);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        CurrentLobbyState = LobbyState.FINDINGMATCH;
    }

    //public override void 

    public void CreateRoom(string mapCode, byte maxPlayers)
    {
        //  This is for creating room
        //  Custom room properties using hashtable
        ExitGames.Client.Photon.Hashtable customRoomProperties = new ExitGames.Client.Photon.Hashtable()
        { { "MAP", mapCode } };
        string[] lobbyProperties = { "MAP" };

        //  This is for setting up room properties provided
        //  by PUN2
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;
        roomOptions.IsVisible = true;
        roomOptions.CustomRoomPropertiesForLobby = lobbyProperties;
        roomOptions.CustomRoomProperties = customRoomProperties;
        roomOptions.CleanupCacheOnLeave = true;

        PhotonNetwork.CreateRoom(null, roomOptions, null);


    }
}
