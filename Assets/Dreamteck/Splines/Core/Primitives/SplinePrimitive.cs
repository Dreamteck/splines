using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines.Primitives {
    public class SplinePrimitive
    {
        protected bool closed = false;
        protected SplinePoint[] points = new SplinePoint[0];

        public Vector3 offset = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public bool is2D = false;

        public virtual void Calculate()
        {
            Generate();
            ApplyOffset();
        }

        protected virtual void Generate()
        {
        
        }

        public Spline CreateSpline()
        {
            Generate();
            ApplyOffset();
            Spline spline = new Spline(GetSplineType());
            spline.points = points;
            if (closed) spline.Close();
            return spline;
        }

        public void UpdateSpline(Spline spline)
        {
            Generate();
            ApplyOffset();
            spline.type = GetSplineType();
            spline.points = points;
            if (closed) spline.Close();
            else if (spline.isClosed) spline.Break();
        }

        public SplineComputer CreateSplineComputer(string name, Vector3 position, Quaternion rotation)
        {
            Generate();
            ApplyOffset();
            GameObject go = new GameObject(name);
            SplineComputer comp = go.AddComponent<SplineComputer>();
            comp.SetPoints(points, SplineComputer.Space.Local);
            if (closed) comp.Close();
            comp.transform.position = position;
            comp.transform.rotation = rotation;
            return comp;
        }

        public void UpdateSplineComputer(SplineComputer comp)
        {
            Generate();
            ApplyOffset();
            comp.type = GetSplineType();
            comp.SetPoints(points, SplineComputer.Space.Local);
            if (closed) comp.Close();
            else if (comp.isClosed) comp.Break();
        }

        public SplinePoint[] GetPoints()
        {
            return points;
        }

        public virtual Spline.Type GetSplineType()
        {
            return Spline.Type.CatmullRom;
        }

        public bool GetIsClosed()
        {
            return closed;
        }

        void ApplyOffset()
        {
            Quaternion freeRot = Quaternion.Euler(rotation);
            if (is2D) freeRot = Quaternion.AngleAxis(-rotation.z, Vector3.forward) * Quaternion.AngleAxis(90f, Vector3.right);
            for (int i = 0; i < points.Length; i++)
            {
                points[i].position = freeRot * points[i].position;
                points[i].tangent = freeRot *  points[i].tangent;
                points[i].tangent2 = freeRot * points[i].tangent2;
                points[i].normal = freeRot * points[i].normal;
            }
            for (int i = 0; i < points.Length; i++) points[i].SetPosition(points[i].position + offset);
        }

        protected void CreatePoints(int count, SplinePoint.Type type)
        {
            if (points.Length != count) points = new SplinePoint[count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i].type = type;
                points[i].normal = Vector3.up;
                points[i].color = Color.white;
                points[i].size = 1f;
            }
        }
    }
}
