using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class GameUI : MonoBehaviour
{
    public Image fadePlane;
    public GameObject gameOverUI;

    public RectTransform newWaveBanner;
    public TextMeshProUGUI newWaveTitle;
    public TextMeshProUGUI newWabeEnemyCount;
    public TextMeshProUGUI scoreUIText;
    public TextMeshProUGUI gameOverScoreText;
    public RectTransform healthBar;

    
    public GameObject pauseUI;

    Spawner spawner;
    Player player;

    
    bool isPaused = false;

    void Start()
    {
        player = FindAnyObjectByType<Player>();
        player.OnDeath += OnGameOver;
    }

    void Awake()
    {
        spawner = FindAnyObjectByType<Spawner>();
        spawner.OnNewWave += OnNewWave;
    }

    void Update()
    {
        scoreUIText.text = ScoreKeeper.score.ToString("D6");
        float healthPercent = 0;
        if (player != null)
        {
            healthPercent = player.health / player.startingHealth;
        }
        healthBar.localScale = new Vector3(healthPercent, 1, 1);

        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Don't allow pausing if game is already over
            if (!gameOverUI.activeSelf)
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseUI.SetActive(true);

        // Freeze everything EXCEPT audio
        Time.timeScale = 0f;

        // Show cursor so player can click buttons
        Cursor.visible = true;

        StartCoroutine(Fade(Color.clear, new Color(0, 0, 0, 1f), 0.5f));
    }

    
    public void ResumeGame()
    {
        isPaused = false;
        pauseUI.SetActive(false);

        // Unfreeze the game
        Time.timeScale = 1f;

        // Hide cursor again during gameplay
        Cursor.visible = false;

        StartCoroutine(Fade(new Color(0, 0, 0, 1f), Color.clear, 0.5f));
    }

    void OnNewWave(int waveNumber)
    {
        string[] numbers = { "One", "Two", "Three", "Four", "Five" };
        newWaveTitle.text = "- Wave " + numbers[waveNumber - 1] + " -";
        string enemyCountString = ((spawner.waves[waveNumber - 1].infinite) ? "Infinite" : spawner.waves[waveNumber - 1].enemyCount + "");
        newWabeEnemyCount.text = "Enemies: " + enemyCountString;

        StopCoroutine("AnimateNewWaveBanner");
        StartCoroutine("AnimateNewWaveBanner");
    }

    IEnumerator AnimateNewWaveBanner()
    {
        float delayTime = 1.5f;
        float speed = 2.5f;
        float animatePercent = 0;
        int dir = 1;

        float endDelayTime = Time.time + 1 / speed + delayTime;

        while (animatePercent >= 0)
        {
            animatePercent += Time.deltaTime * speed * dir;

            if (animatePercent >= 1)
            {
                animatePercent = 1;
                if (Time.time > endDelayTime)
                {
                    dir = -1;
                }
            }
            newWaveBanner.anchoredPosition = Vector2.up * Mathf.Lerp(-230, 40, animatePercent);
            yield return null;
        }
    }

    void OnGameOver()
    {
        
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.visible = true;
        StartCoroutine(Fade(Color.clear, new Color(0, 0, 0, .95f), 1f));
        gameOverScoreText.text = scoreUIText.text;
        scoreUIText.gameObject.SetActive(false);
        healthBar.transform.parent.gameObject.SetActive(false);
        gameOverUI.SetActive(true);
    }

    IEnumerator Fade(Color from, Color to, float time)
    {
        float speed = 1 / time;
        float percent = 0;

        while (percent < 1)
        {
            percent += Time.unscaledDeltaTime * speed;
            fadePlane.color = Color.Lerp(from, to, percent);
            yield return null;
        }
    }

    // UI Input
    public void StartNewGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("Game");
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("Menu");
    }
}