using UnityEditor;
using UnityEditor.SceneManagement;

namespace NightTrolleybus.EditorTools
{
    /// <summary>Play у редакторі завжди стартує з Boot: без неї сцени змісту не мають систем.</summary>
    [InitializeOnLoad]
    public static class PlayFromBoot
    {
        static PlayFromBoot()
        {
            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(BuilderTools.Root + "/Scenes/Boot.unity");
        }
    }
}
