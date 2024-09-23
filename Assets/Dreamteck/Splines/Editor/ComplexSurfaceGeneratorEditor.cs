#if UNITY_EDITOR
using Dreamteck.Splines.Editor;
using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(ComplexSurfaceGenerator), true)]
    public class ComplexSurfaceGeneratorEditor : MeshGenEditor
    {
        private SplineComputer _lastEditedComputer;
        private SplineComputer _highlightedComputer;
        private int _lastEditedPointIndex = -1;
        private bool _positionHandle = false;
        private Vector2 _scroll = Vector2.zero;

        protected override void Awake()
        {
            base.Awake();
            _positionHandle = EditorPrefs.GetBool(nameof(ComplexSurfaceGeneratorEditor) + ".positionHandles", false);
            if (Application.isPlaying) return;
            SerializedProperty initProperty = serializedObject.FindProperty("_initializedInEditor");
            ComplexSurfaceGenerator gen = (ComplexSurfaceGenerator)target;

            if (!initProperty.boolValue)
            {
                AddSpline(gen);
                initProperty.boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }

            SerializedProperty computersProperty = serializedObject.FindProperty("_otherComputers");
            ValidateSplines(gen, computersProperty);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EditorPrefs.SetBool(nameof(ComplexSurfaceGeneratorEditor) + ".positionHandles", _positionHandle);
        }

        protected override void BodyGUI()
        {
            base.BodyGUI();
            ComplexSurfaceGenerator gen = (ComplexSurfaceGenerator)target;
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
            _positionHandle = EditorGUILayout.Toggle("Toggle Move Handles", _positionHandle);
            EditorGUILayout.Space();
            EditorGUI.indentLevel++;

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(Mathf.Min(computersProperty.arraySize * 22, 300)));
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
                    if(EditorUtility.DisplayDialog("Delete Spline", "Also remove spline object?", "Yes", "No"))
                    {
                        DestroyImmediate(spline.gameObject);
                    }

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
            EditorGUILayout.EndScrollView();

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

        private void ValidateSplines(ComplexSurfaceGenerator gen, SerializedProperty computersProperty)
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

        private void AddSpline(ComplexSurfaceGenerator gen)
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
                if (!(components[i] is SplineComputer || components[i] is Transform))
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
            spline.transform.position += direction * reference.CalculateLength() / Mathf.Max((reference.pointCount - 1), 1);
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
            ComplexSurfaceGenerator gen = (ComplexSurfaceGenerator)target;
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

                    Vector3 newPos = point;

                    if (_positionHandle)
                    {
                        newPos = Handles.PositionHandle(newPos, Quaternion.identity);
                    } else
                    {
                        Handles.color = Color.clear;
                        newPos = SplineEditorHandles.FreeMoveRectangle(point, HandleUtility.GetHandleSize(point) * 0.16f);
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

                    bool isSelected = (_lastEditedComputer == otherSplines[i] && _lastEditedPointIndex == j) || (_highlightedComputer == otherSplines[i]);
 

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
