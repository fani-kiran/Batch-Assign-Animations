using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimationAssigner : EditorWindow
{
    private Animation targetAnimationComponent;

    [MenuItem("Tools/Batch Assign Animations")]
    public static void ShowWindow()
    {
        GetWindow<AnimationAssigner>("Anim Assigner");
    }

    void OnGUI()
    {
        GUILayout.Label("Batch Assign Animations", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetAnimationComponent = (Animation)EditorGUILayout.ObjectField(
            "Target Component", 
            targetAnimationComponent, 
            typeof(Animation), 
            true
        );

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("1. Select your FBX files in the Project Window.\n2. Click the button below.", MessageType.Info);

        if (GUILayout.Button("Assign Selected Clips"))
        {
            AssignClips();
        }
    }

    void AssignClips()
    {
        if (targetAnimationComponent == null)
        {
            if (Selection.activeGameObject != null)
                targetAnimationComponent = Selection.activeGameObject.GetComponent<Animation>();

            if (targetAnimationComponent == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Target Animation Component first.", "OK");
                return;
            }
        }

        // V2 CHANGE: Use Selection.objects and manually load assets from paths
        // This is more robust than GetFiltered for embedded FBX clips
        Object[] selectedRawObjects = Selection.objects;
        List<AnimationClip> clipsFound = new List<AnimationClip>();

        foreach (Object obj in selectedRawObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            // Skip if it's not a file (e.g. it's a folder) or invalid
            if (string.IsNullOrEmpty(path)) continue;

            // Force load all assets inside this file path
            Object[] assetsAtFile = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (Object asset in assetsAtFile)
            {
                if (asset is AnimationClip clip)
                {
                    // Filter out internal Unity previews
                    if (!clip.name.StartsWith("__"))
                    {
                        clipsFound.Add(clip);
                    }
                }
            }
        }

        if (clipsFound.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Still no clips found!\n\nPlease check the 'Troubleshooting' steps in the instructions (Verify Import Settings).", "OK");
            return;
        }

        // Proceed to assign
        SerializedObject so = new SerializedObject(targetAnimationComponent);
        SerializedProperty animationsProp = so.FindProperty("m_Animations");

        int addedCount = 0;

        foreach (AnimationClip clip in clipsFound)
        {
            if (!IsClipInList(animationsProp, clip))
            {
                int index = animationsProp.arraySize;
                animationsProp.InsertArrayElementAtIndex(index);
                SerializedProperty element = animationsProp.GetArrayElementAtIndex(index);
                element.objectReferenceValue = clip;
                addedCount++;
            }
        }

        if (targetAnimationComponent.clip == null && clipsFound.Count > 0)
        {
             targetAnimationComponent.clip = clipsFound[0];
        }

        so.ApplyModifiedProperties();
        
        Debug.Log($"<color=green>Success!</color> Found {clipsFound.Count} clips in selection. Added {addedCount} new clips to {targetAnimationComponent.gameObject.name}.");
    }

    private bool IsClipInList(SerializedProperty list, AnimationClip clipToCheck)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue == clipToCheck) return true;
        }
        return false;
    }
}