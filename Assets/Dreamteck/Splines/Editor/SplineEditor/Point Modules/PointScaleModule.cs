namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public class PointScaleModule : PointTransformModule
    {
        public bool scaleSize = true;
        public bool scaleTangents = true;


        public PointScaleModule(SplineEditor editor) : base(editor)
        {
        }

        public override GUIContent GetIconOff()
        {
            return EditorGUIUtility.IconContent("ScaleTool");
        }

        public override GUIContent GetIconOn()
        {
            return EditorGUIUtility.IconContent("ScaleTool On");
        }

        public override void LoadState()
        {
            base.LoadState();
            scaleSize = LoadBool("scaleSize");
            scaleTangents = LoadBool("scaleTangents");
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveBool("scaleSize", scaleSize);
            SaveBool("scaleTangents", scaleTangents);
        }

        protected override void OnDrawInspector()
        {
            editSpace = (EditSpace)EditorGUILayout.EnumPopup("Edit Space", editSpace);
            scaleSize = EditorGUILayout.Toggle("Scale Sizes", scaleSize);
            scaleTangents = EditorGUILayout.Toggle("Scale Tangents", scaleTangents);
        }

        protected override void OnDrawScene()
        {
            if (selectedPoints.Count == 0) return;
            if (eventModule.mouseLeftUp)
            {
                Reset();
            }
            Vector3 lastScale = scale;
            Vector3 c = selectionCenter;
            scale = Handles.ScaleHandle(scale, c, rotation, HandleUtility.GetHandleSize(c));
            if (lastScale != scale)
            {
                PrepareTransform();
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    var point = localPoints[selectedPoints[i]];
                    TransformPoint(ref point, false, scaleTangents, scaleSize);
                    points[selectedPoints[i]].SetPoint(point);
                }
                RegisterChange();
                SetDirty();
            }
        }
    }
}
