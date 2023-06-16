namespace Dreamteck.Splines
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    [System.Serializable]
    public class SplineSampleModifier
    {
        [Range(0f, 1f)]
        public float blend = 1f;
        public bool useClippedPercent = false;

        public virtual List<Key> GetKeys()
        {
            return new List<Key>();
        }

        public virtual void SetKeys(List<Key> input)
        {
        }

        public virtual void Apply(ref SplineSample result)
        {

        }

        public virtual void Apply(ref SplineSample source, ref SplineSample destination)
        {
            destination = source;
            Apply(ref destination);
        }

        [System.Serializable]
        public class Key
        {
            public double start
            {
                get { return _featherStart; }
                set {
                    if (value != _featherStart)
                    {
                        _featherStart = DMath.Clamp01(value);
                    }
                }
            }

            public double end
            {
                get { return _featherEnd; }
                set {
                    if (value != _featherEnd)
                    {
                        _featherEnd = DMath.Clamp01(value);
                    }
                }

            }

            public double centerStart
            {
                get { return _centerStart; }
                set {
                    if (value != _centerStart)
                    {
                        _centerStart = DMath.Clamp01(value);
                        if (_centerStart > _centerEnd) _centerStart = _centerEnd;
                    }
                }
            }

            public double centerEnd
            {
                get { return _centerEnd; }
                set {
                    if (value != _centerEnd)
                    {
                        _centerEnd = DMath.Clamp01(value);
                        if (_centerEnd < _centerStart) _centerEnd = _centerStart;
                    }
                }
            }


            public double globalCenterStart
            {
                get {
                    return LocalToGlobalPercent(centerStart);
                }
                set
                {
                    centerStart = DMath.Clamp01(GlobalToLocalPercent(value));
                }
            }

            public double globalCenterEnd
            {
                get
                {
                    return LocalToGlobalPercent(centerEnd);
                }
                set
                {
                    centerEnd = DMath.Clamp01(GlobalToLocalPercent(value));
                }
            }

            public double position
            {
                get
                {
                    double center = DMath.Lerp(_centerStart, _centerEnd, 0.5);
                    if (start > end)
                    {
                        double pos = DMath.Lerp(_featherStart, _featherEnd, center);
                        double fromToEndDistance = 1.0 - _featherStart;
                        double centerDistance = center * (fromToEndDistance + _featherEnd);
                        pos = _featherStart + centerDistance;
                        if (pos > 1.0) pos -= 1.0;
                        return pos;
                    }
                    else return DMath.Lerp(_featherStart, _featherEnd, center);

                }
                set
                {
                    double delta = value - position;
                    start += delta;
                    end += delta;
                }
            }

            [SerializeField] private double _featherStart = 0.0, _featherEnd = 0.0, _centerStart = 0.25, _centerEnd = 0.75;
            public AnimationCurve interpolation;
            public float blend = 1f;

            internal Key(double f, double t)
            {
                start = f;
                end = t;
                interpolation = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }

            private double GlobalToLocalPercent(double t)
            {
                if (_featherStart > _featherEnd)
                {
                    if (t > _featherStart) return DMath.InverseLerp(_featherStart, _featherStart + (1.0 - _featherStart) + _featherEnd, t);
                    else if (t < _featherEnd) return DMath.InverseLerp(-(1.0 - _featherStart), _featherEnd, t);
                    else return 0f;
                }
                return DMath.InverseLerp(_featherStart, _featherEnd, t);
            }

            private double LocalToGlobalPercent(double t)
            {
                if (_featherStart > _featherEnd)
                {
                    t = DMath.Lerp(_featherStart, _featherStart + (1.0 - _featherStart) + _featherEnd, t);
                    if (t > 1.0) t -= 1.0;
                    return t;
                }
                return DMath.Lerp(_featherStart, _featherEnd, t);
            }

            public float Evaluate(double t)
            {
                t = (float)GlobalToLocalPercent(t);
                if (t < _centerStart)
                {
                    return interpolation.Evaluate((float)t / (float)_centerStart) * blend;
                }

                if (t > _centerEnd)
                {
                    return interpolation.Evaluate(1f - (float)DMath.InverseLerp(_centerEnd, 1.0, t)) * blend;
                }
                return interpolation.Evaluate(1f) * blend;
            }

            public virtual Key Duplicate()
            {
                Key newKey = new Key(start, end);
                newKey._centerStart = _centerStart;
                newKey._centerEnd = _centerEnd;
                newKey.blend = blend;
                newKey.interpolation = DuplicateUtility.DuplicateCurve(interpolation);
                return newKey;
            }
        }
    }
}