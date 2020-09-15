using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{

    public void LoadByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(1);
    }
}