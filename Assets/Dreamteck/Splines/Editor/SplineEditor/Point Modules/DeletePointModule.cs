namespace Dreamteck.Splines.Editor
{
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

    public class DeletePointModule : PointModule
    {
        public float deleteRadius = 50f;
        Vector2 lastMousePos = Vector2.zero;


        public DeletePointModule(SplineEditor editor) : base(editor)
        {

        }

        public override GUIContent GetIconOff()
        {
            return IconContent("-", "remove", "Delete Points");
        }

        public override GUIContent GetIconOn()
        {
            return IconContent("-", "remove_on", "Delete Points");
        }

        public override void LoadState()
        {
            base.LoadState();
            deleteRadius = LoadFloat("deleteRadius", 50f);
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveFloat("deleteRadius", deleteRadius);
        }

        protected override void OnDrawInspector()
        {
            deleteRadius = EditorGUILayout.FloatField("Brush Radius", deleteRadius);
        }

        protected override void OnDrawScene()
        {
            if (selectedPoints.Count > 0) ClearSelection();
            Handles.BeginGUI();
            Handles.color = Color.red;
            Handles.DrawWireDisc(Event.current.mousePosition, -Vector3.forward, deleteRadius);
            Handles.color = Color.white;
            Handles.EndGUI();
            if (!eventModule.alt && SceneView.currentDrawingSceneView.camera.pixelRect.Contains(Event.current.mousePosition)) {
                if (editor.eventModule.mouseLeftDown) GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                if (editor.eventModule.mouseLeft && lastMousePos != Event.current.mousePosition)
                {
                    lastMousePos = Event.current.mousePosition;
                    RunDeleteMethod();
                }
            }
            Repaint();
        }

        void RunDeleteMethod()
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            Vector3 mousPos = Event.current.mousePosition;
            Rect mouseRect = new Rect(mousPos.x - deleteRadius, mousPos.y - deleteRadius, deleteRadius * 2f, deleteRadius * 2f);
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 localPos = cam.transform.InverseTransformPoint(points[i].position);
                if (localPos.z < 0f) continue;
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(points[i].position);
                if (mouseRect.Contains(screenPos))
                {
                    if (Vector2.Distance(mousPos, screenPos) <= deleteRadius)
                    {
                        DeletePoint(i);
                        editor.ApplyModifiedProperties(true);
                        i--;
                    }
                }
            }
        }
    }
}
