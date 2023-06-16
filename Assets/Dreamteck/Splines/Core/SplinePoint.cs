using UnityEngine;
using UnityEngine.Serialization;
using System;

namespace Dreamteck.Splines{
	[System.Serializable]
    //A control point used by the SplineClass
	public struct SplinePoint {
        public enum Type {SmoothMirrored, Broken, SmoothFree};
        public Type type
        {
            get { return _type; }
            set
            {
                isDirty = _type != value;
                _type = value;
                if (value == Type.SmoothMirrored)
                {
                    SmoothMirrorTangent2();
                }
            }
        }

        /// <summary>
        /// Getting the value of isDirty will set the point not dirty
        /// </summary>
        [NonSerialized]
        public bool isDirty;
       
        [FormerlySerializedAs("type")]
        [SerializeField]
        [HideInInspector]
        private Type _type;

        [HideInInspector]
        [FormerlySerializedAs("_position")]
        public Vector3 position;

        [HideInInspector]
        [FormerlySerializedAs("_color")]
        public Color color;

        [HideInInspector]
        [FormerlySerializedAs("_normal")]
        public Vector3 normal;

        [HideInInspector]
        [FormerlySerializedAs("_size")]
        public float size;

        [HideInInspector]
        [FormerlySerializedAs("_tangent")]
        public Vector3 tangent;

        [HideInInspector]
        [FormerlySerializedAs("_tangent2")]
        public Vector3 tangent2;

        public static SplinePoint Lerp(SplinePoint a, SplinePoint b, float t)
        {
            SplinePoint result = a;
            if (a.type == Type.Broken || b.type == Type.Broken) result.type = Type.Broken;
            else if (a.type == Type.SmoothFree || b.type == Type.SmoothFree) result.type = Type.SmoothFree;
            else result.type = Type.SmoothMirrored;
            result.position = Vector3.Lerp(a.position, b.position, t);
            GetInterpolatedTangents(a, b, t, ref result);
            result.color = Color.Lerp(a.color, b.color, t);
            result.size = Mathf.Lerp(a.size, b.size, t);
            result.normal = Vector3.Slerp(a.normal, b.normal, t);
            return result;
        }

        private static void GetInterpolatedTangents(SplinePoint a, SplinePoint b, float t, ref SplinePoint target)
        {
            Vector3 P0_1 = (1f - t) * a.position + t * a.tangent2;
            Vector3 P1_2 = (1f - t) * a.tangent2 + t * b.tangent;
            Vector3 P2_3 = (1f - t) * b.tangent + t * b.position;
            Vector3 P01_12 = (1 - t) * P0_1 + t * P1_2;
            Vector3 P12_23 = (1 - t) * P1_2 + t * P2_3;
            target.tangent = P01_12;
            target.tangent2 = P12_23;
        }

        public override bool Equals(object obj)
        {
            if(obj is SplinePoint)
            {
                return EqualsFast((SplinePoint)obj);
            }
            return false;
        }

        private bool EqualsFast(SplinePoint obj)
        {
            SplinePoint other = (SplinePoint)obj;
            if (position != other.position) return false;
            if (tangent != other.tangent) return false;
            if (tangent2 != other.tangent2) return false;
            if (normal != other.normal) return false;
            if (_type != other._type) return false;
            if (size != other.size) return false;
            if (color != other.color) return false;
            return true;
        }


        public static bool operator == (SplinePoint p1, SplinePoint p2)
        {
            return p1.EqualsFast(p2);
        }

        public static bool operator != (SplinePoint p1, SplinePoint p2)
        {
            return !p1.EqualsFast(p2);
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
            switch (_type)
            {
                case Type.SmoothMirrored: SmoothMirrorTangent2(); break;
                case Type.SmoothFree: SmoothFreeTangent2(); break;
            }
        }

        public void SetTangent2Position(Vector3 pos)
        {
            tangent2 = pos;
            switch (_type)
            {
                case Type.SmoothMirrored: SmoothMirrorTangent(); break;
                case Type.SmoothFree: SmoothFreeTangent(); break;
            }
        }

        public SplinePoint(Vector3 p)
        {
            position = p;
            tangent = p;
            tangent2 = p;
            color = Color.white;
            normal = Vector3.up;
            size = 1f;
            _type = Type.SmoothMirrored;
            isDirty = false;
            SmoothMirrorTangent2();
        }
		
		public SplinePoint(Vector3 p, Vector3 t){
            position = p;
            tangent = t;
            tangent2 = p + (p - t);
            color = Color.white;
            normal = Vector3.up;
            size = 1f;
            _type = Type.SmoothMirrored;
            isDirty = false;
            SmoothMirrorTangent2();
        }	
		
		public SplinePoint(Vector3 pos, Vector3 tan, Vector3 nor, float s, Color col){
            position = pos;
            tangent = tan;
            tangent2 = pos + (pos - tan);
            normal = nor;
            size = s;
            color = col;
            _type = Type.SmoothMirrored;
            isDirty = false;
            SmoothMirrorTangent2();
        }

