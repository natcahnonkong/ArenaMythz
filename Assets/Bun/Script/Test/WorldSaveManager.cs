using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldSaveManager : MonoBehaviour
{
    [SerializeField] int worldSceneIndex = 1;
    public static WorldSaveManager instanse;

    private void Awake()
    {
        if (instanse == null)
        {
            instanse = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    public IEnumerator LoadNewGame()
    {

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(worldSceneIndex);
        yield return null;
    }

}
