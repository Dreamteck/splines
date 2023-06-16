using UnityEngine;
using UnityEngine.Events;

namespace Dreamteck.Splines
{
    public delegate void SplineReachHandler();
    [AddComponentMenu("Dreamteck/Splines/Users/Spline Follower")]
    public class SplineFollower : SplineTracer
    {
        public enum FollowMode { Uniform, Time }
        public enum Wrap { Default, Loop, PingPong }
        [HideInInspector]
        public Wrap wrapMode = Wrap.Default;
        [HideInInspector]
        public FollowMode followMode = FollowMode.Uniform;

        [HideInInspector]
        public bool autoStartPosition = false;

        [SerializeField]
        [HideInInspector]
        [UnityEngine.Serialization.FormerlySerializedAs("follow")]
        private bool _follow = true;

        [SerializeField]
        [HideInInspector]
        [Range(0f, 1f)]
        private double _startPosition;

        /// <summary>
        /// If the follow mode is set to Uniform and there is an added offset in the motion panel, this will presserve the uniformity of the follow speed
        /// </summary>
        [HideInInspector]
        public bool preserveUniformSpeedWithOffset = false;

        /// <summary>
        /// Used when follow mode is set to Uniform. Defines the speed of the follower
        /// </summary>
        public float followSpeed
        {
            get { return _followSpeed; }
            set
            {
                if (_followSpeed != value)
                {
                    _followSpeed = value;
                    Spline.Direction lastDirection = _direction;
                    if (Mathf.Approximately(_followSpeed, 0f)) return;
                    if (_followSpeed < 0f)
                    {
                        direction = Spline.Direction.Backward;
                    }
                    if(_followSpeed > 0f)
                    {
                        direction = Spline.Direction.Forward;
                    }
                }
            }
        }

        public override Spline.Direction direction {
            get {
                return base.direction;
            }
            set {
                base.direction = value;
                if(_direction == Spline.Direction.Forward)
                {
                    if(_followSpeed < 0f)
                    {
                        _followSpeed = -_followSpeed;
                    }
                } else
                {
                    if (_followSpeed > 0f)
                    {
                        _followSpeed = -_followSpeed;
                    }
                }
            }
        }

        /// <summary>
        /// Used when follow mode is set to Time. Defines how much time it takes for the follower to travel through the path
        /// </summary>
        public float followDuration
        {
            get { return _followDuration; }
            set
            {
                if (_followDuration != value)
                {
                    if (value < 0f) value = 0f;
                    _followDuration = value;
                }
            }
        }

        public bool follow
        {
            get { return _follow; }
            set
            {
                if(_follow != value)
                {
                    if (autoStartPosition)
                    {
                        Project(GetTransform().position, ref evalResult);
                        SetPercent(evalResult.percent);
                    }
                    _follow = value;
                }
            }
        }

        public event System.Action<double> onEndReached;
        public event System.Action<double> onBeginningReached;

        public FollowerSpeedModifier speedModifier
        {
            get
            {
                return _speedModifier;
            }
        }

        [SerializeField]
        [HideInInspector]
        private float _followSpeed = 1f;
        [SerializeField]
        [HideInInspector]
        private float _followDuration = 1f;

        [SerializeField]
        [HideInInspector]
        private FollowerSpeedModifier _speedModifier = new FollowerSpeedModifier();

        [SerializeField]
        [HideInInspector]
        private FloatEvent _unityOnEndReached = null;
        [SerializeField]
        [HideInInspector]
        private FloatEvent _unityOnBeginningReached = null;

        private double lastClippedPercent = -1.0;

        protected override void Start()
        {
            base.Start();
            if (_follow && autoStartPosition)
            {
                SetPercent(spline.Project(GetTransform().position).percent);
            }
        }

        protected override void LateRun()
        {
            base.LateRun();
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (_follow)
            {
                Follow();
            }
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            Evaluate(_result.percent, ref _result);
            if (sampleCount > 0)
            {
                if (_follow && !autoStartPosition) ApplyMotion();
            }
        }

        private void Follow()
        {
            switch (followMode)
            {
                case FollowMode.Uniform:
                    double percent = result.percent;
                    if (!_speedModifier.useClippedPercent)
                    {
                        UnclipPercent(ref percent);
                    }
                    float speed = _speedModifier.GetSpeed(Mathf.Abs(_followSpeed), percent);
                    Move(Time.deltaTime * speed); break;
                case FollowMode.Time:
                    if (_followDuration == 0.0) Move(0.0);
                    else Move((double)Time.deltaTime / _followDuration);
                    break;
            }
        }

        public void Restart(double startPosition = 0.0)
        {
            SetPercent(startPosition);
        }

        public override void SetPercent(double percent, bool checkTriggers = false, bool handleJunctions = false)
        {
            base.SetPercent(percent, checkTriggers, handleJunctions);
            lastClippedPercent = percent;

            if (!handleJunctions) return;

            InvokeNodes();
        }

        public override void SetDistance(float distance, bool checkTriggers = false, bool handleJunctions = false)
        {
            base.SetDistance(distance, checkTriggers, handleJunctions);
            lastClippedPercent = ClipPercent(_result.percent);
            if (samplesAreLooped && clipFrom == clipTo && distance > 0f && lastClippedPercent == 0.0) lastClippedPercent = 1.0;

            if (!handleJunctions) return;

            InvokeNodes();
        }

