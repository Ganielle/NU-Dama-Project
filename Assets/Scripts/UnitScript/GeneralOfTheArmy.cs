using UnityEngine;
using System.Collections.Generic;

public class GeneralOfTheArmy : UnitPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref UnitPiece[,] board, int tileCountX, int tileCountY){
        List<Vector2Int> r = new List<Vector2Int>();

        //Right
        if (currentX + 1 < tileCountX){
            if (board[currentX + 1, currentY] == null)
                r.Add(new Vector2Int(currentX + 1, currentY));
                else if (board[currentX + 1, currentY].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY));
        }

        //Left
        if (currentX - 1 >= 0){
            if (board[currentX - 1, currentY] == null)
                r.Add(new Vector2Int(currentX - 1, currentY));
                else if (board[currentX - 1, currentY].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY));
        }

        //UP
        if (currentY + 1 < tileCountY)
            if (board[currentX, currentY + 1] == null || board[currentX, currentY + 1].team != team)
                r.Add(new Vector2Int(currentX, currentY + 1));

        //Down
        if (currentY - 1 >= 0)
            if (board[currentX, currentY - 1] == null || board[currentX, currentY - 1].team != team)
                r.Add(new Vector2Int(currentX, currentY - 1));

        return r;
    }
}