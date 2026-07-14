using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
        {
            return;
        }

        Debug.Log("[BuildPreprocessor] 🛠 Aplicando configuración de firmas para Android...");

        BuildCredentials credentials = FindBuildCredentials();

        if (credentials == null)
        {
            Debug.LogWarning("[BuildPreprocessor] ⚠️ No se encontró el asset BuildCredentials. Usando configuración manual de Unity.");
            return;
        }

        if (!credentials.KeystoreExists())
        {
            Debug.LogError($"[BuildPreprocessor] ❌ Keystore no encontrado en: {credentials.GetKeystoreFullPath()}");
            return;
        }

        ApplyKeystoreSettings(credentials);
        Debug.Log("[BuildPreprocessor] ✅ Configuración aplicada correctamente.");
    }

    private BuildCredentials FindBuildCredentials()
    {
        string[] guids = AssetDatabase.FindAssets("t:BuildCredentials");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<BuildCredentials>(path);
        }
        return null;
    }

    private void ApplyKeystoreSettings(BuildCredentials credentials)
    {
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = credentials.GetKeystoreFullPath();
        PlayerSettings.Android.keystorePass = credentials.keystorePassword;
        PlayerSettings.Android.keyaliasName = credentials.keyAliasName;
        PlayerSettings.Android.keyaliasPass = credentials.keyAliasPassword;
    }
}
