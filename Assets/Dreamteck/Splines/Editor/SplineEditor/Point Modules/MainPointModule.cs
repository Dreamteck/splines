namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;

    public class MainPointModule : PointModule
    {
        public bool excludeSelected = false;
        public int minimumRectSize = 5;
        private Vector2 rectStart = Vector2.zero;
        private Vector2 rectEnd = Vector2.zero;
        private Rect rect;
        private bool drag = false;
        private bool finalize = false;
        private bool pointsMoved = false;
        private bool _tangentMode = false;

        public bool isDragging
        {
            get
            {
                return drag && rect.width >= minimumRectSize && rect.height >= minimumRectSize;
            }
        }

        public bool tangentMode => _tangentMode;

        public MainPointModule(SplineEditor editor) : base(editor)
        {

        }

        protected override void OnDrawInspector()
        {
            string[] options = new string[points.Length + 4];
            options[0] = "- - -";
            if (selectedPoints.Count > 1) options[0] = "- Multiple -";
            options[1] = "All";
            options[2] = "None";
            options[3] = "Inverse";
            for (int i = 0; i < points.Length; i++)
            {
                options[i + 4] = "Point " + (i + 1);
                if (splineType == Spline.Type.Bezier)
                {
                    switch (points[i].type)
                    {
                        case SplinePoint.Type.Broken: options[i + 4] += " - Broken"; break;
                        case SplinePoint.Type.SmoothFree: options[i + 4] += " - Smooth Free"; break;
                        case SplinePoint.Type.SmoothMirrored: options[i + 4] += " - Smooth Mirrored"; break;
                    }
                }
            }
            int option = 0;
            if (selectedPoints.Count == 1) {
                option = selectedPoints[0] + 4;
            }
            option = EditorGUILayout.Popup("Select", option, options);
            switch (option)
            {
                case 1:
                    ClearSelection();
                    for (int i = 0; i < points.Length; i++) AddPointSelection(i);
                    break;

                case 2:
                    ClearSelection();
                    break;

                case 3:
                    InverseSelection();
                    break;
            }
            if(option >= 4)
            {
                SelectPoint(option - 4);
            }
        }

        protected override void OnDrawScene()
        {
            if (eventModule.v) return;
            Transform camTransform = SceneView.currentDrawingSceneView.camera.transform;
            if (!drag)
            {
                if (finalize)
                {
                    if (rect.width > 0f && rect.height > 0f)
                    {
                        if (!eventModule.control) ClearSelection();
                        for (int i = 0; i < points.Length; i++)
                        {
                            Vector2 guiPoint = HandleUtility.WorldToGUIPoint(points[i].position);
                            if (rect.Contains(guiPoint))
                            {
                                Vector3 local = camTransform.InverseTransformPoint(points[i].position);
                                if (local.z >= 0f)
                                {
                                    AddPointSelection(i);
                                }
                            }
                        }
                    }
                    finalize = false;
                }
            }
            else
            {
                rectEnd = Event.current.mousePosition;
                rect = new Rect(Mathf.Min(rectStart.x, rectEnd.x), Mathf.Min(rectStart.y, rectEnd.y), Mathf.Abs(rectEnd.x - rectStart.x), Mathf.Abs(rectEnd.y - rectStart.y));
                if (rect.width >= minimumRectSize && rect.height >= minimumRectSize)
                {
                    Color col = highlightColor;
                    col.a = 0.4f;
                    Handles.BeginGUI();
                    EditorGUI.DrawRect(rect, col);
                    Handles.EndGUI();
                    SceneView.RepaintAll();
                }
            }
            TextAnchor originalAlignment = GUI.skin.label.alignment;
            Color originalColor = GUI.skin.label.normal.textColor;

            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.normal.textColor = color;

            if (selectedPoints.Count > 1)
            {
                _tangentMode = false;
            }

            for (int i = 0; i < points.Length; i++)
            {
                bool isSelected = selectedPoints.Contains(i);
                Vector3 lastPos = points[i].position;

                if (splineType == Spline.Type.Bezier && isSelected)
                {
                    Handles.color = color;


                    if (Event.current.type == EventType.Repaint)
                        Handles.DrawDottedLine(points[i].position, points[i].tangent, 4f);
                    if (Event.current.type == EventType.Repaint)
                        Handles.DrawDottedLine(points[i].position, points[i].tangent2, 4f);

                    if (_tangentMode && selectedPoints.Count == 1)
                    {
                        Handles.color = highlightColor;
                    }

                    Vector3 lastTangentPos = points[i].tangent;
                    
                    Vector3 newPos = SplineEditorHandles.FreeMoveCircle(points[i].tangent, HandleUtility.GetHandleSize(points[i].tangent) * 0.22f);
                    if (lastTangentPos != newPos)
                    {
                        points[i].SetTangentPosition(newPos);
                        RegisterChange();
                    }
                    lastTangentPos = points[i].tangent2;
                    newPos = SplineEditorHandles.FreeMoveCircle(points[i].tangent2, HandleUtility.GetHandleSize(points[i].tangent2) * 0.22f);
                    if (lastTangentPos != newPos)
                    {
                        points[i].SetTangent2Position(newPos);
                        RegisterChange();
                    }

                    Handles.color = color;
                }
                
                Handles.color = Color.clear;

                if (showPointNumbers && camTransform.InverseTransformPoint(points[i].position).z > 0f)
                {
                    if(Event.current.type == EventType.Repaint)
                    {
                        Handles.Label(points[i].position + Camera.current.transform.up * HandleUtility.GetHandleSize(points[i].position) * 0.3f, (i + 1).ToString());
                    }
                }
                if (!eventModule.alt)
                {
                    if (excludeSelected && isSelected)
                    {

                        SplineEditorHandles.FreeMoveRectangle(points[i].position, HandleUtility.GetHandleSize(points[i].position) * 0.1f);
                    }
                    else
                    {
                        points[i].SetPosition(SplineEditorHandles.FreeMoveRectangle(points[i].position, HandleUtility.GetHandleSize(points[i].position) * 0.1f));
                    }
                }

                if (lastPos != points[i].position)
                {
                    _tangentMode = false;
                    pointsMoved = true;
                    if (isSelected)
                    {
                        for (int n = 0; n < selectedPoints.Count; n++)
                        {
                            if (selectedPoints[n] == i) continue;
                            points[selectedPoints[n]].SetPosition(points[selectedPoints[n]].position + (points[i].position - lastPos));
                        }
                    }
                    else
                    {
                        SelectPoint(i);
                    }
                    RegisterChange();
                }

                if (!pointsMoved && !eventModule.alt && editor.eventModule.mouseLeftUp)
                {
                    if(SplineEditorHandles.HoverArea(points[i].position, 0.12f))
                    {
                        if (eventModule.control && selectedPoints.Contains(i))
                        {
                            DeselectPoint(i);
                        }
                        else
                        {
                            if (eventModule.shift) ShiftSelect(i, points.Length);
                            else if (eventModule.control) AddPointSelection(i);
                            else SelectPoint(i);
                        }
                        _tangentMode = false;
                    } else if(splineType == Spline.Type.Bezier)
                    {
                        if (SplineEditorHandles.HoverArea(points[i].tangent, 0.23f))
                        {
                            if (eventModule.shift) ShiftSelect(i, points.Length);
                            else if (eventModule.control) AddPointSelection(i);
                            else SelectPoint(i);
                            _tangentMode = true;
                        }
                    }
                }

                if (!excludeSelected || !isSelected)
                {
                    Handles.color = color;
                    if (isSelected)
                    {
                        if (!_tangentMode || selectedPoints.Count != 1)
                        {
                            Handles.color = highlightColor;
                        }
                        if (Event.current.type == EventType.Repaint)
                        {
                            Handles.DrawWireDisc(points[i].position, -SceneView.currentDrawingSceneView.camera.transform.forward, HandleUtility.GetHandleSize(points[i].position) * 0.14f);
                        }
                    }
                    else
                    {
                        Handles.color = color;
                    }

                    if (Event.current.type == EventType.Repaint)
                    {
                        Handles.DrawSolidDisc(points[i].position, -SceneView.currentDrawingSceneView.camera.transform.forward, HandleUtility.GetHandleSize(points[i].position) * 0.09f);
                    }

                    Handles.color = Color.white;
                }
            }
            GUI.skin.label.alignment = originalAlignment;
            GUI.skin.label.normal.textColor = originalColor;

            if (isDragging)
            {
                if (eventModule.alt || !SceneView.currentDrawingSceneView.camera.pixelRect.Contains(Event.current.mousePosition) || !eventModule.mouseLeft) FinishDrag();
            }

            if (eventModule.mouseLeftUp)
            {
                pointsMoved = false;
            }
        }

        void ShiftSelect(int index, int pointCount)
        {
            if (selectedPoints.Count == 0)
            {
                AddPointSelection(index);
                return;
            }
            int minSelected = pointCount-1, maxSelected = 0;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (minSelected > selectedPoints[i]) minSelected = selectedPoints[i];
                if (maxSelected < selectedPoints[i]) maxSelected = selectedPoints[i];
            }

            if(index > maxSelected)
            {
                for (int i = maxSelected + 1; i <= index; i++) AddPointSelection(i);
            } else if(index < minSelected)
            {
                for (int i = minSelected-1; i >= index; i--) AddPointSelection(i);
            } else
            {
                for (int i = minSelected + 1; i <= index; i++) AddPointSelection(i);
            }
        }

        public void StartDrag(Vector2 position)
        {
            rectStart = position;
            drag = true;
            finalize = false;
        }

        public void FinishDrag()
        {
            if (!drag) return;
            drag = false;
            finalize = true;
        }

        public void CancelDrag()
        {
            drag = false;
            finalize = false;
        }
    }
}
