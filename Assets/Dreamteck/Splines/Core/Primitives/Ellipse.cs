using System.Collections;
using System.Collections.Generic;
using UnityEngine; 

namespace Dreamteck.Splines.Primitives
{
    public class Ellipse : SplinePrimitive
    {
        public float xRadius = 1f;
        public float yRadius = 1f;

        public override Spline.Type GetSplineType()
        {
            return Spline.Type.Bezier;
        }

        protected override void Generate()
        {
            base.Generate();
            closed = true;
            CreatePoints(4, SplinePoint.Type.SmoothMirrored);
            points[0].position = Vector3.up * yRadius;
            points[0].SetTangentPosition(points[0].position + Vector3.right * 2 * (Mathf.Sqrt(2f) - 1f) / 1.5f * xRadius);
            points[1].position = Vector3.left * xRadius;
            points[1].SetTangentPosition(points[1].position + Vector3.up * 2 * (Mathf.Sqrt(2f) - 1f) / 1.5f * yRadius);
            points[2].position = Vector3.down * yRadius;
            points[2].SetTangentPosition(points[2].position + Vector3.left * 2 * (Mathf.Sqrt(2f) - 1f) / 1.5f * xRadius);
            points[3].position = Vector3.right * xRadius;
            points[3].SetTangentPosition(points[3].position + Vector3.down * 2 * (Mathf.Sqrt(2f) - 1f) / 1.5f * yRadius);
        }
    }
}