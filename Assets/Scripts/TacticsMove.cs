using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsMove : MonoBehaviour
{
    List<Tile> selectableTiles = new List<Tile>();
    GameObject[] tiles;

    Stack<Tile> path = new Stack<Tile>();
    Tile currentTile;

    public bool moving = false;
    public int move = 1; 
    public float jumpHeight = 2f;
    public float moveSpeed = 2f;

    Vector3 velocity = new Vector3();
    Vector3 heading = new Vector3();

    float halfHeight = 0;

    public Tile actualTargetTile;

    protected void Init(){
        tiles = GameObject.FindGameObjectsWithTag("Tile");

        halfHeight = GetComponent<Collider>().bounds.extents.y;
    }
    public void GetCurrentTile(){
        currentTile = GetTargetTile(gameObject);
        currentTile.current = true;
    }
    public Tile GetTargetTile(GameObject target){
        RaycastHit hit;
        Tile tile = null;
        if (Physics.Raycast(target.transform.position, -Vector3.up , out hit, 1)){
            tile = hit.collider.GetComponent<Tile>();
        }
        return tile;
    }
    public void ComputedAdjacencyList(float jumpHeight, Tile target){
        //tiles = GameObject.FindGameObjectsWithTag("Tile");
        foreach (GameObject tile in tiles){
            Tile t = tile.GetComponent<Tile>();
            t.FindNeighbors(jumpHeight, target);
        }
    }
    public void FindSelectableTiles(){
        ComputedAdjacencyList(jumpHeight, null);
        GetCurrentTile();

        Queue<Tile> process = new Queue<Tile>();
        process.Enqueue(currentTile);
        currentTile.visited = true;
        //currentTile.parent = leave as null

        while (process.Count > 0){
            Tile t = process.Dequeue();
            selectableTiles.Add(t);
            t.selectable = true;

            if (t.distance < move){
            
            foreach (Tile tile in t.adjacencyList){
                if (!tile.visited){
                    tile.parent = t;
                    tile.visited = true;
                    tile.distance = 1 + t.distance;
                    process.Enqueue(tile);
                }
            }
          }
        }
    }
    public void MoveTOTile(Tile tile){
        path.Clear();
        tile.target = true;
        moving = true;

        Tile next = tile;
        while (next != null){
            path.Push(next);
            next = next.parent;
        }
    }
    public void Move(){
        if (path.Count > 0){
            Tile t = path.Peek();
            Vector3 target = t.transform.position;

            //calculate the unit's position on top of the target tile
            target.y += halfHeight + t.GetComponent<Collider>().bounds.extents.y;

            if (Vector3.Distance(transform.position, target) >= 0.05f){
                CalculateHeading(target);
                SetHorizontalVelocity();

                transform.forward = heading;
                transform.position += velocity * Time.deltaTime;
            }
            else{
                //Tile center reached
                transform.position = target;
                path.Pop();
            }
        }
        else{
            RemoveSelectableTiles();
            moving = false;
        }
    }
    protected void RemoveSelectableTiles(){
        if (currentTile != null){
            currentTile.current = false;
            currentTile = null;
        }

        foreach (Tile tile in selectableTiles){
            tile.Reset();
        }
        selectableTiles.Clear();
    }
    void CalculateHeading(Vector3 target){
        heading = target - transform.position;
        heading.Normalize();
    }
    void SetHorizontalVelocity(){
        velocity = heading * moveSpeed;
    }
}
