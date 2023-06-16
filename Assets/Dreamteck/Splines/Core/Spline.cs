using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Dreamteck;

namespace Dreamteck.Splines {
    //The Spline class defines a spline with world coordinates. It comes with various sampling methods
    [System.Serializable]
    public class Spline {
        public enum Direction { Forward = 1, Backward = -1 }
        public enum Type { CatmullRom, BSpline, Bezier, Linear };
        public SplinePoint[] points = new SplinePoint[0];
        public Type type = Type.Bezier;
        public bool linearAverageDirection = true;
        public AnimationCurve customValueInterpolation = null;
        public AnimationCurve customNormalInterpolation = null;
        public int sampleRate = 10;

        /// <summary>
        /// Returns true if the spline is closed
        /// </summary>
        public bool isClosed
        {
            get
            {
                return closed && points.Length >= 3;
            }
        }
        /// <summary>
        /// The step size of the percent incrementation when evaluating a spline (based on percision)
        /// </summary>
        public double moveStep
        {
            get {
                if (type == Type.Linear) return 1f / (points.Length-1);
                return 1f / (iterations-1);
            }
            set { }
        }
       /// <summary>
        /// The total count of samples for the spline (based on the sample rate)
       /// </summary>
       public int iterations
        {
            get {
                if (type == Type.Linear) return closed ? points.Length + 1 : points.Length;
                int segments = closed ? points.Length : points.Length - 1;
                return sampleRate * segments - segments + 1;
            }
        }

        public float knotParametrization
        {
            get { return _knotParametrization; }
            set
            {
                _knotParametrization = Mathf.Clamp01(value);
            }
        }

        private static Vector3[] P = new Vector3[4];
        private static Vector3 A1;
        private static Vector3 A2;
        private static Vector3 A3;
        private static Vector3 B1;
        private static Vector3 B2;
        private static float t1;
        private static float t2;
        private static float t3;

        [SerializeField]
        private bool closed = false;
        [SerializeField, Range(0f, 1f)]
        private float _knotParametrization;

        public Spline(Type type){
			this.type = type;
			points = new SplinePoint[0];
		}

        public Spline(Type type, int sampleRate)
        {
            this.type = type;
            this.sampleRate = sampleRate;
            points = new SplinePoint[0];
        }

        /// <summary>
        /// Calculate the length of the spline
        /// </summary>
        /// <param name="from">Calculate from [0-1] default: 0f</param>
        /// <param name="to">Calculate to [0-1] default: 1f</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <returns></returns>
        public float CalculateLength(double from = 0.0, double to = 1.0, double resolution = 1.0)
        {
            if (points.Length == 0) return 0f;
            resolution = DMath.Clamp01(resolution);
            if (resolution == 0.0) return 0f;
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            if (to < from) to = from;
            double percent = from;
            Vector3 lastPos = EvaluatePosition(percent);
            float sum = 0f;
            while (true)
            {
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 pos = EvaluatePosition(percent);
                sum += (pos - lastPos).magnitude;
                lastPos = pos;
                if (percent == to) break;
            }
            return sum;
        }

        /// <summary>
        /// Project point on the spline. Returns evaluation percent.
        /// </summary>
        /// <param name="position">3D Point</param>
        /// <param name="subdivide">Subdivisions default: 4</param>
        /// <param name="from">Sample from [0-1] default: 0f</param>
        /// <param name="to">Sample to [0-1] default: 1f</param>
        /// <returns></returns>
        public double Project(Vector3 position, int subdivide = 4, double from = 0.0, double to = 1.0)
        {
            if (points.Length == 0) return 0.0;
            if (closed && from == 0.0 && to == 1.0) //Handle looped splines
            {
                double closest = GetClosestPoint(subdivide, position, from, to, Mathf.RoundToInt(Mathf.Max(iterations / points.Length, 10)) * 5);
                if (closest < moveStep)
                {
                    double nextClosest = GetClosestPoint(subdivide, position, 0.5, to, Mathf.RoundToInt(Mathf.Max(iterations / points.Length, 10)) * 5);
                    if (Vector3.Distance(position, EvaluatePosition(nextClosest)) < Vector3.Distance(position, EvaluatePosition(closest))) return nextClosest;
                }
                return closest;
            }
            return GetClosestPoint(subdivide, position, from, to, Mathf.RoundToInt(Mathf.Max(iterations / points.Length, 10)) * 5);
        }

