namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;

    public static class SplineComputerEditorHandles
    {
        private static SplineSample evalResult = new SplineSample();
        public enum SplineSliderGizmo { ForwardTriangle, BackwardTriangle, DualArrow, Rectangle, Circle }

        public static bool Slider(SplineComputer spline, ref double percent, Color color, string text = "", SplineSliderGizmo gizmo = SplineSliderGizmo.Rectangle, float buttonSize = 1f)
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            spline.Evaluate(percent, ref evalResult);
            float size = HandleUtility.GetHandleSize(evalResult.position);

            Handles.color = new Color(color.r, color.g, color.b, 0.4f);
            Handles.DrawSolidDisc(evalResult.position, cam.transform.position - evalResult.position, size * 0.2f * buttonSize);
            Handles.color = Color.white;
            if ((color.r + color.g + color.b + color.a) / 4f >= 0.9f) Handles.color = Color.black;

            Vector3 center = evalResult.position;
            Vector2 screenPosition = HandleUtility.WorldToGUIPoint(center);
            screenPosition.y += 20f;
            Vector3 localPos = cam.transform.InverseTransformPoint(center);
            if (text != "" && localPos.z > 0f)
            {
                Handles.BeginGUI();
                DreamteckEditorGUI.Label(new Rect(screenPosition.x - 120 + text.Length * 4, screenPosition.y, 120, 25), text);
                Handles.EndGUI();
            }
            bool buttonClick = SplineEditorHandles.SliderButton(center, false, Color.white, 0.3f);
            Vector3 lookAtCamera = (cam.transform.position - evalResult.position).normalized;
            Vector3 right = Vector3.Cross(lookAtCamera, evalResult.forward).normalized * size * 0.1f * buttonSize;
            Vector3 front = Vector3.forward;
            switch (gizmo)
            {
                case SplineSliderGizmo.BackwardTriangle:
                    center += evalResult.forward * size * 0.06f * buttonSize;
                    front = center - evalResult.forward * size * 0.2f * buttonSize;
                    Handles.DrawLine(center + right, front);
                    Handles.DrawLine(front, center - right);
                    Handles.DrawLine(center - right, center + right);
                    break;

                case SplineSliderGizmo.ForwardTriangle:
                    center -= evalResult.forward * size * 0.06f * buttonSize;
                    front = center + evalResult.forward * size * 0.2f * buttonSize;
                    Handles.DrawLine(center + right, front);
                    Handles.DrawLine(front, center - right);
                    Handles.DrawLine(center - right, center + right);
                    break;

                case SplineSliderGizmo.DualArrow:
                    center += evalResult.forward * size * 0.025f * buttonSize;
                    front = center + evalResult.forward * size * 0.17f * buttonSize;
                    Handles.DrawLine(center + right, front);
                    Handles.DrawLine(front, center - right);
                    Handles.DrawLine(center - right, center + right);
                    center -= evalResult.forward * size * 0.05f * buttonSize;
                    front = center - evalResult.forward * size * 0.17f * buttonSize;
                    Handles.DrawLine(center + right, front);
                    Handles.DrawLine(front, center - right);
                    Handles.DrawLine(center - right, center + right);
                    break;
                case SplineSliderGizmo.Rectangle:

                    break;

                case SplineSliderGizmo.Circle:
                    Handles.DrawWireDisc(center, lookAtCamera, 0.13f * size * buttonSize);
                    break;
            }
            Vector3 lastPos = evalResult.position;
            Handles.color = Color.clear;
            var lookRotation = Quaternion.LookRotation(cam.transform.position - evalResult.position); 
            evalResult.position = SplineEditorHandles.FreeMoveHandle(evalResult.position, size * 0.2f * buttonSize, Vector3.zero, Handles.CircleHandleCap);
            if (evalResult.position != lastPos) percent = spline.Project(evalResult.position).percent;
            Handles.color = Color.white;
            return buttonClick;
        }
    }
}