namespace Dreamteck.Splines
{
    using UnityEngine;

    public class CapsuleColliderGenerator : SplineUser, ISerializationCallbackReceiver
    {
        [SerializeField, Min(0f)] private float _radius = 1f;
        [SerializeField, Min(0f)] private float _height = 1f;
        [SerializeField] private bool _autoCalculateHeight = true;
        [SerializeField] private CapsuleColliderZDirection _direction = CapsuleColliderZDirection.Z;

        [SerializeField]
        [HideInInspector]
        public ColliderObject[] _colliders = new ColliderObject[0];

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
                ColliderObject[] newColliders = new ColliderObject[objectCount];
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

            SplineSample current = new SplineSample();
            SplineSample next = new SplineSample();
            Evaluate(0.0, ref current);

            for (int i = 0; i < objectCount; i++)
            {
                double nextPercent = (double)(i + 1) / (sampleCount - 1);
                Evaluate(nextPercent, ref next);
                _colliders[i].transform.position = Vector3.Lerp(current.position, next.position, 0.5f);
                _colliders[i].transform.rotation = Quaternion.LookRotation(next.position - current.position, Vector3.Slerp(current.up, next.up, 0.5f));
                
                _colliders[i].collider.radius = _radius;
                _colliders[i].collider.direction = (int)_direction;

                if (_autoCalculateHeight)
                {
                    var distance = Vector3.Distance(current.position, next.position);
                    _colliders[i].collider.height = distance;
                }
                else
                {
                    _colliders[i].collider.height = _height;
                }

                current = next;
            }
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            Build();
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