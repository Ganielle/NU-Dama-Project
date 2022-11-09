using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DamaMultiplayerController : MonoBehaviour, ITilesGenerator
{
    [SerializeField] private CameraMover cameraMover;
    [SerializeField] private MoveChecker moveChecker;

    public Button returnToMainMenu;
    public Button ExitGame;

    [Header("TILES GENERATOR")]
    public GameObject Tile;
    public Material WhiteMaterial;
    public Material BlackMaterial;
    public int BoardSize { get; private set; } = 8;

    [Header("BOARDER GENERATOR")]
    public GameObject Border;
    public GameObject Corner;
    public int boardSize;
    public GameObject borderGameObject;
    public Vector3 currentPosition;
    public Quaternion currentRotation;
    public Vector3 currentDirection;

    [Header("SPAWN PIECES")]
    public GameObject Pawn;
    public TileGetter tileGetter;
    public Material PieceWhiteMaterial;
    public Material PieceBlackMaterial;
    public int PawnRows { get; private set; } = 3;

    [Header("TURN CONTROLLER")]
    public PawnColor StartingPawnColor;
    public TurnTextChanger TurnTextChanger;
    public GameOverPanel GameOverPanel;
    public PawnColor turn;
    public int whitePawnCount;
    public int blackPawnCount;

    [field: Header("MULTIPLAYER")]
    [field: SerializeField] public string PlayerOneStatus { get; set; }
    [field: SerializeField] public string PlayerTwoStatus { get; set; }
    [field: SerializeField] public PawnColor CurrentTeam { get; set; }
    [field: SerializeField] public string team { get; set; }

    public List<GameObject> whitePieces;
    public List<GameObject> blackPieces;
    public List<GameObject> tiles;

    //  ====================================

    RaiseEventOptions EventOptions = new RaiseEventOptions
    {
        Receivers = ReceiverGroup.Others,
        CachingOption = EventCaching.AddToRoomCache
    };
    SendOptions SendOptions = new SendOptions { Reliability = true };

    //  ====================================

    private void Awake()
    {
        #region NETWORK MULTIPLAYER

        PhotonNetwork.NetworkingClient.EventReceived += MultiplayerEvents;

        object[] data;

        if (PhotonNetwork.IsMasterClient)
            data = new object[] { "LOADING TILES", "1" };
        else
            data = new object[] { "LOADING TILES", "2" };

        ChangePlayerStatus(data);

        #endregion

        if (PlayerPrefs.HasKey("BoardSize"))
            BoardSize = PlayerPrefs.GetInt("BoardSize");

        StartCoroutine(CreateTileColumns());
    }

    public void ForfeitMultiplayer()
    {
        EndGame(CurrentTeam);
        object[] data;
        if (CurrentTeam == PawnColor.White)
        {
            data = new object[]
            {
                (int) PawnColor.Black
            };
        }
        else
        {
            data = new object[]
            {
                (int) PawnColor.White
            };
        }

        PhotonNetwork.RaiseEvent(34, data, EventOptions, SendOptions);
    }

    public void CloseMatchButton()
    {
        ExitGame.interactable = false;
        returnToMainMenu.interactable = false;
        StartCoroutine(CancelFindMatch(() => 
        {
            SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        }, false));
    }

    private IEnumerator CancelFindMatch(Action action, bool reconnect)
    {
        PhotonNetwork.Disconnect();

        MultiplayerController.instance.ClientConnectedToServer = false;

        while (PhotonNetwork.IsConnected)
            yield return null;

        action?.Invoke();

        if (!reconnect) yield break;

        PhotonNetwork.ConnectUsingSettings();
        MultiplayerController.instance.ConnectingToServer = true;
        MultiplayerController.instance.ClientConnectedToServer = false;
    }

    private void ChangePlayerStatus(object[] status)
    {
        object[] data;

        if (status[1].ToString() == "1")
        {
            PlayerOneStatus = status[0].ToString();
            data = status;
        }
        else
        {
            PlayerTwoStatus = status[0].ToString();
            data = status;
        }

        PhotonNetwork.RaiseEvent(26, data, EventOptions, SendOptions);
    }

    #region PHOTON EVENTS

    private void MultiplayerEvents(EventData obj)
    {
        if (obj.Code == 26)
        {
            object[] data = (object[])obj.CustomData;

            if (data[1].ToString() == "1")
            {
                PlayerOneStatus = data[0].ToString();
            }
            else
            {
                PlayerTwoStatus = data[0].ToString();
            }
        }

        if (obj.Code == 27)
        {
            object[] data = (object[])obj.CustomData;

            CurrentTeam = (PawnColor)data[0];
            team = data[2].ToString();
            cameraMover.SetRotationCameraToShowPlayer((float)data[1]);
        }

        if (obj.Code == 28)
        {
            object[] data = (object[])obj.CustomData;
            Transform tileTransform = tileGetter.GetTile((int)data[2], (int)data[3]).transform;
            GameObject instantiatedPawn = Instantiate(Pawn, tileTransform.position, Pawn.transform.rotation, tileTransform);
            instantiatedPawn.GetComponent<PhotonView>().ViewID = (int)data[0];
            instantiatedPawn.name = data[4].ToString();
            instantiatedPawn.GetComponent<Renderer>().material =
                (PawnColor)Convert.ToInt32(data[1]) == PawnColor.White ? PieceWhiteMaterial : PieceBlackMaterial;
            instantiatedPawn.GetComponent<IPawnProperties>().PawnColor = (PawnColor)Convert.ToInt32(data[1]);

            if ((PawnColor)Convert.ToInt32(data[1]) == PawnColor.White)
                whitePieces.Add(instantiatedPawn);
            else
                blackPieces.Add(instantiatedPawn);
        }

        if (obj.Code == 29)
        {
            object[] data = (object[])obj.CustomData;

            turn = (PawnColor)data[0];

            if (data.Length > 1)
            {
                whitePawnCount = (int)data[1];
                blackPawnCount = (int)data[2];
            }

            TurnTextChanger.ChangeTurnText(turn);
        }

        if (obj.Code == 31)
        {
            object[] data = (object[])obj.CustomData;

            whitePawnCount = (int)data[0];
            blackPawnCount = (int)data[1];

            GameObject white = whitePieces.Find(x => x.name == data[2].ToString());
            GameObject black = blackPieces.Find(x => x.name == data[2].ToString());

            if (white != null)
                whitePieces.Remove(white);
            else if (black != null)
                blackPieces.Remove(black);

            CheckVictory();
        }

        if (obj.Code == 34)
        {
            object[] data = (object[])obj.CustomData;
            EndGame((PawnColor)Convert.ToInt32(data[0]));
        }
    }


    #endregion

    #region TILE GENERATOR

    IEnumerator CreateTileColumns()
    {
        for (var i = 0; i < BoardSize; ++i)
        {
            CreateTileColumn(i);

            yield return null;
        }

        StartCoroutine(CreateTiles());
    }

    private void CreateTileColumn(int columnIndex)
    {
        GameObject tileColumn = new GameObject("TileColumn" + columnIndex);
        tileColumn.transform.parent = this.gameObject.transform;
        tileColumn.transform.position = tileColumn.transform.parent.position + Vector3.right * columnIndex;
    }

    IEnumerator CreateTiles()
    {
        int count = 1;
        for (var columnIndex = 0; columnIndex < BoardSize; ++columnIndex)
        {
            for (var rowIndex = 0; rowIndex < BoardSize; ++rowIndex)
            {
                CreateTile(columnIndex, rowIndex, count);
                count++;
                yield return null;
            }

            yield return null;
        }

        object[] data;

        if (PhotonNetwork.IsMasterClient)
            data = new object[] { "LOADING BORDER", "1" };
        else
            data = new object[] { "LOADING BORDER", "2" };

        ChangePlayerStatus(data);

        while (PlayerOneStatus == "LOADING TILES" || PlayerTwoStatus == "LOADING TILES") yield return null;

        ITilesGenerator tilesGenerator = GetComponent<ITilesGenerator>();
        boardSize = tilesGenerator.BoardSize;
        CreateBorderGameObject();
    }

    private void CreateTile(int columnIndex, int rowIndex, int count)
    {
        var columnTransform = transform.GetChild(columnIndex);
        GameObject instantiatedTile = Instantiate(Tile,
            columnTransform.position + Vector3.forward * rowIndex, Tile.transform.rotation,
            columnTransform);
        instantiatedTile.GetComponent<TileClickDetector>().isMultiplayer = true;
        instantiatedTile.name = "Tile" + count;
        tiles.Add(instantiatedTile);
        instantiatedTile.GetComponent<Renderer>().material =
            (columnIndex + rowIndex) % 2 != 0 ? WhiteMaterial : BlackMaterial;
    }

    #endregion

    #region BORDER GENERATOR

    private void CreateBorderGameObject()
    {
        borderGameObject = new GameObject("Border");
        borderGameObject.transform.parent = this.gameObject.transform;
        borderGameObject.transform.position = (Vector3.left + Vector3.back);

        AssignInitialValues();
    }

    private void AssignInitialValues()
    {
        currentPosition = borderGameObject.transform.position;
        currentRotation = borderGameObject.transform.rotation;
        currentDirection = Vector3.forward;

        StartCoroutine(CreateBorder());
    }

    IEnumerator CreateBorder()
    {
        for (var side = 0; side < 4; ++side)
        {
            CreaterBorderLine();

            yield return null;
        }
        object[] data;

        if (PhotonNetwork.IsMasterClient)
            data = new object[] { "LOADING SPAWN PIECES", "1" };
        else
            data = new object[] { "LOADING SPAWN PIECES", "2" };

        ChangePlayerStatus(data);

        while (PlayerOneStatus == "LOADING BORDER" || PlayerTwoStatus == "LOADING BORDER") yield return null;

        SetPlayersTeam();
    }

    private void CreaterBorderLine()
    {
        CreateCornerElement();
        for (var i = 0; i < boardSize; ++i)
            CreateBorderElement();
        RotateBy90Degrees();
    }

    private void CreateCornerElement()
    {
        CreateElement(Corner);
    }

    private void CreateElement(GameObject objectToCreate)
    {
        GameObject instantiatedCorner = Instantiate(objectToCreate, currentPosition,
            objectToCreate.transform.rotation * currentRotation, borderGameObject.transform);
        IncrementCurrentPosition();
    }

    private void IncrementCurrentPosition()
    {
        currentPosition += currentDirection;
    }

    private void CreateBorderElement()
    {
        CreateElement(Border);
    }

    private void RotateBy90Degrees()
    {
        Quaternion rotationBy90Degrees = Quaternion.Euler(0, 90, 0);
        currentDirection = rotationBy90Degrees * currentDirection;
        currentRotation *= rotationBy90Degrees;
    }

    #endregion

    #region SPAWN PIECES

    private void SetPlayersTeam()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            object[] data;
            int rand = UnityEngine.Random.Range(0, 2);

            if (rand == 0)
            {
                CurrentTeam = PawnColor.White;
                team = "White";
                data = new object[]
                {
                    (int) PawnColor.Black, -180f, "Black"
                };
                cameraMover.SetRotationCameraToShowPlayer(0f);
            }
            else
            {
                CurrentTeam = PawnColor.Black;
                team = "Black";
                data = new object[]
                {
                    (int) PawnColor.White, 0f, "White"
                };
                cameraMover.SetRotationCameraToShowPlayer(-180f);
            }

            PhotonNetwork.RaiseEvent(27, data, EventOptions, SendOptions);
        }

        StartCoroutine(SpawnPieces());
    }

    IEnumerator SpawnPieces()
    {
        while (team == "" || team == null) yield return null;

        if (PlayerPrefs.HasKey("PawnRows"))
            PawnRows = PlayerPrefs.GetInt("PawnRows");

        if (team == "White")
        {
            StartCoroutine(GenerateWhitePawns());
        }
        else
        {
            StartCoroutine(GenerateBlackPawns());
        }

        while (PlayerOneStatus == "LOADING SPAWN PIECES" || PlayerTwoStatus == "LOADING SPAWN PIECES") yield return null;

        if (PhotonNetwork.IsMasterClient)
        {
            //  START GAME HERE
            SetUpTurn();
        }

        moveChecker.InitializeMultiplayer();
    }

    IEnumerator GenerateWhitePawns()
    {
        int count = 1;
        for (var rowIndex = 0; rowIndex < boardSize && rowIndex < PawnRows; ++rowIndex)
        {
            for (var columnIndex = 0; columnIndex < boardSize; ++columnIndex)
            {
                if ((columnIndex + rowIndex) % 2 == 0)
                {
                    GeneratePawn(columnIndex, rowIndex, PawnColor.White, "White " + count);
                    count++;
                }

                yield return null;
            }
            yield return null;
        }

        object[] data;

        if (PhotonNetwork.IsMasterClient)
            data = new object[] { "GAME", "1" };
        else
            data = new object[] { "GAME", "2" };

        ChangePlayerStatus(data);
    }

    private void GeneratePawn(int columnIndex, int rowIndex, PawnColor pawnColor, string nameObj)
    {
        Transform tileTransform = tileGetter.GetTile(columnIndex, rowIndex).transform;
        GameObject instantiatedPawn = Instantiate(Pawn, tileTransform.position, Pawn.transform.rotation, tileTransform);
        instantiatedPawn.name = nameObj;
        instantiatedPawn.GetComponent<Renderer>().material =
            pawnColor == PawnColor.White ? PieceWhiteMaterial : PieceBlackMaterial;
        instantiatedPawn.GetComponent<IPawnProperties>().PawnColor = pawnColor;

        if (pawnColor == PawnColor.White)
            whitePieces.Add(instantiatedPawn);
        else
            blackPieces.Add(instantiatedPawn);

        #region NETWORK

        PhotonView piecePV = instantiatedPawn.gameObject.GetComponent<PhotonView>();

        if (PhotonNetwork.AllocateViewID(piecePV))
        {
            object[] data;

            data = new object[]
            {
                piecePV.ViewID, (int)pawnColor, columnIndex, rowIndex, nameObj
            };

            PhotonNetwork.RaiseEvent(28, data, EventOptions, SendOptions);
        }

        #endregion
    }

    IEnumerator GenerateBlackPawns()
    {
        int count = 1;
        for (var rowIndex = boardSize - 1; rowIndex >= 0 && rowIndex >= boardSize - PawnRows; --rowIndex)
        {
            for (var columnIndex = boardSize - 1; columnIndex >= 0; --columnIndex)
            {
                if ((rowIndex + columnIndex) % 2 == 0)
                {
                    GeneratePawn(columnIndex, rowIndex, PawnColor.Black, "Black " + count);
                    count++;
                }

                yield return null;
            }

            yield return null;
        }

        object[] data;

        if (PhotonNetwork.IsMasterClient)
            data = new object[] { "GAME", "1" };
        else
            data = new object[] { "GAME", "2" };

        ChangePlayerStatus(data);
    }

    #endregion

    #region TURN CONTROLLER

    private void SetUpTurn()
    {
        turn = StartingPawnColor;
        whitePawnCount = blackPawnCount = Mathf.CeilToInt(BoardSize * PawnRows / 2f);

        object[] data = new object[] 
        {
            (int)turn, whitePawnCount, blackPawnCount
        };

        PhotonNetwork.RaiseEvent(29, data, EventOptions, SendOptions);

        TurnTextChanger.ChangeTurnText(turn);
    }


    public void NextTurn()
    {
        turn = turn == PawnColor.White ? PawnColor.Black : PawnColor.White;

        object[] data = new object[]
        {
            (int)turn
        };

        PhotonNetwork.RaiseEvent(29, data, EventOptions, SendOptions);

        TurnTextChanger.ChangeTurnText(turn);
    }

    public PawnColor GetTurn()
    {
        return turn;
    }

    public void DecrementPawnCount(GameObject pawn)
    {
        var pawnColor = pawn.GetComponent<IPawnProperties>().PawnColor;
        if (pawnColor == PawnColor.White)
            --whitePawnCount;
        else
            --blackPawnCount;

        object[] data = new object[]
        {
            whitePawnCount, blackPawnCount, pawn.name
        };


        PhotonNetwork.RaiseEvent(31, data, EventOptions, SendOptions);

        if (whitePieces.Contains(pawn))
            whitePieces.Remove(pawn);
        else if (blackPieces.Contains(pawn))
            blackPieces.Remove(pawn);

        CheckVictory();
    }

    private void CheckVictory()
    {
        if (whitePawnCount == 0)
            EndGame(PawnColor.Black);
        else if (blackPawnCount == 0)
            EndGame(PawnColor.White);
    }

    private void EndGame(PawnColor winnerPawnColor)
    {
        GameOverPanel.gameObject.SetActive(true);
        GameOverPanel.SetWinnerText(winnerPawnColor);
    }

    public void Forfeit()
    {
        if (turn == PawnColor.White)
            EndGame(PawnColor.Black);
        else if (turn == PawnColor.Black)
            EndGame(PawnColor.White);
    }

    #endregion
}
