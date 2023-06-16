namespace Dreamteck.Splines
{
    using UnityEngine;
    using System.Collections.Generic;

    public delegate void EmptySplineHandler();
    //MonoBehaviour wrapper for the spline class. It transforms the spline using the object's transform and provides thread-safe methods for sampling
    [AddComponentMenu("Dreamteck/Splines/Spline Computer")]
    [ExecuteInEditMode]
    public partial class SplineComputer : MonoBehaviour
    {
#if UNITY_EDITOR
        public enum EditorUpdateMode { Default, OnMouseUp }
        [HideInInspector]
        public bool editorDrawPivot = true;
        [HideInInspector]
        public Color editorPathColor = Color.white;
        [HideInInspector]
        public bool editorAlwaysDraw = false;
        [HideInInspector]
        public bool editorDrawThickness = false;
        [HideInInspector]
        public bool editorBillboardThickness = true;
        private bool _editorIsPlaying = false;
        [HideInInspector]
        public bool isNewlyCreated = true;
        [HideInInspector]
        public EditorUpdateMode editorUpdateMode = EditorUpdateMode.Default;
#endif
        public enum Space { World, Local };
        public enum EvaluateMode { Cached, Calculate }
        public enum SampleMode { Default, Uniform, Optimized }
        public enum UpdateMode { Update, FixedUpdate, LateUpdate, AllUpdate, None }
        public Space space
        {
            get { return _space; }
            set
            {
                if (value != _space)
                {
                    SplinePoint[] worldPoints = GetPoints();
                    _space = value;
                    SetPoints(worldPoints);
                }
            }
        }
        public Spline.Type type
        {
            get
            {
                return _spline.type;
            }

            set
            {
                if (value != _spline.type)
                {
                    _spline.type = value;
                    Rebuild(true);
                }
            }
        }

        public float knotParametrization
        {
            get { return _spline.knotParametrization; }
            set
            {
                float last = _spline.knotParametrization;
                _spline.knotParametrization = value;
                if(last != _spline.knotParametrization)
                {
                    Rebuild(true);
                }
            }
        }

        public bool linearAverageDirection
        {
            get
            {
                return _spline.linearAverageDirection;
            }

            set
            {
                if (value != _spline.linearAverageDirection)
                {
                    _spline.linearAverageDirection = value;
                    Rebuild(true);
                }
            }
        }

        public bool is2D
        {
            get { return _is2D; }
            set
            {
                if (value != _is2D)
                {
                    _is2D = value;
                    SetPoints(GetPoints());
                }
            }
        }

        public int sampleRate
        {
            get { return _spline.sampleRate; }
            set
            {
                if (value != _spline.sampleRate)
                {
                    if (value < 2) value = 2;
                    _spline.sampleRate = value;
                    Rebuild(true);
                }
            }
        }

        public float optimizeAngleThreshold
        {
            get { return _optimizeAngleThreshold; }
            set
            {
                if (value != _optimizeAngleThreshold)
                {
                    if (value < 0.001f) value = 0.001f;
                    _optimizeAngleThreshold = value;
                    if (_sampleMode == SampleMode.Optimized)
                    {
                        Rebuild(true);
                    }
                }
            }
        }

        public SampleMode sampleMode
        {
            get { return _sampleMode; }
            set
            {
                if (value != _sampleMode)
                {
                    _sampleMode = value;
                    Rebuild(true);
                }
            }
        }
        [HideInInspector]
        public bool multithreaded = false;
        [HideInInspector]
        public UpdateMode updateMode = UpdateMode.Update;
        [HideInInspector]
        public TriggerGroup[] triggerGroups = new TriggerGroup[0];

        public AnimationCurve customValueInterpolation
        {
            get { return _spline.customValueInterpolation; }
            set
            {
                _spline.customValueInterpolation = value;
                Rebuild();
            }
        }

        public AnimationCurve customNormalInterpolation
        {
            get { return _spline.customNormalInterpolation; }
            set
            {
                _spline.customNormalInterpolation = value;
                Rebuild();
            }
        }

        public int iterations
        {
            get
            {
                return _spline.iterations;
            }
        }

        public double moveStep
        {
            get
            {
                return _spline.moveStep;
            }
        }

        public bool isClosed
        {
            get
            {
                return _spline.isClosed;
            }
        }

        public int pointCount
        {
            get
            {
                return _spline.points.Length;
            }
        }

        public int sampleCount
        {
            get { return _sampleCollection.length; }
        }

        /// <summary>
        /// Returns the sample at the index transformed by the object's matrix
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SplineSample this [int index]
        {
            get
            {
                UpdateSampleCollection();
                return _sampleCollection.samples[index];
            }
        }

        /// <summary>
        /// The raw spline samples without transformation applied
        /// </summary>
        public SplineSample[] rawSamples
        {
            get { return _rawSamples; }
        }

        /// <summary>
        /// Thread-safe transform's position
        /// </summary>
        public Vector3 position
        {
            get {
#if UNITY_EDITOR
                if (!_editorIsPlaying) return transform.position;
#endif
                return _localToWorldMatrix.MultiplyPoint3x4(Vector3.zero);
            }
        }
        /// <summary>
        /// Thread-safe transform's rotation
        /// </summary>
        public Quaternion rotation
        {
            get {
#if UNITY_EDITOR
                if (!_editorIsPlaying) return transform.rotation;
#endif
                return _localToWorldMatrix.rotation;
            }
        }
        /// <summary>
        /// Thread-safe transform's scale
        /// </summary>
        public Vector3 scale
        {
            get {
#if UNITY_EDITOR
                if (!_editorIsPlaying) return transform.lossyScale;
#endif
                return _localToWorldMatrix.lossyScale;
            }
        }

        /// <summary>
        /// returns the number of subscribers this computer has
        /// </summary>
        public int subscriberCount
        {
            get
            {
                return _subscribers.Length;
            }
        }

        [HideInInspector]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("spline")]
        private Spline _spline = new Spline(Spline.Type.CatmullRom);

        [HideInInspector]
        private SampleCollection _sampleCollection = new SampleCollection();

        [HideInInspector]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("originalSamplePercents")]
        private double[] _originalSamplePercents = new double[0];
        [HideInInspector]
        [SerializeField]
        private bool _is2D = false;
        [HideInInspector]
        [SerializeField]
        private bool hasSamples = false;
        [HideInInspector]
        [SerializeField]
        [Range(0.001f, 45f)]
        private float _optimizeAngleThreshold = 0.5f;
        [HideInInspector]
        [SerializeField]
        private Space _space = Space.Local;
        [HideInInspector]
        [SerializeField]
        private SampleMode _sampleMode = SampleMode.Default;
        [HideInInspector]
        [SerializeField]
        private SplineUser[] _subscribers = new SplineUser[0];

        [HideInInspector]
        [SerializeField]
        private SplineSample[] _rawSamples = new SplineSample[0];

        private Matrix4x4 _localToWorldMatrix = Matrix4x4.identity;
        private Matrix4x4 _worldToLocalMatrix = Matrix4x4.identity;

        [HideInInspector]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("nodes")]
        private NodeLink[] _nodes = new NodeLink[0];
        private bool _rebuildPending = false;
        private bool _trsCached = false;
        private Transform _trs = null;


        public Transform trs
        {
            get
            {
#if UNITY_EDITOR
                if (!_editorIsPlaying)
                {
                    return transform;
                }
#endif
                if (!_trsCached)
                {
                    _trs = transform;
                    _trsCached = true;
                }
                return _trs;
            }
        }

        private bool _queueResample = false, _queueRebuild = false;

        public event EmptySplineHandler onRebuild;

        private bool useMultithreading
        {
            get
            {
                return multithreaded
#if UNITY_EDITOR
                && _editorIsPlaying
#endif
                ;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Used by the editor - should not be called from the API
        /// </summary>
        public void EditorAwake()
        {
            UpdateConnectedNodes();
            RebuildImmediate(true, true);
        }

        /// <summary>
        /// Used by the editor - should not be called from the API
        /// </summary>
        public void EditorUpdateConnectedNodes()
        {
            UpdateConnectedNodes();
        }
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            _editorIsPlaying = Application.isPlaying;
#endif
            ResampleTransform();
        }

        void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate || updateMode == UpdateMode.AllUpdate)
            {
                RunUpdate();
            }
        }

        void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate || updateMode == UpdateMode.AllUpdate)
            {
                RunUpdate();
            }
        }

        void Update()
        {
            if (updateMode == UpdateMode.Update || updateMode == UpdateMode.AllUpdate)
            {
                RunUpdate();
            }
        }

        private void RunUpdate(bool immediate = false)
        {
            bool transformChanged = ResampleTransformIfNeeded();
            if(_sampleCollection.samples.Length != _rawSamples.Length)
            {
                transformChanged = true;
            }

            if (useMultithreading)
            {
                //Rebuild users at the beginning of the next cycle if multithreaded
                if (_queueRebuild)
                {
                    RebuildUsers(immediate);
                }
            }

            if (_queueResample)
            {
                if (useMultithreading)
                {
                    if (transformChanged)
                    {
                        SplineThreading.Run(CalculateWithoutTransform);
                    } else
                    {
                        SplineThreading.Run(CalculateWithTransform);
                    }
                }
                else
                {
                    CalculateSamples(!transformChanged);
                }
            }

            if (transformChanged)
            {
                if (useMultithreading)
                {
                    SplineThreading.Run(TransformSamples);
                }
                else
                {
                    TransformSamples();
                }
            }

            if (!useMultithreading)
            {
                //If not multithreaded, rebuild users here
                if (_queueRebuild)
                {
                    RebuildUsers(immediate);
                }
            }

            void CalculateWithTransform()
            {
                CalculateSamples();
            }

            void CalculateWithoutTransform()
            {
                CalculateSamples(false);
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            editorPathColor = SplinePrefs.defaultColor;
            editorDrawThickness = SplinePrefs.defaultShowThickness;
            is2D = SplinePrefs.default2D;
            editorAlwaysDraw = SplinePrefs.defaultAlwaysDraw;
            editorUpdateMode = SplinePrefs.defaultEditorUpdateMode;
            space = SplinePrefs.defaultComputerSpace;
            type = SplinePrefs.defaultType;
        }
#endif

        void OnEnable()
        {
            if (_rebuildPending)
            {
                _rebuildPending = false;
                Rebuild();
            }
        }

        public void GetSamples(SampleCollection collection)
        {
            UpdateSampleCollection();
            collection.samples = _sampleCollection.samples;
            collection.optimizedIndices = _sampleCollection.optimizedIndices;
            collection.sampleMode = _sampleMode;
        }

        private void UpdateSampleCollection()
        {
            if (_sampleCollection.samples.Length != _rawSamples.Length)
            {
                TransformSamples();
            }
        }

        private bool ResampleTransformIfNeeded()
        {
            bool changed = false;
            //This is used to skip comparing matrices on every frame during runtime
#if UNITY_EDITOR
            if (_editorIsPlaying)
            {
#endif
                if (!trs.hasChanged) return false;
                trs.hasChanged = false;
#if UNITY_EDITOR
            }
#endif

            if (_localToWorldMatrix != trs.localToWorldMatrix)
            {
                ResampleTransform();
                _queueRebuild = true;
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Immediately sample the computer's transform (thread-unsafe). Call this before SetPoint(s) if the transform has been modified in the same frame
        /// </summary>
        public void ResampleTransform()
        {
            _localToWorldMatrix = trs.localToWorldMatrix;
            _worldToLocalMatrix = trs.worldToLocalMatrix;
        }

        /// <summary>
        /// Subscribe a SplineUser to this computer. This will rebuild the user automatically when there are changes.
        /// </summary>
        /// <param name="input">The SplineUser to subscribe</param>
        public void Subscribe(SplineUser input)
        {
            if (!IsSubscribed(input))
            {
                ArrayUtility.Add(ref _subscribers, input);
            }
        }

        /// <summary>
        /// Unsubscribe a SplineUser from this computer's updates
        /// </summary>
        /// <param name="input">The SplineUser to unsubscribe</param>
        public void Unsubscribe(SplineUser input)
        {
            for (int i = 0; i < _subscribers.Length; i++)
            {
                if (_subscribers[i] == input)
                {
                    ArrayUtility.RemoveAt(ref _subscribers, i);
                    return;
                }
            }
        }

        /// <summary>
        /// Checks if a user is subscribed to that computer
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsSubscribed(SplineUser user)
        {
            for (int i = 0; i < _subscribers.Length; i++)
            {
                if (_subscribers[i] == user)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns an array of subscribed users
        /// </summary>
        /// <returns></returns>
        public SplineUser[] GetSubscribers()
        {
            SplineUser[] subs = new SplineUser[_subscribers.Length];
            _subscribers.CopyTo(subs, 0);
            return subs;
        }

        /// <summary>
        /// Get the points from this computer's spline. All points are transformed in world coordinates.
        /// </summary>
        /// <returns></returns>
        public SplinePoint[] GetPoints(Space getSpace = Space.World)
        {
            SplinePoint[] points = new SplinePoint[_spline.points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = _spline.points[i];
                if (_space == Space.Local && getSpace == Space.World)
                {
                    points[i].position = TransformPoint(points[i].position);
                    points[i].tangent = TransformPoint(points[i].tangent);
                    points[i].tangent2 = TransformPoint(points[i].tangent2);
                    points[i].normal = TransformDirection(points[i].normal);
                }
            }
            return points;
        }

        /// <summary>
        /// Get a point from this computer's spline. The point is transformed in world coordinates.
        /// </summary>
        /// <param name="index">Point index</param>
        /// <returns></returns>
        public SplinePoint GetPoint(int index, Space getSpace = Space.World)
        {
            if (index < 0 || index >= _spline.points.Length) return new SplinePoint();
            if (_space == Space.Local && getSpace == Space.World)
            {
                ResampleTransformIfNeeded();
                SplinePoint point = _spline.points[index];
                point.position = TransformPoint(point.position);
                point.tangent = TransformPoint(point.tangent);
                point.tangent2 = TransformPoint(point.tangent2);
                point.normal = TransformDirection(point.normal);
                return point;
            }
            else
            {
                return _spline.points[index];
            }
        }

        public Vector3 GetPointPosition(int index, Space getSpace = Space.World)
        {
            if (_space == Space.Local && getSpace == Space.World)
            {
                ResampleTransformIfNeeded();
                return TransformPoint(_spline.points[index].position);
            }
            else return _spline.points[index].position;
        }

        public Vector3 GetPointNormal(int index, Space getSpace = Space.World)
        {
            if (_space == Space.Local && getSpace == Space.World)
            {
                ResampleTransformIfNeeded();
                return TransformDirection(_spline.points[index].normal).normalized;
            }
            else return _spline.points[index].normal;
        }

        public Vector3 GetPointTangent(int index, Space getSpace = Space.World)
        {
            if (_space == Space.Local && getSpace == Space.World)
            {
                ResampleTransformIfNeeded();
                return TransformPoint(_spline.points[index].tangent);
            }
            else return _spline.points[index].tangent;
        }

        public Vector3 GetPointTangent2(int index, Space getSpace = Space.World)
        {
            if (_space == Space.Local && getSpace == Space.World)
            {
                ResampleTransformIfNeeded();
                return TransformPoint(_spline.points[index].tangent2);
            }
            else return _spline.points[index].tangent2;
        }

        public float GetPointSize(int index, Space getSpace = Space.World)
        {
            return _spline.points[index].size;
        }

        public Color GetPointColor(int index, Space getSpace = Space.World)
        {
            return _spline.points[index].color;
        }

        private void Make2D(ref SplinePoint point)
        {
            point.Flatten(LinearAlgebraUtility.Axis.Z);
        }

        /// <summary>
        /// Set the points of this computer's spline.
        /// </summary>
        /// <param name="points">The points array</param>
        /// <param name="setSpace">Use world or local space</param>
        public void SetPoints(SplinePoint[] points, Space setSpace = Space.World)
        {
            ResampleTransformIfNeeded();
            bool rebuild = false;
            if (points.Length != _spline.points.Length)
            {
                rebuild = true;
                if (points.Length < 3)
                {
                    Break();
                }
                _spline.points = new SplinePoint[points.Length];
                SetAllDirty();
            }

            for (int i = 0; i < points.Length; i++)
            {
                SplinePoint newPoint = points[i];
                if(_spline.points.Length > i)
                {
                    newPoint.isDirty = _spline.points[i].isDirty;
                }
                if (_space == Space.Local && setSpace == Space.World)
                {
                    newPoint.position = InverseTransformPoint(points[i].position);
                    newPoint.tangent = InverseTransformPoint(points[i].tangent);
                    newPoint.tangent2 = InverseTransformPoint(points[i].tangent2);
                    newPoint.normal = InverseTransformDirection(points[i].normal);
                }

                if (_is2D)
                {
                    Make2D(ref newPoint);
                }

                if (newPoint != _spline.points[i])
                {
                    newPoint.isDirty = true;
                    rebuild = true;
                }

                _spline.points[i] = newPoint;

            }

            if (rebuild)
            {
                Rebuild();
                UpdateConnectedNodes(points);
            }
        }

        /// <summary>
        /// Set the position of a control point. This is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pos"></param>
        /// <param name="setSpace"></param>
        public void SetPointPosition(int index, Vector3 pos, Space setSpace = Space.World)
        {
            if (index < 0) return;
            ResampleTransformIfNeeded();
            if (index >= _spline.points.Length)
            {
                AppendPoints((index + 1) - _spline.points.Length);
            }
            Vector3 newPos = pos;
            if (_space == Space.Local && setSpace == Space.World) newPos = InverseTransformPoint(pos);
            if (newPos != _spline.points[index].position)
            {
                SetDirty(index);
                _spline.points[index].SetPosition(newPos);
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set the tangents of a control point. This is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="tan1"></param>
        /// <param name="tan2"></param>
        /// <param name="setSpace"></param>
        public void SetPointTangents(int index, Vector3 tan1, Vector3 tan2, Space setSpace = Space.World)
        {
            if (index < 0) return;
            ResampleTransformIfNeeded();
            if (index >= _spline.points.Length)
            {
                AppendPoints((index + 1) - _spline.points.Length);
            }
            Vector3 newTan1 = tan1;
            Vector3 newTan2 = tan2;
            if (_space == Space.Local && setSpace == Space.World)
            {
                newTan1 = InverseTransformPoint(tan1);
                newTan2 = InverseTransformPoint(tan2);
            }
            bool rebuild = false;
            if (newTan2 != _spline.points[index].tangent2)
            {
                rebuild = true;
                _spline.points[index].SetTangent2Position(newTan2);
            }
            if (newTan1 != _spline.points[index].tangent)
            {
                rebuild = true;
                _spline.points[index].SetTangentPosition(newTan1);
            }
            if (_is2D) Make2D(ref _spline.points[index]);

            if (rebuild)
            {
                SetDirty(index);
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set the normal of a control point. This is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="nrm"></param>
        /// <param name="setSpace"></param>
        public void SetPointNormal(int index, Vector3 nrm, Space setSpace = Space.World)
        {
            if (index < 0) return;
            ResampleTransformIfNeeded();
            if (index >= _spline.points.Length)
            {
                AppendPoints((index + 1) - _spline.points.Length);
            }
            Vector3 newNrm = nrm;
            if (_space == Space.Local && setSpace == Space.World) newNrm = InverseTransformDirection(nrm);
            if (newNrm != _spline.points[index].normal)
            {
                SetDirty(index);
                _spline.points[index].normal = newNrm;
                if (_is2D) Make2D(ref _spline.points[index]);
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set the size of a control point. This is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="size"></param>
        public void SetPointSize(int index, float size)
        {
            if (index < 0) return;
            if (index >= _spline.points.Length)
            {
                AppendPoints((index + 1) - _spline.points.Length);
            }
            if (size != _spline.points[index].size)
            {
                SetDirty(index);
                _spline.points[index].size = size;
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set the color of a control point. THis is faster than SetPoint
        /// </summary>
        /// <param name="index"></param>
        /// <param name="color"></param>
        public void SetPointColor(int index, Color color)
        {
            if (index < 0) return;
            if (index >= _spline.points.Length)
            {
                AppendPoints((index + 1) - _spline.points.Length);
            }
            if (color != _spline.points[index].color)
            {
                SetDirty(index);
                _spline.points[index].color = color;
                Rebuild();
                SetNodeForPoint(index, GetPoint(index));
            }
        }

        /// <summary>
        /// Set a control point in world coordinates
        /// </summary>
        /// <param name="index"></param>
        /// <param name="point"></param>
        public void SetPoint(int index, SplinePoint point, Space setSpace = Space.World)
        {
            if (index < 0) return;
            ResampleTransformIfNeeded();
            if (index >= _spline.points.Length)
            {
                AppendPoints((index + 1) - _spline.points.Length);
            }
            SplinePoint newPoint = point;
            if (_space == Space.Local && setSpace == Space.World)
            {
                newPoint.position = InverseTransformPoint(point.position);
                newPoint.tangent = InverseTransformPoint(point.tangent);
                newPoint.tangent2 = InverseTransformPoint(point.tangent2);
                newPoint.normal = InverseTransformDirection(point.normal);
            }

            if (_is2D)
            {
                Make2D(ref newPoint);
            }

            if (newPoint != _spline.points[index])
            {
                newPoint.isDirty = true;
                _spline.points[index] = newPoint;
                Rebuild();
                SetNodeForPoint(index, point);
            }
        }

        private void AppendPoints(int count)
        {
            SplinePoint[] newPoints = new SplinePoint[_spline.points.Length + count];
            _spline.points.CopyTo(newPoints, 0);
            _spline.points = newPoints;
            Rebuild(true);
        }

        /// <summary>
        /// Converts a point index to spline percent
        /// </summary>
        /// <param name="pointIndex">The point index</param>
        /// <returns></returns>
        public double GetPointPercent(int pointIndex)
        {
            double percent = DMath.Clamp01((double)pointIndex / (_spline.points.Length - 1));
            if (_spline.isClosed)
            {
                percent = DMath.Clamp01((double)pointIndex / _spline.points.Length);
            }
            if (_sampleMode != SampleMode.Uniform) return percent;

            if (_originalSamplePercents.Length <= 1) return 0.0;
            for (int i = _originalSamplePercents.Length - 2; i >= 0; i--)
            {
                if (_originalSamplePercents[i] < percent)
                {
                    double inverseLerp = DMath.InverseLerp(_originalSamplePercents[i], _originalSamplePercents[i + 1], percent);
                    return DMath.Lerp(_rawSamples[i].percent, _rawSamples[i+1].percent, inverseLerp);
                }
            }
            return 0.0;
        }

        public int PercentToPointIndex(double percent, Spline.Direction direction = Spline.Direction.Forward)
        {
            int count = _spline.points.Length - 1;
            if (isClosed) count = _spline.points.Length;

            if (_sampleMode == SampleMode.Uniform)
            {
                int index;
                double lerp;
                GetSamplingValues(percent, out index, out lerp);
                if (lerp > 0.0 && index < _originalSamplePercents.Length - 1)
                {
                    lerp = DMath.Lerp(_originalSamplePercents[index], _originalSamplePercents[index + 1], lerp);
                    if (direction == Spline.Direction.Forward)
                    {
                        return DMath.FloorInt(lerp * count);
                    }
                    else
                    {
                        return DMath.CeilInt(lerp * count);
                    }
                }

                if (direction == Spline.Direction.Forward)
                {
                    return DMath.FloorInt(_originalSamplePercents[index] * count);
                }
                else
                {
                    return DMath.CeilInt(_originalSamplePercents[index] * count);
                }
            }

            int point = 0;
            if (direction == Spline.Direction.Forward)
            {
                point = DMath.FloorInt(percent * count);
            }
            else
            {
                point = DMath.CeilInt(percent * count);
            }
            if (point >= _spline.points.Length)
            {
                point = 0;
            }
            return point;
        }

        public Vector3 EvaluatePosition(double percent)
        {
            return EvaluatePosition(percent, EvaluateMode.Cached);
        }

        /// <summary>
        /// Same as Spline.EvaluatePosition but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="percent">Evaluation percent</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <returns></returns>
        public Vector3 EvaluatePosition(double percent, EvaluateMode mode = EvaluateMode.Cached)
        {
            if (mode == EvaluateMode.Calculate) return TransformPoint(_spline.EvaluatePosition(percent));
            UpdateSampleCollection();
            return _sampleCollection.EvaluatePosition(percent);
        }

        public Vector3 EvaluatePosition(int pointIndex, EvaluateMode mode = EvaluateMode.Cached)
        {
            return EvaluatePosition(GetPointPercent(pointIndex), mode);
        }

        public SplineSample Evaluate(double percent)
        {
            return Evaluate(percent, EvaluateMode.Cached);
        }

        /// <summary>
        /// Same as Spline.Evaluate but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="percent">Evaluation percent</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <returns></returns>
        public SplineSample Evaluate(double percent, EvaluateMode mode = EvaluateMode.Cached)
        {
            SplineSample result = new SplineSample();
            Evaluate(percent, ref result, mode);
            return result;
        }

        /// <summary>
        /// Evaluate the spline at the position of a given point and return a SplineSample
        /// </summary>
        /// <param name="pointIndex">Point index</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        public SplineSample Evaluate(int pointIndex)
        {
            SplineSample result = new SplineSample();
            Evaluate(pointIndex, ref result);
            return result;
        }

        /// <summary>
        /// Evaluate the spline at the position of a given point and write in the SplineSample output
        /// </summary>
        /// <param name="pointIndex">Point index</param>
        public void Evaluate(int pointIndex, ref SplineSample result)
        {
            Evaluate(GetPointPercent(pointIndex), ref result);
        }

        public void Evaluate(double percent, ref SplineSample result)
        {
            Evaluate(percent, ref result, EvaluateMode.Cached);
        }
        /// <summary>
        /// Same as Spline.Evaluate but the result is transformed by the computer's transform
        /// </summary>
        /// <param name="result"></param>
        /// <param name="percent"></param>
        public void Evaluate(double percent, ref SplineSample result, EvaluateMode mode = EvaluateMode.Cached)
        {
            if (mode == EvaluateMode.Calculate)
            {
                _spline.Evaluate(percent, ref result);
                TransformSample(ref result);
            } else
            {
                UpdateSampleCollection();
                _sampleCollection.Evaluate(percent, ref result);
            }
        }

        /// <summary>
        /// Same as Spline.Evaluate but the results are transformed by the computer's transform
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void Evaluate(ref SplineSample[] results, double from = 0.0, double to = 1.0)
        {
            UpdateSampleCollection();
            _sampleCollection.Evaluate(ref results, from, to);
        }

        /// <summary>
        /// Same as Spline.EvaluatePositions but the results are transformed by the computer's transform
        /// </summary>
        /// <param name="from">Start position [0-1]</param>
        /// <param name="to">Target position [from-1]</param>
        /// <returns></returns>
        public void EvaluatePositions(ref Vector3[] positions, double from = 0.0, double to = 1.0)
        {
            UpdateSampleCollection();
            _sampleCollection.EvaluatePositions(ref positions, from, to);
        }

        /// <summary>
        /// Returns the percent from the spline at a given distance from the start point
        /// </summary>
        /// <param name="start">The start point</param>
        /// /// <param name="distance">The distance to travel</param>
        /// <param name="direction">The direction towards which to move</param>
        /// <returns></returns>
        public double Travel(double start, float distance, out float moved, Spline.Direction direction = Spline.Direction.Forward)
        {
            UpdateSampleCollection();
            return _sampleCollection.Travel(start, distance, direction, out moved);
        }

        public double Travel(double start, float distance, Spline.Direction direction = Spline.Direction.Forward)
        {
            float moved;
            return Travel(start, distance, out moved, direction);
        }


        [System.Obsolete("This project override is obsolete, please use Project(Vector3 position, ref SplineSample result, double from = 0.0, double to = 1.0, EvaluateMode mode = EvaluateMode.Cached, int subdivisions = 4) instead")]
        public void Project(ref SplineSample result, Vector3 position, double from = 0.0, double to = 1.0, EvaluateMode mode = EvaluateMode.Cached, int subdivisions = 4)
        {
            Project(position, ref result, from, to, mode, subdivisions);
        }

        /// <summary>
        /// Same as Spline.Project but the point is transformed by the computer's transform.
        /// </summary>
        /// <param name="worldPoint">Point in world space</param>
        /// <param name="subdivide">Subdivisions default: 4</param>
        /// <param name="from">Sample from [0-1] default: 0f</param>
        /// <param name="to">Sample to [0-1] default: 1f</param>
        /// <param name="mode">Mode to use the method in. Cached uses the cached samples while Calculate is more accurate but heavier</param>
        /// <param name="subdivisions">Subdivisions for the Calculate mode. Don't assign if not using Calculated mode.</param>
        /// <returns></returns>
        public void Project(Vector3 worldPoint, ref SplineSample result, double from = 0.0, double to = 1.0, EvaluateMode mode = EvaluateMode.Cached, int subdivisions = 4)
        {
            if (mode == EvaluateMode.Calculate)
            {
                worldPoint = InverseTransformPoint(worldPoint);
                double percent = _spline.Project(InverseTransformPoint(worldPoint), subdivisions, from, to);
                _spline.Evaluate(percent, ref result);
                TransformSample(ref result);
                return;
            }
            UpdateSampleCollection();
            _sampleCollection.Project(worldPoint, _spline.points.Length, ref result, from, to);
        }

        public SplineSample Project(Vector3 worldPoint, double from = 0.0, double to = 1.0)
        {
            SplineSample result = new SplineSample();
            Project(worldPoint, ref result, from, to);
            return result;
        }

        /// <summary>
        /// Same as Spline.CalculateLength but this takes the computer's transform into account when calculating the length.
        /// </summary>
        /// <param name="from">Calculate from [0-1] default: 0f</param>
        /// <param name="to">Calculate to [0-1] default: 1f</param>
        /// <param name="resolution">Resolution [0-1] default: 1f</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public float CalculateLength(double from = 0.0, double to = 1.0)
        {
            if (!hasSamples) return 0f;
            UpdateSampleCollection();
            return _sampleCollection.CalculateLength(from, to);
        }

        private void TransformSample(ref SplineSample result)
        {
            result.position = _localToWorldMatrix.MultiplyPoint3x4(result.position);
            result.forward = _localToWorldMatrix.MultiplyVector(result.forward);
            result.up = _localToWorldMatrix.MultiplyVector(result.up);
        }

        public void Rebuild(bool forceUpdateAll = false)
        {
            if (forceUpdateAll)
            {
                SetAllDirty();
            }

#if UNITY_EDITOR
            if (!_editorIsPlaying)
            {
                if (editorUpdateMode == EditorUpdateMode.Default)
                {
                    RebuildImmediate(true);
                }
                return;
            }
#endif

            _queueResample = updateMode != UpdateMode.None;
        }

        public void RebuildImmediate()
        {
            RebuildImmediate(true, true);
        }

        public void RebuildImmediate(bool calculateSamples = true, bool forceUpdateAll = false)
        {
            if (calculateSamples)
            {
                _queueResample = true;
                if (forceUpdateAll)
                {
                    SetAllDirty();
                }
            }
            else
            {
                _queueResample = false;
            }
            RunUpdate(true);
        }

        private void RebuildUsers(bool immediate = false)
        {
            for (int i = _subscribers.Length - 1; i >= 0; i--)
            {
                if (_subscribers[i] != null)
                {
                    if (immediate)
                    {
                        _subscribers[i].RebuildImmediate();
                    }
                    else
                    {
                        _subscribers[i].Rebuild();
                    }
                }
                else
                {
                    ArrayUtility.RemoveAt(ref _subscribers, i);
                }
            }

            if (onRebuild != null)
            {
                onRebuild();
            }
            _queueRebuild = false;
        }

        private void SetAllDirty()
        {
            for (int i = 0; i < _spline.points.Length; i++)
            {
                _spline.points[i].isDirty = true;
            }
        }

        private void SetDirty(int index)
        {
            if (sampleMode == SampleMode.Uniform)
            {
                SetAllDirty();
                return;
            }
            _spline.points[index].isDirty = true;
        }

        private void CalculateSamples(bool transformSamples = true)
        {
            _queueResample = false;
            _queueRebuild = true;
            if (_spline.points.Length == 0)
            {
                if (_rawSamples.Length != 0)
                {
                    _rawSamples = new SplineSample[0];
                    if (transformSamples)
                    {
                        TransformSamples();
                    }
                }
                return;
            }

            if (_spline.points.Length == 1)
            {
                if (_rawSamples.Length != 1)
                {
                    _rawSamples = new SplineSample[1];
                    if (transformSamples)
                    {
                        TransformSamples();
                    }
                }
                _spline.Evaluate(0.0, ref _rawSamples[0]);
                return;
            }

            if (_sampleMode == SampleMode.Uniform)
            {
                _spline.EvaluateUniform(ref _rawSamples, ref _originalSamplePercents);
                if (transformSamples)
                {
                    TransformSamples();
                }
            }
            else
            {
                if (_originalSamplePercents.Length > 0)
                {
                    _originalSamplePercents = new double[0];
                }

                if (_rawSamples.Length != _spline.iterations)
                {
                    _rawSamples = new SplineSample[_spline.iterations];
                    for (int i = 0; i < _rawSamples.Length; i++)
                    {
                        _rawSamples[i] = new SplineSample();
                    }
                }

                if (_sampleCollection.samples.Length != _rawSamples.Length)
                {
                    _sampleCollection.samples = new SplineSample[_rawSamples.Length];
                }

                for (int i = 0; i < _rawSamples.Length; i++)
                {
                    double percent = (double)i / (_rawSamples.Length - 1);
                    if (IsDirtySample(percent))
                    {
                        _spline.Evaluate(percent, ref _rawSamples[i]);
                        _sampleCollection.samples[i].FastCopy(ref _rawSamples[i]);
                        if (transformSamples && _space == Space.Local)
                        {
                            TransformSample(ref _sampleCollection.samples[i]);
                        }
                    }
                }

                if (_sampleMode == SampleMode.Optimized && _rawSamples.Length > 2)
                {
                    OptimizeSamples(space == Space.Local);
                }
                else
                {
                    if (_sampleCollection.optimizedIndices.Length > 0)
                    {
                        _sampleCollection.optimizedIndices = new int[0];
                    }
                }
            }

            _sampleCollection.sampleMode = _sampleMode;
            hasSamples = _sampleCollection.length > 0;

            for (int i = 0; i < _spline.points.Length; i++)
            {
                _spline.points[i].isDirty = false;
            }
        }

        private void OptimizeSamples(bool transformSamples)
        {
            if (_sampleCollection.optimizedIndices.Length != _rawSamples.Length)
            {
                _sampleCollection.optimizedIndices = new int[_rawSamples.Length];
            }

            Vector3 lastDirection = _rawSamples[0].forward;
            List<SplineSample> optimized = new List<SplineSample>();
            for (int i = 0; i < _rawSamples.Length; i++)
            {
                SplineSample sample = _rawSamples[i];
                if (transformSamples)
                {
                    TransformSample(ref sample);
                }
                Vector3 direction = sample.forward;
                if (i < _rawSamples.Length - 1)
                {
                    Vector3 pos = _rawSamples[i + 1].position;
                    if (transformSamples)
                    {
                        pos = _localToWorldMatrix.MultiplyPoint3x4(pos);
                    }
                    direction = pos - sample.position;
                }
                float angle = Vector3.Angle(lastDirection, direction);
                bool includeSample = angle >= _optimizeAngleThreshold || i == 0 || i == _rawSamples.Length - 1;

                if (includeSample)
                {


                    optimized.Add(sample);
                    lastDirection = direction;
                }

                _sampleCollection.optimizedIndices[i] = optimized.Count - 1;
            }

            _sampleCollection.samples = optimized.ToArray();
        }

        private void TransformSamples()
        {
            if (_sampleCollection.samples.Length != _rawSamples.Length)
            {
                _sampleCollection.samples = new SplineSample[_rawSamples.Length];
            }

            if (_sampleMode == SampleMode.Optimized && _rawSamples.Length > 2)
            {
                OptimizeSamples(_space == Space.Local);
            } else
            {
                for (int i = 0; i < _rawSamples.Length; i++)
                {
                    _sampleCollection.samples[i].FastCopy(ref _rawSamples[i]);
                    if (_space == Space.Local)
                    {
                        TransformSample(ref _sampleCollection.samples[i]);
                    }
                }
            }
        }

        bool IsDirtySample(double percent)
        {
            if (_sampleMode == SampleMode.Uniform) return true;

            int currentPoint = PercentToPointIndex(percent);

            int from = currentPoint - 1;
            int to = currentPoint + 2;

            if(_spline.type == Spline.Type.Bezier || _spline.type == Spline.Type.Linear)
            {
                from = currentPoint;
                to = currentPoint + 1;
            }

            int fromClamped = Mathf.Clamp(from, 0, _spline.points.Length - 1);
            int toClamped = Mathf.Clamp(to, 0, _spline.points.Length - 1);

            for (int i = fromClamped; i <= toClamped; i++)
            {
                if (_spline.points[i].isDirty)
                {
                    return true;
                }
            }

            if (_spline.isClosed)
            {
                if(from < 0)
                {
                    for (int i = from + _spline.points.Length; i < _spline.points.Length; i++)
                    {
                        if (_spline.points[i].isDirty)
                        {
                            return true;
                        }
                    }
                }

                if(to >= _spline.points.Length)
                {
                    for (int i = 0; i <= to - _spline.points.Length; i++)
                    {
                        if (_spline.points[i].isDirty)
                        {
                            return true;
                        }
                    }
                }
            }

            if (currentPoint > 0 && !_spline.points[currentPoint].isDirty)
            {
                int count = _spline.points.Length - 1;
                if (_spline.isClosed)
                {
                    count = _spline.points.Length;
                }
                double currentPointPercent = (double)currentPoint / count;

                if(Mathf.Abs((float)(currentPointPercent - percent)) <= 0.00001f)
                {
                    return _spline.points[currentPoint - 1].isDirty;
                }
            }

            return false;
        }

        /// <summary>
        /// Same as Spline.Break() but it will update all subscribed users
        /// </summary>
        public void Break()
        {
            Break(0);
        }

        /// <summary>
        /// Same as Spline.Break(at) but it will update all subscribed users
        /// </summary>
        /// <param name="at"></param>
        public void Break(int at)
        {
            if (_spline.isClosed)
            {
                _spline.Break(at);
                SetAllDirty();
                Rebuild();
            }
        }

        /// <summary>
        /// Same as Spline.Close() but it will update all subscribed users
        /// </summary>
        public void Close()
        {
            if (!_spline.isClosed)
            {
                if(_spline.points.Length >= 3)
                {
                    _spline.Close();
                    SetAllDirty();
                    Rebuild();
                } else
                {
                    Debug.LogError("Spline " + name + " needs at least 3 points before it can be closed. Current points: " + _spline.points.Length);
                }

            }
        }

        /// <summary>
        /// Same as Spline.HermiteToBezierTangents() but it will update all subscribed users
        /// </summary>
        public void CatToBezierTangents()
        {
            _spline.CatToBezierTangents();
            SetPoints(_spline.points, Space.Local);
        }

        /// <summary>
        /// Casts a ray along the transformed spline against all scene colliders.
        /// </summary>
        /// <param name="hit">Hit information</param>
        /// <param name="hitPercent">The percent of evaluation where the hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public bool Raycast(out RaycastHit hit, out double hitPercent, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0 , QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal)
        {
            resolution = DMath.Clamp01(resolution);
            Spline.FormatFromTo(ref from, ref to, false);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent);
            hitPercent = 0f;
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
        /// Casts a ray along the transformed spline against all scene colliders and returns all hits. Order is not guaranteed.
        /// </summary>
        /// <param name="hit">Hit information</param>
        /// <param name="hitPercent">The percents of evaluation where each hit occured</param>
        /// <param name="layerMask">Layer mask for the raycast</param>
        /// <param name="resolution">Resolution multiplier for precision [0-1] default: 1f</param>
        /// <param name="from">Raycast from [0-1] default: 0f</param>
        /// <param name="to">Raycast to [0-1] default: 1f</param>
        /// <param name="hitTriggers">Should hit triggers? (not supported in 5.1)</param>
        /// <param name="address">Node address of junctions</param>
        /// <returns></returns>
        public bool RaycastAll(out RaycastHit[] hits, out double[] hitPercents, LayerMask layerMask, double resolution = 1.0, double from = 0.0, double to = 1.0, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal)
        {
            resolution = DMath.Clamp01(resolution);
            Spline.FormatFromTo(ref from, ref to, false);
            double percent = from;
            Vector3 fromPos = EvaluatePosition(percent);
            List<RaycastHit> hitList = new List<RaycastHit>();
            List<double> percentList = new List<double>();
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

        public TriggerGroup AddTriggerGroup()
        {
            TriggerGroup newGroup = new TriggerGroup();
            ArrayUtility.Add(ref triggerGroups, newGroup);
            return newGroup;
        }

        public SplineTrigger AddTrigger(int triggerGroup, double position, SplineTrigger.Type type)
        {
            return AddTrigger(triggerGroup, position, type, "API Trigger", Color.white);
        }

        public SplineTrigger AddTrigger(int triggerGroup, double position, SplineTrigger.Type type, string name, Color color)
        {
            while (triggerGroups.Length <= triggerGroup)
            {
                AddTriggerGroup();
            }
            return triggerGroups[triggerGroup].AddTrigger(position, type, name, color);
        }

        public void RemoveTrigger(int triggerGroup, int triggerIndex)
        {
            if(triggerGroups.Length <= triggerGroup || triggerGroup < 0)
            {
                Debug.LogError("Cannot delete trigger - trigger group " + triggerIndex + " does not exist");
                return;
            }
            triggerGroups[triggerGroup].RemoveTrigger(triggerIndex);
        }

        public void CheckTriggers(double start, double end, SplineUser user = null)
        {
            for (int i = 0; i < triggerGroups.Length; i++)
            {
                triggerGroups[i].Check(start, end);
            }
        }

        public void CheckTriggers(int group, double start, double end)
        {
            if (group < 0 || group >= triggerGroups.Length)
            {
                Debug.LogError("Trigger group " + group + " does not exist");
                return;
            }
            triggerGroups[group].Check(start, end);
        }

        public void ResetTriggers()
        {
            for (int i = 0; i < triggerGroups.Length; i++) triggerGroups[i].Reset();
        }

        public void ResetTriggers(int group)
        {
            if (group < 0 || group >= triggerGroups.Length)
            {
                Debug.LogError("Trigger group " + group + " does not exist");
                return;
            }
            for (int i = 0; i < triggerGroups[group].triggers.Length; i++)
            {
                triggerGroups[group].triggers[i].Reset();
            }
        }

        /// <summary>
        /// Get the available junctions for the given point
        /// </summary>
        /// <param name="pointIndex"></param>
        /// <returns></returns>
        public List<Node.Connection> GetJunctions(int pointIndex)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if(_nodes[i].pointIndex == pointIndex) return _nodes[i].GetConnections(this);
            }
            return new List<Node.Connection>();
        }

        /// <summary>
        /// Get all junctions for all points in the given interval
        /// </summary>
        /// <param name="start"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Dictionary<int, List<Node.Connection>> GetJunctions(double start = 0.0, double end = 1.0)
        {
            int index;
            double lerp;
            UpdateSampleCollection();
            _sampleCollection.GetSamplingValues(start, out index, out lerp);
            Dictionary<int, List<Node.Connection>> junctions = new Dictionary<int, List<Node.Connection>>();
            float startValue = (_spline.points.Length - 1) * (float)start;
            float endValue = (_spline.points.Length - 1) * (float)end;
            for (int i = 0; i < _nodes.Length; i++)
            {
                bool add = false;
                if (end > start && _nodes[i].pointIndex > startValue && _nodes[i].pointIndex < endValue) add = true;
                else if (_nodes[i].pointIndex < startValue && _nodes[i].pointIndex > endValue) add = true;
                if (!add && Mathf.Abs(startValue - _nodes[i].pointIndex) <= 0.0001f) add = true;
                if (!add && Mathf.Abs(endValue - _nodes[i].pointIndex) <= 0.0001f) add = true;
                if (add) junctions.Add(_nodes[i].pointIndex, _nodes[i].GetConnections(this));
            }
            return junctions;
        }

        /// <summary>
        /// Call this to connect a node to a spline's point
        /// </summary>
        /// <param name="node"></param>
        /// <param name="pointIndex"></param>
        public void ConnectNode(Node node, int pointIndex)
        {
            if (node == null)
            {
                Debug.LogError("Missing Node");
                return;
            }

            if (pointIndex < 0 || pointIndex >= _spline.points.Length)
            {
                Debug.Log("Invalid point index " + pointIndex);
                return;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].node == null) continue;
                if (_nodes[i].pointIndex == pointIndex || _nodes[i].node == node)
                {
                    Node.Connection[] connections = _nodes[i].node.GetConnections();
                    for (int j = 0; j < connections.Length; j++)
                    {
                        if (connections[j].spline == this)
                        {
                            Debug.LogError("Node " + node.name + " is already connected to spline " + name + " at point " + _nodes[i].pointIndex);
                            return;
                        }
                    }
                    AddNodeLink(node, pointIndex);
                    Debug.Log("Node link already exists");
                    return;
                }
            }
            node.AddConnection(this, pointIndex);
            AddNodeLink(node, pointIndex);
        }

        public void DisconnectNode(int pointIndex)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].pointIndex == pointIndex)
                {
                    _nodes[i].node.RemoveConnection(this, pointIndex);
                    ArrayUtility.RemoveAt(ref _nodes, i);
                    return;
                }
            }
        }

        private void AddNodeLink(Node node, int pointIndex)
        {
            NodeLink newLink = new NodeLink();
            newLink.node = node;
            newLink.pointIndex = pointIndex;
            ArrayUtility.Add(ref _nodes, newLink);
            UpdateConnectedNodes();
        }

        public Dictionary<int, Node> GetNodes(double start = 0.0, double end = 1.0)
        {
            int index;
            double lerp;
            UpdateSampleCollection();
            _sampleCollection.GetSamplingValues(start, out index, out lerp);
            Dictionary<int, Node> nodeList = new Dictionary<int, Node>();
            float startValue = (_spline.points.Length - 1) * (float)start;
            float endValue = (_spline.points.Length - 1) * (float)end;
            for (int i = 0; i < _nodes.Length; i++)
            {
                bool add = false;
                if (end > start && _nodes[i].pointIndex > startValue && _nodes[i].pointIndex < endValue) add = true;
                else if (_nodes[i].pointIndex < startValue && _nodes[i].pointIndex > endValue) add = true;
                if (!add && Mathf.Abs(startValue - _nodes[i].pointIndex) <= 0.0001f) add = true;
                if (!add && Mathf.Abs(endValue - _nodes[i].pointIndex) <= 0.0001f) add = true;
                if (add) nodeList.Add(_nodes[i].pointIndex, _nodes[i].node);
            }
            return nodeList;
        }

        public Node GetNode(int pointIndex)
        {
            if (pointIndex < 0 || pointIndex >= _spline.points.Length) return null;
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].pointIndex == pointIndex) return _nodes[i].node;
            }
            return null;
        }

        public void TransferNode(int pointIndex, int newPointIndex)
        {
            if(newPointIndex < 0 || newPointIndex >= _spline.points.Length)
            {
                Debug.LogError("Invalid new point index " + newPointIndex);
                return;
            }
            if (GetNode(newPointIndex) != null)
            {
                Debug.LogError("Cannot move node to point " + newPointIndex + ". Point already connected to a node");
                return;
            }
            Node node = GetNode(pointIndex);
            if(node == null)
            {
                Debug.LogError("No node connected to point " + pointIndex);
                return;
            }
            DisconnectNode(pointIndex);
            SplineSample sample = Evaluate(newPointIndex);
            node.transform.position = sample.position;
            node.transform.rotation = sample.rotation;
            ConnectNode(node, newPointIndex);
        }

        public void ShiftNodes(int startIndex, int endIndex, int shift)
        {
            int from = endIndex;
            int to = startIndex;
            if(startIndex > endIndex)
            {
                from = startIndex;
                to = endIndex;
            }

            for (int i = from; i >= to; i--)
            {
                Node node = GetNode(i);
                if (node != null)
                {
                    TransferNode(i, i + shift);
                }
            }
        }

        /// <summary>
        /// Gets all connected computers along with the connected indices and connection indices
        /// </summary>
        /// <param name="computers">A list of the connected computers</param>
        /// <param name="connectionIndices">The point indices of this computer where the other computers are connected</param>
        /// <param name="connectedIndices">The point indices of the other computers where they are connected</param>
        /// <param name="percent"></param>
        /// <param name="direction"></param>
        /// <param name="includeEqual">Should point indices that are placed exactly at the percent be included?</param>
        public void GetConnectedComputers(List<SplineComputer> computers, List<int> connectionIndices, List<int> connectedIndices, double percent, Spline.Direction direction, bool includeEqual)
        {
            if (computers == null) computers = new List<SplineComputer>();
            if (connectionIndices == null) connectionIndices = new List<int>();
            if (connectedIndices == null) connectionIndices = new List<int>();
            computers.Clear();
            connectionIndices.Clear();
            connectedIndices.Clear();
            int pointValue = Mathf.FloorToInt((_spline.points.Length - 1) * (float)percent);
            for (int i = 0; i < _nodes.Length; i++)
            {
                bool condition = false;
                if (includeEqual)
                {
                    if (direction == Spline.Direction.Forward) condition = _nodes[i].pointIndex >= pointValue;
                    else condition = _nodes[i].pointIndex <= pointValue;
                } else
                {

                }
                if (condition)
                {
                    Node.Connection[] connections = _nodes[i].node.GetConnections();
                    for (int j = 0; j < connections.Length; j++)
                    {
                        if (connections[j].spline != this) {
                            computers.Add(connections[j].spline);
                            connectionIndices.Add(_nodes[i].pointIndex);
                            connectedIndices.Add(connections[j].pointIndex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of all connected computers. This includes the base computer too.
        /// </summary>
        /// <returns></returns>
        public List<SplineComputer> GetConnectedComputers()
        {
            List<SplineComputer> computers = new List<SplineComputer>();
            computers.Add(this);
            if (_nodes.Length == 0) return computers;
            GetConnectedComputers(ref computers);
            return computers;
        }

        public void GetSamplingValues(double percent, out int index, out double lerp)
        {
            UpdateSampleCollection();
            _sampleCollection.GetSamplingValues(percent, out index, out lerp);
        }

        private void GetConnectedComputers(ref List<SplineComputer> computers)
        {
            SplineComputer comp = computers[computers.Count - 1];
            if (comp == null) return;
            for (int i = 0; i < comp._nodes.Length; i++)
            {
                if (comp._nodes[i].node == null) continue;
                Node.Connection[] connections = comp._nodes[i].node.GetConnections();
                for (int n = 0; n < connections.Length; n++)
                {
                    bool found = false;
                    if (connections[n].spline == this) continue;
                    for (int x = 0; x < computers.Count; x++)
                    {
                        if (computers[x] == connections[n].spline)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        computers.Add(connections[n].spline);
                        GetConnectedComputers(ref computers);
                    }
                }
            }
        }

        private void RemoveNodeLinkAt(int index)
        {
            //Then remove the node link
            NodeLink[] newLinks = new NodeLink[_nodes.Length - 1];
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (i == index) continue;
                else if (i < index) newLinks[i] = _nodes[i];
                else newLinks[i - 1] = _nodes[i];
            }
            _nodes = newLinks;
        }

        //This "magically" updates the Node's position and all other points, connected to it when a point, linked to a Node is changed.
        private void SetNodeForPoint(int index, SplinePoint worldPoint)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].pointIndex == index)
                {
                    _nodes[i].node.UpdatePoint(this, _nodes[i].pointIndex, worldPoint);
                    break;
                }
            }
        }

        private void UpdateConnectedNodes(SplinePoint[] worldPoints)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].node == null)
                {
                    RemoveNodeLinkAt(i);
                    i--;
                    Rebuild();
                    continue;
                }
                bool found = false;
                foreach(Node.Connection connection in _nodes[i].node.GetConnections())
                {
                    if(connection.spline == this)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    RemoveNodeLinkAt(i);
                    i--;
                    Rebuild();
                    continue;
                }
                _nodes[i].node.UpdatePoint(this, _nodes[i].pointIndex, worldPoints[_nodes[i].pointIndex]);
                _nodes[i].node.UpdateConnectedComputers(this);
            }
        }

        private void UpdateConnectedNodes()
        {
            for (int i = 0; i < _nodes.Length; i++)
            {

                if (_nodes[i] == null || _nodes[i].node == null)
                {
                    RemoveNodeLinkAt(i);
                    Rebuild();
                    i--;
                    continue;
                }
                bool found = false;
                Node.Connection[] connections = _nodes[i].node.GetConnections();
                for (int j = 0; j < connections.Length; j++)
                {
                    if(connections[j].spline == this && connections[j].pointIndex == _nodes[i].pointIndex)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    _nodes[i].node.UpdatePoint(this, _nodes[i].pointIndex, GetPoint(_nodes[i].pointIndex));
                } else
                {
                    RemoveNodeLinkAt(i);
                    Rebuild();
                    i--;
                    continue;
                }
            }
        }

        public Vector3 TransformPoint(Vector3 point)
        {
#if UNITY_EDITOR
            if (!_editorIsPlaying) return transform.TransformPoint(point);
#endif
            return _localToWorldMatrix.MultiplyPoint3x4(point);
        }

        public Vector3 InverseTransformPoint(Vector3 point)
        {
#if UNITY_EDITOR
            if (!_editorIsPlaying) return transform.InverseTransformPoint(point);
#endif
            return _worldToLocalMatrix.MultiplyPoint3x4(point);
        }

        public Vector3 TransformDirection(Vector3 direction)
        {
#if UNITY_EDITOR
            if (!_editorIsPlaying) return transform.TransformDirection(direction);
#endif
            return _localToWorldMatrix.MultiplyVector(direction);
        }

        public Vector3 InverseTransformDirection(Vector3 direction)
        {
#if UNITY_EDITOR
            if (!_editorIsPlaying) return transform.InverseTransformDirection(direction);
#endif
            return _worldToLocalMatrix.MultiplyVector(direction);
        }

#if UNITY_EDITOR
        public void EditorSetPointDirty(int index)
        {
            SetDirty(index);
        }

        public void EditorSetAllPointsDirty()
        {
            SetAllDirty();
        }

#endif

        [System.Serializable]
        internal class NodeLink
        {
            [SerializeField]
            internal Node node = null;
            [SerializeField]
            internal int pointIndex = 0;

            internal List<Node.Connection> GetConnections(SplineComputer exclude)
            {
                Node.Connection[] connections = node.GetConnections();
                List<Node.Connection> connectionList = new List<Node.Connection>();
                for (int i = 0; i < connections.Length; i++)
                {
                    if (connections[i].spline == exclude) continue;
                    connectionList.Add(connections[i]);
                }
                return connectionList;
            }
        }
    }
}
