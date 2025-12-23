using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene.StartScene
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            SceneManager.LoadScene("StartScene");
        }
    }
}