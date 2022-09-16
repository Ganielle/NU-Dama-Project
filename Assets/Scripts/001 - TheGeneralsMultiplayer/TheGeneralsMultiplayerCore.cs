using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    //  ========================================

    [Header("CAMERA")]
    [SerializeField] private GameObject whiteCamObject;
    [SerializeField] private GameObject blackCamObject;

    [Header("TILES")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float yOffset = 0.2f;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //  ========================================

    private GameState currentGameState;

    private string playerControl;
    private Dictionary<string, string> playerStates = new Dictionary<string, string>();

    private const int TILE_COUNT_X = 9;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private UnitPiece[,] unitPieces;
    private Vector3 bounds;

    //  ========================================

    //  DO:
    //  -INSTANTIATION FINAL
    //  -PLAYER TURN FIRST
    //  -CHECK CURRENT PLAYER TURN

    private void OnEnable()
    {
        AddPlayerStates("White", "INITIALIZE");
        AddPlayerStates("Black", "INITIALIZE");

        PhotonNetwork.NetworkingClient.EventReceived += MultiplayerEvents;
        OnPlayerStateChange += PlayerStatesChangeEvent;

        SetPlayerControl();
        StartCoroutine(GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y));
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= MultiplayerEvents;
        OnPlayerStateChange -= PlayerStatesChangeEvent;
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

            unitPieces[(int)data[2], (int)data[3]].type = (UnitPieceType)Convert.ToInt32(data[0]);
            unitPieces[(int)data[2], (int)data[3]].team = (int)data[1];
            unitPieces[(int)data[2], (int)data[3]].GetComponent<MeshRenderer>().material = teamMaterials[(int)data[1]];
        }

        if (obj.Code == 22)
        {
            object[] data = (object[])obj.CustomData;

            playerControl = data[0].ToString();

            if (playerControl == "White")
                blackCamObject.SetActive(false);
            else
                whiteCamObject.SetActive(false);
        }
    }

    #endregion

    #region GAMEPLAY

    private void PlayerStatesChangeEvent(object sender, EventArgs e)
    {

    }

    #region INITIALIZE

    private void SetPlayerControl()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            object[] data;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
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
        }
    }

    IEnumerator GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        while (playerControl == "") yield return null;

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
            unitPieces[1, 2] = SpawnSinglePiece(UnitPieceType.Major, whiteTeam, 1, 2);
            unitPieces[2, 2] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 2, 2);
            unitPieces[3, 2] = SpawnSinglePiece(UnitPieceType.SLieutenant, whiteTeam, 3, 2);
            unitPieces[4, 2] = SpawnSinglePiece(UnitPieceType.Sergeant, whiteTeam, 4, 2);
            unitPieces[5, 2] = SpawnSinglePiece(UnitPieceType.FLieutenant, whiteTeam, 5, 2);
            unitPieces[6, 2] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 6, 2);
            unitPieces[7, 2] = SpawnSinglePiece(UnitPieceType.Captain, whiteTeam, 7, 2);
            unitPieces[1, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 1, 1);
            unitPieces[2, 1] = SpawnSinglePiece(UnitPieceType.Spy, whiteTeam, 2, 1);
            unitPieces[3, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 3, 1);
            unitPieces[4, 1] = SpawnSinglePiece(UnitPieceType.LieutenantColonel, whiteTeam, 4, 1);
            unitPieces[5, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 5, 1);
            unitPieces[6, 1] = SpawnSinglePiece(UnitPieceType.Spy, whiteTeam, 6, 1);
            unitPieces[7, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam, 7, 1);
            unitPieces[1, 0] = SpawnSinglePiece(UnitPieceType.BrigadierGeneral, whiteTeam, 1, 0);
            unitPieces[2, 0] = SpawnSinglePiece(UnitPieceType.LieutenantGeneral, whiteTeam, 2, 0);
            unitPieces[3, 0] = SpawnSinglePiece(UnitPieceType.GeneralOfTheArmy, whiteTeam, 3, 0);
            unitPieces[4, 0] = SpawnSinglePiece(UnitPieceType.Flag, whiteTeam, 4, 0);
            unitPieces[5, 0] = SpawnSinglePiece(UnitPieceType.General, whiteTeam, 5, 0);
            unitPieces[6, 0] = SpawnSinglePiece(UnitPieceType.MajorGeneral, whiteTeam, 6, 0);
            unitPieces[7, 0] = SpawnSinglePiece(UnitPieceType.Colonel, whiteTeam, 7, 0);
        }


        //Black Team
        else if (playerControl == "Black")
        {
            unitPieces[1, 5] = SpawnSinglePiece(UnitPieceType.BMajor, blackTeam, 1, 5);
            unitPieces[2, 5] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 2, 5);
            unitPieces[3, 5] = SpawnSinglePiece(UnitPieceType.BSLieutenant, blackTeam, 3, 5);
            unitPieces[4, 5] = SpawnSinglePiece(UnitPieceType.BSergeant, blackTeam, 4, 5);
            unitPieces[5, 5] = SpawnSinglePiece(UnitPieceType.BFLieutenant, blackTeam, 5, 5);
            unitPieces[6, 5] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 6, 5);
            unitPieces[7, 5] = SpawnSinglePiece(UnitPieceType.BCaptain, blackTeam, 7, 5);
            unitPieces[1, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 1, 6);
            unitPieces[2, 6] = SpawnSinglePiece(UnitPieceType.BSpy, blackTeam, 2, 6);
            unitPieces[3, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 3, 6);
            unitPieces[4, 6] = SpawnSinglePiece(UnitPieceType.BLieutenantColonel, blackTeam, 4, 6);
            unitPieces[5, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 5, 6);
            unitPieces[6, 6] = SpawnSinglePiece(UnitPieceType.BSpy, blackTeam, 6, 6);
            unitPieces[7, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam, 7, 6);
            unitPieces[1, 7] = SpawnSinglePiece(UnitPieceType.BBrigadierGeneral, blackTeam, 1, 7);
            unitPieces[2, 7] = SpawnSinglePiece(UnitPieceType.BLieutenantGeneral, blackTeam, 2, 7);
            unitPieces[3, 7] = SpawnSinglePiece(UnitPieceType.BGeneralOfTheArmy, blackTeam, 3, 7);
            unitPieces[4, 7] = SpawnSinglePiece(UnitPieceType.BFlag, blackTeam, 4, 7);
            unitPieces[5, 7] = SpawnSinglePiece(UnitPieceType.BGeneral, blackTeam, 5, 7);
            unitPieces[6, 7] = SpawnSinglePiece(UnitPieceType.BMajorGeneral, blackTeam, 6, 7);
            unitPieces[7, 7] = SpawnSinglePiece(UnitPieceType.BColonel, blackTeam, 7, 7);
        }

        StartCoroutine(PositionAllPieces());
    }

    IEnumerator PositionAllPieces()
    {
        //  WAIT TO POPULATE ALL PIECES IN ARRAY BEFORE SORTING

        for (int a = 0; a < TILE_COUNT_X; a++)
        {
            for (int b = 0; b < TILE_COUNT_Y; b++)
            {
                if (unitPieces[a, b] == null)
                {
                    a = 0;
                    b = 0;
                }

                yield return null;
            }

            yield return null;
        }

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (unitPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);

                yield return null;
            }
            yield return null;
        }

        //  DONE INITIALIZING AND READY THE PLAYER UP
        ChangePlayerStates(playerControl, "READY");

        object[] data;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        data = new object[]
        {
            playerStates, "READY"
        };

        PhotonNetwork.RaiseEvent(21, data, raiseEventOptions, sendOptions);
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

    #endregion

    #region NETWORK

    private UnitPiece SpawnSinglePiece(UnitPieceType type, int team, int x, int y)
    {
        UnitPiece up = Instantiate(prefabs[(int)type - 1], transform).GetComponent<UnitPiece>();

        PhotonView piecePV = up.gameObject.GetComponent<PhotonView>();
        object[] data;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        if (PhotonNetwork.AllocateViewID(piecePV))
        {
            up.type = type;
            up.team = team;
            up.GetComponent<MeshRenderer>().material = teamMaterials[team];

            data = new object[] 
            {
                (int)type, team, x, y
            };

            PhotonNetwork.RaiseEvent(21, data, raiseEventOptions, sendOptions);

            return up;
        }

        return null;
    }

    #endregion

    #endregion
}
