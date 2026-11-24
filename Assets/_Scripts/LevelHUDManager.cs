using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LevelHUDManager : MonoBehaviour
{
    [SerializeField] private LevelStart levelStart;

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

    private LevelManager boundManager;

    private void Start()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        boundManager = LevelManager.Instance;
        if (boundManager == null)
        {
            Debug.LogWarning("LevelHUDView: No LevelManager found in the scene.");
            return;
        }

        // The HUD is in the level prefab, while the loader lives in the scene
        if (levelStart == null)
        {
            levelStart = FindObjectOfType<LevelStart>();
        }

        if (levelStart != null)
        {
            HandleLevelChanged(levelStart.CurrentLevelIndex + 1);
        }

        // Subscribe to future updates
        boundManager.OnShotsChanged += HandleShotsChanged;
        boundManager.OnScoreChanged += HandleScoreChanged;
        boundManager.OnTimeChanged += HandleTimeChanged;
        boundManager.OnLevelCompleted += HandleLevelCompleted;

        // The level may have initialized before the HUD subscribed.
        HandleShotsChanged(boundManager.ShotsRemaining, boundManager.MaxShots);
        HandleScoreChanged(boundManager.CurrentScore);
        HandleTimeChanged(boundManager.ElapsedTime);
    }

    private void OnDestroy()
    {
        if (boundManager == null) return;

        boundManager.OnShotsChanged -= HandleShotsChanged;
        boundManager.OnScoreChanged -= HandleScoreChanged;
        boundManager.OnTimeChanged -= HandleTimeChanged;
        boundManager.OnLevelCompleted -= HandleLevelCompleted;
    }

    private void HandleLevelChanged(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {level}";
        }
    }

    private void HandleShotsChanged(int remaining, int max)
    {
        if (shotsText != null)
        {
            shotsText.text = $"Shots: {remaining} / {max}";
        }
    }

    private void HandleScoreChanged(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score:N0}";
        }
    }

    private void HandleTimeChanged(float elapsed)
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            float seconds = elapsed % 60f;
            timeText.text = $"Time: {minutes:00}:{seconds:00.0}";
        }
    }

    public void HandleRetryClicked()
    {
        if (levelStart != null) levelStart.RestartLevel();
    }

    public void HandleNextLevelClicked()
    {
        if (levelStart != null) levelStart.LoadNextLevel();
    }

    private void HandleLevelCompleted(LevelCompleteData data)
    {
        if (nextLevelButton != null)
            nextLevelButton.SetActive(levelStart != null && levelStart.HasNextLevel);

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (finalScoreSummaryText != null)
        {
            string heading = levelStart != null
                ? $"Level {levelStart.CurrentLevelIndex + 1} Complete!"
                : "Level Complete!";
            finalScoreSummaryText.text =
                $"{heading}\n" +
                $"Base Score: {data.BaseScore:N0}\n" +
                $"Time Bonus: +{data.TimeBonus:N0}\n" +
                $"Shots Used ({data.ShotsUsed}): -{data.ShotPenalty:N0}\n" +
                $"Final Score: {data.FinalScore:N0}";
        }
    }
}
