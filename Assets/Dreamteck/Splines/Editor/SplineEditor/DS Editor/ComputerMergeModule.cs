namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;

    public class ComputerMergeModule : ComputerEditorModule
    {
        SplineComputer[] availableMergeComputers = new SplineComputer[0];
        public enum MergeSide { Start, End }
        public MergeSide mergeSide = MergeSide.End;
        public bool mergeEndpoints = false;

        public ComputerMergeModule(SplineComputer spline) : base(spline)
        {

        }

        public override GUIContent GetIconOff()
        {
            return IconContent("Merge", "merge", "Merge Splines");
        }

        public override GUIContent GetIconOn()
        {
            return IconContent("Merge", "merge_on", "Merge Splines");
        }

        public override void LoadState()
        {
            mergeEndpoints = LoadBool("mergeEndpoints");
            mergeSide = (MergeSide)LoadInt("mergeSide");
        }

        public override void SaveState()
        {
            SaveBool("mergeEndpoints", mergeEndpoints);
            SaveInt("mergeSide", (int)mergeSide);
        }

        public override void Select()
        {
            base.Select();
            FindAvailableComputers();
        }

        private void FindAvailableComputers()
        {
            SplineComputer[] found = Object.FindObjectsOfType<SplineComputer>();
            List<SplineComputer> available = new List<SplineComputer>();
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != spline && !found[i].isClosed && spline.pointCount >= 2) available.Add(found[i]);
            }
            availableMergeComputers = available.ToArray();
        }

        protected override void OnDrawScene()
        {
            base.OnDrawScene();
            if (spline.isClosed) return;
            Camera editorCamera = SceneView.currentDrawingSceneView.camera;
            for (int i = 0; i < availableMergeComputers.Length; i++)
            {
                DSSplineDrawer.DrawSplineComputer(availableMergeComputers[i]);
                SplinePoint startPoint = availableMergeComputers[i].GetPoint(0);
                SplinePoint endPoint = availableMergeComputers[i].GetPoint(availableMergeComputers[i].pointCount - 1);
                Handles.color = availableMergeComputers[i].editorPathColor;

                if (SplineEditorHandles.CircleButton(startPoint.position, Quaternion.LookRotation(editorCamera.transform.position - startPoint.position), HandleUtility.GetHandleSize(startPoint.position) * 0.15f, 1f, availableMergeComputers[i].editorPathColor))
                {
                    Merge(i, MergeSide.Start);
                    break;
                }
                if (SplineEditorHandles.CircleButton(endPoint.position, Quaternion.LookRotation(editorCamera.transform.position - endPoint.position), HandleUtility.GetHandleSize(endPoint.position) * 0.15f, 1f, availableMergeComputers[i].editorPathColor))
                {
                    Merge(i, MergeSide.End);
                    break;
                }
            }
            Handles.color = Color.white;
        }

        protected override void OnDrawInspector()
        {
            base.OnDrawInspector();
            if (spline.isClosed)
            {
                EditorGUILayout.LabelField("Closed splines cannot be merged with others.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            mergeSide = (MergeSide)EditorGUILayout.EnumPopup("Merge:", mergeSide);
            mergeEndpoints = EditorGUILayout.Toggle("Merge Endpoints", mergeEndpoints);
        }

        private void Merge(int index, MergeSide mergingSide)
        {
            RegisterChange();
            SplineComputer mergedSpline = availableMergeComputers[index];
            SplinePoint[] mergedPoints = mergedSpline.GetPoints();
            SplinePoint[] original = spline.GetPoints();
            List<SplinePoint> pointsList = new List<SplinePoint>();
            SplinePoint[] points;
            if (!mergeEndpoints) points = new SplinePoint[mergedPoints.Length + original.Length];
            else points = new SplinePoint[mergedPoints.Length + original.Length - 1];

            if(mergeSide == MergeSide.End)
            {
                if(mergingSide == MergeSide.Start)
                {
                    for (int i = 0; i < original.Length; i++) pointsList.Add(original[i]);
                    for (int i = mergeEndpoints ? 1 : 0; i < mergedPoints.Length; i++) pointsList.Add(mergedPoints[i]);
                } else
                {
                    for (int i = 0; i < original.Length; i++) pointsList.Add(original[i]);
                    for (int i = 0; i < mergedPoints.Length - (mergeEndpoints ? 1 : 0); i++) pointsList.Add(mergedPoints[(mergedPoints.Length-1)-i]);
                }
            } else
            {
                if (mergingSide == MergeSide.Start)
                {
                    for (int i = 0; i < mergedPoints.Length - (mergeEndpoints ? 1 : 0); i++) pointsList.Add(mergedPoints[(mergedPoints.Length - 1) - i]);
                    for (int i = 0; i < original.Length; i++) pointsList.Add(original[i]);
                }
                else
                {
                    for (int i = mergeEndpoints ? 1 : 0; i < mergedPoints.Length; i++) pointsList.Add(mergedPoints[i]);
                    for (int i = 0; i < original.Length; i++) pointsList.Add(original[i]);
                }
            }
            points = pointsList.ToArray();
            double mergedPercent = (double)(mergedPoints.Length-1) / (points.Length-1);
            double from = 0.0;
            double to = 1.0;
            if (mergeSide == MergeSide.End)
            {
                from = 1.0 - mergedPercent;
                to = 1.0;
            }
            else
            {
                from = 0.0;
                to = mergedPercent;
            }


            List<Node> mergedNodes = new List<Node>();
            List<int> mergedIndices = new List<int>();

            for (int i = 0; i < mergedSpline.pointCount; i++)
            {
                Node node = mergedSpline.GetNode(i);
                if (node != null)
                {
                    mergedNodes.Add(node);
                    mergedIndices.Add(i);
                    Undo.RecordObject(node, "Disconnect Node");
                    mergedSpline.DisconnectNode(i);
                    i--;
                }
            }

            SplineUser[] subs = mergedSpline.GetSubscribers();
            for (int i = 0; i < subs.Length; i++)
            {
                mergedSpline.Unsubscribe(subs[i]);
                subs[i].spline = spline;
                subs[i].clipFrom = DMath.Lerp(from, to, subs[i].clipFrom);
                subs[i].clipTo = DMath.Lerp(from, to, subs[i].clipTo);
            }
            spline.SetPoints(points);

            if (mergeSide == MergeSide.Start)
            {
                spline.ShiftNodes(0, spline.pointCount - 1, mergedSpline.pointCount);
                for (int i = 0; i < mergedNodes.Count; i++)
                {
                    spline.ConnectNode(mergedNodes[i], mergedIndices[i]);
                }
            } else
            {
                for (int i = 0; i < mergedNodes.Count; i++)
                {
                    int connectIndex = mergedIndices[i] + original.Length;
                    if (mergeEndpoints) connectIndex--;
                    spline.ConnectNode(mergedNodes[i], connectIndex);
                }
            }
            if (EditorUtility.DisplayDialog("Keep merged computer's GameObject?", "Do you want to keep the merged computer's game object?", "Yes", "No"))
            {
                Undo.DestroyObjectImmediate(mergedSpline);
            }
            else
            {
                for (int i = 0; i < mergedNodes.Count; i++)
                {
                    if(TransformUtility.IsParent(mergedNodes[i].transform, mergedSpline.transform))
                    {
                        Undo.SetTransformParent(mergedNodes[i].transform, mergedSpline.transform.parent, "Reparent Node");
                    }
                }
                Undo.DestroyObjectImmediate(mergedSpline.gameObject);
            }

            FindAvailableComputers();
        }
    }
}
