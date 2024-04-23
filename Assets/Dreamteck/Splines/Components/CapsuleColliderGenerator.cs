namespace Dreamteck.Splines
{
    using UnityEngine;

    public class CapsuleColliderGenerator : SplineUser, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector, Min(0f)] private float _radius = 1f;
        [SerializeField, HideInInspector, Min(0f)] private float _height = 1f;
        [SerializeField, HideInInspector] private bool _overlapCaps = true;
        [SerializeField, HideInInspector] private CapsuleColliderZDirection _direction = CapsuleColliderZDirection.Z;
        [SerializeField, HideInInspector] private ColliderObject[] _colliders = new ColliderObject[0];

        public float radius
        {
            get { return _radius; }
            set
            {
                if (value != _radius)
                {
                    _radius = value;
                    Rebuild();
                }
            }
        }

        public float height
        {
            get { return _height; }
            set
            {
                if (value != _height)
                {
                    _height = value;
                    Rebuild();
                }
            }
        }

        public bool overlapCaps
        {
            get { return _overlapCaps; }
            set
            {
                if (value != _overlapCaps)
                {
                    _overlapCaps = value;
                    Rebuild();
                }
            }
        }

        public CapsuleColliderZDirection direction
        {
            get { return _direction; }
            set
            {
                if (value != _direction)
                {
                    _direction = value;
                    Rebuild();
                }
            }
        }

        private void DestroyCollider(ColliderObject collider)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Destroy(collider.transform.gameObject);
            }
            else
            {
                DestroyImmediate(collider.transform.gameObject);
            }
#else
            Destroy(collider.transform.gameObject);
#endif
        }

        protected override void Build()
        {
            base.Build();

            if (sampleCount == 0)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    DestroyCollider(_colliders[i]);
                }
                _colliders = new ColliderObject[0];
                return;
            }

            int objectCount = sampleCount - 1;
            if (objectCount != _colliders.Length)
            {
                GenerateColliders(objectCount);
            }

            SplineSample current = new SplineSample();
            SplineSample next = new SplineSample();
            Evaluate(0.0, ref current);

            bool controlHeight = _direction == CapsuleColliderZDirection.Z;

            for (int i = 0; i < objectCount; i++)
            {
                double nextPercent = (double)(i + 1) / (sampleCount - 1);
                Evaluate(nextPercent, ref next);
                _colliders[i].transform.position = Vector3.Lerp(current.position, next.position, 0.5f);
                _colliders[i].transform.rotation = Quaternion.LookRotation(next.position - current.position, Vector3.Slerp(current.up, next.up, 0.5f));
                
                _colliders[i].collider.radius = _radius;
                _colliders[i].collider.direction = (int)_direction;

                var distance = Vector3.Distance(current.position, next.position);

                if (controlHeight)
                {
                    if (_overlapCaps)
                    {
                        _colliders[i].collider.height = distance + _radius * 2f;
                    } else
                    {
                        _colliders[i].collider.height = distance;
                    }
                    _colliders[i].collider.radius = _radius;
                }
                else
                {
                    _colliders[i].collider.height = _height;
                    _colliders[i].collider.radius = distance * 0.5f;
                }

                current = next;
            }
        }

        private void GenerateColliders(int count)
        {
            ColliderObject[] newColliders = new ColliderObject[count];
            for (int i = 0; i < newColliders.Length; i++)
            {
                if (i < _colliders.Length)
                {
                    newColliders[i] = _colliders[i];
                }
                else
                {
                    GameObject newObject = new GameObject("Collider " + i);
                    newObject.layer = gameObject.layer;
                    newObject.transform.parent = trs;
                    newColliders[i] = new ColliderObject(newObject.transform, newObject.AddComponent<CapsuleCollider>(), _direction, _height);
                }
            }
            if (newColliders.Length < _colliders.Length)
            {
                for (int i = newColliders.Length; i < _colliders.Length; i++)
                {
                    DestroyCollider(_colliders[i]);
                }
            }
            _colliders = newColliders;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            Build();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < _colliders.Length; i++)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(_colliders[i].transform.gameObject);
                }
                else
                {
                    Destroy(_colliders[i].transform.gameObject);
                }
#else
                Destroy(_colliders[i].transform.gameObject);
#endif
            }
        }

        [System.Serializable]
        public class ColliderObject
        {
            public Transform transform;
            public CapsuleCollider collider;

            public ColliderObject(Transform transform, CapsuleCollider collider, CapsuleColliderZDirection direction, float height)
            {
                this.transform = transform;
                this.collider = collider;
                this.collider.direction = (int)direction;
                this.collider.height = height;
            }
        }

        public enum CapsuleColliderZDirection
        {
            X = 0, Y = 1, Z = 2,
        }
    }
}