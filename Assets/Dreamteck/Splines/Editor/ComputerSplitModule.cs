namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;

    public class ComputerSplitModule : ComputerEditorModule
    {

        public ComputerSplitModule(SplineComputer spline) : base(spline)
        {

        }

        public override GUIContent GetIconOff()
        {
            return IconContent("Split", "split", "Split Spline");
        }

        public override GUIContent GetIconOn()
        {
            return IconContent("Split", "split_on", "Split Spline");
        }

        protected override void OnDrawScene()
        {
            bool change = false;
            Camera editorCamera = SceneView.currentDrawingSceneView.camera;

            for (int i = 0; i < spline.pointCount; i++)
            {
                Vector3 pos = spline.GetPointPosition(i);
                if (SplineEditorHandles.CircleButton(pos, Quaternion.LookRotation(editorCamera.transform.position - pos), HandleUtility.GetHandleSize(pos) * 0.12f, 1f, spline.editorPathColor))
                {
                    SplitAtPoint(i);
                    change = true;
                    break;
                }
            }
            SplineSample projected  = spline.Evaluate(ProjectMouse());
            if (!change)
            {
                float pointValue = (float)projected.percent * (spline.pointCount - 1);
                int pointIndex = Mathf.FloorToInt(pointValue);
                    float size = HandleUtility.GetHandleSize(projected.position) * 0.3f;
                    Vector3 up = Vector3.Cross(editorCamera.transform.forward, projected.forward).normalized * size + projected.position;
                    Vector3 down = Vector3.Cross(projected.forward, editorCamera.transform.forward).normalized * size + projected.position;
                    Handles.color = spline.editorPathColor;
                    Handles.DrawLine(up, down);
                    Handles.color = Color.white;
                if (pointValue - pointIndex > spline.moveStep) { 
                    if (SplineEditorHandles.CircleButton(projected.position, Quaternion.LookRotation(editorCamera.transform.position - projected.position), HandleUtility.GetHandleSize(projected.position) * 0.12f, 1f, spline.editorPathColor))
                    {
                        SplitAtPercent(projected.percent);
                        change = true;
                    }
                }
                SceneView.RepaintAll();
            }
            Handles.color = Color.white;
            DSSplineDrawer.DrawSplineComputer(spline, 0.0, projected.percent, 1f);
            DSSplineDrawer.DrawSplineComputer(spline, projected.percent, 1.0, 0.4f);
        }
        
        
        void HandleNodes(SplineComputer newSpline, int splitIndex)
        {
            List<Node> nodes = new List<Node>();
            List<int> indices = new List<int>();

            for (int i = splitIndex; i < spline.pointCount; i++)
            {
                Node node = spline.GetNode(i);
                if(node != null)
                {
                    nodes.Add(node);
                    indices.Add(i);
                    spline.DisconnectNode(i);
                    i--;
                }
            }
            for (int i = 0; i < nodes.Count; i++) newSpline.ConnectNode(nodes[i], indices[i] - splitIndex);
        }

       void SplitAtPercent(double percent)
       {
            RecordUndo("Split Spline");
            float pointValue = (spline.pointCount - 1) * (float)percent;
            int lastPointIndex = Mathf.FloorToInt(pointValue);
            int nextPointIndex = Mathf.CeilToInt(pointValue);
            SplinePoint[] splitPoints = new SplinePoint[spline.pointCount - lastPointIndex];
            float lerpPercent = Mathf.InverseLerp(lastPointIndex, nextPointIndex, pointValue);
            SplinePoint splitPoint = SplinePoint.Lerp(spline.GetPoint(lastPointIndex), spline.GetPoint(nextPointIndex), lerpPercent);
            splitPoint.SetPosition(spline.EvaluatePosition(percent));
            splitPoints[0] = splitPoint;
            for (int i = 1; i < splitPoints.Length; i++) splitPoints[i] = spline.GetPoint(lastPointIndex + i);
            SplineComputer newSpline = CreateNewSpline();
            newSpline.SetPoints(splitPoints);

            HandleNodes(newSpline, lastPointIndex);

            SplineUser[] users = newSpline.GetSubscribers();
            for (int i = 0; i < users.Length; i++)
            {
                users[i].clipFrom = DMath.InverseLerp(percent, 1.0, users[i].clipFrom);
                users[i].clipTo = DMath.InverseLerp(percent, 1.0, users[i].clipTo);
            }
            splitPoints = new SplinePoint[lastPointIndex + 2];
            for (int i = 0; i <= lastPointIndex; i++) splitPoints[i] = spline.GetPoint(i);
            splitPoints[splitPoints.Length - 1] = splitPoint;
            spline.SetPoints(splitPoints);
            users = spline.GetSubscribers();
            for (int i = 0; i < users.Length; i++)
            {
                users[i].clipFrom = DMath.InverseLerp(0.0, percent, users[i].clipFrom);
                users[i].clipTo = DMath.InverseLerp(0.0, percent, users[i].clipTo);
            }
        }

        void SplitAtPoint(int index)
        {
            RecordUndo("Split Spline");
            SplinePoint[] splitPoints = new SplinePoint[spline.pointCount - index];
            for(int i = 0; i < splitPoints.Length; i++) splitPoints[i] = spline.GetPoint(index + i);
            SplineComputer newSpline = CreateNewSpline();
            newSpline.SetPoints(splitPoints);

            HandleNodes(newSpline, index);

            SplineUser[] users = newSpline.GetSubscribers();
            for (int i = 0; i < users.Length; i++)
            {
                users[i].clipFrom = DMath.InverseLerp((double)index / (spline.pointCount - 1), 1.0, users[i].clipFrom);
                users[i].clipTo = DMath.InverseLerp((double)index / (spline.pointCount - 1), 1.0, users[i].clipTo);
            }
            splitPoints = new SplinePoint[index + 1];
            for (int i = 0; i <= index; i++) splitPoints[i] = spline.GetPoint(i);
            spline.SetPoints(splitPoints);
            users = spline.GetSubscribers();
            for (int i = 0; i < users.Length; i++)
            {
                users[i].clipFrom = DMath.InverseLerp(0.0, ((double)index) / (spline.pointCount - 1), users[i].clipFrom);
                users[i].clipTo = DMath.InverseLerp(0.0, ((double)index) / (spline.pointCount - 1), users[i].clipTo);
            }

        }

        SplineComputer CreateNewSpline()
        {
            GameObject go = Object.Instantiate(spline.gameObject);
            Undo.RegisterCreatedObjectUndo(go, "New Spline");
            go.name = spline.name + "_split";
            SplineUser[] users = go.GetComponents<SplineUser>();
            SplineComputer newSpline = go.GetComponent<SplineComputer>();
            for (int i = 0; i < users.Length; i++)
            {
                spline.Unsubscribe(users[i]);
                users[i].spline = newSpline;
                newSpline.Subscribe(users[i]);
            }
            for(int i = go.transform.childCount-1; i>=0; i--)
            {
                Undo.DestroyObjectImmediate(go.transform.GetChild(i).gameObject);
            }
            return newSpline;
        }

        private double ProjectMouse()
        {
            if (spline.pointCount == 0) return 0.0;
            float closestDistance = (Event.current.mousePosition - HandleUtility.WorldToGUIPoint(spline.GetPointPosition(0))).sqrMagnitude;
            double closestPercent = 0.0;
            double add = spline.moveStep;
            if (spline.type == Spline.Type.Linear) add /= 2.0;
            int count = 0;
            for (double i = add; i < 1.0; i += add)
            {
                SplineSample result = spline.Evaluate(i);
                Vector2 point = HandleUtility.WorldToGUIPoint(result.position);
                float dist = (point - Event.current.mousePosition).sqrMagnitude;
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPercent = i;
                }
                count++;
            }
            return closestPercent;
        }
    }
}
