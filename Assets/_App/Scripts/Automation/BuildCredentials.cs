using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "BuildCredentials", menuName = "Build/Build Credentials")]
public class BuildCredentials : ScriptableObject
{
    [Header("Android Keystore Settings")]
    [Tooltip("Keystore filename (if in project root) or relative path from project root")]
    public string keystoreRelativePath = "../running_late.keystore";
    
    [Tooltip("Keystore password")]
    public string keystorePassword = "";
    
    [Tooltip("Key alias name")]
    public string keyAliasName = "aldhairvera";
    
    [Tooltip("Key alias password")]
    public string keyAliasPassword = "";
    
    [Header("Build Settings")]
    public bool developmentBuild = false;
    public bool autoIncrementBundleVersion = true;

    public string GetKeystoreFullPath()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string fullPath = Path.Combine(projectRoot, keystoreRelativePath);
        
        fullPath = Path.GetFullPath(fullPath);
        
        return fullPath;
    }

    public bool KeystoreExists()
    {
        return File.Exists(GetKeystoreFullPath());
    }

#if UNITY_EDITOR
    [ContextMenu("Validate Keystore Path")]
    public void ValidateKeystorePath()
    {
        string fullPath = GetKeystoreFullPath();
        
        if (File.Exists(fullPath))
        {
            Debug.Log($"✓ Keystore found at: {fullPath}");
            EditorUtility.DisplayDialog("Keystore Found", $"Keystore exists at:\n{fullPath}", "OK");
        }
        else
        {
            Debug.LogError($"✗ Keystore NOT found at: {fullPath}");
            EditorUtility.DisplayDialog("Keystore Not Found", $"Keystore does not exist at:\n{fullPath}\n\nPlease verify the path.", "OK");
        }
    }

    [ContextMenu("Browse for Keystore")]
    public void BrowseForKeystore()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string selectedPath = EditorUtility.OpenFilePanel("Select Keystore File", projectRoot, "keystore");
        
        if (!string.IsNullOrEmpty(selectedPath))
        {
            if (selectedPath.StartsWith(projectRoot))
            {
                keystoreRelativePath = selectedPath.Substring(projectRoot.Length + 1);
            }
            else
            {
                // If it's outside project root, use absolute path or relative path from root
                keystoreRelativePath = selectedPath; 
            }
            
            keystoreRelativePath = keystoreRelativePath.Replace("\\", "/");
            Debug.Log($"Keystore path set to: {keystoreRelativePath}");
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
