namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(LengthCalculator), true)]
    [CanEditMultipleObjects]
    public class LengthCalculatorEditor : SplineUserEditor
    {
        public override void OnInspectorGUI()
        {
            showAveraging = false;
            base.OnInspectorGUI();
        }

        protected override void BodyGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Length Calculator", EditorStyles.boldLabel);
            base.BodyGUI();
            LengthCalculator calculator = (LengthCalculator)target;
            for (int i = 0; i < targets.Length; i++)
            {
                LengthCalculator lengthCalc = (LengthCalculator)targets[i];
                if (lengthCalc.spline == null) continue;
                EditorGUILayout.HelpBox(lengthCalc.spline.name + " Length: " + lengthCalc.length, MessageType.Info);
            }
            if (targets.Length > 1) return;
            SerializedProperty events = serializedObject.FindProperty("lengthEvents");

            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < events.arraySize; i++)
            {
                SerializedProperty eventProperty = events.GetArrayElementAtIndex(i);
                SerializedProperty onChange = eventProperty.FindPropertyRelative("onChange");
                SerializedProperty enabled = eventProperty.FindPropertyRelative("enabled");
                SerializedProperty targetLength = eventProperty.FindPropertyRelative("targetLength");
                SerializedProperty type = eventProperty.FindPropertyRelative("type");

                EditorGUIUtility.labelWidth = 100;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(enabled, new GUIContent(""), GUILayout.Width(20));
                EditorGUILayout.PropertyField(targetLength);
                EditorGUIUtility.labelWidth = 60;
                EditorGUILayout.PropertyField(type);
                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    Undo.RecordObject(calculator, "Remove Length Event");
                    ArrayUtility.RemoveAt(ref calculator.lengthEvents, i);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.PropertyField(onChange);
                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Add Length Event"))
            {
                Undo.RecordObject(calculator, "Add Length Event");
                ArrayUtility.Add(ref calculator.lengthEvents, new LengthCalculator.LengthEvent());
            }
        }
    }
}
