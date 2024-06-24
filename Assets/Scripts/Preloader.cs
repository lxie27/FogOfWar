using UnityEngine;
using UnityEngine.SceneManagement;

public class Preloader : MonoBehaviour
{
    void Awake()
    {
        Application.targetFrameRate = 144;

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-launch-as-server")
            {
                LaunchHeadlessMode();
                return;
            }
        }
        LaunchAsLocalServer();
    }

    void LaunchHeadlessMode()
    {
        Debug.Log("Launching as Headless mode server");
        SceneManager.LoadScene("HeadlessServer");
    }

    void LaunchAsLocalServer()
    {
        Debug.Log("Launching as Local mode server");
        SceneManager.LoadScene("LocalServer");
    }
}
