using System.Collections.Specialized;
using UnityEngine;
using System;
using System.Collections.Generic;

public enum UnitPieceType{
    None = 0,
    Spy = 1,
    Private = 2,
    Sergeant = 3,
    SLieutenant = 4,
    FLieutenant = 5,
    Captain = 6,
    Major = 7,
    LieutenantColonel = 8,
    Colonel = 9,
    BrigadierGeneral = 10,
    MajorGeneral = 11,
    LieutenantGeneral = 12,
    General = 13,
    GeneralOfTheArmy = 14,
    Flag = 15,
    BSpy = 16,
    BPrivate = 17,
    BSergeant = 18,
    BSLieutenant = 19,
    BFLieutenant = 20,
    BCaptain = 21,
    BMajor = 22,
    BLieutenantColonel = 23,
    BColonel = 24,
    BBrigadierGeneral = 25,
    BMajorGeneral = 26,
    BLieutenantGeneral = 27,
    BGeneral = 28,
    BGeneralOfTheArmy = 29,
    BFlag = 30,
    Pawn = 31,
    King = 32,
    BPawn = 33,
    BKing = 34
}

public class UnitPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public UnitPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    private void Start(){
        transform.rotation = Quaternion.Euler((team == 0) ? Vector3.zero : new Vector3(0, 180, 0));
    }

    private void Update(){
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref UnitPiece[,] board, int tileCountX, int tileCountY){
        List<Vector2Int> r = new List<Vector2Int>();

        r.Add(new Vector2Int(3, 3));
        r.Add(new Vector2Int(3, 4));
        r.Add(new Vector2Int(4, 3));
        r.Add(new Vector2Int(4, 4));

        return r;
    }

    public virtual void SetPosition(Vector3 position, bool force = false){
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false){
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }
}
