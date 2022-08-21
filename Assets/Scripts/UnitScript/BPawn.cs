using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPawn : UnitPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref UnitPiece[,] board, int tileCountX, int tileCountY){
        List<Vector2Int> r = new List<Vector2Int>();

        //BottomRight
        if (currentX + 1 >= 0){
            if (board[currentX + 1, currentY - 1] == null)
                r.Add(new Vector2Int(currentX + 1, currentY - 1));
                else if (board[currentX + 1, currentY - 1].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY - 1));
        }

        //BottonLeft
        if (currentX - 1 >= 0){
            if (board[currentX - 1, currentY - 1] == null)
                r.Add(new Vector2Int(currentX - 1, currentY - 1));
                else if (board[currentX - 1, currentY - 1].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY - 1));
        }


        return r;
    }
    }
