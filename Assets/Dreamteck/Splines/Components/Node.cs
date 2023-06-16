using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    [ExecuteInEditMode]
    [AddComponentMenu("Dreamteck/Splines/Node Connector")]
    public class Node : MonoBehaviour
    {
        [System.Serializable]
        public class Connection
        {
            public SplineComputer spline
            {
                get { return _computer; }
            }

            public int pointIndex
            {
                get { return _pointIndex; }
            }

            public bool invertTangents = false;

            [SerializeField]
            private int _pointIndex = 0;
            [SerializeField]
            private SplineComputer _computer = null;
            [SerializeField]
            [HideInInspector]
            internal SplinePoint point;

            internal bool isValid
            {
                get
                {
                    if (_computer == null) return false;
                    if (_pointIndex >= _computer.pointCount) return false;
                    return true;
                }
            }

            internal Connection(SplineComputer comp, int index, SplinePoint inputPoint)
            {
                _pointIndex = index;
                _computer = comp;
                point = inputPoint;
            }
        }
        public enum Type { Smooth, Free }
        [HideInInspector]
        public Type type = Type.Smooth;

        public bool transformNormals
        {
            get { return _transformNormals; }
            set
            {
                if (value != _transformNormals)
                {
                    _transformNormals = value;
                    UpdatePoints();
                }
            }
        }

        public bool transformSize
        {
            get { return _transformSize; }
            set
            {
                if (value != _transformSize)
                {
                    _transformSize = value;
                    UpdatePoints();
                }
            }
        }

        public bool transformTangents
        {
            get { return _transformTangents; }
            set
            {
                if (value != _transformTangents)
                {
                    _transformTangents = value;
                    UpdatePoints();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        protected Connection[] connections = new Connection[0];
        [SerializeField]
        [HideInInspector]
        private bool _transformSize = true;
        [SerializeField]
        [HideInInspector]
        private bool _transformNormals = true;
        [SerializeField]
        [HideInInspector]
        private bool _transformTangents = true;

        private Vector3 _lastPosition, _lastScale;
        private Quaternion _lastRotation;
        private Transform _trs;

        private void Awake()
        {
            _trs = transform;
            SampleTransform();
        }


        void LateUpdate()
        {
            Run();
        }

        void Update()
        {
            Run();
        }

        bool TransformChanged()
        {
#if UNITY_EDITOR
            if(_trs == null) return _lastPosition != transform.position || _lastRotation != transform.rotation || _lastScale != transform.lossyScale;
#endif
            return _lastPosition != _trs.position || _lastRotation != _trs.rotation || _lastScale != _trs.lossyScale;
        }

        void SampleTransform() {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _lastPosition = transform.position;
                _lastScale = transform.lossyScale;
                _lastRotation = transform.rotation;
            } 
            else
            {
                _lastPosition = _trs.position;
                _lastScale = _trs.lossyScale;
                _lastRotation = _trs.rotation;
            }
            return;
#else
            _lastPosition = _trs.position;
            _lastScale = _trs.lossyScale;
            _lastRotation = _trs.rotation;
#endif
        }

        private void Run()
        {
            if (TransformChanged())
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    for (int i = 0; i < connections.Length; i++)
                    {
                        UnityEditor.EditorUtility.SetDirty(connections[i].spline);
                    }
                }
#endif
                UpdateConnectedComputers();
                SampleTransform();
            }
        }

        public SplinePoint GetPoint(int connectionIndex, bool swapTangents)
        {
            SplinePoint point = PointToWorld(connections[connectionIndex].point);
            if (connections[connectionIndex].invertTangents && swapTangents)
            {
                Vector3 tempTan = point.tangent;
                point.tangent = point.tangent2;
                point.tangent2 = tempTan;
            }
            return point;
        }

        public void SetPoint(int connectionIndex, SplinePoint worldPoint, bool swappedTangents)
        {
            Connection connection = connections[connectionIndex];
            connection.point = PointToLocal(worldPoint);
            if (connection.invertTangents && swappedTangents)
            {
                Vector3 tempTan = connection.point.tangent;
                connection.point.tangent = connection.point.tangent2;
                connection.point.tangent2 = tempTan;
            }
            if (type == Type.Smooth)
            {
                if (connection.point.type == SplinePoint.Type.SmoothFree)
                {
                    for (int i = 0; i < connections.Length; i++)
                    {
                        if (i == connectionIndex) continue;
                        Vector3 tanDir = (connection.point.tangent - connection.point.position).normalized;
                        if (tanDir == Vector3.zero) tanDir = -(connection.point.tangent2 - connection.point.position).normalized;
                        float tan1Length = (connections[i].point.tangent - connections[i].point.position).magnitude;
                        float tan2Length = (connections[i].point.tangent2 - connections[i].point.position).magnitude;
                        connections[i].point = connection.point;
                        connections[i].point.tangent = connections[i].point.position + tanDir * tan1Length;
                        connections[i].point.tangent2 = connections[i].point.position - tanDir * tan2Length;
                    }
                }
                else
                {
                    for (int i = 0; i < connections.Length; i++)
                    {
                        if (i == connectionIndex) continue;
                        connections[i].point = connection.point;
                    }
                }
            }
        }

        void OnDestroy()
        {
            ClearConnections();
        }

        public void ClearConnections()
        {
            for (int i = connections.Length-1; i >= 0; i--)
            {
                if (connections[i].spline != null) connections[i].spline.DisconnectNode(connections[i].pointIndex);
            }
            connections = new Connection[0];
        }

        public void UpdateConnectedComputers(SplineComputer excludeComputer = null)
        {
            for (int i = connections.Length - 1; i >= 0; i--)
            {
                if (!connections[i].isValid)
                {
                    RemoveConnection(i);
                    continue;
                }

                if (connections[i].spline == excludeComputer) continue;

                if (type == Type.Smooth && i != 0)
                {
                    SetPoint(i, GetPoint(0, false), false);
                }
                SplinePoint point = GetPoint(i, true);
                if (!transformNormals)
                {
                    point.normal = connections[i].spline.GetPointNormal(connections[i].pointIndex);
                }
                if (!transformTangents)
                {
                    point.tangent = connections[i].spline.GetPointTangent(connections[i].pointIndex);
                    point.tangent2 = connections[i].spline.GetPointTangent2(connections[i].pointIndex);
                }
                if (!transformSize)
                {
                    point.size = connections[i].spline.GetPointSize(connections[i].pointIndex);
                }
                connections[i].spline.SetPoint(connections[i].pointIndex, point);
            }
        }

        public void UpdatePoint(SplineComputer computer, int pointIndex, SplinePoint point, bool updatePosition = true)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                transform.position = point.position;
            }
            else
            {
                _trs.position = point.position;
            }
