using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BoardBackup : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1f;

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

    private void Awake()
    {
        isWhiteTurn = true;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();

        PositionAllPieces();
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
                if (true){
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

            bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);          
            if (!validMove)
                currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                currentlyDragging = null;
            
                currentlyDragging = null;
                RemoveHighlightTiles();
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
        unitPieces[1, 0] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[2, 0] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[3, 0] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[4, 0] = SpawnSinglePiece(UnitPieceType.Flag, whiteTeam);
        unitPieces[5, 0] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[6, 0] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        unitPieces[7, 0] = SpawnSinglePiece(UnitPieceType.Private, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            unitPieces[i, 1] = SpawnSinglePiece(UnitPieceType.General, whiteTeam);

         //Black Team
        unitPieces[1, 7] = SpawnSinglePiece(UnitPieceType.Private, blackTeam);
        unitPieces[2, 7] = SpawnSinglePiece(UnitPieceType.Private, blackTeam);
        unitPieces[3, 7] = SpawnSinglePiece(UnitPieceType.Private, blackTeam);
        unitPieces[4, 7] = SpawnSinglePiece(UnitPieceType.Flag, blackTeam);
        unitPieces[5, 7] = SpawnSinglePiece(UnitPieceType.Private, blackTeam);
        unitPieces[6, 7] = SpawnSinglePiece(UnitPieceType.Private, blackTeam);
        unitPieces[7, 7] = SpawnSinglePiece(UnitPieceType.Private, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            unitPieces[i, 6] = SpawnSinglePiece(UnitPieceType.General, blackTeam);
        
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

    //Operation
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos){
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

                return false;
    }

    private bool MoveTo(UnitPiece up, int x, int y){
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
            return false;

        Vector2Int previousPosition = new Vector2Int(up.currentX, up.currentY);

        //Is there another piece on the target position?
        if( unitPieces[x, y] != null){
            UnitPiece oup = unitPieces[x, y];

            if (up.team == oup.team)
                return false;      

        //If its the enemy team
        if (oup.team == 0){
            deadWhites.Add(oup);
            oup.SetScale(Vector3.one * deathSize);
            oup.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds + new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.forward * deathSpacing) * deadWhites.Count);
        }
        else{
            deadBlacks.Add(oup);
            oup.SetScale(Vector3.one * deathSize);
            oup.SetPosition(new Vector3(-1 * tileSize, yOffset, 7 * tileSize) - bounds + new Vector3(tileSize / 1, 0, tileSize / 1) + (Vector3.back * deathSpacing) * deadBlacks.Count);
        }
        }
        unitPieces[x, y] = up;
        unitPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        return true;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo){
        for (int x = 0; x <TILE_COUNT_X; x++)
            for (int y = 0; y <TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one; //Invalid
    }
}