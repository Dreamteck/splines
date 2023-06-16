namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(PolygonColliderGenerator))]
    [CanEditMultipleObjects]
    public class PolygonColliderGenEditor : SplineUserEditor
    {
        protected override void BodyGUI()
        {
            base.BodyGUI();
            PolygonColliderGenerator generator = (PolygonColliderGenerator)target;

            serializedObject.Update();
            SerializedProperty type = serializedObject.FindProperty("_type");
            SerializedProperty size = serializedObject.FindProperty("_size");
            SerializedProperty offset = serializedObject.FindProperty("_offset");
            SerializedProperty updateRate = serializedObject.FindProperty("updateRate");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Polygon", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(type, new GUIContent("Type"));
            if (type.intValue == (int)PolygonColliderGenerator.Type.Path) EditorGUILayout.PropertyField(size, new GUIContent("Size"));
            EditorGUILayout.PropertyField(offset, new GUIContent("Offset"));
            EditorGUILayout.PropertyField(updateRate);
            if (updateRate.floatValue < 0f) updateRate.floatValue = 0f;
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
        }
        
    }
}

