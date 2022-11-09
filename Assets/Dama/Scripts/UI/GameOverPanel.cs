using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverPanel : MonoBehaviour
{
    public GameObject Board;
    public TextMeshProUGUI WinnerText;
    public GameAudio GameAudio;

    public bool isMultiplayer;
    private Animator gameOverPanelAnimator;

    private void Awake()
    {
        gameOverPanelAnimator = GetComponent<Animator>();
    }

    public void SetWinnerText(PawnColor winnerPawnColor)
    {
        WinnerText.text = winnerPawnColor.ToString().ToUpper() + " WINS";
    }

    public void DisableBoard()
    {
        if (isMultiplayer) return;

        Board.SetActive(false);
    }

    public void ReturnToMenu()
    {
        gameOverPanelAnimator.SetTrigger("ReturnToMenu");
    }

    public void LoadMenuScene()
    {
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    public void FadeGameMusic()
    {
        GameAudio.FadeGameMusic();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}