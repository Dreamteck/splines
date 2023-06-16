using UnityEngine;
using System.Collections;
using System.Threading;
namespace Dreamteck.Splines
{
    [AddComponentMenu("Dreamteck/Splines/Users/Edge Collider Generator")]
    [RequireComponent(typeof(EdgeCollider2D))]
    public class EdgeColliderGenerator : SplineUser
    {
        public float offset
        {
            get { return _offset; }
            set
            {
                if (value != _offset)
                {
                    _offset = value;
                    Rebuild();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private float _offset = 0f;
        [SerializeField]
        [HideInInspector]
        protected EdgeCollider2D edgeCollider;

        [SerializeField]
        [HideInInspector]
        protected Vector2[] vertices = new Vector2[0];

        [HideInInspector]
        public float updateRate = 0.1f;
        protected float lastUpdateTime = 0f;

        private bool updateCollider = false;

        protected override void Awake()
        {
            base.Awake();
            edgeCollider = GetComponent<EdgeCollider2D>();
        }


        protected override void Reset()
        {
            base.Reset();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void LateRun()
        {
            base.LateRun();
            if (updateCollider)
            {
                if (edgeCollider != null)
                {
                    if (Time.time - lastUpdateTime >= updateRate)
                    {
                        lastUpdateTime = Time.time;
                        updateCollider = false;
                        edgeCollider.points = vertices;
                    }
                }
            }
        }

        protected override void Build()
        {
            base.Build();
            if (vertices.Length != sampleCount) vertices = new Vector2[sampleCount];
            bool hasOffset = offset != 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                GetSample(i, ref evalResult);
                vertices[i] = evalResult.position;
                if (hasOffset)
                {
                    Vector2 right = new Vector2(-evalResult.forward.y, evalResult.forward.x).normalized * evalResult.size;
                    vertices[i] += right * offset;
                }
            }
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            if (edgeCollider == null) return;
            for(int i = 0; i < vertices.Length; i++) vertices[i] = transform.InverseTransformPoint(vertices[i]);
            
#if UNITY_EDITOR
            if (!Application.isPlaying || updateRate <= 0f)
            {
                edgeCollider.points = vertices;
            } else updateCollider = true;
#else
            if(updateRate == 0f) edgeCollider.points = vertices;
            else updateCollider = true;
#endif
        }
    }

  
}
