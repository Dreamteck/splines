using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines.Primitives
{
    public class RoundedRectangle : SplinePrimitive
    {
        public Vector2 size = Vector2.one;
        public float xRadius = 0.25f;
        public float yRadius = 0.25f;

        public override Spline.Type GetSplineType()
        {
            return Spline.Type.Bezier;
        }

        protected override void Generate()
        {
            base.Generate();
            closed = true;
            CreatePoints(8, SplinePoint.Type.Broken);
            Vector2 edgeSize = size - new Vector2(xRadius, yRadius) * 2f;
            points[0].SetPosition(Vector3.up / 2f * edgeSize.y + Vector3.left / 2f * size.x);
            points[1].SetPosition(Vector3.up / 2f * size.y + Vector3.left / 2f * edgeSize.x);
            points[2].SetPosition(Vector3.up / 2f * size.y + Vector3.right / 2f * edgeSize.x);
            points[3].SetPosition(Vector3.up / 2f * edgeSize.y + Vector3.right / 2f * size.x);
            points[4].SetPosition(Vector3.down / 2f * edgeSize.y + Vector3.right / 2f * size.x);
            points[5].SetPosition(Vector3.down / 2f * size.y + Vector3.right / 2f * edgeSize.x);
            points[6].SetPosition(Vector3.down / 2f * size.y + Vector3.left / 2f * edgeSize.x);
            points[7].SetPosition(Vector3.down / 2f * edgeSize.y + Vector3.left / 2f * size.x);

            float xRad = 2f * (Mathf.Sqrt(2f) - 1f) / 3f * xRadius * 2f;
            float yRad = 2f * (Mathf.Sqrt(2f) - 1f) / 3f * yRadius * 2f;
            points[0].SetTangent2Position(points[0].position + Vector3.up * yRad);
            points[1].SetTangentPosition(points[1].position + Vector3.left * xRad);
            points[2].SetTangent2Position(points[2].position + Vector3.right * xRad);
            points[3].SetTangentPosition(points[3].position + Vector3.up * yRad);
            points[4].SetTangent2Position(points[4].position + Vector3.down * yRad);
            points[5].SetTangentPosition(points[5].position + Vector3.right * xRad);
            points[6].SetTangent2Position(points[6].position + Vector3.left * xRad);
            points[7].SetTangentPosition(points[7].position + Vector3.down * yRad);
        }
    }
}