        /// <summary>
        /// Casts rays along the spline against all colliders in the scene
        /// </summary>
        /// <param name="hit">Hit information</param>
        /// <param name="hitPercent">The percent of evaluation where the hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <returns></returns>
        public bool Raycast(out RaycastHit hit, out double hitPercent, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal
        )
        {
            resolution = DMath.Clamp01(resolution);
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent);
            hitPercent = 0f;
            if (resolution == 0f)
            {
                hit = new RaycastHit();
                hitPercent = 0f;
                return false;
            }
            while (true)
            {
                double prevPercent = percent;
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 toPos = EvaluatePosition(percent);
                if (Physics.Linecast(fromPos, toPos, out hit, layerMask, hitTriggers))
                {
                    double segmentPercent = (hit.point - fromPos).sqrMagnitude / (toPos - fromPos).sqrMagnitude;
                    hitPercent = DMath.Lerp(prevPercent, percent, segmentPercent);
                    return true;
                }
                fromPos = toPos;
                if (percent == to) break;
            }
            return false;
        }


        /// <summary>
        /// Casts rays along the spline against all colliders in the scene and returns all hits. Order is not guaranteed.
        /// </summary>
        /// <param name="hits">Hit information</param>
        /// <param name="hitPercents">The percents of evaluation where each hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <returns></returns>
        public bool RaycastAll(out RaycastHit[] hits, out double[] hitPercents, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal
            )
        {
            resolution = DMath.Clamp01(resolution);
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent);
            List<RaycastHit> hitList = new List<RaycastHit>();
            List<double> percentList = new List<double>();
            if (resolution == 0f)
            {
                hits = new RaycastHit[0];
                hitPercents = new double[0];
                return false;
            }
            bool hasHit = false;
            while (true)
            {
                double prevPercent = percent;
                percent = DMath.Move(percent, to, moveStep / resolution);
                Vector3 toPos = EvaluatePosition(percent);
                RaycastHit[] h = Physics.RaycastAll(fromPos, toPos - fromPos, Vector3.Distance(fromPos, toPos), layerMask, hitTriggers);
                for (int i = 0; i < h.Length; i++)
                {
                    hasHit = true;
                    double segmentPercent = (h[i].point - fromPos).sqrMagnitude / (toPos - fromPos).sqrMagnitude;
                    percentList.Add(DMath.Lerp(prevPercent, percent, segmentPercent));
                    hitList.Add(h[i]);
                }
                fromPos = toPos;
                if (percent == to) break;
            }
            hits = hitList.ToArray();
            hitPercents = percentList.ToArray();
            return hasHit;
        } 

        /// <summary>
        /// Converts a point index to spline percent
        /// </summary>
        /// <param name="pointIndex">The point index</param>
        /// <returns></returns>
        public double GetPointPercent(int pointIndex)
        {
            if (closed)
            {
                return DMath.Clamp01((double)pointIndex / points.Length);
            }
            return DMath.Clamp01((double)pointIndex / (points.Length - 1));
        }

        /// <summary>
        /// Evaluate the spline and return position. This is simpler and faster than Evaluate.
        /// </summary>
        /// <param name="percent">Percent of evaluation [0-1]</param>
        public Vector3 EvaluatePosition(double percent)
        {
            if (points.Length == 0) return Vector3.zero;
            Vector3 position = new Vector3();
            EvaluatePosition(percent, ref position);
            return position;
        }

        /// <summary>
        /// Evaluate the spline at the given time and return a SplineSample
        /// </summary>
        /// <param name="percent">Percent of evaluation [0-1]</param>
        public SplineSample Evaluate(double percent)
        {
            SplineSample result = new SplineSample();
            Evaluate(percent, ref result);
            return result;
		}

        /// <summary>
        /// Evaluate the spline at the position of a given point and return a SplineSample
        /// </summary>
        /// <param name="pointIndex">Point index</param>
        public SplineSample Evaluate(int pointIndex)
        {
            SplineSample result = new SplineSample();
            Evaluate(GetPointPercent(pointIndex), ref result);
            return result;
        }

        /// <summary>
        /// Evaluate the splien at the given point and write the result to the "result" object
        /// </summary>
        /// <param name="result">The result output</param>
        /// <param name="pointIndex">Point index</param>
        public void Evaluate(int pointIndex, ref SplineSample result)
        {
            Evaluate(GetPointPercent(pointIndex), ref result);
        }

        /// <summary>
        /// Evaluate the splien at the given time and write the result to the "result" object
        /// </summary>
        /// <param name="sample">The result output</param>
        /// <param name="percent">Percent of evaluation [0-1]</param>
        public void Evaluate(double percent, ref SplineSample sample)
        {
            if (points.Length == 0)
            {
                sample = new SplineSample();
                return;
            }
            percent = DMath.Clamp01(percent);
            if (closed && points.Length <= 2)
            {
                closed = false;
            }
            if (points.Length == 1)
            {
                sample.position = points[0].position;
                sample.up = points[0].normal;
                sample.forward = Vector3.forward;
                sample.size = points[0].size;
                sample.color = points[0].color;
                sample.percent = percent;
                return;
            }

            double doubleIndex = (points.Length - 1) * percent;
            if (closed)
            {
                doubleIndex = points.Length * percent;
            }
            int fromIndex = DMath.FloorInt(doubleIndex);
            int toIndex = fromIndex + 1;
            if (closed)
            {
                if (fromIndex >= points.Length - 1)
                {
                    fromIndex = points.Length - 1;
                }
                if(toIndex > points.Length - 1)
                {
                    toIndex = 0;
                }
            } else
            {
                if(toIndex > points.Length-1)
                {
                    toIndex = points.Length - 1;
                }
            }
            double getPercent = doubleIndex - fromIndex;

            sample.percent = percent;

            float valueInterpolation = (float)getPercent;
            if (customValueInterpolation != null)
            {
                if (customValueInterpolation.length > 0)
                {
                    valueInterpolation = customValueInterpolation.Evaluate(valueInterpolation);
                }
            }
            float normalInterpolation = (float)getPercent;
            if (customNormalInterpolation != null)
            {
                if (customNormalInterpolation.length > 0)
                {
                    normalInterpolation = customNormalInterpolation.Evaluate(normalInterpolation);
                }
            }
            sample.size = Mathf.Lerp(points[fromIndex].size, points[toIndex].size, valueInterpolation);
            sample.color = Color.Lerp(points[fromIndex].color, points[toIndex].color, valueInterpolation);
            sample.up = Vector3.Slerp(points[fromIndex].normal, points[toIndex].normal, normalInterpolation);

            EvaluatePositionAndTangent(ref sample.position, ref sample.forward, percent);

            if (type == Type.BSpline)
            {
                double step = 1.0 / (iterations - 1);
                if (percent <= 1.0 - step && percent >= step)
                {
                    sample.forward = EvaluatePosition(percent + step) - EvaluatePosition(percent - step);
                }
                else
                {
                    Vector3 back = Vector3.zero, front = Vector3.zero;
                    if (closed)
                    {
                        if (percent < step) EvaluatePosition(1.0 - (step - percent), ref back);
                        else  EvaluatePosition(percent - step, ref back);
                        if (percent > 1.0 - step) EvaluatePosition(step - (1.0 - percent), ref front);
                        else EvaluatePosition(percent + step, ref front);
                        sample.forward = front - back;
                    }
                    else
                    {
                        EvaluatePosition(percent - step, ref back);
                        back = sample.position - back;
                        EvaluatePosition(percent + step, ref front);
                        front = front - sample.position;
                        sample.forward = Vector3.Slerp(front, back, back.magnitude / front.magnitude);
                    }
                }
            }
            
            sample.forward.Normalize();
        }

        [System.Obsolete("This override is obsolete. Use Evaluate(int pointIndex, ref SplineSample sample) instead")]
        public void Evaluate(ref SplineSample sample, int pointIndex)
        {
            Evaluate(pointIndex, ref sample);
        }

        [System.Obsolete("This override is obsolete. Use Evaluate(double percent, ref SplineSample sample) instead")]
        public void Evaluate(ref SplineSample sample, double percent)
        {
            Evaluate(percent, ref sample);
        }

        /// <summary>
        /// Evaluates the spline segment and writes the results to the array
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void Evaluate(ref SplineSample[] samples, double from = 0.0, double to = 1.0)
        {
            if (points.Length == 0) {
                samples = new SplineSample[0];
                return;
            }
            from = DMath.Clamp01(from);
            to = DMath.Clamp(to, from, 1.0);
            double fromValue = from * (iterations - 1);
            double toValue = to * (iterations - 1);
            int clippedIterations = DMath.CeilInt(toValue) - DMath.FloorInt(fromValue) + 1;
            if (samples == null) samples = new SplineSample[clippedIterations];
            else if (samples.Length != clippedIterations) samples = new SplineSample[clippedIterations];
            double percent = from;
            double ms = moveStep;
            int index = 0;
            while (true)
            {
                samples[index] = Evaluate(percent);
                index++;
                if (index >= samples.Length) break;
                percent = DMath.Move(percent, to, ms);
            }
        }

        /// <summary>
        /// Evaluates the spline segment and writes uniformly spaced results to the array
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void EvaluateUniform(ref SplineSample[] samples, ref double[] originalSamplePercents, double from = 0.0, double to = 1.0)
        {
            if (points.Length == 0)
            {
                samples = new SplineSample[0];
                return;
            }
            from = DMath.Clamp01(from);
            to = DMath.Clamp(to, from, 1.0);
            double fromValue = from * (iterations - 1);
            double toValue = to * (iterations - 1);
            int clippedIterations = DMath.CeilInt(toValue) - DMath.FloorInt(fromValue) + 1;
            if (samples == null || samples.Length != clippedIterations) samples = new SplineSample[clippedIterations];
            if (originalSamplePercents == null || originalSamplePercents.Length != clippedIterations)
            {
                originalSamplePercents = new double[clippedIterations];
            }
            float lengthStep = CalculateLength(from, to) / (iterations - 1);
            Evaluate(from, ref samples[0]);
            samples[0].percent = originalSamplePercents[0] = from;
            double lastPercent = from;
            float moved = 0f;
            for (int i = 1; i < samples.Length - 1; i++)
            {
                Evaluate(Travel(lastPercent, lengthStep, out moved, Direction.Forward), ref samples[i]);
                lastPercent = samples[i].percent;
                originalSamplePercents[i] = lastPercent;
                samples[i].percent = DMath.Lerp(from, to, (double)i/ (samples.Length - 1));
            }
            Evaluate(to, ref samples[samples.Length - 1]);
            samples[samples.Length - 1].percent = originalSamplePercents[originalSamplePercents.Length - 1] = to;
        }

        /// <summary>
        /// Evaluates the spline segment based on the spline's precision and returns only the position. 
        /// </summary>
        /// <param name="positions">The position buffer</param>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void EvaluatePositions(ref Vector3[] positions, double from = 0.0, double to = 1.0)
        {
            if (points.Length == 0) {
                positions = new Vector3[0];
                return;
            }
            from = DMath.Clamp01(from);
            to = DMath.Clamp(to, from, 1.0);
            double fromValue = from * (iterations - 1);
            double toValue = to * (iterations - 1);
            int clippedIterations = DMath.CeilInt(toValue) - DMath.FloorInt(fromValue) + 1;
            if (positions.Length != clippedIterations) positions = new Vector3[clippedIterations];
            double percent = from;
            double ms = moveStep;
            int index = 0;
            while (true)
            {
                positions[index] = EvaluatePosition(percent);
                index++;
                if (index >= positions.Length) break;
                percent = DMath.Move(percent, to, ms);
            }
        }

        /// <summary>
        /// Returns the percent from the spline at a given distance from the start point
        /// </summary>
        /// <param name="start">The start point</param>
        /// /// <param name="distance">The distance to travel</param>
        /// <param name="direction">The direction towards which to move</param>
        /// <returns></returns>
        public double Travel(double start, float distance, out float moved, Direction direction)
        {
            moved = 0f;
            if (points.Length <= 1) return 0.0;
            if (direction == Direction.Forward && start >= 1.0) return 1.0;
            else if (direction == Direction.Backward && start <= 0.0) return 0.0; ;
            if (distance == 0f) return DMath.Clamp01(start);
            Vector3 pos = Vector3.zero;
            EvaluatePosition(start, ref pos);
            Vector3 lastPosition = pos;
            double lastPercent = start;
            int i = iterations - 1;
            int nextSampleIndex = direction == Spline.Direction.Forward ? DMath.CeilInt(start * i) : DMath.FloorInt(start * i);
            float lastDistance = 0f;
            double percent = start;
            while (true)
            {
                percent = (double)nextSampleIndex / i;
                pos = EvaluatePosition(percent);
                lastDistance = Vector3.Distance(pos, lastPosition);
                lastPosition = pos;
                moved += lastDistance;
                if (moved >= distance) break;
                lastPercent = percent;
                if (direction == Spline.Direction.Forward)
                {
                    if (nextSampleIndex == i) break;
                    nextSampleIndex++;
                }
                else
                {
                    if (nextSampleIndex == 0) break;
                    nextSampleIndex--;
                }
            }
            return DMath.Lerp(lastPercent, percent, 1f - (moved - distance) / lastDistance);
        }

        public double Travel(double start, float distance, Spline.Direction direction = Spline.Direction.Forward)
        {
            float moved;
            return Travel(start, distance, out moved, direction);
        }

        public void EvaluatePosition(double percent, ref Vector3 position)
        {
            if (points.Length == 0)
            {
                position = Vector3.zero;
                return;
            }

            if (points.Length == 1)
            {
                position = points[0].position;
                return;
            }

            percent = DMath.Clamp01(percent);
            double doubleIndex = (points.Length - 1) * percent;
            if (closed)
            {
                doubleIndex = points.Length * percent;
            }
            int pointIndex = DMath.FloorInt(doubleIndex);
            if (type == Type.Bezier)
            {
                pointIndex = Mathf.Clamp(pointIndex, 0, Mathf.Max(points.Length - 1, 0));
            }
            CalculatePosition(ref position, doubleIndex - pointIndex, pointIndex);
        }

        [System.Obsolete("This override is obsolete. Use EvaluatePosition(double percent, ref Vector3 position) instead")]

        public void EvaluatePosition(ref Vector3 position, double percent)
        {
            EvaluatePosition(percent, ref position);
        }

        public void EvaluateTangent(double percent, ref Vector3 tangent)
        {
            if (points.Length < 2)
            {
                tangent = Vector3.forward;
                return;
            }

            percent = DMath.Clamp01(percent);
            double doubleIndex = (points.Length - 1) * percent;
            if (closed)
            {
                doubleIndex = points.Length * percent;
            }
            int pointIndex = DMath.FloorInt(doubleIndex);
            if (type == Type.Bezier)
            {
                pointIndex = Mathf.Clamp(pointIndex, 0, Mathf.Max(points.Length - 1, 0));
            }
            CalculateTangent(ref tangent, doubleIndex - pointIndex, pointIndex);
        }

        public void EvaluatePositionAndTangent(ref Vector3 position, ref Vector3 tangent, double percent)
        {
            if (points.Length == 0)
            {
                position = Vector3.zero;
                tangent = Vector3.forward;
                return;
            }

            if (points.Length == 1)
            {
                position = points[0].position;
                tangent = Vector3.forward;
                return;
            }

            percent = DMath.Clamp01(percent);
            double doubleIndex = (points.Length - 1) * percent;
            if (closed)
            {
                doubleIndex = points.Length * percent;
            }
            int pointIndex = DMath.FloorInt(doubleIndex);
            if (type == Type.Bezier)
            {
                pointIndex = Mathf.Clamp(pointIndex, 0, Mathf.Max(points.Length - 1, 0));
            }
            CalculatePositionAndTangent(doubleIndex - pointIndex, pointIndex, ref position, ref tangent);
        }

        //Get closest point in spline segment. Used for projection
        private double GetClosestPoint(int iterations, Vector3 point, double start, double end, int slices)
        {
            if (iterations <= 0)
            {
                float startDist = (point - EvaluatePosition(start)).sqrMagnitude;
                float endDist = (point - EvaluatePosition(end)).sqrMagnitude;
                if (startDist < endDist) return start;
                else if (endDist < startDist) return end;
                else return (start + end) / 2;
            }
            double closestPercent = 0.0;
            float closestDistance = Mathf.Infinity;
            double tick = (end - start) / slices;
            double t = start;
            Vector3 pos = Vector3.zero;
            while (true)
            {
                EvaluatePosition(t, ref pos);
                float dist = (point - pos).sqrMagnitude;
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPercent = t;
                }
                if (t == end) break;
                t = DMath.Move(t, end, tick);
            }
            double newStart = closestPercent - tick;
            if (newStart < start) newStart = start;
            double newEnd = closestPercent + tick;
            if (newEnd > end) newEnd = end;
            return GetClosestPoint(--iterations, point, newStart, newEnd, slices);
        }

        /// <summary>
        /// Break the closed spline
        /// </summary>
        public void Break()
        {
            Break(0);
        }

        /// <summary>
        /// Break the closed spline at given point
        /// </summary>
        /// <param name="at"></param>
        public void Break(int at)
        {
            if (!closed) return;
            if (at >= points.Length) return;
            if (at < 0) return;
            SplinePoint[] previousPoints = new SplinePoint[points.Length];
            points.CopyTo(previousPoints, 0);

            for (int i = at; i < previousPoints.Length; i++)
            {
                points[i - at] = previousPoints[i];
            }

            for (int i = 0; i < at; i++)
            {
                points[(points.Length - at) + i] = previousPoints[i];
            }

            closed = false;
        }

        /// <summary>
        /// Close the spline. This will cause the first and last points of the spline to merge
        /// </summary>
        public void Close()
        {
            if (points.Length < 3)
            {
                Debug.LogError("Points need to be at least 3 to close the spline");
                return;
            }
            closed = true;
        }

        /// <summary>
        /// Convert the spline to a Bezier path
        /// </summary>
        public void CatToBezierTangents()
        {
            switch (type)
            {
                case Type.Linear:
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i].type = SplinePoint.Type.Broken;
                        points[i].SetTangentPosition(points[i].position);
                        points[i].SetTangent2Position(points[i].position);
                    }
                    break;
                case Type.CatmullRom:
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i].type = SplinePoint.Type.SmoothMirrored;
                        double percent = GetPointPercent(i);
                        Vector3 tangent = Vector3.forward;
                        EvaluateTangent(percent, ref tangent);
                        if(_knotParametrization > 0f)
                        {
                            ComputeCatPoints(i);
                            points[i].SetTangent2Position(points[i].position + tangent.normalized * Vector3.Distance(P[0], P[2]) / 6f);
                        } else
                        {
                            points[i].SetTangent2Position(points[i].position + tangent / 3f);
                        }
                    }
                    break;
                case Type.BSpline:
                    //No BSPline support yet
                    break;
            }
            type = Type.Bezier;
        }

        /// <summary>
        /// Evaluates the position of the spline using one of the algorithms
        /// </summary>
        private void CalculatePosition(ref Vector3 position, double percent, int pointIndex)
        {
            switch (type)
            {
                case Type.CatmullRom:
                    ComputeCatPoints(pointIndex);
                    if (_knotParametrization < 0.000001f)
                    {
                        CalculateCatmullRomPositionFast(ref position, percent, pointIndex);
                    } else
                    {
                        CalculateCatmullRomComponents(percent);
                        CalculateCatmullRomPosition(percent, ref position);
                    }
                    break;
                case Type.Bezier: CalculateBezierPosition(ref position, percent, pointIndex); break;
                case Type.BSpline:
                    ComputeCatPoints(pointIndex);
                    CalculateBSplinePosition(ref position, percent, pointIndex); break;
                case Type.Linear:
                    ComputeCatPoints(pointIndex);
                    CalculateLinearPosition(ref position, percent, pointIndex); 
                    break;
            }
        }

        /// <summary>
        /// Evaluates the direction of the spline using one of the algorithms
        /// </summary>
        private void CalculateTangent(ref Vector3 tangent, double percent, int pointIndex)
        {
            switch (type)
            {
                case Type.CatmullRom:
                    ComputeCatPoints(pointIndex);
                    if (_knotParametrization < 0.000001f)
                    {
                        CalculateCatmullRomTangentFast(ref tangent, percent, pointIndex);
                    }
                    else
                    {
                        CalculateCatmullRomComponents(percent);
                        CalculateCatmullRomTangent(percent, ref tangent);
                    }
                    break;
                case Type.Bezier: 
                    CalculateBezierTangent(ref tangent, percent, pointIndex); 
                    break;
                case Type.Linear:
                    ComputeCatPoints(pointIndex);
                    CalculateLinearTangent(ref tangent, percent, pointIndex); 
                    break;
            }
        }

        /// <summary>
        /// Slightly faster than calling GetPoint and GetTangent separately
        /// </summary>
        private void CalculatePositionAndTangent(double percent, int pointIndex, ref Vector3 position, ref Vector3 tangent)
        {
            switch (type)
            {
                case Type.CatmullRom:
                    ComputeCatPoints(pointIndex);
                    if (_knotParametrization < 0.000001f)
                    {
                        CalculateCatmullRomPositionFast(ref position, percent, pointIndex);
                        CalculateCatmullRomTangentFast(ref tangent, percent, pointIndex);
                    }
                    else
                    {
                        CalculateCatmullRomComponents(percent);
                        CalculateCatmullRomPosition(percent, ref position);
                        CalculateCatmullRomTangent(percent, ref tangent);
                    }
                    break;
                case Type.Bezier: 
                    CalculateBezierPosition(ref position, percent, pointIndex);
                    CalculateBezierTangent(ref tangent, percent, pointIndex);
                    break;
                case Type.BSpline:
                    ComputeCatPoints(pointIndex);
                    CalculateBSplinePosition(ref position, percent, pointIndex); 
                    break;
                case Type.Linear:
                    ComputeCatPoints(pointIndex);
                    CalculateLinearPosition(ref position, percent, pointIndex);
                    CalculateLinearTangent(ref tangent, percent, pointIndex);
                    break;
            }
        }

        private void CalculateLinearPosition(ref Vector3 position, double t, int i)
        {
            if (points.Length == 0)
            {
                position = Vector3.zero;
                return;
            }

            position = Vector3.Lerp(P[1], P[2], (float)t);
        }

        private void CalculateLinearTangent(ref Vector3 tangent, double t, int i)
        {
            if (points.Length == 0)
            {
                tangent = Vector3.forward;
                return;
            }

            if (linearAverageDirection) tangent = Vector3.Slerp(P[1] - P[0], P[2] - P[1], 0.5f);
            else tangent = P[2] - P[1];
        }

        private void CalculateBSplinePosition(ref Vector3 position, double time, int i)
        {
            if (points.Length > 0) position = points[0].position;
            if (points.Length > 1)
            {
                float tf = (float)DMath.Clamp01(time);
                position = ((-P[0] + P[2]) / 2f 
                + tf * ((P[0] - 2f * P[1] + P[2]) / 2f 
                + tf * (-P[0] + 3f * P[1] - 3f * P[2] + P[3]) / 6f)) * tf 
                + (P[0] + 4f * P[1] + P[2]) / 6f;
            }
        }

        private void CalculateBezierPosition(ref Vector3 position, double t, int i)
        {
            if (points.Length > 0) position = points[0].position;
            else return;
            if (!closed && points.Length == 1) return;
            t = DMath.Clamp01(t);
            int it = i + 1;
            if (it >= points.Length)
            {
                it = 0;
            }

            float ft = (float)t;
            float nt = 1f - ft;
            position = nt * nt * nt * points[i].position + 
                3f * nt * nt * ft * points[i].tangent2 + 
                3f * nt * ft * ft * points[it].tangent + 
                ft * ft * ft * points[it].position;
        }

        private void CalculateBezierTangent(ref Vector3 tangent, double t, int i)
        {
            if (points.Length > 0) tangent = points[0].tangent;
            else return;
            if (!closed && points.Length == 1) return;
            t = DMath.Clamp01(t);
            int it = i + 1;
            if (it >= points.Length)
            {
                it = 0;
            }
            float ft = (float)t;
            float nt = 1f - ft;
            tangent = -3f * nt * nt * points[i].position + 
                3f * nt * nt * points[i].tangent2 - 
                6f * ft * nt * points[i].tangent2 - 
                3f * ft * ft * points[it].tangent + 
                6f * ft * nt * points[it].tangent + 
                3f * ft * ft * points[it].position;
           
        }

        private void CalculateCatmullRomComponents(double t)
        {
            const float t0 = 0f;
            t1 = GetInterval(P[0], P[1]);
            t2 = GetInterval(P[1], P[2]) + t1;
            t3 = GetInterval(P[2], P[3]) + t2;
            float tf = Mathf.LerpUnclamped(t1, t2, (float)t);

            A1 = (t1 - tf) / (t1 - t0) * P[0] + (tf - t0) / (t1 - t0) * P[1];
            A2 = (t2 - tf) / (t2 - t1) * P[1] + (tf - t1) / (t2 - t1) * P[2];
            A3 = (t3 - tf) / (t3 - t2) * P[2] + (tf - t2) / (t3 - t2) * P[3];

            B1 = (t2 - tf) / (t2 - t0) * A1 + (tf - t0) / (t2 - t0) * A2;
            B2 = (t3 - tf) / (t3 - t1) * A2 + (tf - t1) / (t3 - t1) * A3;
            

            float GetInterval(Vector3 a, Vector3 b)
            {
                return Mathf.Pow((a - b).sqrMagnitude, _knotParametrization * 0.5f);
            }
        }

        private void CalculateCatmullRomPosition(double t, ref Vector3 position)
        {
            float tf = Mathf.LerpUnclamped(t1, t2, (float)t);
            position = (t2 - tf) / (t2 - t1) * B1 + (tf - t1) / (t2 - t1) * B2;
        }

        private void CalculateCatmullRomTangent(double t, ref Vector3 tangent)
        {
            float tf = Mathf.LerpUnclamped(t1, t2, (float)t);
            Vector3 A1p = (P[1] - P[0]) / t1;
            Vector3 A2p = (P[2] - P[1]) / (t2 - t1);
            Vector3 A3p = (P[3] - P[2]) / (t3 - t2);

            Vector3 B1p = (A2 - A1) / t2  + (t2 - tf) / t2 * A1p + tf / t2  * A2p;
            Vector3 B2p = (A3 - A2) / (t3 - t1) + (t3 - tf) / (t3 - t1) * A2p + (tf - t1) / (t3 - t1) * A3p;

            tangent = (B2 - B1) / (t2 - t1) + (t2 - tf) / (t2 - t1) * B1p + (tf - t1) / (t2 - t1) * B2p;
        }

        private void CalculateCatmullRomPositionFast(ref Vector3 position, double t, int i)
        {
            float t1 = (float)t;
            float t2 = t1 * t1;
            float t3 = t2 * t1;
            if (points.Length > 0)
            {
                position = points[0].position;
            }

            if (!closed && i >= points.Length) return;

            if (points.Length > 1)
            {
                position = 0.5f * ((2f * P[1]) + (-P[0] + P[2]) * t1
                + (2f * P[0] - 5f * P[1] + 4f * P[2] - P[3]) * t2
                + (-P[0] + 3f * P[1] - 3f * P[2] + P[3]) * t3);
            }
        }

        private void CalculateCatmullRomTangentFast(ref Vector3 tangent, double t, int i)
        {
            float t1 = (float)t;
            float t2 = t1 * t1;
            if (!closed && i >= points.Length) return;
            if (points.Length > 1)
            {
                tangent = (6 * t2 - 6 * t1) * P[1]
                + (3 * t2 - 4 * t1 + 1) * (P[2] - P[0]) * 0.5f
                + (-6 * t2 + 6 * t1) * P[2]
                + (3 * t2 - 2 * t1) * (P[3] - P[1]) * 0.5f;
            }
        }

        private void ComputeCatPoints(int i)
        {
            int p1 = i - 1;
            int p2 = i;
            int p3 = i + 1;
            int p4 = i + 2;

            if (closed)
            {
                if(p1 < 0)
                {
                    p1 += points.Length;
                }
                if (p2 >= points.Length)
                {
                    p2 -= points.Length;
                }
                if (p3 >= points.Length)
                {
                    p3 -= points.Length;
                }
                if(p4 >= points.Length)
                {
                    p4 -= points.Length;
                }
                P[0] = points[p1].position;
                P[1] = points[p2].position;
                P[2] = points[p3].position;
                P[3] = points[p4].position;
            } else
            {
                if(p1 < 0)
                {
                    P[0] = points[0].position;
                    P[0] += (P[0] - points[1].position);
                } else 
                {
                    P[0] = points[p1].position;
                }

                P[1] = points[p2].position;

                if (p3 >= points.Length)
                {
                    P[2] = points[points.Length - 1].position;
                    Vector3 pos = P[2];
                    P[2] += P[2] - points[points.Length - 2].position;
                    P[3] = P[2] + (P[2] - pos);
                }
                else
                {
                    P[2] = points[p3].position;
                    if(p4 >= points.Length)
                    {
                        P[3] = P[2] + (P[2] - points[p3 - 1].position);
                    } 
                    else
                    {
                        P[3] = points[p4].position;
                    }
                }
            }
        }

        public static void FormatFromTo(ref double from, ref double to, bool preventInvert = true)
        {
            from = DMath.Clamp01(from);
            to = DMath.Clamp01(to);
            if (preventInvert && from > to)
            {
                double tmp = from;
                from = to;
                to = tmp;
            } else  to = DMath.Clamp(to, 0.0, 1.0);
        }
    }


}
