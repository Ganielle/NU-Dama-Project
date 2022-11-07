using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentTurnEventAnimator : MonoBehaviour
{
    public TheGeneralsMultiplayerCore multiplayerCore;

    public void CanAttack()
    {
        Debug.Log("hellooooo");
        multiplayerCore.CanNowAttack = true;
    }
}
