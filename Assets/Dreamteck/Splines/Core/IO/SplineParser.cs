using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines.IO
{
    public class SplineParser 
    {
        protected string fileName = "";
        public string name
        {
            get { return fileName; }
        }

        private System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en-US");
        private System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Any;

        internal class Transformation
        {
            protected static Matrix4x4 matrix = new Matrix4x4();

            internal static void ResetMatrix()
            {
                matrix.SetTRS(Vector3.zero, Quaternion.identity, Vector3.one);
            }

            internal virtual void Push()
            {

            }

            internal static void Apply(SplinePoint[] points)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    SplinePoint p = points[i];
                    p.position = matrix.MultiplyPoint(p.position);
                    p.tangent = matrix.MultiplyPoint(p.tangent);
                    p.tangent2 = matrix.MultiplyPoint(p.tangent2);
                    points[i] = p;
                }
            }
        }

        internal class Translate : Transformation
        {
            private Vector2 offset = Vector2.zero;
            public Translate(Vector2 o)
            {
                offset = o;
            }

            internal override void Push()
            {
                Matrix4x4 translate = new Matrix4x4();
                translate.SetTRS(new Vector2(offset.x, -offset.y), Quaternion.identity, Vector3.one);
                matrix = matrix * translate;
            }
        }

        internal class Rotate : Transformation
        {
            private float angle = 0f;
            public Rotate(float a)
            {
                angle = a;
            }

            internal override void Push()
            {
                Matrix4x4 rotate = new Matrix4x4();
                rotate.SetTRS(Vector3.zero, Quaternion.AngleAxis(angle, Vector3.back), Vector3.one);
                matrix = matrix * rotate;
            }
        }

        internal class Scale : Transformation
        {
            private Vector2 multiplier = Vector2.one;
            public Scale(Vector2 s)
            {
                multiplier = s;
            }

            internal override void Push()
            {
                Matrix4x4 scale = new Matrix4x4();
                scale.SetTRS(Vector3.zero, Quaternion.identity, multiplier);
                matrix = matrix * scale;
            }
        }

        internal class SkewX : Transformation
        {
            private float amount = 0f;
            public SkewX(float a)
            {
                amount = a;
            }

            internal override void Push()
            {
                Matrix4x4 skew = new Matrix4x4();
                skew[0, 0] = 1.0f;
                skew[1, 1] = 1.0f;
                skew[2, 2] = 1.0f;
                skew[3, 3] = 1.0f;
                skew[0, 1] = Mathf.Tan(-amount * Mathf.Deg2Rad);
                matrix = matrix * skew;
            }
        }

        internal class SkewY : Transformation
        {
            private float amount = 0f;
            public SkewY(float a)
            {
                amount = a;
            }

            internal override void Push()
            {
                Matrix4x4 skew = new Matrix4x4();
                skew[0, 0] = 1.0f;
                skew[1, 1] = 1.0f;
                skew[2, 2] = 1.0f;
                skew[3, 3] = 1.0f;
                skew[1, 0] = Mathf.Tan(-amount * Mathf.Deg2Rad);
                matrix = matrix *skew;
            }
        }

        internal class MatrixTransform : Transformation
        {
            private Matrix4x4 transformMatrix = new Matrix4x4();

            public MatrixTransform(float a, float b, float c, float d, float e, float f)
            { 
                transformMatrix.SetRow(0, new Vector4(a, c, 0f, e));
                transformMatrix.SetRow(1, new Vector4(b, d, 0f, -f));
                transformMatrix.SetRow(2, new Vector4(0f, 0f, 1f, 0f));
                transformMatrix.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
            }

            internal override void Push()
            {
                matrix = matrix * transformMatrix;
            }
        }


        internal class SplineDefinition
        {
            internal string name = "";
            internal Spline.Type type = Spline.Type.Linear;
            internal List<SplinePoint> points = new List<SplinePoint>();
            internal bool closed = false;

            internal int pointCount
            {
                get { return points.Count; }
            }

            internal Vector3 position = Vector3.zero;
            internal Vector3 tangent = Vector3.zero;
            internal Vector3 tangent2 = Vector3.zero;
            internal Vector3 normal = Vector3.back;
            internal float size = 1f;
            internal Color color = Color.white;

            internal SplineDefinition(string n, Spline.Type t)
            {
                name = n;
                type = t;
            }

            internal SplineDefinition(string n, Spline spline)
            {
                name = n;
                type = spline.type;
                closed = spline.isClosed;
                points = new List<SplinePoint>(spline.points);
            }

            internal SplinePoint GetLastPoint()
            {
                if (points.Count == 0) return new SplinePoint();
                return points[points.Count - 1];
            }

            internal void SetLastPoint(SplinePoint point)
            {
                if (points.Count == 0) return;
                points[points.Count - 1] = point;
            }

            internal void CreateClosingPoint()
            {
                SplinePoint p = new SplinePoint(points[0]);
                points.Add(p);
            }

            internal void CreateSmooth()
            {
                points.Add(new SplinePoint(position, tangent, normal, size, color));
            }

            internal void CreateBroken()
            {
                SplinePoint point = new SplinePoint(new SplinePoint(position, tangent, normal, size, color));
                point.type = SplinePoint.Type.Broken;
                point.SetTangent2Position(point.position);
                point.normal = normal;
                point.color = color;
                point.size = size;
                points.Add(point);
            }

            internal void CreateLinear()
            {
                tangent = position;
                CreateSmooth();
            }

            internal SplineComputer CreateSplineComputer(Vector3 position, Quaternion rotation)
            {
                GameObject go = new GameObject(name);
                go.transform.position = position;
                go.transform.rotation = rotation;
                SplineComputer computer = go.AddComponent<SplineComputer>();
#if UNITY_EDITOR
                if(Application.isPlaying) computer.ResampleTransform();
#endif
                computer.type = type;
                if(closed)
                {
                    if (points[0].type == SplinePoint.Type.Broken) points[0].SetTangentPosition(GetLastPoint().tangent2);
                }
                computer.SetPoints(points.ToArray(), SplineComputer.Space.Local);
                if (closed) computer.Close();
                return computer;
            }

            internal Spline CreateSpline()
            {
                Spline spline = new Spline(type);
                spline.points = points.ToArray();
                if (closed) spline.Close();
                return spline;
            }

            internal void Transform(List<Transformation> trs)
            {
                SplinePoint[] p = points.ToArray();
                Transformation.ResetMatrix();
                foreach(Transformation t in trs) t.Push();
                Transformation.Apply(p);
                for (int i = 0; i < p.Length; i++) points[i] = p[i];
                SplinePoint[] debugPoints = new SplinePoint[1];
                debugPoints[0] = new SplinePoint();
                Transformation.Apply(debugPoints);
            }
        }

        internal SplineDefinition buffer = null;

        internal Vector2[] ParseVector2(string coord)
        {
            List<float> list = ParseFloatArray(coord.Substring(1));
            int count = list.Count / 2;
            if (count == 0)
            {
                Debug.Log("Error in " + coord);
                return new Vector2[] { Vector2.zero };
            }
            Vector2[] vectors = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vectors[i] = new Vector2(list[0 + i * 2], -list[1 + i * 2]);
            }
            return vectors;
        }

        internal float[] ParseFloat(string coord)
        {
            List<float> list = ParseFloatArray(coord.Substring(1));
            if (list.Count < 1)
            {
                Debug.Log("Error in " + coord);
                return new float[] { 0f };
            }
            return list.ToArray();
        }

        internal List<float> ParseFloatArray(string content)
        {
            string accumulated = "";
            List<float> list = new List<float>();
            foreach (char c in content)
            {
                if (c == ',' || c == '-' || char.IsWhiteSpace(c) || (accumulated.Contains(".") && c == '.'))
                {
                    if (!IsWHiteSpace(accumulated))
                    {
                        float parsed = 0f;
                        float.TryParse(accumulated, style, culture, out parsed);
                        list.Add(parsed);
                        accumulated = "";
                        if (c == '-') accumulated = "-";
                        if (c == '.') accumulated = "0.";
                        continue;
                    }
                }
                if (!char.IsWhiteSpace(c)) accumulated += c;
            }
            if (!IsWHiteSpace(accumulated))
            {
                float p = 0f;
                float.TryParse(accumulated, style, culture, out p);
                list.Add(p);
            }
            return list;
        }

        public bool IsWHiteSpace(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsWhiteSpace(c))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
