using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Settings")]
    [Tooltip("Maximum fireballs the mage can cast in this level")]
    [SerializeField] private int maxShotsForLevel = 5;

    [Header("Scoring & Bonus Settings")]
    [SerializeField] private int skeletonKillPoints = 100;
    // [SerializeField] private float comboWindow = 1.2f;
    // [SerializeField] private float comboBonusStep = 0.5f;
    [Tooltip("Clearing faster than this time (in seconds) awards bonus points")]
    [SerializeField] private float parTimeSeconds = 30f;
    [SerializeField] private int timeBonusPerSecond = 25;
    [SerializeField] private int shotPenaltyPoints = 50;

    // Events for UI or audio listeners
    public event Action<int> OnScoreChanged;
    public event Action<float> OnTimeChanged;
    public event Action<int, int> OnShotsChanged; // (shotsRemaining, maxShots)
    public event Action<LevelCompleteData> OnLevelCompleted;

    private int currentScore = 0;
    private float elapsedTime = 0f;
    private int shotsTaken = 0;
    private int remainingSkeletons = 0;

    private bool isLevelRunning = true;
    private bool isLevelComplete = false;

    // private int comboCount = 0;
    private float lastKillTime = -10f;

    public int CurrentScore => currentScore;
    public float ElapsedTime => elapsedTime;
    public int MaxShots => maxShotsForLevel;
    public int ShotsTaken => shotsTaken;
    public int ShotsRemaining => Mathf.Max(0, maxShotsForLevel - shotsTaken);
    public bool CanShoot => isLevelRunning && !isLevelComplete && shotsTaken < maxShotsForLevel;
    public bool IsLevelComplete => isLevelComplete;

    private void Awake()
    {
        // The outgoing level is deactivated before its deferred destruction.
        if (Instance != null && Instance != this && Instance.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void InitializeLevelState(int enemyCount)
    {
        remainingSkeletons = enemyCount;

        currentScore = 0;
        elapsedTime = 0f;
        shotsTaken = 0;
        isLevelComplete = false;
        isLevelRunning = true;

        OnScoreChanged?.Invoke(currentScore);
        OnTimeChanged?.Invoke(elapsedTime);
        OnShotsChanged?.Invoke(ShotsRemaining, maxShotsForLevel);
    }

    private void Update()
    {
        if (!isLevelRunning || isLevelComplete) return;

        elapsedTime += Time.deltaTime;
        OnTimeChanged?.Invoke(elapsedTime);
    }

    /// <summary>
    /// Call from the mage controller before casting a fireball.
    /// Returns true and consumes 1 shot if shots remain; returns false otherwise.
    /// </summary>
    public bool TryConsumeShot()
    {
        if (!CanShoot) return false;

        shotsTaken++;
        OnShotsChanged?.Invoke(ShotsRemaining, maxShotsForLevel);
        return true;
    }

    /// <summary>
    /// Call when a skeleton dies. Awards combo-scaled points and ends the level when none remain.
    /// </summary>
    public void RegisterSkeletonKill()
    {
        if (isLevelComplete) return;

        /*if (Time.time - lastKillTime <= comboWindow)
        {
            comboCount++;
        }
        else
        {
            comboCount = 1;
        }*/

        lastKillTime = Time.time;

        // float multiplier = 1f + (comboCount - 1) * comboBonusStep;
        float multiplier = 1;
        int awardedPoints = Mathf.RoundToInt(skeletonKillPoints * multiplier);
        AddScore(awardedPoints);

        remainingSkeletons = Mathf.Max(0, remainingSkeletons - 1);
        if (remainingSkeletons == 0)
        {
            CompleteLevel();
        }
    }

    public void AddScore(int points)
    {
        currentScore = Mathf.Max(0, currentScore + points);
        OnScoreChanged?.Invoke(currentScore);
    }

    public void CompleteLevel()
    {
        if (isLevelComplete) return;

        isLevelComplete = true;
        isLevelRunning = false;

        int baseScore = currentScore;
        int wholeSecondsSaved = Mathf.Max(0, Mathf.FloorToInt(parTimeSeconds - elapsedTime));
        int timeBonus = wholeSecondsSaved * timeBonusPerSecond;
        int shotPenalty = shotsTaken * shotPenaltyPoints;

        currentScore = Mathf.Max(0, baseScore + timeBonus - shotPenalty);

        OnScoreChanged?.Invoke(currentScore);
        OnTimeChanged?.Invoke(elapsedTime);

        OnLevelCompleted?.Invoke(new LevelCompleteData
        {
            BaseScore = baseScore,
            TimeBonus = timeBonus,
            ShotsUsed = shotsTaken,
            ShotPenalty = shotPenalty,
            FinalScore = currentScore,
            ElapsedTime = elapsedTime
        });
    }
}

public struct LevelCompleteData
{
    public int BaseScore;
    public int TimeBonus;
    public int ShotsUsed;
    public int ShotPenalty;
    public int FinalScore;
    public float ElapsedTime;
}
