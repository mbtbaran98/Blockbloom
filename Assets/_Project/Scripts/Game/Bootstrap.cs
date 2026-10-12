using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// Sahneden bagimsiz baslatma: herhangi bir sahnede (bos sahne dahil) Play'e basinca oyunu kodla kurar.
    /// Sahnede zaten bir GameApp varsa (elle eklenmis ya da onceki sahneden DontDestroyOnLoad ile gelmis) yenisini kurmaz.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (GameApp.Instance != null) return;
            var go = new GameObject("Blockbloom");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameApp>();
        }
    }
}
