namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class PointTransformModule : PointModule
    {
        public enum EditSpace { World, Transform, Spline }
        public EditSpace editSpace = EditSpace.World;
        public Vector3 scale = Vector3.one, offset = Vector3.zero;
        protected Quaternion rotation = Quaternion.identity;
        protected Vector3 origin = Vector3.zero;
        protected SplinePoint[] originalPoints = new SplinePoint[0];
        protected SplinePoint[] localPoints = new SplinePoint[0];

        private Matrix4x4 matrix = new Matrix4x4();
        private Matrix4x4 inverseMatrix = new Matrix4x4();
        private bool _unapplied = true;
        SplineSample evalResult = new SplineSample();

        public PointTransformModule(SplineEditor editor) : base(editor)
        {

        }

        public override void Reset()
        {
            base.Reset();
            GetRotation();
            origin = selectionCenter;
            scale = Vector3.one;
            matrix.SetTRS(origin, rotation, Vector3.one);
            inverseMatrix = matrix.inverse;
            localPoints = editor.GetPointsArray();
            for (int i = 0; i < localPoints.Length; i++) InverseTransformPoint(ref localPoints[i]);
        }

        protected void GetRotation()
        {
            switch (editSpace)
            {
                case EditSpace.World: rotation = Quaternion.identity; break;
                case EditSpace.Transform: rotation = TransformUtility.GetRotation(editor.matrix); break;
                case EditSpace.Spline:
                    if (editor.evaluate == null)
                    {
                        Debug.LogError("Unassigned handler evaluate for Spline Editor.");
                        break;
                    }
                    if (selectedPoints.Count == 1)
                    {
                        editor.evaluate((double)selectedPoints[0] / (points.Length - 1), ref evalResult);
                        rotation = evalResult.rotation;
                    }
                    else rotation = Quaternion.identity;
                    break;
            }
        }

        public override void LoadState()
        {
            base.LoadState();
            editSpace = (EditSpace)LoadInt("editSpace");
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveInt("editSpace", (int)editSpace);
        }

        protected void SetDirty()
        {
            RegisterChange();
            _unapplied = true;
        }

        protected bool IsDirty()
        {
            return _unapplied;
        }

        public virtual void Revert()
        {
            editor.SetPointsArray(originalPoints);
            editor.ApplyModifiedProperties();
            _unapplied = false;
        }

        public virtual void Apply()
        {
            RegisterChange();
            CacheOriginalPoints();
            _unapplied = false;
        }

        public override void Select()
        {
            base.Select();
            CacheOriginalPoints();
        }

        public override void Deselect()
        {
            base.Deselect();
            _unapplied = false;
        }

        private void CacheOriginalPoints()
        {
            originalPoints = editor.GetPointsArray();
        }

        protected void PrepareTransform()
        {
            matrix.SetTRS(origin + offset, rotation, scale);
        }

        protected Vector3 TransformPosition(Vector3 position)
        {
            return matrix.MultiplyPoint3x4(position);
        }

        protected Vector3 InverseTransformPosition(Vector3 position)
        {
            return inverseMatrix.MultiplyPoint3x4(position);
        }

        protected Vector3 TransformDirection(Vector3 direction)
        {
            return matrix.MultiplyVector(direction);
        }

        protected Vector3 InverseTransformDirection(Vector3 direction)
        {
            return inverseMatrix.MultiplyVector(direction);
        }

        protected void TransformPoint(ref SplinePoint point, bool normals = true, bool tangents = true, bool size = false)
        {
            if (tangents)
            {
                point.position = TransformPosition(point.position);
                point.tangent = TransformPosition(point.tangent);
                point.tangent2 = TransformPosition(point.tangent2);
            }
            else
            {
                point.SetPosition(TransformPosition(point.position));
            }
            if(normals) point.normal = TransformDirection(point.normal).normalized;
            if (size)
            {
                float avg = (scale.x + scale.y + scale.z) / 3f;
                point.size *= avg;
            }
        }

        protected void InverseTransformPoint(ref SplinePoint point, bool normals = true, bool tangents = true, bool size = false)
        {
            if (tangents)
            {
                point.position = InverseTransformPosition(point.position);
                point.tangent = InverseTransformPosition(point.tangent);
                point.tangent2 = InverseTransformPosition(point.tangent2);
            } else point.SetPosition(TransformPosition(point.position));

            if (normals) point.normal = InverseTransformDirection(point.normal).normalized;
            if (size)
            {
                float avg = (scale.x + scale.y + scale.z) / 3f;
                point.size /= avg;
            }
        }
    }
}
