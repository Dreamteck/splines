using UnityEngine;

namespace Dreamteck.Splines
{
    [AddComponentMenu("Dreamteck/Splines/Users/Spline Positioner")]
    [ExecuteInEditMode]
    public class SplinePositioner : SplineTracer
    {
        public enum Mode { Percent, Distance }

        public GameObject targetObject
        {
            get
            {
                if (_targetObject == null) return gameObject;
                return _targetObject;
            }

            set
            {
                if (value != _targetObject)
                {
                    _targetObject = value;
                    RefreshTargets();
                    Rebuild();
                }
            }
        }

        public SplineTracer followTarget
        {
            get { return _followTarget; }
            set
            {
                if(value != _followTarget)
                {
                    if(_followTarget != null)
                    {
                        _followTarget.onMotionApplied -= OnFollowTargetMotionApplied;
                    }
                    if(value == this)
                    {
                        Debug.Log("You should not be assigning a self-reference to the followTarget field.");
                        return;
                    }
                    _followTarget = value;
                    if(_followTarget != null)
                    {
                        _followTarget.onMotionApplied += OnFollowTargetMotionApplied;
                        OnFollowTargetMotionApplied();
                    }
                }
            }
        }

        public float followTargetDistance
        {
            get { return _followTargetDistance;  }
            set
            {
                if(value != _followTargetDistance)
                {
                    _followTargetDistance = value;
                    if(followTarget != null)
                    {
                        OnFollowTargetMotionApplied();
                    }
                }
            }
        }

        public bool followLoop
        {
            get { return _followLoop; }
            set
            {
                if (value != _followLoop)
                {
                    _followLoop = value;
                    if (followTarget != null)
                    {
                        OnFollowTargetMotionApplied();
                    }
                }
            }
        }

        public Spline.Direction followTargetDirection
        {
            get { return _followTargetDirection; }
            set
            {
                if (value != _followTargetDirection)
                {
                    _followTargetDirection = value;
                    if (followTarget != null)
                    {
                        OnFollowTargetMotionApplied();
                    }
                }
            }
        }

        public double position
        {
            get
            {
                return _result.percent;
            }
            set
            {
                if (value != _position)
                {
                    _position = (float)value;
                    if (mode == Mode.Distance)
                    {
                        SetDistance(_position, true, true);
                    }
                    else
                    {
                        SetPercent(value, true, true);
                    }
                }
            }
        }

        public Mode mode
        {
            get { return _mode;  }
            set
            {
                if (value != _mode)
                {
                    _mode = value;
                    Rebuild();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private GameObject _targetObject;
        [SerializeField]
        [HideInInspector]
        private SplineTracer _followTarget;
        [SerializeField]
        [HideInInspector]
        private float _followTargetDistance;
        [SerializeField]
        [HideInInspector]
        private bool _followLoop;
        [SerializeField]
        [HideInInspector]
        private Spline.Direction _followTargetDirection = Spline.Direction.Backward;
        [SerializeField]
        [HideInInspector]
        private float _position = 0f;
        [SerializeField]
        [HideInInspector]
        private Mode _mode = Mode.Percent;
        private float _lastPosition = 0f;

        private void OnFollowTargetMotionApplied()
        {
            float moved;
            double percent = Travel(followTarget.result.percent, _followTargetDistance, _followTargetDirection, out moved);
            if (_followLoop)
            {
                if (_followTargetDistance - moved > 0.000001f)
                {
                    if (percent <= 0.000001)
                    {
                        percent = Travel(1.0, _followTargetDistance - moved, _followTargetDirection, out moved);
                    }
                    else if (percent >= 0.999999)
                    {
                        percent = Travel(0.0, _followTargetDistance - moved, _followTargetDirection, out moved);
                    }
                }
            }
            SetPercent(percent, true);
        }

        protected override void Awake()
        {
            base.Awake();
            if(_followTarget != null)
            {
                _followTarget.onMotionApplied += OnFollowTargetMotionApplied;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_followTarget != null)
            {
                _followTarget.onMotionApplied -= OnFollowTargetMotionApplied;
            }
        }


        protected override void OnDidApplyAnimationProperties()
        {
            if (_lastPosition != _position)
            {
                _lastPosition = _position;
                if (mode == Mode.Distance)
                {
                    SetDistance(_position, true);
                }
                else
                {
                    SetPercent(_position, true);
                }
            }
            base.OnDidApplyAnimationProperties();
        }

        protected override Transform GetTransform()
        {
            return targetObject.transform;
        }

        protected override Rigidbody GetRigidbody()
        {
            return targetObject.GetComponent<Rigidbody>();
        }

        protected override Rigidbody2D GetRigidbody2D()
        {
            return targetObject.GetComponent<Rigidbody2D>();
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            if (mode == Mode.Distance) SetDistance((float)_position, true);
            else SetPercent(_position, true);
        }

        public override void SetPercent(double percent, bool checkTriggers = false, bool handleJunctions = false)
        {
            base.SetPercent(percent, checkTriggers, handleJunctions);
            _position = (float)percent;

            if (!handleJunctions) return;

            InvokeNodes();
        }

        public override void SetDistance(float distance, bool checkTriggers = false, bool handleJunctions = false)
        {
            double lastPercent = _result.percent;
            double travel = Travel(0.0, distance, Spline.Direction.Forward);
            Evaluate(travel, ref _result);
            ApplyMotion();

            if (checkTriggers)
            {
                CheckTriggers(lastPercent, _result.percent);
                InvokeTriggers();
            }
            if (handleJunctions)
            {
                CheckNodes(lastPercent, _result.percent);
            }

            _position = mode == Mode.Distance ? distance : (float)travel;

            if (!handleJunctions) return;

            InvokeNodes();
        }
    }
}
