using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>  
{
    private static T instance;
    public static T Instance { get { return instance; } }

    protected virtual void Awake() {
        if (instance != null && this.gameObject != null) {
            Destroy(this.gameObject);
        } else {
            instance = (T)this;
        }

        // Subscribe to scene loaded events
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Check if this singleton should persist based on current scene
        if (!gameObject.transform.parent && ShouldPersistInCurrentScene()) {
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Handle scene loading events. Override in derived classes for custom behavior.
    /// </summary>
    /// <param name="scene">The loaded scene</param>
    /// <param name="mode">The load mode</param>
    protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Default behavior: check if we should continue to persist
        CheckPersistenceOnSceneChange();
    }

    /// <summary>
    /// Override this method in derived classes to control when the singleton should persist
    /// Default behavior: always persist (maintains original functionality)
    /// </summary>
    /// <returns>True if the singleton should persist across scenes</returns>
    protected virtual bool ShouldPersistInCurrentScene()
    {
        return true; // Default behavior - always persist
    }

    /// <summary>
    /// Call this when scene changes to check if singleton should continue to persist
    /// </summary>
    protected void CheckPersistenceOnSceneChange()
    {
        if (!ShouldPersistInCurrentScene())
        {
            // If we shouldn't persist in the new scene, destroy this instance
            if (instance == this)
            {
                instance = null;
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Clean up event subscriptions when the singleton is destroyed
    /// </summary>
    protected virtual void OnDestroy()
    {
        // Unsubscribe from scene events to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Clear the instance if this was the active instance
        if (instance == this)
        {
            instance = null;
        }
    }
}