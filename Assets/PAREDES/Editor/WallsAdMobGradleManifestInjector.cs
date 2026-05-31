#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Depois de gerar o projeto Gradle, garante com.google.android.gms.ads.APPLICATION_ID no
/// unityLibrary e no launcher. Isto sobrevive a GoogleMobileAdsSettings.asset vazio (ex.: OneDrive)
/// e a falhas de merge do manifest em Plugins/Android.
/// </summary>
public sealed class WallsAdMobGradleManifestInjector : IPostGenerateGradleAndroidProject
{
    const string MetaName = "com.google.android.gms.ads.APPLICATION_ID";

    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        var appId = WallsAdMobConfig.AndroidAppId;
        var unityLibManifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        InjectIfNeeded(unityLibManifest, appId);

        var parent = Directory.GetParent(path)?.FullName;
        if (string.IsNullOrEmpty(parent))
            return;
        var launcherManifest = Path.Combine(parent, "launcher", "src", "main", "AndroidManifest.xml");
        InjectIfNeeded(launcherManifest, appId);
    }

    static void InjectIfNeeded(string manifestPath, string appId)
    {
        if (!File.Exists(manifestPath))
            return;

        try
        {
            var doc = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
            XNamespace ns = "http://schemas.android.com/apk/res/android";
            var root = doc.Root;
            if (root == null)
                return;

            var application = root.Elements().FirstOrDefault(e => e.Name.LocalName == "application");
            if (application == null)
            {
                application = new XElement("application");
                root.Add(application);
            }

            XElement meta = null;
            foreach (var el in application.Elements())
            {
                if (el.Name.LocalName != "meta-data")
                    continue;
                var nameAttr = el.Attribute(ns + "name");
                if (nameAttr != null && nameAttr.Value == MetaName)
                {
                    meta = el;
                    break;
                }
            }

            if (meta != null)
            {
                meta.SetAttributeValue(ns + "value", appId);
            }
            else
            {
                application.AddFirst(new XElement("meta-data",
                    new XAttribute(ns + "name", MetaName),
                    new XAttribute(ns + "value", appId)));
            }

            var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Indent = true };
            using (var writer = XmlWriter.Create(manifestPath, settings))
                doc.Save(writer);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Wall Rush] Falha ao injetar APPLICATION_ID em {manifestPath}: {e.Message}");
        }
    }
}
#endif // UNITY_EDITOR