        public void Move(double percent)
        {
            if (percent == 0.0) return;
            if (sampleCount <= 1)
            {
                if (sampleCount == 1)
                {
                    GetSampleRaw(0, ref _result);
                    ApplyMotion();
                }
                return;
            }
            Evaluate(_result.percent, ref _result);
            double startPercent = _result.percent;
            if (wrapMode == Wrap.Default && lastClippedPercent >= 1.0 && startPercent == 0.0) startPercent = 1.0;
            double p = startPercent + (_direction == Spline.Direction.Forward ? percent : -percent);
            bool callOnEndReached = false, callOnBeginningReached = false;
            lastClippedPercent = p;
            if (_direction == Spline.Direction.Forward && p >= 1.0)
            {
                if (startPercent < 1.0)
                {
                    callOnEndReached = true;
                }
                switch (wrapMode)
                {
                    case Wrap.Default:
                        p = 1.0;
                        break;
                    case Wrap.Loop:
                        CheckTriggers(startPercent, 1.0);
                        CheckNodes(startPercent, 1.0);
                        while (p > 1.0) p -= 1.0;
                        startPercent = 0.0;
                        break;
                    case Wrap.PingPong:
                        p = DMath.Clamp01(1.0 - (p - 1.0));
                        startPercent = 1.0;
                        direction = Spline.Direction.Backward;
                        break;
                }
            }
            else if (_direction == Spline.Direction.Backward && p <= 0.0)
            {
                if (startPercent > 0.0)
                {
                    callOnBeginningReached = true;
                }
                switch (wrapMode)
                {
                    case Wrap.Default:
                        p = 0.0;
                        break;
                    case Wrap.Loop:
                        CheckTriggers(startPercent, 0.0);
                        CheckNodes(startPercent, 0.0);
                        while (p < 0.0) p += 1.0;
                        startPercent = 1.0;
                        break;
                    case Wrap.PingPong:
                        p = DMath.Clamp01(-p);
                        startPercent = 0.0;
                        direction = Spline.Direction.Forward;
                        break;
                }
            }
            CheckTriggers(startPercent, p);
            CheckNodes(startPercent, p);
            Evaluate(p, ref _result);
            ApplyMotion();
            if (callOnEndReached)
            {
                if (onEndReached != null)
                {
                    onEndReached(startPercent);
                }
                if (_unityOnEndReached != null)
                {
                    _unityOnEndReached.Invoke((float)startPercent);
                }
            }
            else if (callOnBeginningReached)
            {
                if (onBeginningReached != null)
                {
                    onBeginningReached(startPercent);
                }
                if (_unityOnBeginningReached != null)
                {
                    _unityOnBeginningReached.Invoke((float)startPercent);
                }
            }
            InvokeTriggers();
            InvokeNodes();
        }

        public void Move(float distance)
        {
            bool endReached = false, beginningReached = false;
            float moved = 0f;
            double startPercent = _result.percent;

            double travelPercent = DoTravel(_result.percent, distance, out moved);
            if (startPercent != travelPercent)
            {
                CheckTriggers(startPercent, travelPercent);
                CheckNodes(startPercent, travelPercent);
            }

            if (direction == Spline.Direction.Forward)
            {
                if (travelPercent >= 1.0)
                {
                    if (startPercent < 1.0)
                    {
                        endReached = true;
                    }
                    switch (wrapMode)
                    {
                        case Wrap.Loop:
                            travelPercent = DoTravel(0.0, Mathf.Abs(distance - moved), out moved);
                            CheckTriggers(0.0, travelPercent);
                            CheckNodes(0.0, travelPercent);
                            break;
                        case Wrap.PingPong:
                            direction = Spline.Direction.Backward;
                            travelPercent = DoTravel(1.0, distance - moved, out moved);
                            CheckTriggers(1.0, travelPercent);
                            CheckNodes(1.0, travelPercent);
                            break;
                    }
                }
            } else
            {
                if (travelPercent <= 0.0)
                {
                    if (startPercent > 0.0)
                    {
                        beginningReached = true;
                    }
                    switch (wrapMode)
                    {
                        case Wrap.Loop:
                            travelPercent = DoTravel(1.0, distance - moved, out moved);
                            CheckTriggers(1.0, travelPercent);
                            CheckNodes(1.0, travelPercent);
                            break;
                        case Wrap.PingPong:
                            direction = Spline.Direction.Forward;
                            travelPercent = DoTravel(0.0, Mathf.Abs(distance - moved), out moved);
                            CheckTriggers(0.0, travelPercent);
                            CheckNodes(0.0, travelPercent);
                            break;
                    }
                }
            }

            Evaluate(travelPercent, ref _result);
            ApplyMotion();
            if (endReached)
            {
                if (onEndReached != null)
                {
                    onEndReached(startPercent);
                }
                if (_unityOnEndReached != null)
                {
                    _unityOnEndReached.Invoke((float)startPercent);
                }
            }
            else if (beginningReached)
            {
                if (onBeginningReached != null)
                {
                    onBeginningReached(startPercent);
                }
                if (_unityOnBeginningReached != null)
                {
                    _unityOnBeginningReached.Invoke((float)startPercent);
                }
            }
            InvokeTriggers();
            InvokeNodes();
        }

        protected virtual double DoTravel(double start, float distance, out float moved)
        {
            moved = 0f;
            double result = 0.0;
            if (preserveUniformSpeedWithOffset && _motion.hasOffset)
            {
                result = TravelWithOffset(start, distance, _direction, _motion.offset, out moved);
            } else
            {
                result = Travel(start, distance, _direction, out moved);
            }
            return result;
        }

        [System.Serializable]
        public class FloatEvent : UnityEvent<float> { }
    }
}
