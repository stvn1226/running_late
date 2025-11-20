using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AutoSceneTransition : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [Tooltip("Time in seconds before transitioning to the next scene")]
    [SerializeField] private float delayInSeconds = 3f;
    
    [Tooltip("Name of the scene to load (must match the scene name in Build Settings)")]
    [SerializeField] private string targetSceneName = "MainGame";

    void Start()
    {
        StartCoroutine(TransitionAfterDelay());
    }

    private IEnumerator TransitionAfterDelay()
    {
        yield return new WaitForSeconds(delayInSeconds);
        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("Target scene name is not set in AutoSceneTransition script!");
        }
    }
}
