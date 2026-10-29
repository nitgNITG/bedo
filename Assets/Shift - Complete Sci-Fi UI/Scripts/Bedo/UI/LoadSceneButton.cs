
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class LoadSceneButton : MonoBehaviour
{
    
  
    public void LoadSceneWithName(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LoadSceneWithNameAfterDelay(string sceneName)
    {
        StartCoroutine (LoadSceneWithNameAfterDelay( sceneName, 1f));
    }

    public IEnumerator LoadSceneWithNameAfterDelay(string sceneName , float timeInSeconds)
    { 
        
        yield return new WaitForSecondsRealtime(timeInSeconds);
        LoadSceneWithName(sceneName);
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex,
            LoadSceneMode.Single
        );
    }

    public void QuitAfterDelay()
    {
        StartCoroutine(QuitAfterDelay(1f));
    }

    public IEnumerator QuitAfterDelay( float timeInSeconds)
    {

        yield return new WaitForSeconds(timeInSeconds);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Quit()
    {

      

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
