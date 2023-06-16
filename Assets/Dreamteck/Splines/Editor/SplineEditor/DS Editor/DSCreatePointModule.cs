namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class DSCreatePointModule : CreatePointModule
    {
        DreamteckSplinesEditor dsEditor;
        private bool createNode = false;

        public DSCreatePointModule(SplineEditor editor) : base(editor)
        {
            dsEditor = (DreamteckSplinesEditor)editor;
        }

        public override void LoadState()
        {
            base.LoadState();
            createNode = LoadBool("createNode");
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveBool("createNode", createNode);
        }

        protected override void OnDrawInspector()
        {
            base.OnDrawInspector();
            createNode = EditorGUILayout.Toggle("Create Node", createNode);
        }

        protected override void CreateSplinePoint(Vector3 position, Vector3 normal)
        {
            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            List<int> indices = new List<int>();
            List<Node> nodes = new List<Node>();
            SplineComputer spline = dsEditor.spline;

            dsEditor.CacheTriggerPositions();

            if (!isClosed && points.Length >= 3)
            {
                Vector2 first = HandleUtility.WorldToGUIPoint(points[0].position);
                Vector2 last = HandleUtility.WorldToGUIPoint(position);
                if (Vector2.Distance(first, last) <= 20f)
                {
                    if (EditorUtility.DisplayDialog("Close spline?", "Do you want to make the spline path closed ?", "Yes", "No"))
                    {
                        editor.SetSplineClosed(true);
                        spline.EditorSetAllPointsDirty();
                        RegisterChange();
                        SceneView.currentDrawingSceneView.Focus();
                        SceneView.RepaintAll();
                        return;
                    }
                }
            }  

            AddPoint();

            if (appendMode == AppendMode.End)
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    nodes[i].AddConnection(spline, indices[i] + 1);
                }
            }

            dsEditor.ApplyModifiedProperties(true);
            dsEditor.WriteTriggerPositions();
            RegisterChange();
            if (appendMode == AppendMode.Beginning)
            {
                spline.ShiftNodes(0, spline.pointCount - 1, 1);
            }

            if (createNode)
            {
                dsEditor.ApplyModifiedProperties();
                if (appendMode == 0)
                {
                    CreateNodeForPoint(0);
                }
                else
                {
                    CreateNodeForPoint(points.Length - 1);
                }
            }
        }

        protected override void InsertMode(Vector3 screenCoordinates)
        {
            base.InsertMode(screenCoordinates);
            double percent = ProjectScreenSpace(screenCoordinates);
            editor.evaluate(percent, ref evalResult);
            if (editor.eventModule.mouseRight)
            {
                SplineEditorHandles.DrawCircle(evalResult.position, Quaternion.LookRotation(editorCamera.transform.position - evalResult.position), HandleUtility.GetHandleSize(evalResult.position) * 0.2f);
                return;
            }
            if (SplineEditorHandles.CircleButton(evalResult.position, Quaternion.LookRotation(editorCamera.transform.position - evalResult.position), HandleUtility.GetHandleSize(evalResult.position) * 0.2f, 1.5f, color))
            {
                dsEditor.CacheTriggerPositions();
                SplinePoint newPoint = new SplinePoint(evalResult.position, evalResult.position);
                newPoint.size = evalResult.size;
                newPoint.color = evalResult.color;
                newPoint.normal = evalResult.up;
                
                
                int pointIndex = dsEditor.spline.PercentToPointIndex(percent);
                editor.AddPointAt(pointIndex + 1);
                points[pointIndex + 1].SetPoint(newPoint);
                SplineComputer spline = dsEditor.spline;
                lastCreated = points.Length - 1;
                editor.ApplyModifiedProperties(true);
                spline.ShiftNodes(pointIndex + 1, spline.pointCount - 1, 1);
                if (createNode) CreateNodeForPoint(pointIndex + 1);
                RegisterChange();
                dsEditor.WriteTriggerPositions();
            }
        }

        void CreateNodeForPoint(int index)
        {
            GameObject obj = new GameObject("Node_" + (points.Length - 1));
            obj.transform.parent = dsEditor.spline.transform;
            Node node = obj.AddComponent<Node>();
            node.transform.localRotation = Quaternion.identity;
            node.transform.position = points[index].position;
            Undo.SetCurrentGroupName("Create Node For Point " + index);
            Undo.RegisterCreatedObjectUndo(obj, "Create Node object");
            Undo.RegisterCompleteObjectUndo(dsEditor.spline, "Link Node");
            dsEditor.spline.ConnectNode(node, index);
        }
    }
}
