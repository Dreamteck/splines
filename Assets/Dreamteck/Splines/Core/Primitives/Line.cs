using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines.Primitives
{
    public class Line : SplinePrimitive
    {
        public bool mirror = true;
        public float length = 1f;
        public int segments = 1;

        public override Spline.Type GetSplineType()
        {
            return Spline.Type.Linear;
        }

        protected override void Generate()
        {
            base.Generate();
            closed = false;
            CreatePoints(segments + 1, SplinePoint.Type.SmoothMirrored);
            Vector3 origin = Vector3.zero;
            if (mirror) origin = -Vector3.up * length * 0.5f;
            for (int i = 0; i < points.Length; i++)
            {
                points[i].position = origin + Vector3.up * length * ((float)i / (points.Length - 1));
            }
        }
    }
}