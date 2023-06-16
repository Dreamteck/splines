using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.Serialization;

namespace Dreamteck.Splines
{
    [ExecuteInEditMode]
    [AddComponentMenu("Dreamteck/Splines/Users/Spline Projector")]
    public class SplineProjector : SplineTracer
    {
        public enum Mode {Accurate, Cached}
        public Mode mode
        {
            get { return _mode; }
            set
            {
                if(value != _mode)
                {
                    _mode = value;
                    Rebuild();
                }
            }
        }

        public bool autoProject
        {
            get { return _autoProject; }
            set
            {
                if(value != _autoProject)
                {
                    _autoProject = value;
                    if (_autoProject) Rebuild();
                }
            }
        }

        public int subdivide
        {
            get { return _subdivide; }
            set
            {
                if (value != _subdivide)
                {
                    _subdivide = value;
                    if (_mode == Mode.Accurate) Rebuild();
                }
            }
        }

        public Transform projectTarget
        {
            get {
                if (_projectTarget == null) return transform;
                return _projectTarget; 
            }
            set
            {
                if (value != _projectTarget)
                {
                    _projectTarget = value;
                    Rebuild();
                }
            }
        }

        public GameObject targetObject
        {
            get
            {
                if (_targetObject == null)
                {
                    if (applyTarget != null) //Temporary check to migrate SplineProjectors that use target
                    {
                        _targetObject = applyTarget.gameObject;
                        applyTarget = null;
                        return _targetObject;
                    }
                }
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

        [SerializeField]
        [HideInInspector]
        private Mode _mode = Mode.Cached;
        [SerializeField]
        [HideInInspector]
        private bool _autoProject = true;
        [SerializeField]
        [HideInInspector]
        [Range(3, 8)]
        private int _subdivide = 4;
        [SerializeField]
        [HideInInspector]
        private Transform _projectTarget;


        [SerializeField]
        [HideInInspector]
        private Transform applyTarget = null;
        [SerializeField]
        [HideInInspector]
        private GameObject _targetObject;

        [SerializeField]
        [HideInInspector]
        public Vector2 _offset;
        [SerializeField]
        [HideInInspector]
        public Vector3 _rotationOffset = Vector3.zero;

        public event SplineReachHandler onEndReached;
        public event SplineReachHandler onBeginningReached;

        [SerializeField]
        [HideInInspector]
        Vector3 lastPosition = Vector3.zero;

        protected override void Reset()
        {
            base.Reset();
            _projectTarget = transform;
        }

        protected override Transform GetTransform()
        {
            if (targetObject == null) return null;
            return targetObject.transform;
        }

        protected override Rigidbody GetRigidbody()
        {
            if (targetObject == null) return null;
            return targetObject.GetComponent<Rigidbody>();
        }

        protected override Rigidbody2D GetRigidbody2D()
        {
            if (targetObject == null) return null;
            return targetObject.GetComponent<Rigidbody2D>();
        }


        protected override void LateRun()
        {
            base.LateRun();
            if (autoProject)
            {
                if (projectTarget && lastPosition != projectTarget.position)
                {
                    lastPosition = projectTarget.position;
                    CalculateProjection();
                }
            }
         }

        protected override void PostBuild()
        {
            base.PostBuild();
            CalculateProjection();
        }

        protected override void OnSplineChanged()
        {
            if (spline != null)
            {
                if (_mode == Mode.Accurate)
                {
                    spline.Project(_projectTarget.position, ref _result, clipFrom, clipTo, SplineComputer.EvaluateMode.Calculate, subdivide);
                } 
                else
                {
                    spline.Project(_projectTarget.position, ref _result, clipFrom, clipTo);
                }
                _result.percent = ClipPercent(_result.percent);
            }
        }


        private void Project()
        {
            if (_mode == Mode.Accurate && spline != null)
            {
                spline.Project(_projectTarget.position, ref _result, clipFrom, clipTo, SplineComputer.EvaluateMode.Calculate, subdivide);
                _result.percent = ClipPercent(_result.percent);
            }
            else
            {
                Project(_projectTarget.position, ref _result);
            }
        }

        public void CalculateProjection()
        {
            if (_projectTarget == null) return;
            double lastPercent = _result.percent;
            Project();

            if (onBeginningReached != null && _result.percent <= clipFrom)
            {
                if (!Mathf.Approximately((float)lastPercent, (float)_result.percent))
                {
                    onBeginningReached();
                    if (samplesAreLooped)
                    {
                        CheckTriggers(lastPercent, 0.0);
                        CheckNodes(lastPercent, 0.0);
                        lastPercent = 1.0;
                    }
                }
            }
            else if (onEndReached != null && _result.percent >= clipTo)
            {
                if (!Mathf.Approximately((float)lastPercent, (float)_result.percent))
                {
                    onEndReached();
                    if (samplesAreLooped)
                    {
                        CheckTriggers(lastPercent, 1.0);
                        CheckNodes(lastPercent, 1.0);
                        lastPercent = 0.0;
                    }
                }
            }

            CheckTriggers(lastPercent, _result.percent);
            CheckNodes(lastPercent, _result.percent);
            

            if (targetObject != null)
            {
                ApplyMotion();
            }

            InvokeTriggers();
            InvokeNodes();
            lastPosition = projectTarget.position;
        }
    }
}
