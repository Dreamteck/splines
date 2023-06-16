namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public class PointRotateModule : PointTransformModule
    {
        public bool rotateNormals = true;
        public bool rotateTangents = true;

        public PointRotateModule(SplineEditor editor) : base(editor)
        {
        }

        public override GUIContent GetIconOff()
        {
            return EditorGUIUtility.IconContent("RotateTool");
        }

        public override GUIContent GetIconOn()
        {
            return EditorGUIUtility.IconContent("RotateTool On");
        }

        public override void LoadState()
        {
            base.LoadState();
            rotateNormals = LoadBool("rotateNormals");
            rotateTangents = LoadBool("rotateTangents");
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveBool("rotateNormals", rotateNormals);
            SaveBool("rotateTangents", rotateTangents);
        }

        protected override void OnDrawInspector()
        {
            editSpace = (EditSpace)EditorGUILayout.EnumPopup("Edit Space", editSpace);
            rotateNormals = EditorGUILayout.Toggle("Rotate Normals", rotateNormals);
            rotateTangents = EditorGUILayout.Toggle("Rotate Tangents", rotateTangents);
        }

        protected override void OnDrawScene()
        {
            if (selectedPoints.Count == 0) return;
            if (rotateNormals)
            {
                Handles.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.4f);
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    Vector3 normal = points[selectedPoints[i]].normal;
                    normal *= HandleUtility.GetHandleSize(points[selectedPoints[i]].position);
                    Handles.DrawLine(points[selectedPoints[i]].position, points[selectedPoints[i]].position + normal);
                    SplineEditorHandles.DrawArrowCap(points[selectedPoints[i]].position + normal, Quaternion.LookRotation(normal), HandleUtility.GetHandleSize(points[selectedPoints[i]].position));
                }
            }
            Handles.color = Color.white;
            Quaternion lastRotation = rotation;
            rotation = Handles.RotationHandle(lastRotation, selectionCenter);
            if (lastRotation != rotation)
            {
                PrepareTransform();
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    var point = localPoints[selectedPoints[i]];
                    TransformPoint(ref point, rotateNormals, rotateTangents);
                    points[selectedPoints[i]].SetPoint(point);
                }
                RegisterChange();
                SetDirty();
            }
        }
    }
}
