using UnityEngine;
using TMPro;

public class LevelHUDManager : MonoBehaviour
{
    [Header("Top-Left UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text shotsText;

    [Header("Top-Right UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;

    [Header("Level Complete UI")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TMP_Text finalScoreSummaryText;
    [SerializeField] private GameObject nextLevelButton;
    [SerializeField] private GameObject titleScreenButton;

    [Header("Level Failed UI")]
    [SerializeField] private GameObject levelFailedPanel;
    [SerializeField] private GameObject failedTitleScreenButton;
    [SerializeField] private TMP_Text failedScoreSummaryText;


    private LevelManager levelManager;
    public void HandleRetryClicked() => levelManager.RestartLevel();
    public void HandleNextLevelClicked() => levelManager.LoadNextLevel();
    public void HandleTitleScreenClicked() => levelManager.GoToTitle();

    private void Start()
    {
        levelCompletePanel.SetActive(false);
        levelFailedPanel.SetActive(false);

        levelManager = LevelManager.Instance;
        if (levelManager == null)
        {
            Debug.LogError("LevelHUDManager: No LevelManager found in the scene.", this);
            gameObject.SetActive(false);
            return;
        }

        HandleLevelChanged(levelManager.CurrentLevelIndex + 1);

        // Subscribe to future updates
        levelManager.OnShotsChanged += HandleShotsChanged;
        levelManager.OnScoreChanged += HandleScoreChanged;
        levelManager.OnTimeChanged += HandleTimeChanged;
        levelManager.OnLevelCompleted += HandleLevelCompleted;
        levelManager.OnLevelFailed += HandleLevelFailed;

        // The level may have initialized before the HUD subscribed.
        HandleShotsChanged(levelManager.ShotsRemaining, levelManager.MaxShots);
        HandleScoreChanged(levelManager.CurrentScore);
        HandleTimeChanged(levelManager.ElapsedTime);
    }

    private void OnDestroy()
    {
        if (levelManager == null) return;

        levelManager.OnShotsChanged -= HandleShotsChanged;
        levelManager.OnScoreChanged -= HandleScoreChanged;
        levelManager.OnTimeChanged -= HandleTimeChanged;
        levelManager.OnLevelCompleted -= HandleLevelCompleted;
        levelManager.OnLevelFailed -= HandleLevelFailed;
    }

    private void HandleLevelChanged(int level)
    {
        levelText.text = $"Level {level}";
    }

    private void HandleShotsChanged(int remaining, int max)
    {
        shotsText.text = $"Shots: {remaining} / {max}";
    }

    private void HandleScoreChanged(int score)
    {
        scoreText.text = $"Score: {score:N0}";
    }

    private void HandleTimeChanged(float elapsed)
    {
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        float seconds = elapsed % 60f;
        timeText.text = $"Time: {minutes:00}:{seconds:00.0}";
    }

    private void HandleLevelCompleted(LevelCompleteData data)
    {
        bool hasNextLevel = levelManager.HasNextLevel;
        nextLevelButton.SetActive(hasNextLevel);
        titleScreenButton.SetActive(!hasNextLevel);
        levelCompletePanel.SetActive(true);

        finalScoreSummaryText.text =
            $"Level {levelManager.CurrentLevelIndex + 1} Complete!\n" +
            $"Base Score: {data.BaseScore:N0}\n" +
            $"Time Bonus: +{data.TimeBonus:N0}\n" +
            $"Shots Used ({data.ShotsUsed}): -{data.ShotPenalty:N0}\n" +
            $"Final Score: {data.FinalScore:N0}";
    }

    private void HandleLevelFailed()
    {
        levelCompletePanel.SetActive(false);
        levelFailedPanel.SetActive(true);
        failedTitleScreenButton.SetActive(true);

        failedScoreSummaryText.text =
            $"Level {levelManager.CurrentLevelIndex + 1} Failed!\n" +
            $"Score: {levelManager.CurrentScore:N0}\n" +
            $"Shots Used: {levelManager.ShotsTaken}\n" +
            $"Time: {levelManager.ElapsedTime:F1}s";
    }
}
