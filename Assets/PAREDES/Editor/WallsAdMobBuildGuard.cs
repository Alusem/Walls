using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// ManifestProcessor usa Resources.Load — instância em cache pode ficar vazia (OneDrive).
/// Não chamar GoogleMobileAdsSettings.LoadInstance() via reflexão: leva a SaveAssets() global e
/// erro "Saving Prefab to immutable folder" no PackageCache.
/// </summary>
public sealed class WallsAdMobBuildGuard : IPreprocessBuildWithReport
{
    const string SettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

    const string DefaultAndroidAppId = WallsAdMobConfig.AndroidAppId;
    const string TestIosAppId = WallsAdMobConfig.IosPlaceholderAppId;

    public int callbackOrder => int.MinValue + 1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        OverwriteGoogleMobileAdsSettingsAsset(force: true);
        UnloadGoogleMobileAdsSettingsResourcesCache();
        if (!EnsureAdMobAppIds())
            Debug.LogError("[Wall Rush] Não foi possível corrigir GoogleMobileAdsSettings. Verifica se o ficheiro existe.");
        UnloadGoogleMobileAdsSettingsResourcesCache();
        AssetDatabase.Refresh();
    }

    /// <summary>Limpa cache do Resources para o ManifestProcessor voltar a ler o .asset do disco.</summary>
    static void UnloadGoogleMobileAdsSettingsResourcesCache()
    {
        var loaded = Resources.Load<ScriptableObject>("GoogleMobileAdsSettings");
        if (loaded != null)
            Resources.UnloadAsset(loaded);
    }

    /// <param name="force">Se true (pré-build Android), reescreve sempre — OneDrive costuma repor o ficheiro vazio após o build.</param>
    static void OverwriteGoogleMobileAdsSettingsAsset(bool force)
    {
        var dir = Path.Combine(Application.dataPath, "GoogleMobileAds", "Resources");
        Directory.CreateDirectory(dir);
        var full = Path.Combine(dir, "GoogleMobileAdsSettings.asset");

        if (!force && File.Exists(full))
        {
            var existing = File.ReadAllText(full);
            if (existing.Contains("adMobAndroidAppId: " + DefaultAndroidAppId))
                return;
        }

        var yaml = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n" +
            "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n" +
            "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n" +
            "  m_Script: {fileID: 11500000, guid: a187246822bbb47529482707f3e0eff8, type: 3}\n" +
            "  m_Name: GoogleMobileAdsSettings\n" +
            "  m_EditorClassIdentifier: GoogleMobileAds.Editor::GoogleMobileAds.Editor.GoogleMobileAdsSettings\n" +
            "  adMobAndroidAppId: " + DefaultAndroidAppId + "\n" +
            "  adMobIOSAppId: " + TestIosAppId + "\n" +
            "  enableKotlinXCoroutinesPackagingOption: 1\n  enableGradleBuildPreProcessor: 1\n" +
            "  disableOptimizeInitialization: 0\n  disableOptimizeAdLoading: 0\n" +
            "  userTrackingUsageDescription: \n  userLanguage: en\n";

        File.WriteAllText(full, yaml);
        AssetDatabase.ImportAsset(SettingsPath, ImportAssetOptions.ForceUpdate);
        UnloadGoogleMobileAdsSettingsResourcesCache();
    }

    static void WriteAdMobIdsToSettingsYamlOnDisk()
    {
        OverwriteGoogleMobileAdsSettingsAsset(force: true);
    }

    [MenuItem("Wall Rush/Verificar App ID AdMob (Android)")]
    static void MenuVerify()
    {
        WriteAdMobIdsToSettingsYamlOnDisk();
        UnloadGoogleMobileAdsSettingsResourcesCache();
        EnsureAdMobAppIds();
        UnloadGoogleMobileAdsSettingsResourcesCache();
        if (AssetDatabase.LoadMainAssetAtPath(SettingsPath) != null)
            EditorUtility.DisplayDialog("Wall Rush", "GoogleMobileAdsSettings atualizado no disco e cache do Resources limpa. Tenta o build Android.", "OK");
        else
            EditorUtility.DisplayDialog("Wall Rush", "Falhou: não encontrei GoogleMobileAdsSettings.asset.", "OK");
    }

    static bool EnsureAdMobAppIds()
    {
        var obj = AssetDatabase.LoadMainAssetAtPath(SettingsPath) as ScriptableObject;
        if (obj == null)
            return false;

        var so = new SerializedObject(obj);
        var android = so.FindProperty("adMobAndroidAppId");
        var ios = so.FindProperty("adMobIOSAppId");
        if (android == null)
            return false;

        var changed = false;
        if (string.IsNullOrWhiteSpace(android.stringValue))
        {
            android.stringValue = DefaultAndroidAppId;
            changed = true;
            Debug.LogWarning("[Wall Rush] AdMob Android App ID estava vazio — reposto o ID da app Wall Rush (AdMob).");
        }

        if (ios != null && string.IsNullOrWhiteSpace(ios.stringValue))
        {
            ios.stringValue = TestIosAppId;
            changed = true;
        }

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssetIfDirty(obj);
        }

        return !string.IsNullOrWhiteSpace(android.stringValue);
    }

    [InitializeOnLoad]
    sealed class EnsureSettingsOnEditorLoad
    {
        static EnsureSettingsOnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode)
                    return;
                var full = Path.Combine(Application.dataPath, "GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset");
                if (!File.Exists(full) || !File.ReadAllText(full).Contains(DefaultAndroidAppId))
                    OverwriteGoogleMobileAdsSettingsAsset(force: false);
                else
                    UnloadGoogleMobileAdsSettingsResourcesCache();
                EnsureAdMobAppIds();
                UnloadGoogleMobileAdsSettingsResourcesCache();
            };
        }
    }
}
