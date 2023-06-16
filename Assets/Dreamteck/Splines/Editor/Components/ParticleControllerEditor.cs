namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(ParticleController))]
    [CanEditMultipleObjects]
    public class ParticleControllerEditor : SplineUserEditor
    {
        protected override void BodyGUI()
        {
            base.BodyGUI();
            ParticleController user = (ParticleController)target;

            serializedObject.Update();
            SerializedProperty _particleSystem = serializedObject.FindProperty("_particleSystem");

            SerializedProperty emitPoint = serializedObject.FindProperty("emitPoint");
            SerializedProperty offset = serializedObject.FindProperty("offset");
            SerializedProperty volumetric = serializedObject.FindProperty("volumetric");
            SerializedProperty pauseWhenNotVisible = serializedObject.FindProperty("pauseWhenNotVisible");
            SerializedProperty applyRotation = serializedObject.FindProperty("apply3DRotation");
            SerializedProperty emitFromShell = serializedObject.FindProperty("emitFromShell");
            SerializedProperty scale = serializedObject.FindProperty("scale");
            SerializedProperty motionType = serializedObject.FindProperty("motionType");
            SerializedProperty wrapMode = serializedObject.FindProperty("wrapMode");
            SerializedProperty minCycles = serializedObject.FindProperty("minCycles");
            SerializedProperty maxCycles = serializedObject.FindProperty("maxCycles");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_particleSystem, new GUIContent("Particle System"));
            if (_particleSystem.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No particle system is assigned", MessageType.Error);
                return;
            }
            EditorGUILayout.PropertyField(pauseWhenNotVisible);
            EditorGUILayout.PropertyField(emitPoint);
            EditorGUILayout.PropertyField(offset);
            EditorGUILayout.PropertyField(applyRotation);
            EditorGUILayout.PropertyField(volumetric);
            if (volumetric.boolValue)
            {
                EditorGUILayout.PropertyField(emitFromShell);
                EditorGUILayout.PropertyField(scale);
            }
            EditorGUILayout.PropertyField(motionType);
            if(motionType.intValue == (int)ParticleController.MotionType.FollowForward || motionType.intValue == (int)ParticleController.MotionType.FollowBackward)
            {
                EditorGUILayout.PropertyField(wrapMode);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Path cycles (over " + user.particleSystemComponent.main.startLifetime.constantMax + "s.)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(minCycles, new GUIContent("Min. Cycles"));
                if (minCycles.floatValue < 0f) minCycles.floatValue = 0f;
                EditorGUILayout.PropertyField(maxCycles, new GUIContent("Max. Cycles"));
                if (maxCycles.floatValue < minCycles.floatValue) maxCycles.floatValue = minCycles.floatValue; 
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Particles may not work in the editor preview. Play the game to see the in-game result.", MessageType.Info);
            }

        }
    }
}
