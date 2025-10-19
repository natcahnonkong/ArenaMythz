using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TitleScreenManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void StartNewGame()
    {
        StartCoroutine(WorldSaveManager.instanse.LoadNewGame());
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
