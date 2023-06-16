using System.Collections;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Dreamteck.Splines
{
    public class SplineTracer : SplineUser
    {
        public class NodeConnection
        {
            public Node node;
            public int point = 0;

            public NodeConnection(Node node, int point)
            {
                this.node = node;
                this.point = point;
            }
        }

        public enum PhysicsMode { Transform, Rigidbody, Rigidbody2D }
        public PhysicsMode physicsMode
        {
            get { return _physicsMode; }
            set
            {
                _physicsMode = value;
                RefreshTargets();
            }
        }

        public TransformModule motion
        {
            get
            {
                if (_motion == null) _motion = new TransformModule();
                return _motion;
            }
        }

        /// <summary>
        /// Returns the unmodified result from the evaluation
        /// </summary>
        public SplineSample result
        {
            get { return _result; }
        }

        /// <summary>
        /// Returns the offsetted evaluation result from the current follow position
        /// </summary>
        public SplineSample modifiedResult
        {
            get
            {
                return _finalResult;
            }
        }

        public bool dontLerpDirection
        {
            get { return _dontLerpDirection; }
            set
            {
                if (value != _dontLerpDirection)
                {
                    _dontLerpDirection = value;
                    ApplyMotion();
                }
            }
        }

        public virtual Spline.Direction direction
        {
            get { return _direction; }
            set
            {
                if (value != _direction)
                {
                    _direction = value;
                    ApplyMotion();
                }
            }
        }

        [HideInInspector]
        public bool applyDirectionRotation = true;
        [HideInInspector]
        public bool useTriggers = false;
        [HideInInspector]
        public int triggerGroup = 0;
        [SerializeField]
        [HideInInspector]
        protected Spline.Direction _direction = Spline.Direction.Forward;

        [SerializeField]
        [HideInInspector]
        protected bool _dontLerpDirection = false;

        [SerializeField]
        [HideInInspector]
        protected PhysicsMode _physicsMode = PhysicsMode.Transform;
        [SerializeField]
        [HideInInspector]
        protected TransformModule _motion = null;


        [SerializeField]
        [HideInInspector]
        protected Rigidbody targetRigidbody = null;
        [SerializeField]
        [HideInInspector]
        protected Rigidbody2D targetRigidbody2D = null;
        [SerializeField]
        [HideInInspector]
        protected Transform targetTransform = null;
        [SerializeField]
        [HideInInspector]
        protected SplineSample _result = new SplineSample();
        [SerializeField]
        [HideInInspector]
        protected SplineSample _finalResult = new SplineSample();

        public delegate void JunctionHandler(List<NodeConnection> passed);

        public event JunctionHandler onNode;
        public event EmptySplineHandler onMotionApplied;

        private SplineTrigger[] triggerInvokeQueue = new SplineTrigger[0];
        private List<NodeConnection> nodeConnectionQueue = new List<NodeConnection>();
        private int addTriggerIndex = 0;

        private const double MIN_DELTA = 0.000001;

#if UNITY_EDITOR
        public override void EditorAwake()
        {
            base.EditorAwake();
            RefreshTargets();
            ApplyMotion();
        }
#endif 

        protected override void Awake()
        {
            base.Awake();
            RefreshTargets();
        }

        protected virtual void Start()
        {

        }

        public virtual void SetPercent(double percent, bool checkTriggers = false, bool handleJunctions = false)
        {
            if (sampleCount == 0) return;
            double lastPercent = _result.percent;
            Evaluate(percent, ref _result);
            ApplyMotion();
            if (checkTriggers)
            {
                CheckTriggers(lastPercent, percent);
                InvokeTriggers();
            }
            if (handleJunctions)
            {
                CheckNodes(lastPercent, percent);
            }
        }

        public double GetPercent()
        {
            return _result.percent;
        }

        public virtual void SetDistance(float distance, bool checkTriggers = false, bool handleJunctions = false)
        {
            double lastPercent = _result.percent;
            Evaluate(Travel(0.0, distance, Spline.Direction.Forward), ref _result);
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
        }

        protected virtual Rigidbody GetRigidbody()
        {
            return GetComponent<Rigidbody>();
        }

        protected virtual Rigidbody2D GetRigidbody2D()
        {
            return GetComponent<Rigidbody2D>();
        }

        protected virtual Transform GetTransform()
        {
            return transform;
        }

        protected void ApplyMotion()
        {
            if (sampleCount == 0) return;
            ModifySample(ref _result, ref _finalResult);
            if (_dontLerpDirection)
            {
                double unclippedPercent = UnclipPercent(_result.percent);
                int index;
                double lerp;
                spline.GetSamplingValues(unclippedPercent, out index, out lerp);
                _finalResult.forward = spline[index].forward;
                _finalResult.up = spline[index].up;
            }

            motion.targetUser = this;
            motion.splineResult = _finalResult;
            if (applyDirectionRotation) motion.direction = _direction;
            else motion.direction = Spline.Direction.Forward;

            switch (_physicsMode)
            {
                case PhysicsMode.Transform:
                    if (targetTransform == null) RefreshTargets();
                    if (targetTransform == null) return;
                    motion.ApplyTransform(targetTransform);
                    if (onMotionApplied != null) onMotionApplied();
                    break;
                case PhysicsMode.Rigidbody:
                    if (targetRigidbody == null)
                    {
                        RefreshTargets();
                        if (targetRigidbody == null)  throw new MissingComponentException("There is no Rigidbody attached to " + name + " but the Physics mode is set to use one.");
                    }
                    motion.ApplyRigidbody(targetRigidbody);
                    if (onMotionApplied != null) onMotionApplied();
                    break;
                case PhysicsMode.Rigidbody2D:
                    if (targetRigidbody2D == null)
                    {
                        RefreshTargets();
                        if (targetRigidbody2D == null) throw new MissingComponentException("There is no Rigidbody2D attached to " + name + " but the Physics mode is set to use one.");
                    }
                    motion.ApplyRigidbody2D(targetRigidbody2D);
                    if (onMotionApplied != null) onMotionApplied();
                    break;
            }
        }

        protected void CheckNodes(double from, double to)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (onNode == null) return;
            if (from == to) return;
            UnclipPercent(ref from);
            UnclipPercent(ref to);
            Spline.FormatFromTo(ref from, ref to, true);
            int fromPoint, toPoint;
            fromPoint = spline.PercentToPointIndex(from, _direction);
            toPoint = spline.PercentToPointIndex(to, _direction);

            if (fromPoint != toPoint)
            {
                if (_direction == Spline.Direction.Forward)
                {
                    for (int i = fromPoint + 1; i <= toPoint; i++)
                    {
                        NodeConnection junction = GetJunction(i);
                        if (junction != null) nodeConnectionQueue.Add(junction);
                    }
                }
                else
                {
                    for (int i = toPoint - 1; i >= fromPoint; i--)
                    {
                        NodeConnection junction = GetJunction(i);
                        if (junction != null) nodeConnectionQueue.Add(junction);
                    }
                }
            }
            else if (from < MIN_DELTA && to > from)
            {
                NodeConnection junction = GetJunction(0);
                if (junction != null) nodeConnectionQueue.Add(junction);
            }
            else if (to > 1.0 - MIN_DELTA && from < to)
            {
                int pointCount = spline.pointCount - 1;
                if (spline.isClosed)
                {
                    pointCount = spline.pointCount;
                }
                NodeConnection junction = GetJunction(pointCount);
                if (junction != null) nodeConnectionQueue.Add(junction);
            }
        }

        protected void InvokeNodes()
        {
            if(nodeConnectionQueue.Count > 0)
            {
                onNode(nodeConnectionQueue);
                nodeConnectionQueue.Clear();
            }
        }

        protected void CheckTriggers(double from, double to)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (!useTriggers) return;
            if (from == to) return;
            UnclipPercent(ref from);
            UnclipPercent(ref to);
            if (triggerGroup < 0 || triggerGroup >= spline.triggerGroups.Length) return;
            for (int i = 0; i < spline.triggerGroups[triggerGroup].triggers.Length; i++)
            {
                if (spline.triggerGroups[triggerGroup].triggers[i] == null) continue;
                if (spline.triggerGroups[triggerGroup].triggers[i].Check(from, to)) AddTriggerToQueue(spline.triggerGroups[triggerGroup].triggers[i]);
            }
        }

        NodeConnection GetJunction(int pointIndex)
        {
            Node node = spline.GetNode(pointIndex);
            if (node == null) return null;
            return new NodeConnection(node, pointIndex);
        }

        protected void InvokeTriggers()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            for (int i = 0; i < addTriggerIndex; i++)
            {
                if (triggerInvokeQueue[i] != null)
                {
                    triggerInvokeQueue[i].Invoke(this);
                }
            }
            addTriggerIndex = 0;
        }

        protected void RefreshTargets()
        {
            switch (_physicsMode)
            {
                case PhysicsMode.Transform:
                    targetTransform = GetTransform();
                    break;
                case PhysicsMode.Rigidbody:
                    targetRigidbody = GetRigidbody();
                    break;
                case PhysicsMode.Rigidbody2D:
                    targetRigidbody2D = GetRigidbody2D();
                    break;
            }
        }

        private void AddTriggerToQueue(SplineTrigger trigger)
        {
            if (addTriggerIndex >= triggerInvokeQueue.Length)
            {
                SplineTrigger[] newQueue = new SplineTrigger[triggerInvokeQueue.Length + spline.triggerGroups[triggerGroup].triggers.Length];
                triggerInvokeQueue.CopyTo(newQueue, 0);
                triggerInvokeQueue = newQueue;
            }
            triggerInvokeQueue[addTriggerIndex] = trigger;
            addTriggerIndex++;
        }
    }
}
