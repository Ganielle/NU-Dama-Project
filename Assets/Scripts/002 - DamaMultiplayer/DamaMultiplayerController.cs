using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamaMultiplayerController : MonoBehaviour, ITilesGenerator
{
    [Header("TILES GENERATOR")]
    public GameObject Tile;
    public Material WhiteMaterial;
    public Material BlackMaterial;
    public int BoardSize { get; private set; } = 8;

    [Header("BOARDER GENERATOR")] 
    public GameObject Border;
    public GameObject Corner;
    private int boardSize;
    private GameObject borderGameObject;
    private Vector3 currentPosition;
    private Quaternion currentRotation;
    private Vector3 currentDirection;

    [Header("SPAWN PIECES")]
    public GameObject Pawn;
    public TileGetter tileGetter;
    public Material PieceWhiteMaterial;
    public Material PieceBlackMaterial;
    public int PawnRows { get; private set; } = 3;

    [field: Header("MULTIPLAYER")]
    [field: SerializeField] public string PlayerOneStatus { get; set; }
    [field: SerializeField] public string PlayerTwoStatus { get; set; }
    [field: SerializeField] public string CurrentTeam { get; set; }

    //  ====================================

    RaiseEventOptions EventOptions;
    SendOptions SendOptions;

    //  ====================================

    private void Awake()
    {
        #region NETWORK MULTIPLAYER

        PhotonNetwork.NetworkingClient.EventReceived += MultiplayerEvents;

        EventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others,
            CachingOption = EventCaching.AddToRoomCache
        };
        SendOptions = new SendOptions { Reliability = true };

        object[] data = new object[] { "LOADING TILES" };
        ChangePlayerStatus(data);


        #endregion

        StartCoroutine(CreateTileColumns());
    }

    private void ChangePlayerStatus(object[] status)
    {
        object[] data;

        if (PhotonNetwork.IsMasterClient)
        {
            PlayerOneStatus = "LOADING TILES";
            data = status;
        }
        else
        {
            PlayerTwoStatus = "LOADING TILES";
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

            if (PhotonNetwork.IsMasterClient)
                PlayerOneStatus = data[0].ToString();
            else
                PlayerTwoStatus = data[0].ToString();
        }

        if (obj.Code == 27)
        {
            object[] data = (object[])obj.CustomData;

            CurrentTeam = data[0].ToString();
        }

        if (obj.Code == 28)
        {
            object[] data = (object[])obj.CustomData;

            Transform tileTransform = tileGetter.GetTile((int)data[2], (int)data[3]).transform;
            GameObject instantiatedPawn = Instantiate(Pawn, tileTransform.position, Pawn.transform.rotation, tileTransform);
            instantiatedPawn.GetComponent<PhotonView>().ViewID = (int)data[0];
            instantiatedPawn.GetComponent<Renderer>().material =
                (PawnColor)Convert.ToInt32(data[1]) == PawnColor.White ? PieceWhiteMaterial : PieceBlackMaterial;
            instantiatedPawn.GetComponent<IPawnProperties>().PawnColor = (PawnColor)Convert.ToInt32(data[1]);
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
        for (var columnIndex = 0; columnIndex < BoardSize; ++columnIndex)
        {
            for (var rowIndex = 0; rowIndex < BoardSize; ++rowIndex)
            {
                CreateTile(columnIndex, rowIndex);
                yield return null;
            }

            yield return null;
        }

        object[] data = new object[] { "LOADING BORDER" };
        ChangePlayerStatus(data);

        while (PlayerOneStatus == "LOADING TILES" || PlayerTwoStatus == "LOADING TILES") yield return null;

        CreateBorderGameObject();
    }

    private void CreateTile(int columnIndex, int rowIndex)
    {
        var columnTransform = transform.GetChild(columnIndex);
        GameObject instantiatedTile = Instantiate(Tile,
            columnTransform.position + Vector3.forward * rowIndex, Tile.transform.rotation,
            columnTransform);
        instantiatedTile.name = "Tile" + rowIndex;
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

        object[] data = new object[] { "LOADING SPAWN PIECES" };
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
                CurrentTeam = "White";
                data = new object[]
                {
                    "White"
                };
            }
            else
            {
                CurrentTeam = "Black";
                data = new object[]
                {
                    "Black"
                };
            }

            PhotonNetwork.RaiseEvent(27, data, EventOptions, SendOptions);
        }

        StartCoroutine(SpawnPieces());
    }

    IEnumerator SpawnPieces()
    {
        while (CurrentTeam == "") yield return null;

        if (CurrentTeam == "White")
            StartCoroutine(GenerateWhitePawns());
        else
            StartCoroutine(GenerateBlackPawns());

        if (PhotonNetwork.IsMasterClient)
        {
            while (PlayerOneStatus == "LOADING SPAWN PIECES" || PlayerTwoStatus == "LOADING SPAWN PIECES") yield return null;

            //  START GAME HERE
        }
    }

    IEnumerator GenerateWhitePawns()
    {
        for (var rowIndex = 0; rowIndex < boardSize && rowIndex < PawnRows; ++rowIndex)
        {
            for (var columnIndex = 0; columnIndex < boardSize; ++columnIndex)
            {
                if ((columnIndex + rowIndex) % 2 == 0)
                    GeneratePawn(columnIndex, rowIndex, PawnColor.White);

                yield return null;
            }
            yield return null;
        }

        object[] data = new object[] { "GAME" };
        ChangePlayerStatus(data);
    }

    private void GeneratePawn(int columnIndex, int rowIndex, PawnColor pawnColor)
    {
        Transform tileTransform = tileGetter.GetTile(columnIndex, rowIndex).transform;
        GameObject instantiatedPawn = Instantiate(Pawn, tileTransform.position, Pawn.transform.rotation, tileTransform);
        instantiatedPawn.GetComponent<Renderer>().material =
            pawnColor == PawnColor.White ? WhiteMaterial : BlackMaterial;
        instantiatedPawn.GetComponent<IPawnProperties>().PawnColor = pawnColor;

        #region NETWORK

        PhotonView piecePV = instantiatedPawn.gameObject.GetComponent<PhotonView>();

        if (PhotonNetwork.AllocateViewID(piecePV))
        {
            object[] data;

            data = new object[]
            {
                piecePV.ViewID, (int)pawnColor, columnIndex, rowIndex
            };

            PhotonNetwork.RaiseEvent(28, data, EventOptions, SendOptions);
        }

        #endregion
    }

    IEnumerator GenerateBlackPawns()
    {
        for (var rowIndex = boardSize - 1; rowIndex >= 0 && rowIndex >= boardSize - PawnRows; --rowIndex)
        {
            for (var columnIndex = boardSize - 1; columnIndex >= 0; --columnIndex)
            {
                if ((rowIndex + columnIndex) % 2 == 0)
                    GeneratePawn(columnIndex, rowIndex, PawnColor.Black);

                yield return null;
            }

            yield return null;
        }

        object[] data = new object[] { "GAME" };
        ChangePlayerStatus(data);
    }

    #endregion
}
