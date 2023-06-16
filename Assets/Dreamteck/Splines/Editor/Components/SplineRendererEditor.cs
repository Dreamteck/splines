namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(SplineRenderer), true)]
    [CanEditMultipleObjects]
    public class SplineRendererEditor : MeshGenEditor
    {
        protected override void BodyGUI()
        {
            showDoubleSided = false;
            showFlipFaces = false;
            showRotation = false;
            showNormalMethod = false;

            serializedObject.Update();
            SerializedProperty slices = serializedObject.FindProperty("_slices");
            SerializedProperty autoOrient = serializedObject.FindProperty("autoOrient");
            SerializedProperty updateFrameInterval = serializedObject.FindProperty("updateFrameInterval");

            base.BodyGUI();
            EditorGUI.BeginChangeCheck();
            SplineRenderer user = (SplineRenderer)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(slices);
            if (slices.intValue < 1) slices.intValue = 1;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Render", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoOrient);
            if (user.autoOrient)
            {
                EditorGUILayout.PropertyField(updateFrameInterval);
                if (updateFrameInterval.intValue < 0) updateFrameInterval.intValue = 0; 
            }

            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            UVControls(user);
        }

    }
}
