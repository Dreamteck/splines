namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.SceneManagement;

    [InitializeOnLoad]
    public static class DSSplineDrawer
    {
        private static bool refreshComputers = false;
        private static List<SplineComputer> drawComputers = new List<SplineComputer>();
        private static Vector3[] positions = new Vector3[0];
        private static UnityEngine.SceneManagement.Scene currentScene;

        static DSSplineDrawer()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += AutoDrawComputers;
#else
            SceneView.onSceneGUIDelegate += AutoDrawComputers;
#endif

            FindComputers();
            EditorApplication.hierarchyChanged += HerarchyWindowChanged;
            EditorApplication.playModeStateChanged += ModeChanged;
        }


        static void ModeChanged(PlayModeStateChange stateChange)
        {
            refreshComputers = true;
        }

        private static void HerarchyWindowChanged()
        {
        if (currentScene != EditorSceneManager.GetActiveScene())
            {
                currentScene = EditorSceneManager.GetActiveScene();
                FindComputers();
            }
            
        }

        private static void FindComputers()
        {
            drawComputers.Clear();
            SplineComputer[] computers = GameObject.FindObjectsOfType<SplineComputer>();
            drawComputers.AddRange(computers);
        }

        private static void AutoDrawComputers(SceneView current)
        {
            if (refreshComputers)
            {
                refreshComputers = false;
                FindComputers();
            }
            for (int i = 0; i < drawComputers.Count; i++)
            {
                if (!drawComputers[i].editorAlwaysDraw)
                {
                    drawComputers.RemoveAt(i);
                    i--;
                    continue;
                }
                DrawSplineComputer(drawComputers[i]);
            }
        }

        public static void RegisterComputer(SplineComputer comp)
        {
            if (drawComputers.Contains(comp)) return;
            comp.editorAlwaysDraw = true;
            drawComputers.Add(comp);
        }

        public static void UnregisterComputer(SplineComputer comp)
        {
            for(int i = 0; i < drawComputers.Count; i++)
            {
                if(drawComputers[i] == comp)
                {
                    drawComputers[i].editorAlwaysDraw = false;
                    drawComputers.RemoveAt(i);
                    return;
                }
            }
        }

        public static void DrawSplineComputer(SplineComputer comp, double fromPercent = 0.0, double toPercent = 1.0, float alpha = 1f)
        {
            if (comp == null) return;
            if (comp.pointCount < 2) return;
            if (Event.current.type != EventType.Repaint) return;
            Color prevColor = Handles.color;
            Color handleColor = comp.editorPathColor;
            handleColor.a = alpha;
            Handles.color = handleColor;

            if (comp.type == Spline.Type.BSpline && comp.pointCount > 1)
            {
                SplinePoint[] compPoints = comp.GetPoints();
                Handles.color = new Color(handleColor.r, handleColor.g, handleColor.b, 0.5f * alpha);
                for (int i = 0; i < compPoints.Length - 1; i++)
                {
                    Handles.DrawLine(compPoints[i].position, compPoints[i + 1].position);
                }
                Handles.color = handleColor;
            }

            if (!comp.editorDrawThickness)
            {
                if (positions.Length != comp.sampleCount * 2)
                {
                    positions = new Vector3[comp.sampleCount * 2];
                }
                Vector3 prevPoint = comp.EvaluatePosition(fromPercent);
                int pointIndex = 0;
                for (int i = 1; i < comp.sampleCount; i++)
                {
                    positions[pointIndex] = prevPoint;
                    pointIndex++;
                    positions[pointIndex] = comp[i].position;
                    pointIndex++;
                    prevPoint = positions[pointIndex - 1];
                }
                Handles.DrawLines(positions);
            }
            else
            {
                Transform editorCamera = SceneView.currentDrawingSceneView.camera.transform;
                if (positions.Length != comp.sampleCount * 6) positions = new Vector3[comp.sampleCount * 6];
                SplineSample prevResult = comp.Evaluate(fromPercent);
                Vector3 prevNormal = prevResult.up;
                if (comp.editorBillboardThickness) prevNormal = (editorCamera.position - prevResult.position).normalized;
                Vector3 prevRight = Vector3.Cross(prevResult.forward, prevNormal).normalized * prevResult.size * 0.5f;
                int pointIndex = 0;
                for (int i = 1; i < comp.sampleCount; i++)
                {
                    Vector3 newNormal = comp[i].up;
                    if (comp.editorBillboardThickness) newNormal = (editorCamera.position - comp[i].position).normalized;
                    Vector3 newRight = Vector3.Cross(comp[i].forward, newNormal).normalized * comp[i].size * 0.5f;

                    positions[pointIndex] = prevResult.position + prevRight;
                    positions[pointIndex + comp.sampleCount * 2] = prevResult.position - prevRight;
                    positions[pointIndex + comp.sampleCount * 4] = comp[i].position - newRight;
                    pointIndex++;
                    positions[pointIndex] = comp[i].position + newRight;
                    positions[pointIndex + comp.sampleCount * 2] = comp[i].position - newRight;
                    positions[pointIndex + comp.sampleCount * 4] = comp[i].position + newRight;
                    pointIndex++;
                    prevResult = comp[i];
                    prevRight = newRight;
                    prevNormal = newNormal;
                }
                Handles.DrawLines(positions);
            }
            Handles.color = prevColor;
        }
    }
}
