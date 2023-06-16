namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public class PointMoveModule : PointTransformModule
    {
        public bool snap = false;
        public float snapGridSize = 1f;
        public bool surfaceMode = false;
        public float surfaceOffset = 0f;
        public LayerMask surfaceLayerMask = ~0;

        private bool useTangentHandles => editor.mainModule.tangentMode || editor.selectedPoints.Count != 1;

        public PointMoveModule(SplineEditor editor) : base(editor)
        {

        }

        public override GUIContent GetIconOff()
        {
            return EditorGUIUtility.IconContent("MoveTool");
        }

        public override GUIContent GetIconOn()
        {
            return EditorGUIUtility.IconContent("MoveTool On");
        }

        public override void LoadState()
        {
            base.LoadState();
            snap = LoadBool("snap");
            snapGridSize = LoadFloat("snapGridSize", 0.5f);
            surfaceOffset = LoadFloat("surfaceOffset", 0f);
            surfaceMode = LoadBool("surfaceMode");
            surfaceLayerMask = LoadInt("surfaceLayerMask", ~0);
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveBool("snap", snap);
            SaveFloat("snapGridSize", snapGridSize);
            SaveFloat("surfaceOffset", surfaceOffset);
            SaveBool("surfaceMode", surfaceMode);
            SaveInt("surfaceLayerMask", surfaceLayerMask);
        }

        public override void BeforeSceneDraw(SceneView current)
        {
            base.BeforeSceneDraw(current);
            if (Event.current.type == EventType.MouseUp) GetRotation();
        }

        protected override void OnDrawInspector()
        {
            editSpace = (EditSpace)EditorGUILayout.EnumPopup("Edit Space", editSpace);
            surfaceMode = EditorGUILayout.Toggle("Move On Surface", surfaceMode);
            if (surfaceMode)
            {
                surfaceLayerMask = DreamteckEditorGUI.LayermaskField("Surface Mask", surfaceLayerMask);
                surfaceOffset = EditorGUILayout.FloatField("Surface Offset", surfaceOffset);
            }
            snap = EditorGUILayout.Toggle("Snap to Grid", snap);
            if (snap)
            {
                snapGridSize = EditorGUILayout.FloatField("Grid Size", snapGridSize);
                if (snapGridSize < 0.0001f) snapGridSize = 0.0001f;
            }
        }

        private Vector3 SurfaceMoveHandle(Vector3 inputPosition, float size = 0.2f)
        {
            Vector3 lastPosition = inputPosition;
            inputPosition = SplineEditorHandles.FreeMoveHandle(inputPosition, HandleUtility.GetHandleSize(inputPosition) * size, Vector3.zero, Handles.CircleHandleCap);
            if (lastPosition != inputPosition)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, surfaceLayerMask))
                {
                    inputPosition = hit.point + hit.normal * surfaceOffset;
                    Handles.DrawLine(hit.point, hit.point + hit.normal * HandleUtility.GetHandleSize(hit.point) * 0.5f);
                }
            }
            return inputPosition;
        }

        protected override void OnDrawScene()
        {
            if (selectedPoints.Count == 0) return;
            Vector3 c = selectionCenter;
            Vector3 lastPos = c;
            if (surfaceMode)
            {
                c = SurfaceMoveHandle(c, 0.2f);
            }
            else
            {
                c = Handles.PositionHandle(c, rotation);
            }
            if (lastPos != c)
            {
                RegisterChange();
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    points[selectedPoints[i]].SetPosition(points[selectedPoints[i]].position + (c - lastPos));
                    if (snap) points[selectedPoints[i]].SetPosition(SnapPoint(points[selectedPoints[i]].position));
                }
            }

            if (splineType == Spline.Type.Bezier && selectedPoints.Count == 1 && useTangentHandles)
            {
                int index = selectedPoints[0];
                lastPos = points[index].tangent;
                Vector3 newPos = Vector3.zero;
                if (surfaceMode)
                {
                    newPos = SurfaceMoveHandle(points[index].tangent, 0.15f);
                } else
                {
                    newPos = Handles.PositionHandle(points[index].tangent, rotation);
                }

                if (snap) newPos = SnapPoint(newPos);
                if (newPos != lastPos)
                {
                    RegisterChange();
                }
                points[index].SetTangentPosition(newPos);

                lastPos = points[index].tangent2;
                if (surfaceMode)
                {
                    newPos = SurfaceMoveHandle(points[index].tangent2, 0.15f);
                } else
                {
                    newPos = Handles.PositionHandle(points[index].tangent2, rotation);
                }
                    
                if (snap) newPos = SnapPoint(newPos);
                if (newPos != lastPos)
                {
                    RegisterChange();
                }
                points[index].SetTangent2Position(newPos);
            }
        }

        public Vector3 SnapPoint(Vector3 point)
        {
            point.x = Mathf.RoundToInt(point.x / snapGridSize) * snapGridSize;
            point.y = Mathf.RoundToInt(point.y / snapGridSize) * snapGridSize;
            point.z = Mathf.RoundToInt(point.z / snapGridSize) * snapGridSize;
            return point;
        }
    }
}
