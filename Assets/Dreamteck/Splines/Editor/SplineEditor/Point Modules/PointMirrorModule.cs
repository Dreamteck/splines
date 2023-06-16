namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;

    public class PointMirrorModule : PointTransformModule
    {
        public enum Axis { X, Y, Z }
        public Axis axis = Axis.X;
        public bool flip = false;
        public float weldDistance = 0f;
        Vector3 mirrorCenter = Vector3.zero;


        private SplinePoint[] mirrored = new SplinePoint[0];


        public PointMirrorModule(SplineEditor editor) : base(editor)
        {
            LoadState();
        }

        public override GUIContent GetIconOff()
        {
            return IconContent("||", "mirror", "Mirror Path");
        }

        public override GUIContent GetIconOn()
        {
            return IconContent("||", "mirror_on", "Mirror Path");
        }

        public override void LoadState()
        {
            axis = (Axis)LoadInt("axis");
            flip = LoadBool("flip");
            weldDistance = LoadFloat("weldDistance");
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveInt("axis", (int)axis);
            SaveBool("flip", flip);
            SaveFloat("weldDistance", weldDistance);
        }

        public override void Select()
        {
            base.Select();
            ClearSelection();
            DoMirror();
            SetDirty();
        }

        public override void Deselect()
        {
            if (IsDirty())
            {
                if (EditorUtility.DisplayDialog("Unapplied Mirror Operation", "There is an unapplied mirror operation. Do you want to apply the changes?", "Apply", "Revert"))
                {
                    Apply();
                }
                else
                {
                    Revert();
                }
            }
            base.Deselect();
        }

        protected override void OnDrawInspector()
        {
            if (selectedPoints.Count > 0) ClearSelection();
            EditorGUI.BeginChangeCheck();
            axis = (Axis)EditorGUILayout.EnumPopup("Axis", axis);
            flip = EditorGUILayout.Toggle("Flip", flip);
            weldDistance = EditorGUILayout.FloatField("Weld Distance", weldDistance);
            mirrorCenter = EditorGUILayout.Vector3Field("Center", mirrorCenter);
            if (EditorGUI.EndChangeCheck()) DoMirror();
            if (IsDirty())
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply")) Apply();
                if (GUILayout.Button("Revert")) Revert();
                EditorGUILayout.EndHorizontal();
            }
        }

        protected override void OnDrawScene()
        {
            if (selectedPoints.Count > 0) ClearSelection();
            Vector3 worldCenter = TransformPosition(mirrorCenter);
            Vector3 lastCenter = worldCenter;
            worldCenter = Handles.PositionHandle(worldCenter, rotation);
            mirrorCenter = InverseTransformPosition(worldCenter);
            DrawMirror();
            if (lastCenter != worldCenter) DoMirror();
            selectedPoints.Clear();
        }

        public void DoMirror()
        {
            List<int> half = GetHalf(ref originalPoints);
            int welded = -1;
            if (half.Count > 0)
            {
                if (flip)
                {
                    if (IsWeldable(originalPoints[half[0]]))
                    {
                        welded = half[0];
                        half.RemoveAt(0);
                    }
                }
                else
                {
                    if (IsWeldable(originalPoints[half[half.Count - 1]]))
                    {
                        welded = half[half.Count - 1];
                        half.RemoveAt(half.Count - 1);
                    }
                }

                int offset = welded >= 0 ? 1 : 0;
                int mirroredLength = half.Count * 2 + offset;
                if(mirrored.Length != mirroredLength) mirrored = new SplinePoint[mirroredLength];
                for (int i = 0; i < half.Count; i++)
                {
                    if (flip)
                    {
                        mirrored[i] = new SplinePoint(originalPoints[half[(half.Count - 1) - i]]);
                        mirrored[i + half.Count + offset] = GetMirrored(originalPoints[half[i]]);
                        SwapTangents(ref mirrored[i]);
                        SwapTangents(ref mirrored[i + half.Count + offset]);
                    }
                    else
                    {
                        mirrored[i] = new SplinePoint(originalPoints[half[i]]);
                        mirrored[i + half.Count + offset] = GetMirrored(originalPoints[half[(half.Count - 1) - i]]);
                    }
                }
                if (welded >= 0)
                {
                    mirrored[half.Count] = new SplinePoint(originalPoints[welded]);
                    if (flip) SwapTangents(ref mirrored[half.Count]);
                    MakeMiddlePoint(ref mirrored[half.Count]);
                }

                if (isClosed && mirrored.Length > 0)
                {
                    MakeMiddlePoint(ref mirrored[0]);
                    mirrored[mirrored.Length - 1] = new SplinePoint(mirrored[0]);
                }
            }
            else mirrored = new SplinePoint[0];
            editor.SetPointsArray(mirrored);
            RegisterChange();
            SetDirty();
        }

        void SwapTangents(ref SplinePoint point)
        {
            Vector3 temp = point.tangent;
            point.tangent = point.tangent2;
            point.tangent2 = temp;
        }

        void MakeMiddlePoint(ref SplinePoint point)
        {
            point.type = SplinePoint.Type.Broken;
            InverseTransformPoint(ref point);
            Vector3 newPos = point.position;
            switch (axis)
            {
                case Axis.X:
                   
                    newPos.x = mirrorCenter.x;
                    point.SetPosition(newPos);
                    if ((point.tangent.x >= mirrorCenter.x && flip) || (point.tangent.x <= mirrorCenter.x && !flip))
                    {
                        point.tangent2 = point.tangent;
                        point.SetTangent2X(point.position.x + (point.position.x - point.tangent.x));
                    }
                    else
                    {
                        point.tangent = point.tangent2;
                        point.SetTangentX(point.position.x + (point.position.x - point.tangent2.x));
                    }
                    break;
                case Axis.Y:
                    newPos.y = mirrorCenter.y;
                    point.SetPosition(newPos);
                    if ((point.tangent.y >= mirrorCenter.y && flip) || (point.tangent.y <= mirrorCenter.y && !flip))
                    {
                        point.tangent2 = point.tangent;
                        point.SetTangent2Y(point.position.y + (point.position.y - point.tangent.y));
                    }
                    else
                    {
                        point.tangent = point.tangent2;
                        point.SetTangentY(point.position.y + (point.position.y - point.tangent2.y));
                    }
                    break;
                case Axis.Z:
                    newPos.z = mirrorCenter.z;
                    point.SetPosition(newPos);
                    if ((point.tangent.z >= mirrorCenter.z && flip) || (point.tangent.z <= mirrorCenter.z && !flip))
                    {
                        point.tangent2 = point.tangent;
                        point.SetTangent2Z(point.position.z + (point.position.z - point.tangent.z));
                    }
                    else
                    {
                        point.tangent = point.tangent2;
                        point.SetTangentZ(point.position.z + (point.position.z - point.tangent2.z));
                    }
                    break;
            }
            TransformPoint(ref point);
        }

        bool IsWeldable(SplinePoint point)
        {
            switch (axis)
            {
                case Axis.X:
                    if (Mathf.Abs(point.position.x - mirrorCenter.x) <= weldDistance) return true;
                    break;
                case Axis.Y:
                    if (Mathf.Abs(point.position.y - mirrorCenter.y) <= weldDistance) return true;
                    break;
                case Axis.Z:
                    if (Mathf.Abs(point.position.z - mirrorCenter.z) <= weldDistance) return true;
                    break;
            }
            return false;
        }

        void DrawMirror()
        {
            Vector3[] points = new Vector3[4];
            Color color = Color.white;
            Vector3 worldCenter = TransformPosition(mirrorCenter);
            float size = HandleUtility.GetHandleSize(worldCenter);
            Vector3 forward = rotation * Vector3.forward * size;
            Vector3 back = -forward;
            Vector3 right = rotation * Vector3.right * size;
            Vector3 left = -right;
            Vector3 up = rotation * Vector3.up * size;
            Vector3 down = -up;
            switch (axis)
            {
                case Axis.X:
                    points[0] = back + up;
                    points[1] = forward + up;
                    points[2] = forward + down;
                    points[3] = back + down;
                    color = Color.red;
                    break;
                case Axis.Y:
                    points[0] = back + left;
                    points[1] = forward + left;
                    points[2] = forward + right;
                    points[3] = back + right;
                    color = Color.green;
                    break;
                case Axis.Z:
                    points[0] = left + up;
                    points[1] = right + up;
                    points[2] = right + down;
                    points[3] = left + down;
                    color = Color.blue;
                    break;
            }
            Handles.color = color;
            Handles.DrawLine(worldCenter + points[0], worldCenter + points[1]);
            Handles.DrawLine(worldCenter + points[1], worldCenter + points[2]);
            Handles.DrawLine(worldCenter + points[2], worldCenter + points[3]);
            Handles.DrawLine(worldCenter + points[3], worldCenter + points[0]);
            Handles.color = Color.white;
        }

        SplinePoint GetMirrored(SplinePoint source)
        {
            SplinePoint newPoint = new SplinePoint(source);
            InverseTransformPoint(ref newPoint);
            switch (axis)
            {
                case Axis.X:
                    newPoint.SetPositionX(mirrorCenter.x - (newPoint.position.x - mirrorCenter.x));
                    newPoint.SetNormalX(-newPoint.normal.x);
                    newPoint.SetTangentX(mirrorCenter.x - (newPoint.tangent.x - mirrorCenter.x));
                    newPoint.SetTangent2X(mirrorCenter.x - (newPoint.tangent2.x - mirrorCenter.x));
                    break;
                case Axis.Y:
                    newPoint.SetPositionY(mirrorCenter.y - (newPoint.position.y - mirrorCenter.y));
                    newPoint.SetNormalY(-newPoint.normal.y);
                    newPoint.SetTangentY(mirrorCenter.y - (newPoint.tangent.y - mirrorCenter.y));
                    newPoint.SetTangent2Y(mirrorCenter.y - (newPoint.tangent2.y - mirrorCenter.y));
                    break;
                case Axis.Z:
                    newPoint.SetPositionZ(mirrorCenter.z - (newPoint.position.z - mirrorCenter.z));
                    newPoint.SetNormalZ(-newPoint.normal.z);
                    newPoint.SetTangentZ(mirrorCenter.z - (newPoint.tangent.z - mirrorCenter.z));
                    newPoint.SetTangent2Z(mirrorCenter.z - (newPoint.tangent2.z - mirrorCenter.z));
                    break;
            }
            SwapTangents(ref newPoint);
            TransformPoint(ref newPoint);
            return newPoint;
        }



        List<int> GetHalf(ref SplinePoint[] points)
        {
            List<int> found = new List<int>();
            switch (axis)
            {
                case Axis.X:

                    for (int i = 0; i < points.Length; i++)
                    {
                        if (flip)
                        {
                            if (InverseTransformPosition(points[i].position).x >= mirrorCenter.x) found.Add(i);
                        }
                        else
                        {
                            if (InverseTransformPosition(points[i].position).x <= mirrorCenter.x) found.Add(i);
                        }
                    }
                    break;

                case Axis.Y:
                    for (int i = 0; i < points.Length; i++)
                    {
                        if (flip)
                        {
                            if (InverseTransformPosition(points[i].position).y >= mirrorCenter.y) found.Add(i);
                            else
                            {
                                if (InverseTransformPosition(points[i].position).y <= mirrorCenter.y) found.Add(i);
                            }
                        }
                    }
                    break;
                case Axis.Z:
                    for (int i = 0; i < points.Length; i++)
                    {
                        if (flip)
                        {
                            if (InverseTransformPosition(points[i].position).z >= mirrorCenter.z) found.Add(i);
                        }
                        else
                        {
                            if (InverseTransformPosition(points[i].position).z <= mirrorCenter.z) found.Add(i);
                        }
                    }
                    break;
            }
            return found;
        }



    }
}