        public SplinePoint(Vector3 pos, Vector3 tan, Vector3 tan2, Vector3 nor, float s, Color col)
        {
            position = pos;
            tangent = tan;
            tangent2 = tan2;
            normal = nor;
            size = s;
            color = col;
            _type = Type.Broken;
            isDirty = false;
            switch (_type)
            {
                case Type.SmoothMirrored: SmoothMirrorTangent2(); break;
                case Type.SmoothFree: SmoothFreeTangent2(); break;
            }
        }

        public SplinePoint(SplinePoint source)
        {
            position = source.position;
            tangent = source.tangent;
            tangent2 = source.tangent2;
            color = source.color;
            normal = source.normal;
            size = source.size;
            _type = source.type;
            isDirty = false;
            switch (_type)
            {
                case Type.SmoothMirrored: SmoothMirrorTangent2(); break;
                case Type.SmoothFree: SmoothFreeTangent2(); break;
            }
        }

        public void Flatten(LinearAlgebraUtility.Axis axis, float flatValue = 0f)
        {
            position = LinearAlgebraUtility.FlattenVector(position, axis, flatValue);
            tangent = LinearAlgebraUtility.FlattenVector(tangent, axis, flatValue);
            tangent2 = LinearAlgebraUtility.FlattenVector(tangent2, axis, flatValue);
            switch (axis)
            {
                case LinearAlgebraUtility.Axis.X: normal = Vector3.right; break;
                case LinearAlgebraUtility.Axis.Y: normal = Vector3.up; break;
                case LinearAlgebraUtility.Axis.Z: normal = Vector3.forward; break;
            }
        }

        public void SetPositionX(float value)
        {
            if(position.x != value)
            {
                isDirty = true;
            }
            position.x = value;
        }

        public void SetPositionY(float value)
        {
            if(position.y != value)
            {
                isDirty = true;
            }
            position.y = value;
        }

        public void SetPositionZ(float value)
        {
            if(position.z != value)
            {
                isDirty = true;
            }
            position.z = value;
        }

        public void SetTangentX(float value)
        {
            if(tangent.x != value)
            {
                isDirty = true;
            }
            tangent.x = value;
        }

        public void SetTangentY(float value)
        {
            if (tangent.y != value)
            {
                isDirty = true;
            }
            tangent.y = value;
        }

        public void SetTangentZ(float value)
        {
            if(tangent.z != value)
            {
                isDirty = true;
            }
            tangent.z = value;
        }

        public void SetTangent2X(float value)
        {
            if (tangent2.x != value)
            {
                isDirty = true;
            }
            tangent2.x = value;
        }

        public void SetTangent2Y(float value)
        {
            if (tangent2.y != value)
            {
                isDirty = true;
            }
            tangent2.y = value;
        }

        public void SetTangent2Z(float value)
        {
            if (tangent2.z != value)
            {
                isDirty = true;
            }
            tangent2.z = value;
        }

        public void SetNormalX(float value)
        {
            if (normal.x != value)
            {
                isDirty = true;
            }
            normal.x = value;
        }

        public void SetNormalY(float value)
        {
            if (normal.y != value)
            {
                isDirty = true;
            }
            normal.y = value;
        }

        public void SetNormalZ(float value)
        {
            if(normal.z != value)
            {
                isDirty = true;
            }
            normal.z = value;
        }

        public void SetColorR(float value)
        {
            if (color.r != value)
            {
                isDirty = true;
            }
            color.r = value;
        }

        public void SetColorG(float value)
        {
            if (color.g != value)
            {
                isDirty = true;
            }
            color.g = value;
        }

        public void SetColorB(float value)
        {
            if(color.b != value)
            {
                isDirty = true;
            }
            color.b = value;
        }

        public void SetColorA(float value)
        {
            if (color.a != value)
            {
                isDirty = true;
            }
            color.a = value;
        }

        private void SmoothMirrorTangent2()
        {
            tangent2 = position + (position - tangent);
            isDirty = true;
        }

        private void SmoothMirrorTangent()
        {
            tangent = position + (position - tangent2);
            isDirty = true;
        }

        private void SmoothFreeTangent2()
        {
            tangent2 = position + (position - tangent).normalized * (tangent2 - position).magnitude;
            isDirty = true;
        }

        private void SmoothFreeTangent()
        {
            tangent = position + (position - tangent2).normalized * (tangent - position).magnitude;
            isDirty = true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash *= hash * 23 + _type.GetHashCode();
                hash = hash * 23 + position.GetHashCode();
                hash = hash * 23 + normal.GetHashCode();
                hash = hash * 23 + tangent.GetHashCode();
                hash = hash * 23 + tangent2.GetHashCode();
                hash = hash * 23 + color.GetHashCode();
                hash = hash * 23 + size.GetHashCode();
                return hash;
            }
        }
    }
}
