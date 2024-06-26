#if UNITY_EDITOR
using Dreamteck.Splines.Editor;
using PlasticGui;
using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines
{
    [CustomEditor(typeof(MultiSplineSurfaceGenerator), true)]
    public class MultiSplineSurfaceEditor : MeshGenEditor
    {
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

            EditorGUILayout.PropertyField(computersProperty, new GUIContent("Other Splines"));

            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < computersProperty.arraySize; i++)
                {
                    SerializedProperty compProperty = computersProperty.GetArrayElementAtIndex(i);
                    SplineComputer spline = (SplineComputer)compProperty.objectReferenceValue;
                    spline.Unsubscribe(gen);
                    spline.Subscribe(gen);
                }
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("Add Spline"))
            {
                SplineComputer reference = gen.spline;
                if(gen.otherComputers.Length > 0)
                {
                    reference = gen.otherComputers[gen.otherComputers.Length - 1];
                }
   
                SplineComputer spline = Instantiate(reference, gen.transform);
                Undo.RegisterCreatedObjectUndo(spline.gameObject, "Surface Add Spline");

                Vector3 direction = Vector3.Slerp(reference.Evaluate(0.0).right, reference.Evaluate(1.0).right, 0.5f);
                spline.Subscribe(gen);
                spline.transform.position += direction * reference.CalculateLength();
                computersProperty.arraySize += 1;
                computersProperty.GetArrayElementAtIndex(computersProperty.arraySize - 1).objectReferenceValue = spline;
                spline.RebuildImmediate();
                gen.RebuildImmediate();
            }

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
            EditorGUILayout.PropertyField(subdivisionsProperty);
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            UVControls(gen);


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
                for (int j = 0; j < otherSplines[i].pointCount; j++)
                {
                    if (otherSplines[i].subscriberCount == 1)
                    {
                        otherSplines[i].name = "Surface Spline " + (i + 1);
                    }
                    Vector3 point = otherSplines[i].GetPointPosition(j);
                    Vector3 newPos = SplineEditorHandles.FreeMoveCircle(point, HandleUtility.GetHandleSize(point) * 0.22f);
                    if (Vector3.Distance(point, newPos) > 0.01f)
                    {
                        markDirty = true;
                        otherSplines[i].SetPointPosition(j, newPos);
                    }
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
