using UnityEngine;
using Dreamteck;

namespace Dreamteck.Splines{
    [System.Serializable]
	public struct SplineSample {
        public Vector3 position;
        public Vector3 up;
        public Vector3 forward;
        public Color color;
        public float size;
        public double percent;

        public void FastCopy(ref SplineSample sample)
        {
            position = sample.position;
            up = sample.up;
            forward = sample.forward;
            color = sample.color;
            size = sample.size;
            percent = sample.percent;
        }
         

        public Quaternion rotation
        {
            get {
                if (up == forward)
                {
                    if (up == Vector3.up) return Quaternion.LookRotation(Vector3.up, Vector3.back);
                    else return Quaternion.LookRotation(forward, Vector3.up);
                }
                return Quaternion.LookRotation(forward, up); }
        }

        public Vector3 right
        {
            get {
                if(up == forward)
                {
                    if (up == Vector3.up) return Vector3.right;
                    else return Vector3.Cross(Vector3.up, forward).normalized;
                }
                return Vector3.Cross(up, forward).normalized; }
        }


        public static SplineSample Lerp(ref SplineSample a, ref SplineSample b, float t)
        {
            SplineSample result = new SplineSample();
            Lerp(ref a, ref b, t, ref result);
            return result;
        }

        public static SplineSample Lerp(ref SplineSample a, ref SplineSample b, double t)
        {
            SplineSample result = new SplineSample();
            Lerp(ref a, ref b, t, ref result);
            return result;
        }

        public static void Lerp(ref SplineSample a, ref SplineSample b, double t, ref SplineSample target)
        {
            float ft = (float)t;
            DMath.LerpVector3NonAlloc(a.position, b.position, t, ref target.position);
            target.forward = Vector3.Slerp(a.forward, b.forward, ft);
            target.up = Vector3.Slerp(a.up, b.up, ft);
            target.color = Color.Lerp(a.color, b.color, ft);
            target.size = Mathf.Lerp(a.size, b.size, ft);
            target.percent = DMath.Lerp(a.percent, b.percent, t);
        }

        public static void Lerp(ref SplineSample a, ref SplineSample b, float t, ref SplineSample target)
        {
            DMath.LerpVector3NonAlloc(a.position, b.position, t, ref target.position);
            target.forward = Vector3.Slerp(a.forward, b.forward, t);
            target.up = Vector3.Slerp(a.up, b.up, t);
            target.color = Color.Lerp(a.color, b.color, t);
            target.size = Mathf.Lerp(a.size, b.size, t);
            target.percent = DMath.Lerp(a.percent, b.percent, t);
        }

        public void Lerp(ref SplineSample b, double t)
        {
            Lerp(ref this, ref b, t, ref this);
        }

        public void Lerp(ref SplineSample b, float t)
        {
            Lerp(ref this, ref b, t, ref this);
        }
		
        public SplineSample(Vector3 position, Vector3 up, Vector3 forward, Color color, float size, double percent)
        {
            this.position = position;
            this.up = up;
            this.forward = forward;
            this.color = color;
            this.size = size;
            this.percent = percent;
        }
	}
}
