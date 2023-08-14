namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;
    using Dreamteck.Splines;

    [CustomEditor(typeof(ObjectController))]
    [CanEditMultipleObjects]
    public class ObjectControllerEditor : SplineUserEditor
    {

        protected override void BodyGUI()
        {
            base.BodyGUI();
            ObjectController user = (ObjectController)target;
            serializedObject.Update();
            SerializedProperty objectMethod = serializedObject.FindProperty("_objectMethod");
            SerializedProperty retainPrefabInstancesInEditor = serializedObject.FindProperty("_retainPrefabInstancesInEditor");
            SerializedProperty spawnMethod = serializedObject.FindProperty("_spawnMethod");
            SerializedProperty spawnCount = serializedObject.FindProperty("_spawnCount");
            SerializedProperty delayedSpawn = serializedObject.FindProperty("delayedSpawn");
            SerializedProperty spawnDelay = serializedObject.FindProperty("spawnDelay");
            SerializedProperty iteration = serializedObject.FindProperty("_iteration");
            SerializedProperty applyRotation = serializedObject.FindProperty("_applyRotation");
            SerializedProperty minRotation = serializedObject.FindProperty("_minRotation");
            SerializedProperty maxRotation = serializedObject.FindProperty("_maxRotation");
            SerializedProperty applyScale = serializedObject.FindProperty("_applyScale");
            SerializedProperty minScaleMultiplier = serializedObject.FindProperty("_minScaleMultiplier");
            SerializedProperty maxScaleMultiplier = serializedObject.FindProperty("_maxScaleMultiplier");
            SerializedProperty uniformScaleLerp = serializedObject.FindProperty("_uniformScaleLerp");
            SerializedProperty objectPositioning = serializedObject.FindProperty("_objectPositioning");
            SerializedProperty evaluateOffset = serializedObject.FindProperty("_evaluateOffset");
            SerializedProperty offsetUseWorldCoords = serializedObject.FindProperty("_offsetUseWorldCoords");
            SerializedProperty minOffset = serializedObject.FindProperty("_minOffset");
            SerializedProperty maxOffset = serializedObject.FindProperty("_maxOffset");
            SerializedProperty shellOffset = serializedObject.FindProperty("_shellOffset");
            SerializedProperty rotateByOffset = serializedObject.FindProperty("_rotateByOffset");
            SerializedProperty randomSeed = serializedObject.FindProperty("_randomSeed");
            SerializedProperty useCustomObjectDistance = serializedObject.FindProperty("_useCustomObjectDistance");
            SerializedProperty minObjectDistance = serializedObject.FindProperty("_minObjectDistance");
            SerializedProperty maxObjectDistance = serializedObject.FindProperty("_maxObjectDistance");
            SerializedProperty customOffsetRule = serializedObject.FindProperty("_customOffsetRule");
            SerializedProperty customRotationRule = serializedObject.FindProperty("_customRotationRule");
            SerializedProperty customScaleRule = serializedObject.FindProperty("_customScaleRule");


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(objectMethod, new GUIContent("Object Method"));
            if (objectMethod.intValue == (int)ObjectController.ObjectMethod.Instantiate) EditorGUILayout.PropertyField(retainPrefabInstancesInEditor, new GUIContent("Retain Prefab Instances"));
            if (objectMethod.intValue == (int)ObjectController.ObjectMethod.Instantiate)
            {
                bool objectsChanged = false;
                bool hasObj = false;
                if (users.Length > 1)
                {
                    EditorGUILayout.HelpBox("Editing unavailable when multiple objects are selected", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Instantiate Objects", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical();

                    for (int i = 0; i < user.objects.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        user.objects[i] = (GameObject)EditorGUILayout.ObjectField(user.objects[i], typeof(GameObject), true);
                        if (GUILayout.Button("x", GUILayout.Width(20)))
                        {
                            GameObject[] newObjects = new GameObject[user.objects.Length - 1];
                            for (int n = 0; n < user.objects.Length; n++)
                            {
                                if (n < i) newObjects[n] = user.objects[n];
                                else if (n == i) continue;
                                else newObjects[n - 1] = user.objects[n];
                                objectsChanged = true;
                            }
                            user.objects = newObjects;
                        }
                        if (i > 0)
                        {
                            if (GUILayout.Button("▲", GUILayout.Width(20)))
                            {
                                GameObject temp = user.objects[i - 1];
                                user.objects[i - 1] = user.objects[i];
                                user.objects[i] = temp;
                                objectsChanged = true;
                            }
                        }
                        if (i < user.objects.Length - 1)
                        {
                            if (GUILayout.Button("▼", GUILayout.Width(20)))
                            {
                                GameObject temp = user.objects[i + 1];
                                user.objects[i + 1] = user.objects[i];
                                user.objects[i] = temp;
                                objectsChanged = true;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    GameObject newObj = null;
                    newObj = (GameObject)EditorGUILayout.ObjectField("Add Object", newObj, typeof(GameObject), true);
                    if (newObj != null)
                    {
                        GameObject[] newObjects = new GameObject[user.objects.Length + 1];
                        user.objects.CopyTo(newObjects, 0);
                        newObjects[newObjects.Length - 1] = newObj;
                        user.objects = newObjects;
                        objectsChanged = true;
                    }

                    for (int i = 0; i < user.objects.Length; i++)
                    {
                        if (user.objects[i] != null)
                        {
                            hasObj = true;
                            break;
                        }
                    }
                }
                
                int lastSpawnMethod = spawnMethod.intValue;
                EditorGUILayout.PropertyField(spawnMethod, new GUIContent("Spawn Method"));
                if (lastSpawnMethod != spawnMethod.intValue)
                {
                    objectsChanged = true;
                }

                if (spawnMethod.intValue == (int)ObjectController.SpawnMethod.Count)
                {
                    int lastSpawnCount = spawnCount.intValue;
                    if (hasObj) EditorGUILayout.PropertyField(spawnCount, new GUIContent("Spawn Count"));
                    else spawnCount.intValue = 0;
                    if (lastSpawnCount != spawnCount.intValue) objectsChanged = true;
                }
                EditorGUILayout.PropertyField(delayedSpawn, new GUIContent("Delayed Spawn"));
                if (delayedSpawn.boolValue)
                {
                    EditorGUILayout.PropertyField(spawnDelay, new GUIContent("Spawn Delay"));
                }

                int lastIteration = iteration.intValue;
                EditorGUILayout.PropertyField(iteration, new GUIContent("Iteration"));
                if (lastIteration != iteration.intValue)
                {
                    objectsChanged = true;
                }

                if (objectsChanged)
                {
                    serializedObject.ApplyModifiedProperties();
                    user.Clear();
                    user.Spawn();
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(useCustomObjectDistance, new GUIContent("Custom Object Distance"));
            if (useCustomObjectDistance.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(minObjectDistance, new GUIContent("Min. Distance"));
                EditorGUILayout.PropertyField(maxObjectDistance, new GUIContent("Max. Distance"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Position and Offset");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(customOffsetRule);

            if (customOffsetRule.objectReferenceValue == null)
            {
                if (offsetUseWorldCoords.boolValue)
                {
                    minOffset.vector3Value = EditorGUILayout.Vector3Field("Min. Offset", minOffset.vector3Value);
                    maxOffset.vector3Value = EditorGUILayout.Vector3Field("Max. Offset", maxOffset.vector3Value);
                }
                else
                {
                    minOffset.vector3Value = EditorGUILayout.Vector2Field("Min. Offset", minOffset.vector3Value);
                    maxOffset.vector3Value = EditorGUILayout.Vector2Field("Max. Offset", maxOffset.vector3Value);
                }
            } else
            {
                CustomRuleUI((ObjectControllerCustomRuleBase)customOffsetRule.objectReferenceValue);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(applyRotation, new GUIContent("Apply Rotation"));
            if (user.applyRotation)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customRotationRule);
                if (customRotationRule.objectReferenceValue == null)
                {
                    EditorGUILayout.PropertyField(minRotation, new GUIContent("Min. Rotation Offset"));
                    EditorGUILayout.PropertyField(maxRotation, new GUIContent("Max. Rotation Offset"));
                } else
                {
                    CustomRuleUI((ObjectControllerCustomRuleBase)customRotationRule.objectReferenceValue);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(applyScale, new GUIContent("Apply Scale"));
            if (user.applyScale)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customScaleRule);
                if (customScaleRule.objectReferenceValue == null)
                {
                    EditorGUILayout.PropertyField(minScaleMultiplier, new GUIContent("Min. Scale Multiplier"));
                    EditorGUILayout.PropertyField(maxScaleMultiplier, new GUIContent("Max. Scale Multiplier"));
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(uniformScaleLerp, new GUIContent("Uniform Lerp"));
                    EditorGUI.indentLevel--;
                } else
                {
                    CustomRuleUI((ObjectControllerCustomRuleBase)customScaleRule.objectReferenceValue);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(objectPositioning, new GUIContent("Object Positioning"));
            EditorGUILayout.PropertyField(evaluateOffset, new GUIContent("Evaluate Offset"));

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(offsetUseWorldCoords, new GUIContent("Use World Coords."));
            if(minOffset.vector3Value != maxOffset.vector3Value) EditorGUILayout.PropertyField(shellOffset, new GUIContent("Shell"));
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(rotateByOffset, new GUIContent("Rotate by Offset"));
            EditorGUILayout.PropertyField(randomSeed, new GUIContent("Random Seed"));
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            
        }

        private void CustomRuleUI(ObjectControllerCustomRuleBase customRule)
        {
            SerializedObject serializedRule = new SerializedObject(customRule);
            SerializedProperty property = serializedRule.GetIterator();
            property.NextVisible(true);
            property.NextVisible(false);
            EditorGUI.BeginChangeCheck();
            do
            {
                EditorGUILayout.PropertyField(property);
            } while (property.NextVisible(false));
            if (EditorGUI.EndChangeCheck())
            {
                serializedRule.ApplyModifiedProperties();
            }
        }

    }


}
