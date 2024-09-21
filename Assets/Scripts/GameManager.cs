using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameObject backgroundPanel;
    public GameObject victoryPanel;
    public GameObject losePanel;

    public int goal;
    public int moves;
    public int points;

    public bool isGameEnded;

    public TMP_Text pointsText;
    public TMP_Text movesText;
    public TMP_Text goalText;

    public void Awake() {
        instance = this;
    }

    public void Initialize(int _moves, int _goal) {
        moves = _moves;
        goal = _goal;
    }

    private void Update() {
        pointsText.text = "Points: " + points.ToString();
        movesText.text = "Moves: " + moves.ToString();
        goalText.text = "Goal: " + goal.ToString();
    }

    public void ProcessTurn(int _pointsToGain, bool _subtractMoves) {
        points += _pointsToGain;
        if (_subtractMoves) {
            moves--;
        }

        if (points >= goal) { 
            isGameEnded = true;
            backgroundPanel.SetActive(true);
            victoryPanel.SetActive(true);
            PotionBoard.Instance.potionParent.SetActive(false);
            return;
        }

        if (moves == 0) {
            isGameEnded = true;
            backgroundPanel.SetActive(true);
            losePanel.SetActive(true);
            PotionBoard.Instance.potionParent.SetActive(false);
            return;
        }
    }

    public void WinGame() {
        SceneManager.LoadScene(0);
    }

    public void LoseGame() {
        SceneManager.LoadScene(0);
    }
}
