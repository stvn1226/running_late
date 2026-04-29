using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.IO;

public class AutoBuildWindow : EditorWindow
{
    private BuildCredentials credentials;
    private string lastBuildPath = "Builds/Android/";
    private bool showPasswords = false;

    [MenuItem("Build/Auto Build Manager")]
    public static void ShowWindow()
    {
        GetWindow<AutoBuildWindow>("Auto Build Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto Build Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        credentials = (BuildCredentials)EditorGUILayout.ObjectField(
            "Build Credentials", 
            credentials, 
            typeof(BuildCredentials), 
            false
        );

        if (credentials == null)
        {
            EditorGUILayout.HelpBox(
                "Create BuildCredentials asset: Right-click in Project → Create → Build → Build Credentials", 
                MessageType.Info
            );
            return;
        }

        EditorGUILayout.Space();
        
        DrawKeystoreInfo();
        
        EditorGUILayout.Space();
        GUILayout.Label("Build Settings", EditorStyles.boldLabel);
        
        lastBuildPath = EditorGUILayout.TextField("Output Path", lastBuildPath);
        
        EditorGUILayout.Space();
        GUILayout.Label("Credentials Preview", EditorStyles.boldLabel);
        
        showPasswords = EditorGUILayout.Toggle("Show Passwords", showPasswords);
        
        GUI.enabled = false;
        EditorGUILayout.TextField("Keystore Relative Path", credentials.keystoreRelativePath);
        EditorGUILayout.TextField("Keystore Full Path", credentials.GetKeystoreFullPath());
        EditorGUILayout.TextField("Keystore Pass", showPasswords ? credentials.keystorePassword : "********");
        EditorGUILayout.TextField("Key Alias", credentials.keyAliasName);
        EditorGUILayout.TextField("Alias Pass", showPasswords ? credentials.keyAliasPassword : "********");
        GUI.enabled = true;

        EditorGUILayout.Space();

        GUI.enabled = credentials.KeystoreExists();
        
        if (GUILayout.Button("Build Android APK", GUILayout.Height(40)))
        {
            BuildAndroid(false);
        }
        
        if (GUILayout.Button("Build Android AAB (Google Play)", GUILayout.Height(40)))
        {
            BuildAndroid(true);
        }

        GUI.enabled = true;

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Validate Keystore"))
        {
            credentials.ValidateKeystorePath();
        }
        
        if (GUILayout.Button("Browse Keystore"))
        {
            credentials.BrowseForKeystore();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Open Build Credentials"))
        {
            Selection.activeObject = credentials;
            EditorGUIUtility.PingObject(credentials);
        }
    }

    private void DrawKeystoreInfo()
    {
        bool keystoreExists = credentials.KeystoreExists();
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        GUILayout.Label("Keystore Status", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Status:", GUILayout.Width(60));
        
        GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
        statusStyle.normal.textColor = keystoreExists ? Color.green : Color.red;
        statusStyle.fontStyle = FontStyle.Bold;
        
        GUILayout.Label(keystoreExists ? "✓ Found" : "✗ Not Found", statusStyle);
        EditorGUILayout.EndHorizontal();
        
        if (!keystoreExists)
        {
            EditorGUILayout.HelpBox(
                $"Keystore not found at:\n{credentials.GetKeystoreFullPath()}\n\nClick 'Browse Keystore' to locate it.", 
                MessageType.Warning
            );
        }
        
        EditorGUILayout.EndVertical();
    }

    private void BuildAndroid(bool buildAAB)
    {
        if (!ValidateCredentials())
        {
            EditorUtility.DisplayDialog("Error", "Invalid credentials. Please check BuildCredentials asset.", "OK");
            return;
        }

        if (!credentials.KeystoreExists())
        {
            EditorUtility.DisplayDialog(
                "Keystore Not Found", 
                $"Keystore file not found at:\n{credentials.GetKeystoreFullPath()}\n\nPlease verify the path and try again.", 
                "OK"
            );
            return;
        }

        try
        {
            ApplyKeystoreSettings();
            
            if (credentials.autoIncrementBundleVersion)
            {
                IncrementBundleVersion();
            }

            string fileName = buildAAB ? 
                $"{Application.productName}_{PlayerSettings.bundleVersion}.aab" : 
                $"{Application.productName}_{PlayerSettings.bundleVersion}.apk";
            
            string fullPath = Path.Combine(lastBuildPath, fileName);
            
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = fullPath,
                target = BuildTarget.Android,
                options = credentials.developmentBuild ? BuildOptions.Development : BuildOptions.None
            };

            EditorUserBuildSettings.buildAppBundle = buildAAB;

            Debug.Log($"Starting Android build: {fullPath}");
            Debug.Log($"Using keystore: {credentials.GetKeystoreFullPath()}");
            
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {fullPath}");
                EditorUtility.DisplayDialog(
                    "Build Successful", 
                    $"Build completed successfully!\n\nPath: {fullPath}\nSize: {FormatBytes(report.summary.totalSize)}", 
                    "OK"
                );
                
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                Debug.LogError($"Build failed: {report.summary.result}");
                EditorUtility.DisplayDialog("Build Failed", $"Build failed with result: {report.summary.result}", "OK");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Build error: {e.Message}");
            EditorUtility.DisplayDialog("Build Error", $"An error occurred:\n{e.Message}", "OK");
        }
    }

    private void ApplyKeystoreSettings()
    {
        string keystorePath = credentials.GetKeystoreFullPath();
    
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = credentials.keystorePassword;
        PlayerSettings.Android.keyaliasName = credentials.keyAliasName;
        PlayerSettings.Android.keyaliasPass = credentials.keyAliasPassword;
    
        Debug.Log($"[AutoBuildWindow] Keystore settings applied:");
        Debug.Log($"  - Path: {keystorePath}");
        Debug.Log($"  - Alias: {credentials.keyAliasName}");
    }


    private bool ValidateCredentials()
    {
        if (credentials == null) return false;
        if (string.IsNullOrEmpty(credentials.keystoreRelativePath)) return false;
        if (string.IsNullOrEmpty(credentials.keystorePassword)) return false;
        if (string.IsNullOrEmpty(credentials.keyAliasName)) return false;
        if (string.IsNullOrEmpty(credentials.keyAliasPassword)) return false;
        
        return true;
    }

    private void IncrementBundleVersion()
    {
        string currentVersion = PlayerSettings.bundleVersion;
        string[] parts = currentVersion.Split('.');
        
        if (parts.Length >= 3)
        {
            if (int.TryParse(parts[2], out int patch))
            {
                patch++;
                PlayerSettings.bundleVersion = $"{parts[0]}.{parts[1]}.{patch}";
                Debug.Log($"Bundle version incremented to {PlayerSettings.bundleVersion}");
            }
        }
    }

    private string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }
        return scenes.ToArray();
    }

    private string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
