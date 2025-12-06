using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(PlayerConfig))]
public class PlayerConfigEditor : Editor
{
    private string searchText = "";
    private Vector2 scrollPosition;
    private List<SerializedProperty> allProperties;
    private bool isInitialized = false;

    private void OnEnable()
    {
        CacheAllProperties();
    }

    private void CacheAllProperties()
    {
        allProperties = new List<SerializedProperty>();
        SerializedProperty iterator = serializedObject.GetIterator();
        
        if (iterator.NextVisible(true))
        {
            do
            {
                if (iterator.name != "m_Script")
                {
                    allProperties.Add(serializedObject.FindProperty(iterator.name));
                }
            }
            while (iterator.NextVisible(false));
        }
        
        isInitialized = true;
    }

    public override void OnInspectorGUI()
    {
        if (!isInitialized)
        {
            CacheAllProperties();
        }

        serializedObject.Update();

        DrawScriptField();
        EditorGUILayout.Space(10);
        DrawSearchBox();
        EditorGUILayout.Space(5);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (string.IsNullOrEmpty(searchText))
        {
            DrawDefaultInspector();
        }
        else
        {
            DrawFilteredProperties();
        }

        EditorGUILayout.EndScrollView();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        GUI.enabled = false;
        SerializedProperty scriptProp = serializedObject.FindProperty("m_Script");
        EditorGUILayout.PropertyField(scriptProp);
        GUI.enabled = true;
    }

    private void DrawSearchBox()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUIStyle searchFieldStyle = new GUIStyle(GUI.skin.textField);
        searchFieldStyle.fontSize = 12;
        
        string newSearchText = EditorGUILayout.TextField("🔍 Search", searchText, searchFieldStyle);
        
        if (newSearchText != searchText)
        {
            searchText = newSearchText;
            Repaint();
        }

        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchText = "";
            GUI.FocusControl(null);
            Repaint();
        }
        
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(searchText))
        {
            int matchCount = GetMatchingPropertiesCount();
            EditorGUILayout.HelpBox($"Found {matchCount} matching field(s)", MessageType.Info);
        }
    }

    private void DrawFilteredProperties()
    {
        bool foundAny = false;
        string lowerSearchText = searchText.ToLower();

        foreach (var prop in allProperties)
        {
            string propertyName = prop.displayName.ToLower();
            string fieldName = prop.name.ToLower();
            
            string tooltip = GetTooltipForProperty(prop);
            string tooltipLower = tooltip.ToLower();

            bool nameMatch = propertyName.Contains(lowerSearchText) || fieldName.Contains(lowerSearchText);
            bool tooltipMatch = !string.IsNullOrEmpty(tooltip) && tooltipLower.Contains(lowerSearchText);

            if (nameMatch || tooltipMatch)
            {
                foundAny = true;
                
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f, 0.3f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = originalColor;

                EditorGUILayout.PropertyField(prop, true);

                if (!string.IsNullOrEmpty(tooltip))
                {
                    EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }

        if (!foundAny)
        {
            EditorGUILayout.HelpBox("No fields match your search.", MessageType.Warning);
        }
    }

    private int GetMatchingPropertiesCount()
    {
        if (string.IsNullOrEmpty(searchText)) return 0;
        
        int count = 0;
        string lowerSearchText = searchText.ToLower();

        foreach (var prop in allProperties)
        {
            string propertyName = prop.displayName.ToLower();
            string fieldName = prop.name.ToLower();
            string tooltip = GetTooltipForProperty(prop).ToLower();

            if (propertyName.Contains(lowerSearchText) || 
                fieldName.Contains(lowerSearchText) || 
                tooltip.Contains(lowerSearchText))
            {
                count++;
            }
        }

        return count;
    }

    private string GetTooltipForProperty(SerializedProperty property)
    {
        FieldInfo field = target.GetType().GetField(property.name, 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        if (field != null)
        {
            TooltipAttribute tooltip = field.GetCustomAttribute<TooltipAttribute>();
            if (tooltip != null)
            {
                return tooltip.tooltip;
            }
        }
        
        return string.Empty;
    }
}
