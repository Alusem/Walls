#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Garante que, ao premir Play no Editor, o jogo arranca sempre na cena Menu
/// (independentemente da cena aberta no Hierarchy).
/// </summary>
[InitializeOnLoad]
public static class WallsPlayModeStartMenu
{
    const string MenuScenePath = "Assets/PAREDES/Scenes/Menu.unity";

    static WallsPlayModeStartMenu()
    {
        ApplyPlayModeStartScene();
    }

    static void ApplyPlayModeStartScene()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
        if (scene == null)
        {
            Debug.LogWarning($"[Wall Rush] Cena do menu não encontrada em {MenuScenePath}. Play Mode Start Scene não foi definida.");
            return;
        }

        if (EditorSceneManager.playModeStartScene != scene)
            EditorSceneManager.playModeStartScene = scene;
    }
}
#endif