#else
            _trs.position = point.position;
#endif
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].spline == computer && connections[i].pointIndex == pointIndex)
                {
                    SetPoint(i, point, true);
                }
            }
        }

        public void UpdatePoints()
        {
            for (int i = connections.Length - 1; i >= 0; i--)
            {
                if (!connections[i].isValid)
                {
                    RemoveConnection(i);
                    continue;
                }
                SplinePoint point = connections[i].spline.GetPoint(connections[i].pointIndex);
                point.SetPosition(transform.position);
                SetPoint(i, point, true);
            }
        }

#if UNITY_EDITOR
        //Use this to maintain the connections between computers in the editor
        public void EditorMaintainConnections()
        {
            RemoveInvalidConnections();
        }
#endif
        //Remove invalid connections
        protected void RemoveInvalidConnections()
        {
            for (int i = connections.Length - 1; i >= 0; i--)
            {
                if (connections[i] == null || !connections[i].isValid) RemoveConnection(i);
            }
        }

        public virtual void AddConnection(SplineComputer computer, int pointIndex)
        {
            RemoveInvalidConnections();
            Node connected = computer.GetNode(pointIndex);
            if (connected != null)
            {
                Debug.LogError(computer.name + " is already connected to node " + connected.name + " at point " + pointIndex);
                return;
            }
            SplinePoint point = computer.GetPoint(pointIndex);
            point.SetPosition(transform.position);
            ArrayUtility.Add(ref connections, new Connection(computer, pointIndex, PointToLocal(point)));
            if(connections.Length == 1) SetPoint(connections.Length - 1, point, true);
            UpdateConnectedComputers();
        }

        protected SplinePoint PointToLocal(SplinePoint worldPoint)
        {
            worldPoint.position = Vector3.zero;
            worldPoint.tangent = transform.InverseTransformPoint(worldPoint.tangent);
            worldPoint.tangent2 = transform.InverseTransformPoint(worldPoint.tangent2);
            worldPoint.normal = transform.InverseTransformDirection(worldPoint.normal);
            worldPoint.size /= (transform.localScale.x + transform.localScale.y + transform.localScale.z)/ 3f;
            return worldPoint;
        }

        protected SplinePoint PointToWorld(SplinePoint localPoint)
        {
            localPoint.position = transform.position;
            localPoint.tangent = transform.TransformPoint(localPoint.tangent);
            localPoint.tangent2 = transform.TransformPoint(localPoint.tangent2);
            localPoint.normal = transform.TransformDirection(localPoint.normal);
            localPoint.size *= (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3f;
            return localPoint;
        }

        public virtual void RemoveConnection(SplineComputer computer, int pointIndex)
        {
            int index = -1;
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].pointIndex == pointIndex && connections[i].spline == computer)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0) return;
            RemoveConnection(index);
        }

        private void RemoveConnection(int index)
        {
            Connection[] newConnections = new Connection[connections.Length - 1];
            SplineComputer spline = connections[index].spline;
            int pointIndex = connections[index].pointIndex;
            for (int i = 0; i < connections.Length; i++)
            {
                if (i < index) newConnections[i] = connections[i];
                else if (i == index) continue;
                else newConnections[i - 1] = connections[i];
            }
            connections = newConnections;
        }

        public virtual bool HasConnection(SplineComputer computer, int pointIndex)
        {
            for (int i = connections.Length - 1; i >= 0; i--)
            {
                if (!connections[i].isValid)
                {
                    RemoveConnection(i);
                    continue;
                }
                if (connections[i].spline == computer && connections[i].pointIndex == pointIndex) return true;
            }
            return false;
        }

        public Connection[] GetConnections()
        {
            return connections;
        }

    }
}
