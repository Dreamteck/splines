namespace Dreamteck.Splines
{
    using UnityEngine;

    public class BoxColliderGenerator : SplineUser, ISerializationCallbackReceiver
    {
        [SerializeField] private Vector2 _boxSize = Vector2.one;
        [SerializeField] private bool _debugDraw = false;
        [SerializeField] private Color _debugDrawColor = Color.white;


        [SerializeField]
        [HideInInspector]
        public ColliderObject[] _colliders = new ColliderObject[0];


        public Vector2 boxSize
        {
            get { return _boxSize; }
            set
            {
                if (value != _boxSize)
                {
                    _boxSize = value;
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
                        newColliders[i] = new ColliderObject(newObject.transform, newObject.AddComponent<BoxCollider>());
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
                float size = Mathf.Lerp(current.size, next.size, 0.5f);
                _colliders[i].collider.size = new Vector3(_boxSize.x * size, _boxSize.y * size, Vector3.Distance(current.position, next.position));
                current = next;
            }
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            Build();
        }

        private void OnDrawGizmos()
        {
            if (_debugDraw)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    Gizmos.matrix = _colliders[i].transform.localToWorldMatrix;
                    Gizmos.color = _debugDrawColor;
                    Gizmos.DrawCube(Vector3.zero, _colliders[i].collider.size);
                }
                Gizmos.matrix = Matrix4x4.identity;
            }
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
                } else
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
            public BoxCollider collider;

            public ColliderObject(Transform transform, BoxCollider collider)
            {
                this.transform = transform;
                this.collider = collider;
            }
        }
    }
}