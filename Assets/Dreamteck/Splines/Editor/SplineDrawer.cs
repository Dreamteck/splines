namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.SceneManagement;

    [InitializeOnLoad]
    public static class SplineDrawer
    {
        private static Vector3[] positions = new Vector3[0];

        public static void DrawSpline(Spline spline, Color color, double from = 0.0, double to = 1.0, bool drawThickness = false, bool thicknessAutoRotate = false)
        {
            double add = spline.moveStep;
            if (add < 0.0025) add = 0.0025;
            Color prevColor = Handles.color;
            Handles.color = color;
            int iterations = spline.iterations;
            if (iterations <= 0) return;
            if (drawThickness)
            {
                Transform editorCamera = SceneView.currentDrawingSceneView.camera.transform;
                if (positions.Length != iterations * 6) positions = new Vector3[iterations * 6];
                SplineSample prevResult = spline.Evaluate(from);
                Vector3 prevNormal = prevResult.up;
                if (thicknessAutoRotate) prevNormal = (editorCamera.position - prevResult.position).normalized;
                Vector3 prevRight = Vector3.Cross(prevResult.forward, prevNormal).normalized * prevResult.size * 0.5f;
                int pointIndex = 0;
                for (int i = 1; i < iterations; i++)
                {
                    double p = DMath.Lerp(from, to, (double)i / (iterations - 1));
                    SplineSample newResult = spline.Evaluate(p);
                    Vector3 newNormal = newResult.up;
                    if (thicknessAutoRotate) newNormal = (editorCamera.position - newResult.position).normalized;
                    Vector3 newRight = Vector3.Cross(newResult.forward, newNormal).normalized * newResult.size * 0.5f;

                    positions[pointIndex] = prevResult.position + prevRight;
                    positions[pointIndex + iterations * 2] = prevResult.position - prevRight;
                    positions[pointIndex + iterations * 4] = newResult.position - newRight;
                    pointIndex++;
                    positions[pointIndex] = newResult.position + newRight;
                    positions[pointIndex + iterations * 2] = newResult.position - newRight;
                    positions[pointIndex + iterations * 4] = newResult.position + newRight;
                    pointIndex++;
                    prevResult = newResult;
                    prevRight = newRight;
                    prevNormal = newNormal;
                }
                Handles.DrawLines(positions);
            }
            else
            {
                if (positions.Length != iterations * 2) positions = new Vector3[iterations * 2];
                Vector3 prevPoint = spline.EvaluatePosition(from);
                int pointIndex = 0;
                for (int i = 1; i < iterations; i++)
                {
                    double p = DMath.Lerp(from, to, (double)i / (iterations - 1));
                    positions[pointIndex] = prevPoint;
                    pointIndex++;
                    positions[pointIndex] = spline.EvaluatePosition(p);
                    pointIndex++;
                    prevPoint = positions[pointIndex - 1];
                }
                Handles.DrawLines(positions);
            }
            Handles.color = prevColor;
        }

        public static void DrawPath(ref Vector3[] points)
        {
            Vector3[] linePoints = new Vector3[points.Length * 2];
            Vector3 prevPoint = points[0];
            int pointIndex = 0;
            for (int currObjectIndex = 1; currObjectIndex < points.Length; currObjectIndex++)
            {
                linePoints[pointIndex] = prevPoint;
                pointIndex++;
                linePoints[pointIndex] = points[currObjectIndex];
                pointIndex++;
                prevPoint = points[currObjectIndex];
            }
            Handles.DrawLines(linePoints);
        }
    }
}
