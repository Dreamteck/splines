namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(CapsuleColliderGenerator), true)]
    [CanEditMultipleObjects]
    public class CapsuleColliderGeneratorEditor : SplineUserEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        protected override void BodyGUI()
        {
            base.BodyGUI();
            CapsuleColliderGenerator generator = (CapsuleColliderGenerator)target;
            SerializedProperty directionProperty = serializedObject.FindProperty("_direction");
            SerializedProperty heightProperty = serializedObject.FindProperty("_height");
            SerializedProperty radiusProperty = serializedObject.FindProperty("_radius");
            SerializedProperty overlapCapsProperty = serializedObject.FindProperty("_overlapCaps");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(directionProperty);
            CapsuleColliderGenerator.CapsuleColliderZDirection direction = (CapsuleColliderGenerator.CapsuleColliderZDirection)directionProperty.intValue;
            if(direction == CapsuleColliderGenerator.CapsuleColliderZDirection.Z)
            {
                EditorGUILayout.PropertyField(radiusProperty);
                EditorGUILayout.PropertyField(overlapCapsProperty);
            } else
            {
                EditorGUILayout.PropertyField(heightProperty);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}
