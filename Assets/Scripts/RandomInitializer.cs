using UnityEngine;

public class RandomInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeRandom()
    {
        Random.InitState(System.Environment.TickCount + System.DateTime.Now.Millisecond);
        Debug.Log("Random initialized for build");
    }
}