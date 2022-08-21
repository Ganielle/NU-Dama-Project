using System.Security.AccessControl;
using UnityEngine;
using System.Collections.Generic;

public class Pawn : UnitPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref UnitPiece[,] board, int tileCountX, int tileCountY){
        List<Vector2Int> r = new List<Vector2Int>();

        //TopRight
        if (currentX + 1 < tileCountX){
            if (board[currentX + 1, currentY + 1] == null)
                r.Add(new Vector2Int(currentX + 1, currentY + 1));
                else if (board[currentX + 1, currentY + 1].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY + 1));
        }

        //TopLeft
        if (currentX + 1 >= 0){
            if (board[currentX - 1, currentY + 1] == null)
                r.Add(new Vector2Int(currentX - 1, currentY + 1));
                else if (board[currentX - 1, currentY + 1].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY + 1));
        }


        return r;
    }
}
