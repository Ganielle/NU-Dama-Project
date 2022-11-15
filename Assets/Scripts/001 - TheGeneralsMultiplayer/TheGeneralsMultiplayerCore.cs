using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TheGeneralsMultiplayerCore : MonoBehaviour
{
    public enum GameState
    {
        READY,
        GAME,
        END
    }
    private event EventHandler GameStateChange;
    public event EventHandler OnGameStateChange
    {
        add
        {
            if (GameStateChange == null || !GameStateChange.GetInvocationList().Contains(value))
                GameStateChange += value;
        }
        remove { GameStateChange -= value; }
    }
    public GameState CurrentGameState
    {
        get => currentGameState;
        set
        {
            currentGameState = value;
            GameStateChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public string CurrentTurn
    {
        get => currentTurn;
        set => currentTurn = value;
    }

    private event EventHandler PlayerStatesChange;
    public event EventHandler OnPlayerStateChange
    {
        add
        {
            if (PlayerStatesChange == null || !PlayerStatesChange.GetInvocationList().Contains(value))
                PlayerStatesChange += value;
        }
        remove { PlayerStatesChange -= value; }
    }
    public void ChangePlayerStates(string key, string value)
    {
        try
        {
            playerStates[key] = value;

            PlayerStatesChange?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            playerStates.Add(key, value);
        }
    }
    public void AddPlayerStates(string key, string value) => playerStates.Add(key, value);

    public bool CanNowAttack
    {
        get => canAttack;
        set => canAttack = value;
    }

    //  ========================================

    public GameObject forfeitBtnObj;

    [Header("CAMERA")]
    [SerializeField] private Camera currentCamera;
    [SerializeField] private GameObject whiteCamObject;
    [SerializeField] private GameObject blackCamObject;

    [Header("LOADER")]
    [SerializeField] private GameObject loadingObj;
    [TextArea] [SerializeField] private List<string> loadings;
    [SerializeField] private TextMeshProUGUI loadingStatusTMP;
    [SerializeField] private float typingSpeed;
    [SerializeField] private float delayShow;

    [Header("NEXT ATTACKER STATE")]
    [SerializeField] private GameObject attackerStateObj;
    [SerializeField] private TextMeshProUGUI attackerState;
    [SerializeField] private GameObject doneTacticsBtn;

    [Header("TILES")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private float dragOffset = 1f;

    [Header("GAME")]
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;

    [Header("WIN")]
    [SerializeField] private GameObject winPanelObj;
    [SerializeField] private TextMeshProUGUI winStatusTMP;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //  ========================================

    [Header("DEBUGGER")]
    [SerializeField] private GameState currentGameState;
    [SerializeField] private string currentTurn;

    [SerializeField] private string playerControl;
    [SerializeField] private string win = "";
    private Dictionary<string, string> playerStates = new Dictionary<string, string>();

    private const int TILE_COUNT_X = 9;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private UnitPiece[,] unitPieces;
    private Vector3 bounds;
    private bool doneInstantiateLastPiece;

    RaycastHit info;
    Ray ray;
    public bool canAttack;
    private Vector2Int currentHover;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private UnitPiece currentlyDragging;
    private List<UnitPiece> deadWhites = new List<UnitPiece>();
    private List<UnitPiece> deadBlacks = new List<UnitPiece>();

    private bool doneOtherPlayerTiles;

    Coroutine loadingStatus;

    //  ========================================

    RaiseEventOptions EventOptions = new RaiseEventOptions
    {
        Receivers = ReceiverGroup.Others,
        CachingOption = EventCaching.AddToRoomCache
    };
    SendOptions SendOptions = new SendOptions { Reliability = true };

    //  ========================================

    private void OnEnable()
    {
        loadingStatus = StartCoroutine(TriviaShower());

        AddPlayerStates("White", "INITIALIZE");
        AddPlayerStates("Black", "INITIALIZE");

        PhotonNetwork.NetworkingClient.EventReceived += MultiplayerEvents;
        OnPlayerStateChange += PlayerStatesChangeEvent;

        SetPlayerControl();
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= MultiplayerEvents;
        OnPlayerStateChange -= PlayerStatesChangeEvent;
    }

    private void Update()
    {
        MoveCharacter();
    }

    public void ReturnToMainMenu()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.DestroyAll();

        PhotonNetwork.Disconnect();

        MultiplayerController.instance.ClientConnectedToServer = false;

        SceneManager.LoadScene("NewMainMenu");
    }

    #region MULTIPLAYER EVENTS

    private void MultiplayerEvents(EventData obj)
    {
        if (obj.Code == 20)
        {
            object[] data = (object[])obj.CustomData;
            ChangePlayerStates(data[0].ToString(), data[1].ToString());
        }

        if (obj.Code == 21)
        {
            object[] data = (object[])obj.CustomData;

            unitPieces[(int)data[2], (int)data[3]] = Instantiate(prefabs[(int)data[0] - 1], transform).GetComponent<UnitPiece>();

            unitPieces[(int)data[2], (int)data[3]].isMultiplayer = true;
            unitPieces[(int)data[2], (int)data[3]].GetComponent<PhotonView>().ViewID = (int)data[4];
            unitPieces[(int)data[2], (int)data[3]].type = (UnitPieceType)Convert.ToInt32(data[0]);
            unitPieces[(int)data[2], (int)data[3]].team = (int)data[1];
            unitPieces[(int)data[2], (int)data[3]].GetComponent<MeshRenderer>().material = teamMaterials[(int)data[1]];
            unitPieces[(int)data[2], (int)data[3]].CheckPieceName();
        }

        if (obj.Code == 22)
        {
            object[] data = (object[])obj.CustomData;

            playerControl = data[0].ToString();

            if (playerControl == "White")
                blackCamObject.SetActive(false);
            else
                whiteCamObject.SetActive(false);

            if (playerControl != "")
                StartCoroutine(GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y));
        }

        if (obj.Code == 23)
        {
            object[] data = (object[])obj.CustomData;

            StartCoroutine(NextTurn(data[0].ToString()));
        }

        if (obj.Code == 25)
        {
            object[] data = (object[])obj.CustomData;

            doneOtherPlayerTiles = (bool)data[0];
        }

        if (obj.Code == 35)
        {
            object[] data = (object[])obj.CustomData;

            winStatusTMP.text = data[0].ToString() +  " TEAM WINS!";
            winPanelObj.SetActive(true);
        }


        if (obj.Code == 36)
        {
            object[] data = (object[])obj.CustomData;

            //unitPieces[(int)data[1], (int)data[2]] = null;
            CanNowAttack = false;
            StartCoroutine(NextTurn(data[0].ToString()));
        }

        if (obj.Code == 37)
        {
            object[] data = (object[])obj.CustomData;

            winStatusTMP.text = data[0].ToString().ToUpper() + " TEAM WINS!";
            winPanelObj.SetActive(true);
        }

        if (obj.Code == 38)
        {
            object[] data = (object[])obj.CustomData;

            //  DATA SETUP:
            //  enemy team, position X Enemy, position Y Enemy, position X Player, position Y player, next turn 
            if (data[0].ToString() == "white")
            {
                //  I'M THE ENEMY
                deadWhites.Add(unitPieces[(int)data[1], (int)data[2]]);
                //  THE ENEMY IS THE PLAYER
                deadBlacks.Add(unitPieces[(int)data[3], (int)data[4]]);

                unitPieces[(int)data[1], (int)data[2]].SetScale(Vector3.one * deathSize);
                unitPieces[(int)data[1], (int)data[2]].SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds +
                    new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else if (data[0].ToString() == "black")
            {
                deadBlacks.Add(unitPieces[(int)data[1], (int)data[2]]);
                deadWhites.Add(unitPieces[(int)data[3], (int)data[4]]);

                unitPieces[(int)data[1], (int)data[2]].SetScale(Vector3.one * deathSize);
                unitPieces[(int)data[1], (int)data[2]].SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds +
                    new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.forward * deathSpacing) * deadBlacks.Count);
            }

            //  SET YOUR TILES TO NULL BOTH ENEMY AND PLAYER
            unitPieces[(int)data[1], (int)data[2]] = null;
            unitPieces[(int)data[3], (int)data[4]] = null;
        }

        if (obj.Code == 39)
        {
            object[] data = (object[])obj.CustomData;

            //  I KILL MY SELF BECAUSE I'M A BITCH
            //  DATA SETUP
            //  enemy position X, enemy position Y, player previous position X, player previous position Y,
            if (data[0].ToString() == "black")
                deadBlacks.Add(unitPieces[(int)data[1], (int)data[2]]);
            else if (data[0].ToString() == "white")
                deadWhites.Add(unitPieces[(int)data[1], (int)data[2]]);

            unitPieces[(int)data[1], (int)data[2]] = unitPieces[(int)data[3], (int)data[4]];
            unitPieces[(int)data[3], (int)data[4]] = null;
        }
    }

    #endregion

    #region GAMEPLAY

    private void PlayerStatesChangeEvent(object sender, EventArgs e)
    {
        CheckIfDoneInitialize();
        SetFirstTurn();
    }

    #region INITIALIZE

    private void CheckIfDoneInitialize()
    {
        if (playerStates["White"] != "TACTICS" || playerStates["Black"] != "TACTICS")
            return;

        CanNowAttack = false;

        loadingObj.SetActive(false);

        if (loadingStatus != null)
        {
            StopCoroutine(loadingStatus);
            loadingStatus = null;
        }

        StartCoroutine(TacticsTime());

        doneTacticsBtn.SetActive(true);
    }
    private IEnumerator TacticsTime()
    {
        attackerStateObj.SetActive(false);
        attackerState.text = "TACTICS TIME";
        attackerStateObj.SetActive(true);

        while (!CanNowAttack) yield return null;

        attackerState.text = "";
        attackerStateObj.SetActive(false);
    }

    private void SetFirstTurn()
    {
        if (playerStates["White"] != "GAME" || playerStates["Black"] != "GAME")
            return;

        forfeitBtnObj.SetActive(true);

        CurrentGameState = GameState.GAME;

        if (PhotonNetwork.IsMasterClient)
        {
            object[] dataCurrentTurn;
            string nextTurn;
            int randTurn = UnityEngine.Random.Range(0, 2);

            if (randTurn == 0)
                nextTurn = "White";
            else
                nextTurn = "Black";

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
            };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            dataCurrentTurn = new object[]
            {
                nextTurn
            };

            PhotonNetwork.RaiseEvent(23, dataCurrentTurn, raiseEventOptions, sendOptions);
            StartCoroutine(NextTurn(nextTurn));
        }
    }

    IEnumerator TriviaShower()
    {
        loadingStatusTMP.text = "";

        int currentIndex = UnityEngine.Random.Range(0, loadings.Count);

        foreach (char c in loadings[currentIndex])
        {
            loadingStatusTMP.text += c;

            yield return new WaitForSecondsRealtime(typingSpeed);

            yield return null;
        }

        yield return new WaitForSecondsRealtime(delayShow);

        loadingStatus = StartCoroutine(TriviaShower());
    }

    IEnumerator GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        while (playerControl == "")
        {
            Debug.Log("there's no player control yet");
            yield return null;
        }

        unitPieces = new UnitPiece[TILE_COUNT_X, TILE_COUNT_Y];

        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);

                yield return null;
            }

            yield return null;
        }

        object[] data;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others, 
            CachingOption = EventCaching.AddToRoomCache };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        data = new object[]
        {
            true
        };

        PhotonNetwork.RaiseEvent(25, data, raiseEventOptions, sendOptions);

        while (!doneOtherPlayerTiles) yield return null;

        //  AFTER SPAWNING TILES, SPAWN THE PIECES AND
        //  SEND IT ON NETWORK FOR ALL THE PLAYERS TO RECEIVE
        SpawnAllPieces();
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        TileController tileController = tileObject.AddComponent<TileController>();
        tileController.xPos = x;
        tileController.yPos = y;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    //Spawning of the pieces
    private void SpawnAllPieces()
    {
        int whiteTeam = 0, blackTeam = 1;

        if (playerControl == "White")
        {
            //White Team
            unitPieces[1, 2] = SpawnSinglePiece(UnitPieceType.Major, whiteTeam, 1, 2, false);
            unitPieces[2, 2] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 2, 2, false);
            unitPieces[3, 2] = SpawnSinglePiece(UnitPieceType.SLieutenant, whiteTeam, 3, 2, false);
            unitPieces[4, 2] = SpawnSinglePiece(UnitPieceType.Sergeant, whiteTeam, 4, 2, false);
            unitPieces[5, 2] = SpawnSinglePiece(UnitPieceType.FLieutenant, whiteTeam, 5, 2, false);
            unitPieces[6, 2] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 6, 2, false);
            unitPieces[7, 2] = SpawnSinglePiece(UnitPieceType.Captain, whiteTeam, 7, 2, false);
            unitPieces[1, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 1, 1, false);
            unitPieces[2, 1] = SpawnSinglePiece(UnitPieceType.Spy, whiteTeam, 2, 1, false);
            unitPieces[3, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 3, 1, false);
            unitPieces[4, 1] = SpawnSinglePiece(UnitPieceType.LieutenantColonel, whiteTeam, 4, 1, false);
            unitPieces[5, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 5, 1, false);
            unitPieces[6, 1] = SpawnSinglePiece(UnitPieceType.Spy, whiteTeam, 6, 1, false);
            unitPieces[7, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 7, 1, false);
            unitPieces[1, 0] = SpawnSinglePiece(UnitPieceType.BrigadierGeneral, whiteTeam, 1, 0, false);
            unitPieces[2, 0] = SpawnSinglePiece(UnitPieceType.LieutenantGeneral, whiteTeam, 2, 0, false);
            unitPieces[3, 0] = SpawnSinglePiece(UnitPieceType.GeneralOfTheArmy, whiteTeam, 3, 0, false);
            unitPieces[4, 0] = SpawnSinglePiece(UnitPieceType.Flag, whiteTeam, 4, 0, false);
            unitPieces[5, 0] = SpawnSinglePiece(UnitPieceType.General, whiteTeam, 5, 0, false);
            unitPieces[6, 0] = SpawnSinglePiece(UnitPieceType.MajorGeneral, whiteTeam, 6, 0, false);
            unitPieces[7, 0] = SpawnSinglePiece(UnitPieceType.Colonel, whiteTeam, 7, 0, true);
        }


        //Black Team
        else if (playerControl == "Black")
        {
            unitPieces[1, 5] = SpawnSinglePiece(UnitPieceType.BMajor, blackTeam, 1, 5, false);
            unitPieces[2, 5] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 2, 5, false);
            unitPieces[3, 5] = SpawnSinglePiece(UnitPieceType.BSLieutenant, blackTeam, 3, 5, false);
            unitPieces[4, 5] = SpawnSinglePiece(UnitPieceType.BSergeant, blackTeam, 4, 5, false);
            unitPieces[5, 5] = SpawnSinglePiece(UnitPieceType.BFLieutenant, blackTeam, 5, 5, false);
            unitPieces[6, 5] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 6, 5, false);
            unitPieces[7, 5] = SpawnSinglePiece(UnitPieceType.BCaptain, blackTeam, 7, 5, false);
            unitPieces[1, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 1, 6, false);
            unitPieces[2, 6] = SpawnSinglePiece(UnitPieceType.BSpy, blackTeam, 2, 6, false);
            unitPieces[3, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 3, 6, false);
            unitPieces[4, 6] = SpawnSinglePiece(UnitPieceType.BLieutenantColonel, blackTeam, 4, 6, false);
            unitPieces[5, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 5, 6, false);
            unitPieces[6, 6] = SpawnSinglePiece(UnitPieceType.BSpy, blackTeam, 6, 6, false);
            unitPieces[7, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 7, 6, false);
            unitPieces[1, 7] = SpawnSinglePiece(UnitPieceType.BBrigadierGeneral, blackTeam, 1, 7, false);
            unitPieces[2, 7] = SpawnSinglePiece(UnitPieceType.BLieutenantGeneral, blackTeam, 2, 7, false);
            unitPieces[3, 7] = SpawnSinglePiece(UnitPieceType.BGeneralOfTheArmy, blackTeam, 3, 7, false);
            unitPieces[4, 7] = SpawnSinglePiece(UnitPieceType.BFlag, blackTeam, 4, 7, false);
            unitPieces[5, 7] = SpawnSinglePiece(UnitPieceType.BGeneral, blackTeam, 5, 7, false);
            unitPieces[6, 7] = SpawnSinglePiece(UnitPieceType.BMajorGeneral, blackTeam, 6, 7, false);
            unitPieces[7, 7] = SpawnSinglePiece(UnitPieceType.BColonel, blackTeam, 7, 7, true);
        }

        StartCoroutine(PositionAllPieces());
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        unitPieces[x, y].currentX = x;
        unitPieces[x, y].currentY = y;
        unitPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    public void DoneTactics()
    {
        if (!CanNowAttack)
            return;

        doneTacticsBtn.SetActive(false);

        //  DONE INITIALIZING AND READY THE PLAYER UP
        ChangePlayerStates(playerControl, "GAME");

        object[] data;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
        };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        data = new object[]
        {
            playerControl, "GAME"
        };

        PhotonNetwork.RaiseEvent(20, data, raiseEventOptions, sendOptions);
    }

    #region NETWORK

    private void SetPlayerControl()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            object[] data;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
            };
            SendOptions sendOptions = new SendOptions { Reliability = true };

            int rand = UnityEngine.Random.Range(0, 2);

            if (rand == 0)
            {
                playerControl = "White";
                blackCamObject.SetActive(false);

                data = new object[]
                {
                    "Black"
                };
            }
            else
            {
                playerControl = "Black";
                whiteCamObject.SetActive(false);

                data = new object[]
                {
                    "White"
                };
            }

            PhotonNetwork.RaiseEvent(22, data, raiseEventOptions, sendOptions);

            StartCoroutine(GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y));
        }
    }

    IEnumerator PositionAllPieces()
    {
        while (!doneInstantiateLastPiece) yield return null;

        doneInstantiateLastPiece = false;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (unitPieces[x, y] != null && unitPieces[x, y].GetComponent<PhotonView>().IsMine)
                {
                    PositionSinglePiece(x, y, true);
                }

                yield return null;
            }
            yield return null;
        }

        //  DONE INITIALIZING AND READY THE PLAYER UP FOR TACTICS
        ChangePlayerStates(playerControl, "TACTICS");

        object[] data;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
        };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        data = new object[]
        {
            playerControl, "TACTICS"
        };

        PhotonNetwork.RaiseEvent(20, data, raiseEventOptions, sendOptions);
    }

    private UnitPiece SpawnSinglePiece(UnitPieceType type, int team, int x, int y, bool isLastPiece)
    {
        UnitPiece up = Instantiate(prefabs[(int)type - 1], transform).GetComponent<UnitPiece>();

        up.pieceName.pieceNameTMP.text = type.ToString();

        up.isMultiplayer = true;

        PhotonView piecePV = up.gameObject.GetComponent<PhotonView>();
        object[] data;

        if (PhotonNetwork.AllocateViewID(piecePV))
        {
            up.type = type;
            up.team = team;
            up.GetComponent<MeshRenderer>().material = teamMaterials[team];
            up.CheckPieceName();

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
            };
            SendOptions sendOptions = new SendOptions { Reliability = true };

            data = new object[]
            {
                (int)type, team, x, y, piecePV.ViewID
            };

            PhotonNetwork.RaiseEvent(21, data, raiseEventOptions, sendOptions);

            if (isLastPiece)
                doneInstantiateLastPiece = true;

            return up;
        }

        return null;
    }

    #endregion

    #endregion

    #region BATTLE

    private IEnumerator NextTurn(string currentTurn)
    {
        CanNowAttack = false;
        attackerStateObj.SetActive(false);
        CurrentTurn = currentTurn;
        attackerState.text = currentTurn.ToUpper() + "'S TURN";
        attackerStateObj.SetActive(true);

        while (!CanNowAttack) yield return null;

        attackerState.text = "";
        attackerStateObj.SetActive(false);
    }

    private void MoveCharacter()
    {
        ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        if (CurrentGameState == GameState.READY && playerStates.ContainsKey(playerControl))
        {
            if (playerStates[playerControl] != "TACTICS")
                return;

            if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
            {
                //Get the indexes of the tile i've hit
                Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

                //If we're hovering a tile after not hovering any tiles
                if (currentHover == -Vector2Int.one)
                {
                    currentHover = hitPosition;
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }
                //If we were already hovering a tile, change the previous one
                if (currentHover != hitPosition)
                {
                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                    currentHover = hitPosition;
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }
                //If we press down on the mouse
                if (Input.GetMouseButtonDown(0))
                {
                    if (unitPieces[hitPosition.x, hitPosition.y] != null)
                    {
                        if (playerControl == "White" && unitPieces[hitPosition.x, hitPosition.y].team != 0)
                            return;

                        else if (playerControl == "Black" && unitPieces[hitPosition.x, hitPosition.y].team != 1)
                            return;

                        currentlyDragging = unitPieces[hitPosition.x, hitPosition.y];

                        //Get a list of where I can go, highlight tiles as well
                        availableMoves = currentlyDragging.GetAvailableMoves(ref unitPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        HighlightTiles();
                    }
                }

                //If we are releasing the mouse button
                if (currentlyDragging != null && Input.GetMouseButtonUp(0))
                {
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                    if (playerControl == "White" && info.transform.GetComponent<TileController>().yPos >= 3)
                    {
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                        return;
                    }

                    else if (playerControl == "Black" && info.transform.GetComponent<TileController>().yPos <= 4)
                    {
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                        return;
                    }

                    if (ContainsValidMove(ref availableMoves, new Vector2(hitPosition.x, hitPosition.y)))
                    {
                        MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);
                    }
                    else
                    {
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                    }
                }
            }
            else
            {
                if (currentlyDragging && Input.GetMouseButtonUp(0))
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }

            //If we're dragging a piece
            if (currentlyDragging)
            {
                Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                float distance = 0.0f;
                if (horizontalPlane.Raycast(ray, out distance))
                    currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
        else if (CurrentGameState == GameState.GAME)
        {
            if (CurrentTurn != playerControl || !CanNowAttack)
            {
                RemoveHighlightTiles();
                return;
            }

            if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
            {

                //Get the indexes of the tile i've hit
                Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

                //If we're hovering a tile after not hovering any tiles
                if (currentHover == -Vector2Int.one)
                {
                    currentHover = hitPosition;
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }

                //If we were already hovering a tile, change the previous one
                if (currentHover != hitPosition)
                {
                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                    currentHover = hitPosition;
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }
                //If we press down on the mouse
                if (Input.GetMouseButtonDown(0))
                {
                    if (unitPieces[hitPosition.x, hitPosition.y] != null)
                    {
                        if (playerControl == "White" && unitPieces[hitPosition.x, hitPosition.y].team != 0)
                            return;

                        else if (playerControl == "Black" && unitPieces[hitPosition.x, hitPosition.y].team != 1)
                            return;

                        currentlyDragging = unitPieces[hitPosition.x, hitPosition.y];

                        //Get a list of where I can go, highlight tiles as well
                        availableMoves = currentlyDragging.GetAvailableMoves(ref unitPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        HighlightTiles();
                    }
                }

                //If we are releasing the mouse button
                if (currentlyDragging != null && Input.GetMouseButtonUp(0))
                {
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                    if (ContainsValidMove(ref availableMoves, new Vector2(hitPosition.x, hitPosition.y)))
                    {
                        MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);
                    }
                    else
                    {
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                    }
                }

            }
            else
            {
                if (currentHover != -Vector2Int.one)
                {
                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                    currentHover = -Vector2Int.one;
                }

                if (currentlyDragging && Input.GetMouseButtonUp(0))
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }

            //If we're dragging a piece
            if (currentlyDragging)
            {
                Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                float distance = 0.0f;
                if (horizontalPlane.Raycast(ray, out distance))
                    currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
        else
        {
            RemoveHighlightTiles();
        }
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one; //Invalid
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }

    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }

    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();
    }

    private void MoveTo(int originalX, int originalY, int x, int y)
    {
        UnitPiece player = unitPieces[originalX, originalY];
        Vector2Int previousPosition = new Vector2Int(originalX, originalY);

        string nextTurn;
        string died = "";

        if (unitPieces[x, y] != null)
        {
            UnitPiece opponent = unitPieces[x, y];

            if (player.team == opponent.team)
                return;

            //  OPPONENT IS WHITE
            if (opponent.team == 0)
            {
                //  FOR FLAG
                if (opponent.type == UnitPieceType.Flag)
                {
                    win = "black";
                    winStatusTMP.text = win.ToString().ToUpper() + " TEAM WINS!";

                    //  SEND MULTIPLAYER EVENT FOR WIN
                    object[] data = new object[]
                    {
                        win
                    };

                    PhotonNetwork.RaiseEvent(37, data, EventOptions, SendOptions);

                    winPanelObj.SetActive(true);

                    return;
                }

                //  KILL EACH OTHER
                else if (player.attackType == opponent.attackType)
                {
                    deadWhites.Add(opponent);
                    deadBlacks.Add(player);

                    //  SET YOUR PLAYER SCALE AND POSITION
                    player.SetScale(Vector3.one * deathSize);
                    player.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds +
                        new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.forward * deathSpacing) * deadWhites.Count);

                    //  SEND DATA TO OTHER PLAYER TO TELL
                    //  THAT THEIR PIECE IS DEAD
                    //  DATA SETUP:
                    //  enemy team, position X Enemy, position Y Enemy, position X Player, position Y player, next turn 
                    object[] data = new object[]
                    {
                        "white", x, y, previousPosition.x, previousPosition.y
                    };

                    PhotonNetwork.RaiseEvent(38, data, EventOptions, SendOptions);

                    //  SET YOUR TILES TO NULL BOTH ENEMY AND PLAYER
                    unitPieces[x, y] = null;
                    unitPieces[previousPosition.x, previousPosition.y] = null;

                    if (currentlyDragging)
                        currentlyDragging = null;
                    RemoveHighlightTiles();

                    object[] attackNext = new object[]
                    {
                         "White"
                    };

                    PhotonNetwork.RaiseEvent(23, attackNext, EventOptions, SendOptions);

                    //  START NEXT TURN
                    StartCoroutine(NextTurn("White"));

                    return;
                }

                //  KILL MY SELF FOR ATTACK ENEMY WHOSE HIGHER THAN ME
                else if (!player.canAttackPieces.Contains(opponent.type))
                {
                    //  KILL MY SELF FOR BEING A BITCH
                    deadBlacks.Add(player);

                    //  SET YOUR PLAYER SCALE AND POSITION
                    player.SetScale(Vector3.one * deathSize);
                    player.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds +
                        new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.forward * deathSpacing) * deadBlacks.Count);

                    //  SET MY TILES TO NULL
                    unitPieces[previousPosition.x, previousPosition.y] = null;

                    //  SEND DATA TO OHTER PLAYER TO TELL THAT
                    //  I KILL MY SELF BECAUSE I'M A BITCH
                    //  DATA SETUP
                    //  player team, player position X, player position Y, next turn
                    object[] data = new object[]
                    {
                        "black", previousPosition.x, previousPosition.y
                    };

                    PhotonNetwork.RaiseEvent(39, data, EventOptions, SendOptions);

                    if (currentlyDragging)
                        currentlyDragging = null;
                    RemoveHighlightTiles();

                    object[] attackNext = new object[]
                    {
                         "White"
                    };

                    PhotonNetwork.RaiseEvent(23, attackNext, EventOptions, SendOptions);

                    //  START NEXT TURN
                    StartCoroutine(NextTurn("White"));

                    return;
                }

                //  KILL ENEMY BECAUSE I'M STRONG
                else
                {
                    deadWhites.Add(opponent);
                    died = "white";
                }
            }
            else
            {
                //  FOR FLAG
                if (opponent.type == UnitPieceType.Flag)
                {
                    win = "white";
                    winStatusTMP.text = win.ToString().ToUpper() + " TEAM WINS!";

                    //  SEND MULTIPLAYER EVENT FOR WIN
                    object[] data = new object[]
                    {
                        win
                    };

                    PhotonNetwork.RaiseEvent(37, data, EventOptions, SendOptions);

                    winPanelObj.SetActive(true);

                    return;
                }

                //  KILL EACH OTHER
                else if (player.attackType == opponent.attackType)
                {
                    deadBlacks.Add(opponent);
                    deadWhites.Add(player);

                    //  SET YOUR PLAYER SCALE AND POSITION
                    player.SetScale(Vector3.one * deathSize);
                    player.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds +
                        new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.forward * deathSpacing) * deadBlacks.Count);

                    //  SEND DATA TO OTHER PLAYER TO TELL
                    //  THAT THEIR PIECE IS DEAD
                    //  DATA SETUP:
                    //  enemy team, position X Enemy, position Y Enemy, position X Player, position Y player, next turn 
                    object[] data = new object[]
                    {
                        "black", x, y, previousPosition.x, previousPosition.y
                    };

                    PhotonNetwork.RaiseEvent(38, data, EventOptions, SendOptions);

                    //  SET YOUR TILES TO NULL BOTH ENEMY AND PLAYER
                    unitPieces[x, y] = null;
                    unitPieces[previousPosition.x, previousPosition.y] = null;

                    if (currentlyDragging)
                        currentlyDragging = null;
                    RemoveHighlightTiles();

                    object[] attackNext = new object[]
                    {
                         "Black"
                    };

                    PhotonNetwork.RaiseEvent(23, attackNext, EventOptions, SendOptions);

                    //  START NEXT TURN
                    StartCoroutine(NextTurn("Black"));

                    return;
                }

                //  KILL MY SELF FOR ATTACK ENEMY WHOSE HIGHER THAN ME
                else if (!player.canAttackPieces.Contains(opponent.type))
                {
                    //  KILL MY SELF FOR BEING A BITCH
                    deadWhites.Add(player);

                    //  SET YOUR PLAYER SCALE AND POSITION
                    player.SetScale(Vector3.one * deathSize);
                    player.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds +
                        new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.forward * deathSpacing) * deadWhites.Count);

                    //  SET MY TILES TO NULL
                    unitPieces[previousPosition.x, previousPosition.y] = null;

                    //  SEND DATA TO OHTER PLAYER TO TELL THAT
                    //  I KILL MY SELF BECAUSE I'M A BITCH
                    //  DATA SETUP
                    //  player team, player position X, player position Y, next turn
                    object[] data = new object[]
                    {
                        "white", previousPosition.x, previousPosition.y
                    };

                    PhotonNetwork.RaiseEvent(39, data, EventOptions, SendOptions);

                    if (currentlyDragging)
                        currentlyDragging = null;
                    RemoveHighlightTiles();

                    object[] attackNext = new object[]
                    {
                         "Black"
                    };

                    PhotonNetwork.RaiseEvent(23, attackNext, EventOptions, SendOptions);

                    //  START NEXT TURN
                    StartCoroutine(NextTurn("Black"));

                    return;
                }

                //  KILL ENEMY BECAUSE I'M STRONG
                else
                {
                    deadBlacks.Add(opponent);
                    died = "black";
                }
            }
        }

        nextTurn = CurrentTurn == "White" ? "Black" : "White";

        if (died == "")
            died = "";

        object[] moveData = new object[]
        {
            died, x, y, previousPosition.x, previousPosition.y
        };

        PhotonNetwork.RaiseEvent(39, moveData, EventOptions, SendOptions);

        unitPieces[x, y] = player;
        unitPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        if (currentlyDragging)
            currentlyDragging = null;
        RemoveHighlightTiles();

        object[] nextTurnData = new object[]
        {
            nextTurn
        };

        PhotonNetwork.RaiseEvent(23, nextTurnData, EventOptions, SendOptions);

        //  START NEXT TURN
        StartCoroutine(NextTurn(nextTurn));
    }

    #endregion

    #endregion

    public void ForfeitMultiplayer()
    {
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
        };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        object[] data;

        if (playerControl == "White")
        {
            winStatusTMP.text = "BLACK TEAM WINS!";
            winPanelObj.SetActive(true);

            data = new object[]
            {
                "BLACK"
            };
        }
        else if (playerControl == "Black")
        {
            winStatusTMP.text = "WHITE TEAM WINS!";
            winPanelObj.SetActive(true);

            data = new object[]
            {
                "WHITE"
            };
        }
        else
        {
            data = new object[]
            {
                ""
            };
        }

        PhotonNetwork.RaiseEvent(35, data, raiseEventOptions, sendOptions);
    }
}
