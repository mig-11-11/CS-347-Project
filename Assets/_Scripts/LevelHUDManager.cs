using System.Collections;
using System.Collections.Generic;
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

        // Subscribe to future updates
        boundManager.OnLevelChanged += HandleLevelChanged;
        boundManager.OnShotsChanged += HandleShotsChanged;
        boundManager.OnScoreChanged += HandleScoreChanged;
        boundManager.OnTimeChanged += HandleTimeChanged;
        boundManager.OnLevelCompleted += HandleLevelCompleted;

        // Sync initial values in case LevelManager.Start() ran first
        HandleLevelChanged(boundManager.CurrentLevel);
        HandleShotsChanged(boundManager.ShotsRemaining, boundManager.MaxShots);
        HandleScoreChanged(boundManager.CurrentScore);
        HandleTimeChanged(boundManager.ElapsedTime);
    }

    private void OnDestroy()
    {
        if (boundManager == null) return;

        boundManager.OnLevelChanged -= HandleLevelChanged;
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

    private void HandleLevelCompleted(LevelCompleteData data)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (finalScoreSummaryText != null)
        {
            finalScoreSummaryText.text =
                $"Level {data.Level} Complete!\n" +
                $"Base Score: {data.BaseScore:N0}\n" +
                $"Time Bonus: +{data.TimeBonus:N0}\n" +
                $"Shots Used ({data.ShotsUsed}): -{data.ShotPenalty:N0}\n" +
                $"Final Score: {data.FinalScore:N0}";
        }
    }
}