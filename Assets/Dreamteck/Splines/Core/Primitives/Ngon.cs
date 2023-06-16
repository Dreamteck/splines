using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines.Primitives
{
    public class Ngon : SplinePrimitive
    {
        public float radius = 1f;
        public int sides = 3;

        public override Spline.Type GetSplineType()
        {
            return Spline.Type.Linear;
        }

        protected override void Generate()
        {
            base.Generate();
            closed = true;
            CreatePoints(sides, SplinePoint.Type.SmoothMirrored);
            for (int i = 0; i < sides; i++)
            {
                float percent = (float)i / sides;
                Vector3 pos = Quaternion.AngleAxis(360f * percent, Vector3.forward) * Vector3.up * radius;
                points[i].SetPosition(pos);
            }
        }
    }
}