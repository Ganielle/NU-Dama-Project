using UnityEngine;

public class TileClickDetector : MonoBehaviour
{
    private TileProperties tileProperties;
    private PawnMover pawnMover;
    public bool isMultiplayer;

    private void Awake()
    {
        tileProperties = GetComponent<TileProperties>();
    }

    private void Start()
    {
        pawnMover = GetComponentInParent<PawnMover>();
    }

    public void ChildPawnClicked()
    {
        OnMouseDown();
    }

    private void OnMouseDown()
    {
        if (isMultiplayer)
        {
            if (pawnMover.multiplayerController.CurrentTeam == pawnMover.multiplayerController.GetTurn())
            {
                if (tileProperties.IsOccupied())
                    pawnMover.PawnClicked(tileProperties.GetPawn());
                else
                    pawnMover.TileClicked(this.gameObject);
            }

            return;
        }

        if (tileProperties.IsOccupied())
            pawnMover.PawnClicked(tileProperties.GetPawn());
        else
            pawnMover.TileClicked(this.gameObject);
    }

    public void ClickTile()
    {
        OnMouseDown();
    }
}