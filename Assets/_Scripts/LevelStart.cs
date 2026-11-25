using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelStart : MonoBehaviour
{
    [Header("Level Prefabs (Level_1, Level_2, ...)")]
    [SerializeField] private List<GameObject> levelPrefabs = new List<GameObject>();
    [SerializeField] private int startingLevelIndex = 0;

    [Header("Character Prefabs")]
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private GameObject skeletonEnemyPrefab;

    public int CurrentLevelIndex { get; private set; } = 0;
    private GameObject activeLevelInstance;
    private readonly List<SkeletonEnemy> activeEnemies = new List<SkeletonEnemy>();
    public bool HasNextLevel =>
        CurrentLevelIndex < levelPrefabs.Count - 1;


    private void Start()
    {
        LoadLevel(startingLevelIndex);
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelPrefabs == null || levelPrefabs.Count == 0)
        {
            Debug.LogError("LevelStart: No Level_i prefabs assigned in the Inspector!");
            return;
        }

        if (levelIndex < 0 || levelIndex >= levelPrefabs.Count)
        {
            levelIndex = 0;
        }

        CurrentLevelIndex = levelIndex;
        ClearCurrentLevel();

        activeLevelInstance = Instantiate(levelPrefabs[CurrentLevelIndex], Vector3.zero, Quaternion.identity);
        SpawnCharactersFromMarkers(activeLevelInstance.transform);
        LevelManager.Instance.InitializeLevelState(this, GetRemainingEnemyCount());
    }

    private void SpawnCharactersFromMarkers(Transform levelRoot)
    {
        foreach (Transform child in levelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "MageSpawn")
            {
                SpawnMageAt(child);
            }
            else if (child.name.StartsWith("EnemySpawn_"))
            {
                SpawnEnemyAt(child);
            }
        }
    }

    private void SpawnMageAt(Transform marker)
    {
        if (magePrefab == null) return;

        Vector3 pos = marker.position;
        pos.z = 0f;
        Instantiate(magePrefab, pos, marker.rotation, activeLevelInstance.transform);
    }

    private void SpawnEnemyAt(Transform marker)
    {
        if (skeletonEnemyPrefab == null) return;

        Vector3 pos = marker.position;
        pos.z = 0f;

        GameObject enemyObj = Instantiate(skeletonEnemyPrefab, pos, marker.rotation, activeLevelInstance.transform);
        SkeletonEnemy skeleton = enemyObj.GetComponent<SkeletonEnemy>();
        if (skeleton != null)
        {
            activeEnemies.Add(skeleton);
        }
    }

    public void ClearCurrentLevel()
    {
        activeEnemies.Clear();

        if (activeLevelInstance != null)
        {
            activeLevelInstance.SetActive(false);
            Destroy(activeLevelInstance);
            activeLevelInstance = null;
        }

        BouncySpell[] activeSpells = FindObjectsOfType<BouncySpell>();
        foreach (BouncySpell spell in activeSpells)
        {
            Destroy(spell.gameObject);
        }
    }

    public void RestartLevel()
    {
        LoadLevel(CurrentLevelIndex);
    }

    public void LoadNextLevel()
    {
        if (!HasNextLevel) return;
        LoadLevel(CurrentLevelIndex + 1);
    }

    public void GoToTitle()
    {
        SceneManager.LoadScene("TitleScreen");
    }

    public int GetRemainingEnemyCount()
    {
        int aliveCount = 0;
        foreach (SkeletonEnemy skeleton in activeEnemies)
        {
            if (skeleton != null && !skeleton.IsDead)
            {
                aliveCount++;
            }
        }
        return aliveCount;
    }
}
