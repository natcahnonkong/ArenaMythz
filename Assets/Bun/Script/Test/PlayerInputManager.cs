using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager instance;

    PlayerController playerControls;
    [SerializeField] Vector2 movementInput;
    [SerializeField] float horizontalInput;
    [SerializeField] float verticalInput;
    private void Awake()   
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        SceneManager.activeSceneChanged += OnSceneChange;
    }

    private void OnSceneChange(Scene oldScene, Scene newScene)
    {

    }
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChange;
    }

}
