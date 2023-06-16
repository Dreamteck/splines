namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(WaveformGenerator), true)]
    [CanEditMultipleObjects]
    public class WaveGeneratorEditor : MeshGenEditor
    {
        protected override void BodyGUI()
        {
            showSize = false;
            showRotation = false;
            base.BodyGUI();
            WaveformGenerator user = (WaveformGenerator)target;

            serializedObject.Update();
            SerializedProperty axis = serializedObject.FindProperty("_axis");
            SerializedProperty slices = serializedObject.FindProperty("_slices");
            SerializedProperty symmetry = serializedObject.FindProperty("_symmetry");
            SerializedProperty uvWrapMode = serializedObject.FindProperty("_uvWrapMode");
            SerializedProperty uvOffset = serializedObject.FindProperty("_uvOffset");
            SerializedProperty uvScale = serializedObject.FindProperty("_uvScale");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Axis", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(axis, new GUIContent("Axis"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(slices, new GUIContent("Slices"));
            if (slices.intValue < 1) slices.intValue = 1;

            EditorGUILayout.PropertyField(symmetry, new GUIContent("Use Symmetry"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(uvWrapMode, new GUIContent("Wrap Mode"));
            EditorGUILayout.PropertyField(uvOffset, new GUIContent("UV Offset"));
            EditorGUILayout.PropertyField(uvScale, new GUIContent("UV Scale"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}

