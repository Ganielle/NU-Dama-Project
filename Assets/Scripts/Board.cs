using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1f;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //Logic
    private UnitPiece[,] unitPieces;
    private UnitPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<UnitPiece> deadWhites = new List<UnitPiece>();
    private List<UnitPiece> deadBlacks = new List<UnitPiece>();
    private const int TILE_COUNT_X = 9;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;

    //Multi logic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = true;
    private bool [] playerRematch = new bool [2];

    private void Start()
    {
        isWhiteTurn = true;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();

        PositionAllPieces();

        RegisterEvents();
    }
    private void Update(){
        if (!currentCamera)
    {
        currentCamera = Camera.main;
        return;
    }

    RaycastHit info;
    Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
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
        if (Input.GetMouseButtonDown(0)){
            if (unitPieces[hitPosition.x, hitPosition.y] != null){
                //Is it our turn?
                if ((unitPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam == 0) || (unitPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1)){
                    currentlyDragging = unitPieces[hitPosition.x, hitPosition.y];

                    //Get a list of where I can go, highlight tiles as well
                    availableMoves = currentlyDragging.GetAvailableMoves(ref unitPieces, TILE_COUNT_X, TILE_COUNT_Y);
                    HighlightTiles();
                }
            }
        }

        //If we are releasing the mouse button
        if (currentlyDragging != null && Input.GetMouseButtonUp(0)){
            Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
            if (ContainsValidMove(ref availableMoves, new Vector2(hitPosition.x, hitPosition.y))){
                MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);
                //Net Implementation
                NetMakeMove mm = new NetMakeMove();
                mm.originalX = previousPosition.x;
                mm.originalY = previousPosition.y;
                mm.destinationX = hitPosition.x;
                mm.destinationY = hitPosition.y;
                mm.teamId = currentTeam;
                Client.Instance.SendToServer(mm);
            }
            else{
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

            if (currentlyDragging && Input.GetMouseButtonUp(0)){
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }  

        //If we're dragging a piece
        if (currentlyDragging){
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if(horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }

    //Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY){
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX/2) * tileSize, 0, (tileCountX/2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
            tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }
    
    private GameObject GenerateSingleTile(float tileSize, int x, int y){
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

        int[] tris = new int[] {0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    //Spawning of the pieces
    private void SpawnAllPieces(){
        unitPieces = new UnitPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;

        //White Team
        unitPieces[1, 2] = SpawnSinglePiece(UnitPieceType.Major, whiteTeam);
        unitPieces[2, 2] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[3, 2] = SpawnSinglePiece(UnitPieceType.SLieutenant, whiteTeam);
        unitPieces[4, 2] = SpawnSinglePiece(UnitPieceType.Sergeant, whiteTeam);
        unitPieces[5, 2] = SpawnSinglePiece(UnitPieceType.FLieutenant, whiteTeam);
        unitPieces[6, 2] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[7, 2] = SpawnSinglePiece(UnitPieceType.Captain, whiteTeam);
        unitPieces[1, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[2, 1] = SpawnSinglePiece(UnitPieceType.Spy, whiteTeam);
        unitPieces[3, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[4, 1] = SpawnSinglePiece(UnitPieceType.LieutenantColonel, whiteTeam);
        unitPieces[5, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[6, 1] = SpawnSinglePiece(UnitPieceType.Spy, whiteTeam);
        unitPieces[7, 1] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[1, 0] = SpawnSinglePiece(UnitPieceType.BrigadierGeneral, whiteTeam);
        unitPieces[2, 0] = SpawnSinglePiece(UnitPieceType.LieutenantGeneral, whiteTeam);
        unitPieces[3, 0] = SpawnSinglePiece(UnitPieceType.GeneralOfTheArmy, whiteTeam);
        unitPieces[4, 0] = SpawnSinglePiece(UnitPieceType.Flag, whiteTeam);
        unitPieces[5, 0] = SpawnSinglePiece(UnitPieceType.General, whiteTeam);
        unitPieces[6, 0] = SpawnSinglePiece(UnitPieceType.MajorGeneral, whiteTeam);
        unitPieces[7, 0] = SpawnSinglePiece(UnitPieceType.Colonel, whiteTeam);
        

         //Black Team
        unitPieces[1, 5] = SpawnSinglePiece(UnitPieceType.BMajor, blackTeam);
        unitPieces[2, 5] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam);
        unitPieces[3, 5] = SpawnSinglePiece(UnitPieceType.BSLieutenant, blackTeam);
        unitPieces[4, 5] = SpawnSinglePiece(UnitPieceType.BSergeant, blackTeam);
        unitPieces[5, 5] = SpawnSinglePiece(UnitPieceType.BFLieutenant, blackTeam);
        unitPieces[6, 5] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam);
        unitPieces[7, 5] = SpawnSinglePiece(UnitPieceType.BCaptain, blackTeam);
        unitPieces[1, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam);
        unitPieces[2, 6] = SpawnSinglePiece(UnitPieceType.BSpy, blackTeam);
        unitPieces[3, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam);
        unitPieces[4, 6] = SpawnSinglePiece(UnitPieceType.BLieutenantColonel, blackTeam);
        unitPieces[5, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam);
        unitPieces[6, 6] = SpawnSinglePiece(UnitPieceType.BSpy, blackTeam);
        unitPieces[7, 6] = SpawnSinglePiece(UnitPieceType.BPrivate, blackTeam);
        unitPieces[1, 7] = SpawnSinglePiece(UnitPieceType.BBrigadierGeneral, blackTeam);
        unitPieces[2, 7] = SpawnSinglePiece(UnitPieceType.BLieutenantGeneral, blackTeam);
        unitPieces[3, 7] = SpawnSinglePiece(UnitPieceType.BGeneralOfTheArmy, blackTeam);
        unitPieces[4, 7] = SpawnSinglePiece(UnitPieceType.BFlag, blackTeam);
        unitPieces[5, 7] = SpawnSinglePiece(UnitPieceType.BGeneral, blackTeam);
        unitPieces[6, 7] = SpawnSinglePiece(UnitPieceType.BMajorGeneral, blackTeam);
        unitPieces[7, 7] = SpawnSinglePiece(UnitPieceType.BColonel, blackTeam);        
    }

    private UnitPiece SpawnSinglePiece(UnitPieceType type, int team){
        UnitPiece up = Instantiate(prefabs[(int)type - 1], transform).GetComponent<UnitPiece>();

        up.type = type;
        up.team = team;
        up.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return up;
    }

    //Positioning
    private void PositionAllPieces(){
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (unitPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }

    private void PositionSinglePiece(int x, int y, bool force = false){
        unitPieces[x, y].currentX = x;
        unitPieces[x, y].currentY = y;
        unitPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    private Vector3 GetTileCenter(int x, int y){
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    //Highlight Tiles
    private void HighlightTiles(){
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }

    private void RemoveHighlightTiles(){
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

            availableMoves.Clear();
    }

    //Checkmate
    private void CheckMate(int team){
        DisplayVictory(team);
    }

    private void DisplayVictory(int winningTeam){
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }

    public void OnRematchButton(){
        if (localGame){
            NetRematch wrm = new NetRematch();
            wrm.teamId = 0;
            wrm.wantRematch = 1;
            Client.Instance.SendToServer(wrm);

            NetRematch brm = new NetRematch();
            brm.teamId = 1;
            brm.wantRematch = 1;
            Client.Instance.SendToServer(brm);
        }
        else{
            NetRematch rm = new NetRematch();
            rm.teamId = currentTeam;
            rm.wantRematch = 1;
            Client.Instance.SendToServer(rm);
        }
    }

    public void GameReset(){
        //UI
        rematchButton.interactable = true;

        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);

        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        //Fields reset
        currentlyDragging = null;
        availableMoves = new List<Vector2Int>();
        playerRematch[0] = playerRematch[1] = false;

        //CLean up
        for (int x = 0; x < TILE_COUNT_X; x++){
            for (int y = 0; y < TILE_COUNT_Y; y++){
                if (unitPieces[x, y] != null)
                    Destroy(unitPieces[x, y].gameObject);

                    unitPieces[x, y] = null;
            }
        }
        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);
        
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }

    public void OnMenuButton(){
        NetRematch rm = new NetRematch();
        rm.teamId = currentTeam;
        rm.wantRematch = 0;
        Client.Instance.SendToServer(rm);

        GameReset();
        GameUI.Instance.OnLeaveFromGameMenu();

        Invoke("ShutdownRelay", 1.0f);

        //Reset some value
        playerCount = -1;
        currentTeam = -1;
    }

    //Operation
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos){
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

                return false;
    }

    private void MoveTo(int originalX, int originalY, int x, int y){

        UnitPiece up = unitPieces[originalX, originalY];
        Vector2Int previousPosition = new Vector2Int(originalX, originalY);

        //Is there another piece on the target position?
        if( unitPieces[x, y] != null){
            UnitPiece oup = unitPieces[x, y];

            if (up.team == oup.team)
                return;      

        //If its the enemy team
        if (oup.team == 0){
            if(oup.type == UnitPieceType.Flag)
                CheckMate(1);

            deadWhites.Add(oup);
            oup.SetScale(Vector3.one * deathSize);
            oup.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds + new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.forward * deathSpacing) * deadWhites.Count);
        }
        else{
            if(oup.type == UnitPieceType.BFlag)
                CheckMate(0);

            deadBlacks.Add(oup);
            oup.SetScale(Vector3.one * deathSize);
            oup.SetPosition(new Vector3(-1 * tileSize, yOffset, 7 * tileSize) - bounds + new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.back * deathSpacing) * deadBlacks.Count);
        }
        }
        unitPieces[x, y] = up;
        unitPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;
        if (localGame)
            currentTeam = (currentTeam == 0) ? 1 : 0;

        if (currentlyDragging)
            currentlyDragging = null;
            RemoveHighlightTiles(); 

        return;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo){
        for (int x = 0; x <TILE_COUNT_X; x++)
            for (int y = 0; y <TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one; //Invalid
    }
    #region 
    private void RegisterEvents(){
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_MAKE_MOVE += OnRematchServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;

        GameUI.Instance.SetLocalGame += OnSetLocalGame;
    }

    private void UnregisterToEvents(){
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_MAKE_MOVE -= OnRematchServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;

        GameUI.Instance.SetLocalGame -= OnSetLocalGame;
    }
    //Server
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn){
        //Client has connected, assign a team and return the message back to him
        NetWelcome nw = msg as NetWelcome;

        //Assign a team
        nw.AssignedTeam = ++playerCount;

        //Return back to the client
        Server.Instance.SendToClient(cnn, nw);

        //If full, start the game
        if (playerCount == 1)
            Server.Instance.Broadcast(new NetStartGame());
    }

    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn){
        //Receive, and just broadcast it back
        NetMakeMove mm = msg as NetMakeMove;

        //This is where you could do some validation checks

        //Receive, and just broadcast it back
        Server.Instance.Broadcast(msg);
    }

    private void OnRematchServer(NetMessage msg, NetworkConnection cnn){
        Server.Instance.Broadcast(msg);
    }

    //Client
    private void OnWelcomeClient(NetMessage msg){
        //Receive the connection message
        NetWelcome nw = msg as NetWelcome;

        //Assign the team
        currentTeam = nw.AssignedTeam;

        Debug.Log($"My assigned team is {nw.AssignedTeam}");

        if (localGame && currentTeam == 0)
            Server.Instance.Broadcast(new NetStartGame());
    }

    private void OnStartGameClient(NetMessage msg){
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }

    private void OnMakeMoveClient(NetMessage msg){
        NetMakeMove mm = msg as NetMakeMove;

        Debug.Log($"MM : {mm.teamId} : {mm.originalX} {mm.originalY} -> {mm.destinationX} {mm.destinationY}");

        if (mm.teamId != currentTeam){
            UnitPiece target = unitPieces[mm.originalX, mm.originalY];

            availableMoves = target.GetAvailableMoves(ref unitPieces, TILE_COUNT_X, TILE_COUNT_Y);
            MoveTo(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);
        }
    }

    private void OnRematchClient(NetMessage msg){
        //Receive the connection message
        NetRematch rm = msg as NetRematch;
        //Set the boolean for rematch
        playerRematch[rm.teamId] = rm.wantRematch == 1;

        //Activate the piece of UI
        if (rm.teamId != currentTeam){
            rematchIndicator.transform.GetChild((rm.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if (rm.wantRematch != 1){
                rematchButton.interactable = false;
            }
        }
        //If both wants to rematch
        if (playerRematch[0] && playerRematch[1])
            GameReset();
    }

    private void ShutdownRelay(){
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }

    private void OnSetLocalGame(bool v){
        playerCount = -1;
        currentTeam = -1;
        localGame = v;
    }

    #endregion
}