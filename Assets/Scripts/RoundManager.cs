using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundManager : MonoBehaviour
{
    public float roundTime = 60f;
    private UIManager uiManager;

    private bool endingRound = false;

    private Board board;

    /// <summary>
    /// Score
    /// </summary>
    public int currentScore;
    public float displayScore;
    public float scoreSpeed;

    public int scoreTarget1, scoreTarget2, scoreTarget3;

    void Awake()
    {
        uiManager = FindObjectOfType<UIManager>();
        board = FindObjectOfType<Board>();
    }

    
    void Update()
    {
        if(roundTime > 0)
        {
            roundTime -= Time.deltaTime;

            if(roundTime <= 0)
            {
                roundTime = 0;

                endingRound = true;
            }
        }

        if(endingRound && board.currentState == Board.BoardState.move)
        {
            WinCheck();
            endingRound = false;
        }

        uiManager.timeText.text = roundTime.ToString("0.0") + "sec";
        displayScore = Mathf.Lerp(displayScore, currentScore, scoreSpeed * Time.deltaTime);
        uiManager.scoreText.text = displayScore.ToString("0");
    }

    private void WinCheck()
    {
        uiManager.roundOverScreen.SetActive(true);

        uiManager.winScore.text = currentScore.ToString();

        if(currentScore >= scoreTarget3)
        {
            uiManager.winText.text = "Congratulations! You earned 3 stars!";
            uiManager.winStars3.SetActive(true);

            PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "_Star1", 1);
            PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "_Star2", 1);
            PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "_Star3", 1);
        }
        else if(currentScore >= scoreTarget2)
        {
            uiManager.winText.text = "Congratulations! You earned 2 stars!";
            uiManager.winStars2.SetActive(true);

            PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "_Star1", 1);
            PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "_Star2", 1);
        }
        else if (currentScore >= scoreTarget1)
        {
            uiManager.winText.text = "Congratulations! You earned 1 stars!";
            uiManager.winStars1.SetActive(true);

            PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "_Star1", 1);
        }
        else
        {
            uiManager.winText.text = "Try again?";
        }

        SFXManager.instance.PlayRoundOver();
    }
}
