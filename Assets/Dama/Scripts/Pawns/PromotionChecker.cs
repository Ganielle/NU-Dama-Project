using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

public class PromotionChecker : MonoBehaviour
{
    private int boardSize;
    public DamaMultiplayerController multiplayerController;
    public bool isMultiplayer;

    //  ====================================

    RaiseEventOptions EventOptions = new RaiseEventOptions
    {
        Receivers = ReceiverGroup.Others,
        CachingOption = EventCaching.AddToRoomCache
    };
    SendOptions SendOptions = new SendOptions { Reliability = true };

    //  ====================================

    private void Start()
    {
        boardSize = GetComponent<ITilesGenerator>().BoardSize;

        if (isMultiplayer)
            PhotonNetwork.NetworkingClient.EventReceived += MultiplayerEvents;
    }

    private void MultiplayerEvents(EventData obj)
    {
        if (obj.Code == 33)
        {
            object[] data = (object[])obj.CustomData;

            GameObject white = multiplayerController.whitePieces.Find(x => x.name == data[0].ToString());
            GameObject black = multiplayerController.blackPieces.Find(x => x.name == data[0].ToString());

            if (white != null)
            {
                white.GetComponent<IPawnProperties>().PromoteToKing();
            }
            else if (black != null)
            {
                black.GetComponent<IPawnProperties>().PromoteToKing();
            }
        }
    }

    public void CheckPromotion(GameObject pawnToCheck)
    {
        var pawnProperties = pawnToCheck.GetComponent<IPawnProperties>();
        if (pawnProperties.IsKing)
            return;
        var tileIndex = pawnProperties.GetTileIndex();
        int promotionRow = GetPromotionRow(pawnProperties);
        if (tileIndex.Row == promotionRow)
        {
            pawnProperties.PromoteToKing();

            if (isMultiplayer)
            {
                object[] data = new object[]
                {
                pawnToCheck.name
                };

                PhotonNetwork.RaiseEvent(33, data, EventOptions, SendOptions);
            }
        }
    }

    private int GetPromotionRow(IPawnProperties pawnProperties)
    {
        return pawnProperties.PawnColor == PawnColor.White ? boardSize - 1 : 0;
    }
}