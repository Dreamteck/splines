#if UNITY_EDITOR
using Dreamteck.Splines.Editor;
using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(MultiSplineSurfaceGenerator), true)]
    public class MultiSplineSurfaceEditor : MeshGenEditor
    {
        private SplineComputer _lastEditedComputer;
        private SplineComputer _highlightedComputer;
        private int _lastEditedPointIndex = -1;

        protected override void Awake()
        {
            base.Awake();
            if (Application.isPlaying) return;
            SerializedProperty initProperty = serializedObject.FindProperty("_initializedInEditor");
            MultiSplineSurfaceGenerator gen = (MultiSplineSurfaceGenerator)target;

            if (!initProperty.boolValue)
            {
                AddSpline(gen);
                initProperty.boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }

            SerializedProperty computersProperty = serializedObject.FindProperty("_otherComputers");
            ValidateSplines(gen, computersProperty);
        }

        protected override void BodyGUI()
        {
            base.BodyGUI();
            MultiSplineSurfaceGenerator gen = (MultiSplineSurfaceGenerator)target;
            EditorGUI.BeginChangeCheck();
            gen.separateMaterialIDs = EditorGUILayout.Toggle("Separate Material IDs", gen.separateMaterialIDs);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Paths", EditorStyles.boldLabel);

            SerializedProperty computersProperty = serializedObject.FindProperty("_otherComputers");
            SerializedProperty subdivisionsProperty = serializedObject.FindProperty("_subdivisions");
            SerializedProperty subdivisionModeProperty = serializedObject.FindProperty("_subdivisionMode");
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginChangeCheck();
            bool hasNullSpline = false;
            for (int i = 0; i < gen.otherComputers.Length; i++)
            {
                if (gen.otherComputers[i] == null)
                {
                    hasNullSpline = true;
                    break;
                }
            }
            if(hasNullSpline)
            {
                EditorGUILayout.HelpBox("Missing or not enough splines. Please, link at least one splines and remove any missing references.", MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Splines", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUI.indentLevel++;
            for (int i = 0; i < computersProperty.arraySize; i++)
            {
                SerializedProperty compProperty = computersProperty.GetArrayElementAtIndex(i);
                SplineComputer spline = (SplineComputer)compProperty.objectReferenceValue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(spline.name);
                if (GUILayout.Button("Edit", GUILayout.MaxWidth(75)))
                {
                    Selection.activeGameObject = spline.gameObject;
                }

                if (GUILayout.Button("Highlight", GUILayout.MaxWidth(75)))
                {
                    if(_highlightedComputer == spline)
                    {
                        _highlightedComputer = null;
                    } else
                    {
                        _highlightedComputer = spline;
                    }
                }
                if (GUILayout.Button("Remove", GUILayout.MaxWidth(75)))
                {
                    computersProperty.DeleteArrayElementAtIndex(i);
                    i--;
                    serializedObject.ApplyModifiedProperties();
                    gen.RebuildImmediate();
                }
                EditorGUILayout.EndHorizontal();
            }

            //sEditorGUILayout.PropertyField(computersProperty, new GUIContent("Other Splines"));

            if (EditorGUI.EndChangeCheck())
            {
                ValidateSplines(gen, computersProperty);
                serializedObject.ApplyModifiedProperties();
                gen.RebuildImmediate();
            }

            if (GUILayout.Button("Add Spline"))
            {
                AddSpline(gen);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Normals", EditorStyles.boldLabel);
            gen.automaticNormals = EditorGUILayout.Toggle("Automatic Normals", gen.automaticNormals);

            var normalMethods = new string[]
            {
                MeshGenerator.NormalMethod.Recalculate.ToString(),
                MeshGenerator.NormalMethod.SplineNormals.ToString(),
            };

            if (!gen.automaticNormals) gen.normalMethod = (MeshGenerator.NormalMethod)EditorGUILayout.Popup("Normal Method", (int)gen.normalMethod, normalMethods);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(subdivisionModeProperty);
            EditorGUILayout.PropertyField(subdivisionsProperty);
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            UVControls(gen);
        }

        private void ValidateSplines(MultiSplineSurfaceGenerator gen, SerializedProperty computersProperty)
        {
            for (int i = 0; i < computersProperty.arraySize; i++)
            {
                SerializedProperty compProperty = computersProperty.GetArrayElementAtIndex(i);
                SplineComputer spline = (SplineComputer)compProperty.objectReferenceValue;

                bool isValid = spline != null;

                if (isValid)
                {
                    for (int j = 0; j < i; j++)
                    {
                        SerializedProperty compPropertyPrevious = computersProperty.GetArrayElementAtIndex(j);
                        SplineComputer previousSpline = (SplineComputer)compPropertyPrevious.objectReferenceValue;
                        if (spline == previousSpline)
                        {
                            isValid = false;
                            break;
                        }
                    }
                }



                if (!isValid)
                {
                    computersProperty.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    gen.RebuildImmediate();
                    i--;
                    continue;
                }

                spline.Unsubscribe(gen);
                spline.Subscribe(gen);
            }
        }

        private void AddSpline(MultiSplineSurfaceGenerator gen)
        {
            SplineComputer reference = gen.spline;
            if (gen.otherComputers.Length > 0)
            {
                for (int i = gen.otherComputers.Length - 1; i >= 0; i--)
                {
                    if (gen.otherComputers[i] != null)
                    {
                        reference = gen.otherComputers[i];
                        break;
                    }
                }
            }

            SplineComputer spline = Instantiate(reference, gen.transform);
            Component[] components = spline.GetComponents<Component>();
            for (int i = components.Length-1; i >= 0; i--)
            {
                if (components[i] is not SplineComputer && components[i] is not Transform)
                {
                    DestroyImmediate(components[i]);
                }
            }

            while(spline.transform.childCount > 0)
            {
                DestroyImmediate(spline.transform.GetChild(0).gameObject);
            }

            Undo.RegisterCreatedObjectUndo(spline.gameObject, "Surface Add Spline");

            Vector3 direction = Vector3.Slerp(reference.Evaluate(0.0).right, reference.Evaluate(1.0).right, 0.5f);
            spline.Subscribe(gen);
            spline.transform.position += direction * reference.CalculateLength();
            SerializedProperty computersProperty = serializedObject.FindProperty("_otherComputers");
            computersProperty.arraySize += 1;
            computersProperty.GetArrayElementAtIndex(computersProperty.arraySize - 1).objectReferenceValue = spline;
            serializedObject.ApplyModifiedProperties();
            spline.RebuildImmediate();
            gen.RebuildImmediate();
        }

        public override void OnInspectorGUI()
        {
            showSize = false;
            showRotation = false;
            showNormalMethod = false;
            showOffset = false;
            base.OnInspectorGUI();
        }

        protected override void DuringSceneGUI(SceneView currentSceneView)
        {
            MultiSplineSurfaceGenerator gen = (MultiSplineSurfaceGenerator)target;
            base.DuringSceneGUI(currentSceneView);
            for (int i = 0; i < gen.otherComputers.Length; i++)
            {
                //SplineDrawer.DrawSplineComputer(gen.otherComputers[i]);
            }

            SplineComputer[] otherSplines = gen.otherComputers;

            bool rebuild = false;
            for (int i = 0; i < otherSplines.Length; i++)
            {
                bool markDirty = false;
                if (otherSplines[i] == null) continue;
                for (int j = 0; j < otherSplines[i].pointCount; j++)
                {
                    if (otherSplines[i].subscriberCount == 1)
                    {
                        otherSplines[i].name = "Surface Spline " + (i + 1);
                    }
                    Vector3 point = otherSplines[i].GetPointPosition(j);
                    Handles.color = Color.clear;
                    Vector3 newPos = SplineEditorHandles.FreeMoveRectangle(point, HandleUtility.GetHandleSize(point) * 0.16f);

                    bool isSelected = (_lastEditedComputer == otherSplines[i] && _lastEditedPointIndex == j) || (_highlightedComputer == otherSplines[i]);
                    if (isSelected)
                    {
                        newPos = Handles.PositionHandle(newPos, Quaternion.identity);
                    }

                    if (Vector3.Distance(point, newPos) > 0.01f)
                    {
                        _lastEditedComputer = otherSplines[i];
                        _lastEditedPointIndex = j;
                        _highlightedComputer = null;
                        MainPointModule.HoldInteraction();
                        markDirty = true;
                        otherSplines[i].SetPointPosition(j, newPos);
                    }

                   



                    if (Event.current.type == EventType.Repaint)
                    {    
                        SplineEditorHandles.DrawPoint(point, isSelected, MainPointModule.isSelecting ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : Color.white);
                    }
                }

                if(Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    _lastEditedPointIndex = -1;
                    _lastEditedComputer = null;
                }

                if (markDirty)
                {
                    EditorUtility.SetDirty(otherSplines[i]);
                    rebuild = true;
                }
            }
            if (rebuild)
            {
                for (int i = 0; i < users.Length; i++)
                {
                    users[i].RebuildImmediate();
                }
            }
        }
    }
}
#endif
