using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseSingleton : Singleton<BaseSingleton>
{
    [Header("Persistence Settings")]
    [SerializeField] private List<string> persistenceScenes = new List<string>();

    /// <summary>
    /// Override to use the persistenceScenes list for this specific singleton
    /// </summary>
    protected override bool ShouldPersistInCurrentScene()
    {
        // If no specific scenes defined, use default behavior
        if (persistenceScenes == null || persistenceScenes.Count == 0)
        {
            return base.ShouldPersistInCurrentScene();
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        bool shouldPersist = persistenceScenes.Contains(currentSceneName);
        
        // Debug.Log($"BaseSingleton: Current scene '{currentSceneName}' - Should persist: {shouldPersist}");
        
        return shouldPersist;
    }

    /// <summary>
    /// Add a scene to this singleton's persistence list
    /// </summary>
    public void AddPersistenceScene(string sceneName)
    {
        if (persistenceScenes == null)
            persistenceScenes = new List<string>();

        if (!persistenceScenes.Contains(sceneName))
        {
            persistenceScenes.Add(sceneName);
        }
    }

    /// <summary>
    /// Remove a scene from this singleton's persistence list
    /// </summary>
    public void RemovePersistenceScene(string sceneName)
    {
        if (persistenceScenes != null)
        {
            persistenceScenes.Remove(sceneName);
        }
    }
}