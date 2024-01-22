#if UNITY_EDITOR
using Dreamteck.Splines.Editor;
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

            for (int i = 0; i < gen.otherComputers.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(gen.otherComputers[i].name);
                if (i > 0)
                {
                    if (GUILayout.Button("▲", GUILayout.Width(30)))
                    {
                        SplineComputer temp = gen.otherComputers[i];
                        gen.otherComputers[i] = gen.otherComputers[i - 1];
                        gen.otherComputers[i - 1] = temp;
                        gen.Rebuild();
                    }
                }
                if (i < gen.otherComputers.Length - 1)
                {
                    if (GUILayout.Button("▼", GUILayout.Width(30)))
                    {
                        SplineComputer temp = gen.otherComputers[i];
                        gen.otherComputers[i] = gen.otherComputers[i + 1];
                        gen.otherComputers[i + 1] = temp;
                        gen.Rebuild();
                    }
                }
                if (GUILayout.Button("x", GUILayout.Width(30)))
                {
                    SplineComputer[] newComputers = new SplineComputer[gen.otherComputers.Length - 1];
                    for (int n = 0; n < gen.otherComputers.Length; n++)
                    {
                        if (i < n) newComputers[n] = gen.otherComputers[n];
                        else if (i > n) newComputers[Mathf.Max(0, n - 1)] = gen.otherComputers[n];
                    }
                    gen.otherComputers = newComputers;
                }
                EditorGUILayout.EndHorizontal();
            }
            SplineComputer newComp = null;
            newComp = (SplineComputer)EditorGUILayout.ObjectField("Add Spline", newComp, typeof(SplineComputer), true);
            if (newComp != null)
            {
                bool fail = false;
                if (newComp == gen.spline) fail = true;
                else if (gen.spline != null && newComp == gen.spline) fail = true;
                else
                {
                    for (int i = 0; i < gen.otherComputers.Length; i++)
                    {
                        if (gen.otherComputers[i] == newComp)
                        {
                            fail = true;
                            break;
                        }
                    }
                }
                if (!fail)
                {
                    SplineComputer[] newComputers = new SplineComputer[gen.otherComputers.Length + 1];
                    gen.otherComputers.CopyTo(newComputers, 0);
                    newComputers[newComputers.Length - 1] = newComp;
                    gen.otherComputers = newComputers;
                }
                else EditorUtility.DisplayDialog("Can't add computer", "This computer is already added to the generator", "OK");
            }

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
            gen.iterations = EditorGUILayout.IntField("Iterations", gen.iterations);
            if (gen.iterations < 0) gen.iterations = 0;
            EditorGUILayout.LabelField("UVs", EditorStyles.boldLabel);
            gen.uvWrapMode = (MultiSplineSurfaceGenerator.UVWrapMode)EditorGUILayout.EnumPopup("Wrap Mode", gen.uvWrapMode);
            gen.uvOffset = EditorGUILayout.Vector2Field("UV Offset", gen.uvOffset);
            gen.uvScale = EditorGUILayout.Vector2Field("UV Scale", gen.uvScale);
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
        }
    }
}
#endif
