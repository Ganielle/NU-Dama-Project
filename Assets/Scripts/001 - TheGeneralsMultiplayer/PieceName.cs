using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PieceName : MonoBehaviour
{
    public TextMeshProUGUI pieceNameTMP;
    private Transform mainCamera;
    private Quaternion facingDirection;

    private void Awake()
    {
        mainCamera = Camera.main.transform;
    }

    private void Update()
    {
        facingDirection = Quaternion.LookRotation(transform.position - mainCamera.position);
        transform.rotation = Quaternion.Euler(facingDirection.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
    }
}
