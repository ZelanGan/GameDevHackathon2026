using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NightTrolleybus.Core
{
    /// <summary>Тримає поверх Boot одну сцену змісту й замінює її адитивно.</summary>
    public class SceneLoader : MonoBehaviour
    {
        private string currentScene;
        private Coroutine loading;

        public bool IsLoading => loading != null;

        public void Load(string sceneName)
        {
            if (loading != null)
                StopCoroutine(loading);

            loading = StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            if (!string.IsNullOrEmpty(currentScene) && SceneManager.GetSceneByName(currentScene).isLoaded)
                yield return SceneManager.UnloadSceneAsync(currentScene);

            currentScene = sceneName;
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            loading = null;
        }
    }
}
