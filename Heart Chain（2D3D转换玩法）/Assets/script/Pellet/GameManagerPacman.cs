using UnityEngine;
using TMPro;   // 如果你用 TextMeshPro；如果用旧 Text，改成 using UnityEngine.UI;

public class GameManagerPacman : MonoBehaviour
{
    public static GameManagerPacman Instance { get; private set; }

    public int score = 0;
    public TextMeshProUGUI scoreText; // 如果用旧 Text：public Text scoreText;

    private int totalPellets;
    private int collectedPellets;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 统计场景中所有豆子数量
        Pellet[] pellets = FindObjectsOfType<Pellet>();
        totalPellets = pellets.Length;
        collectedPellets = 0;

        UpdateScoreUI();
    }

    public void AddScore(int value)
    {
        score += value;
        collectedPellets++;
        UpdateScoreUI();

        if (collectedPellets >= totalPellets)
        {
            Debug.Log("Win! All pellets collected.");
            // 这里可以弹出胜利 UI 或重开
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}