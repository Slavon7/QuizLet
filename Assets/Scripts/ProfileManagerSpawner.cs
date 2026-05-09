using UnityEngine;

public class ProfileManagerSpawner : MonoBehaviour
{
    public GameObject profileManagerPrefab;

    void Awake()
    {
        if (ProfileManager.Instance == null)
        {
            Instantiate(profileManagerPrefab);
        }
    }
}