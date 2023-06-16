namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(SplineProjector), true)]
    [CanEditMultipleObjects]
    public class SplineProjectorEditor : SplineTracerEditor
    {
        private bool info = false;

        public override void OnInspectorGUI()
        {
            SplineProjector user = (SplineProjector)target;
            if (user.mode == SplineProjector.Mode.Accurate)
            {
                showAveraging = false;
            }
            else
            {
                showAveraging = true;
            }
            base.OnInspectorGUI();
        }

        protected override void BodyGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Projector", EditorStyles.boldLabel);

            serializedObject.Update();
            SerializedProperty mode = serializedObject.FindProperty("_mode");
            SerializedProperty projectTarget = serializedObject.FindProperty("_projectTarget");
            SerializedProperty targetObject = serializedObject.FindProperty("_targetObject");
            SerializedProperty autoProject = serializedObject.FindProperty("_autoProject");


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));
            if (mode.intValue == (int)SplineProjector.Mode.Accurate)
            {
                SerializedProperty subdivide = serializedObject.FindProperty("_subdivide");
                EditorGUILayout.PropertyField(subdivide, new GUIContent("Subdivide"));
            }
            EditorGUILayout.PropertyField(projectTarget, new GUIContent("Project Target"));
            EditorGUILayout.PropertyField(targetObject, new GUIContent("Apply Target"));

            GUI.color = Color.white;
            EditorGUILayout.PropertyField(autoProject, new GUIContent("Auto Project"));

            info = EditorGUILayout.Foldout(info, "Info");
            SerializedProperty percent = serializedObject.FindProperty("_result").FindPropertyRelative("percent");
            if (info) EditorGUILayout.HelpBox("Projection percent: " + percent.floatValue, MessageType.Info);

            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            base.BodyGUI();
        }

        protected override void DuringSceneGUI(SceneView currentSceneView)
        {
            base.DuringSceneGUI(currentSceneView);
            for (int i = 0; i < users.Length; i++)
            {
                SplineProjector user = (SplineProjector)users[i];
                if (user.spline == null) return;
                if (!user.autoProject) return;
                DrawResult(user.result);
            }
        }
    }
}
