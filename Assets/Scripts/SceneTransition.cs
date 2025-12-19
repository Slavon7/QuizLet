using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    // Метод для перехода на другую сцену по имени
    public void GoToSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Метод для перехода на другую сцену по индексу
    public void GoToSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}