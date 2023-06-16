namespace Dreamteck.Splines.Editor
{
    using UnityEditor;
    using UnityEngine;

    public struct SerializedSplinePoint
    {
        public bool changed;

        public SplinePoint.Type type
        {
            get
            {
                return (SplinePoint.Type)_type.enumValueIndex;
            }
            set
            {
                if (value != type)
                {
                    _type.enumValueIndex = (int)value;
                    changed = true;
                }
            }
        }

        public Vector3 position
        {
            get { return _position.vector3Value; }
            set
            {
                if (value != position)
                {
                    _position.vector3Value = value;
                    changed = true;
                }
            }
        }

        public Vector3 tangent
        {
            get { return _tangent.vector3Value; }
            set
            {
                if (value != tangent)
                {
                    _tangent.vector3Value = value;
                    changed = true;
                }
            }
        }
        public Vector3 tangent2
        {
            get { return _tangent2.vector3Value; }
            set
            {
                if (value != tangent2)
                {
                    _tangent2.vector3Value = value;
                    changed = true;
                }
            }
        }

        public Color color
        {
            get { return _color.colorValue; }
            set
            {
                if (value != color)
                {
                    _color.colorValue = value;
                    changed = true;
                }
            }
        }

        public Vector3 normal
        {
            get { return _normal.vector3Value; }
            set
            {
                if (value != normal)
                {
                    _normal.vector3Value = value;
                    changed = true;
                }
            }
        }
        public float size
        {
            get { return _size.floatValue; }
            set
            {
                if (value != size)
                {
                    _size.floatValue = value;
                    changed = true;
                }
            }
        }


        private SerializedProperty _point;
        private SerializedProperty _position;
        private SerializedProperty _tangent;
        private SerializedProperty _tangent2;
        private SerializedProperty _normal;
        private SerializedProperty _size;
        private SerializedProperty _color;
        private SerializedProperty _type;


        public SerializedSplinePoint(SerializedProperty input)
        {
            _point = input;
            _position = _point.FindPropertyRelative("position");
            _tangent = _point.FindPropertyRelative("tangent");
            _tangent2 = _point.FindPropertyRelative("tangent2");
            _normal = _point.FindPropertyRelative("normal");
            _size = _point.FindPropertyRelative("size");
            _color = _point.FindPropertyRelative("color");
            _type = _point.FindPropertyRelative("_type");
            changed = false;
        }

        public void SetPoint(SplinePoint point)
        {
            CheckForChange(point);
            position = point.position;
            tangent = point.tangent;
            tangent2 = point.tangent2;
            normal = point.normal;
            size = point.size;
            color = point.color;
            type = point.type;
        }

        private void CheckForChange(SplinePoint point)
        {
            if (position != point.position)
            {
                changed = true;
                return;
            }

            if (tangent != point.tangent)
            {
                changed = true;
                return;
            }

            if (tangent2 != point.tangent2)
            {
                changed = true;
                return;
            }

            if (normal != point.normal)
            {
                changed = true;
                return;
            }

            if (size != point.size)
            {
                changed = true;
                return;
            }

            if (color != point.color)
            {
                changed = true;
                return;
            }

            if (type != point.type)
            {
                changed = true;
                return;
            }
        }

        public void CopyFrom(SerializedSplinePoint point)
        {
            position = point.position;
            tangent = point.tangent;
            tangent2 = point.tangent2;
            normal = point.normal;
            size = point.size;
            color = point.color;
            type = point.type;
        }

        public SplinePoint CreateSplinePoint()
        {
            SplinePoint point = new SplinePoint();
            point.type = type;
            point.position = position;
            point.tangent = tangent;
            point.tangent2 = tangent2;
            point.normal = normal;
            point.size = size;
            point.color = color;
            point.isDirty = changed;
            return point;
        }

        public void SetPosition(Vector3 pos)
        {
            tangent -= position - pos;
            tangent2 -= position - pos;
            position = pos;
        }

        public void SetTangentPosition(Vector3 pos)
        {
            tangent = pos;
            switch ((SplinePoint.Type)_type.enumValueIndex)
            {
                case SplinePoint.Type.SmoothMirrored: SmoothMirrorTangent2(); break;
                case SplinePoint.Type.SmoothFree: SmoothFreeTangent2(); break;
            }
        }

        public void SetTangent2Position(Vector3 pos)
        {
            tangent2 = pos;
            switch ((SplinePoint.Type)_type.enumValueIndex)
            {
                case SplinePoint.Type.SmoothMirrored: SmoothMirrorTangent(); break;
                case SplinePoint.Type.SmoothFree: SmoothFreeTangent(); break;
            }
        }

        private void SmoothMirrorTangent2()
        {
            tangent2 = position + (position - tangent);
        }

        private void SmoothMirrorTangent()
        {
            tangent = position + (position - tangent2);
        }

        private void SmoothFreeTangent2()
        {
            tangent2 = position + (position - tangent).normalized * (tangent2 - position).magnitude;
        }

        private void SmoothFreeTangent()
        {
            tangent = position + (position - tangent2).normalized * (tangent - position).magnitude;
        }
    }
}
