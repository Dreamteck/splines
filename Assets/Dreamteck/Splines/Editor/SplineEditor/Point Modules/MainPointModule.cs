namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;

    public class MainPointModule : PointModule
    {
        public bool excludeSelected = false;
        public int minimumRectSize = 5;
        private Vector2 _rectStart = Vector2.zero;
        private Vector2 _rectEnd = Vector2.zero;
        private Rect _dragRect;
        private bool _drag = false;
        private bool _finalizeDrag = false;
        private bool _pointsMoved = false;
        private bool _tangentMode = false;
        private Color _bgColor = Color.black;

        public static bool isSelecting => __isDragging;
        private static bool __holdInteraction = false;
        private static bool __isDragging = false;


        public bool isDragging
        {
            get
            {
                return _drag && _dragRect.width >= minimumRectSize && _dragRect.height >= minimumRectSize;
            }
        }

        public bool tangentMode => _tangentMode;

        public MainPointModule(SplineEditor editor) : base(editor)
        {
            _bgColor = Color.Lerp(color, Color.black, 0.75f);
            _bgColor.a = 0.75f;
        }

        public static void HoldInteraction()
        {
            __holdInteraction = true;
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

            if (isDragging)
            {
                if (!eventModule.mouseLeft)
                {
                    FinishDrag();
                }
            }
        }

        protected override void OnDrawScene()
        {
            if (eventModule.v) return;
            Transform camTransform = SceneView.currentDrawingSceneView.camera.transform;
            if (!_drag)
            {
                if (_finalizeDrag)
                {
                    if (_dragRect.width > 0f && _dragRect.height > 0f)
                    {
                        if (!eventModule.control) ClearSelection();
                        for (int i = 0; i < points.Length; i++)
                        {
                            Vector2 guiPoint = HandleUtility.WorldToGUIPoint(points[i].position);
                            if (_dragRect.Contains(guiPoint))
                            {
                                Vector3 local = camTransform.InverseTransformPoint(points[i].position);
                                if (local.z >= 0f)
                                {
                                    AddPointSelection(i);
                                }
                            }
                        }
                    }
                    _finalizeDrag = false;
                }
            }
            else
            {
                if (__holdInteraction)
                {
                    CancelDrag();
                }
                else
                {
                    _rectEnd = Event.current.mousePosition;
                    _dragRect = new Rect(Mathf.Min(_rectStart.x, _rectEnd.x), Mathf.Min(_rectStart.y, _rectEnd.y), Mathf.Abs(_rectEnd.x - _rectStart.x), Mathf.Abs(_rectEnd.y - _rectStart.y));
                    if (_dragRect.width >= minimumRectSize && _dragRect.height >= minimumRectSize)
                    {
                        Color col = highlightColor;
                        col.a = 0.4f;
                        Handles.BeginGUI();
                        EditorGUI.DrawRect(_dragRect, col);
                        Handles.EndGUI();
                        SceneView.RepaintAll();
                    }
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
                    if (!__holdInteraction && lastTangentPos != newPos)
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
                if (!eventModule.alt && !__holdInteraction)
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

                if (!__holdInteraction && lastPos != points[i].position)
                {
                    _tangentMode = false;
                    _pointsMoved = true;
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

                if (!_pointsMoved && !eventModule.alt && editor.eventModule.mouseLeftUp)
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
                    if (Event.current.type == EventType.Repaint)
                    {
                        SplineEditorHandles.DrawPoint(points[i].position, isSelected && (!_tangentMode || selectedPoints.Count != 1));
                    }
                }
            }
            GUI.skin.label.alignment = originalAlignment;
            GUI.skin.label.normal.textColor = originalColor;

            if (isDragging && Event.current.type == EventType.MouseDrag)
            {
                bool mouseIsOutside = false;
#if UNITY_2022_1_OR_NEWER
                Vector2 mousePos = Event.current.mousePosition;
                Vector2 viewportSize = new Vector2(_currentSceneView.position.width, _currentSceneView.position.height);
                mouseIsOutside = mousePos.x <= 0 || mousePos.y <= 0f || mousePos.x >= viewportSize.x || mousePos.y >= viewportSize.y;
#else
                mouseIsOutside = !SceneView.currentDrawingSceneView.camera.pixelRect.Contains(Event.current.mousePosition);
#endif
                if (eventModule.alt || mouseIsOutside || !eventModule.mouseLeft)
                {
                    FinishDrag();
                }
            }

            if (eventModule.mouseLeftUp)
            {
                _pointsMoved = false;
            }

            __holdInteraction = false;

            __isDragging = isDragging;
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
            if (__holdInteraction) return;
            _rectStart = position;
            _drag = true;
            _finalizeDrag = false;
        }

        public void FinishDrag()
        {
            if (!_drag) return;
            _drag = false;
            _finalizeDrag = true;
        }

        public void CancelDrag()
        {
            _drag = false;
            _finalizeDrag = false;
        }
    }
}
