using UnityEngine;
using System.Collections;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif 

namespace Dreamteck.Splines
{
    public class MeshGenerator : SplineUser
    {
        protected const int UNITY_16_VERTEX_LIMIT = 65535;

        public float size
        {
            get { return _size; }
            set
            {
                if (value != _size)
                {
                    _size = value;
                    Rebuild();
                } else _size = value;
            }
        }

        public Color color
        {
            get { return _color; }
            set
            {
                if (value != _color)
                {
                    _color = value;
                    Rebuild();
                }
            }
        }

        public Vector3 offset
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

        public NormalMethod normalMethod
        {
            get { return _normalMethod; }
            set
            {
                if (value != _normalMethod)
                {
                    _normalMethod = value;
                    Rebuild();
                }
            }
        }

        public bool useSplineSize
        {
            get { return _useSplineSize; }
            set
            {
                if (value != _useSplineSize)
                {
                    _useSplineSize = value;
                    Rebuild();
                }
            }
        }

        public bool useSplineColor
        {
            get { return _useSplineColor; }
            set
            {
                if (value != _useSplineColor)
                {
                    _useSplineColor = value;
                    Rebuild();
                }
            }
        }

        public bool calculateTangents
        {
            get { return _calculateTangents; }
            set
            {
                if (value != _calculateTangents)
                {
                    _calculateTangents = value;
                    Rebuild();
                }
            }
        }

        public float rotation
        {
            get { return _rotation; }
            set
            {
                if (value != _rotation)
                {
                    _rotation = value;
                    Rebuild();
                }
            }
        }

        public bool flipFaces
        {
            get { return _flipFaces; }
            set
            {
                if (value != _flipFaces)
                {
                    _flipFaces = value;
                    Rebuild();
                }
            }
        }

        public bool doubleSided
        {
            get { return _doubleSided; }
            set
            {
                if (value != _doubleSided)
                {
                    _doubleSided = value;
                    Rebuild();
                }
            }
        }

        public UVMode uvMode
        {
            get { return _uvMode; }
            set
            {
                if (value != _uvMode)
                {
                    _uvMode = value;
                    Rebuild();
                }
            }
        }

        public Vector2 uvScale
        {
            get { return _uvScale; }
            set
            {
                if (value != _uvScale)
                {
                    _uvScale = value;
                    Rebuild();
                }
            }
        }

        public Vector2 uvOffset
        {
            get { return _uvOffset; }
            set
            {
                if (value != _uvOffset)
                {
                    _uvOffset = value;
                    Rebuild();
                }
            }
        }

        public float uvRotation
        {
            get { return _uvRotation; }
            set
            {
                if (value != _uvRotation)
                {
                    _uvRotation = value;
                    Rebuild();
                }
            }
        }

        public UnityEngine.Rendering.IndexFormat meshIndexFormat
        {
            get { return _meshIndexFormat; }
            set
            {
                if (value != _meshIndexFormat)
                {
                    _meshIndexFormat = value;
                    RefreshMesh();
                    Rebuild();
                }
            }
        }

        public bool baked
        {
            get
            {
                return _baked;
            }
        }

        public bool markDynamic
        {
            get { return _markDynamic; }
            set
            {
                if (value != _markDynamic)
                {
                    _markDynamic = value;
                    RefreshMesh();
                    Rebuild();
                }
            }
        }

        public enum UVMode { Clip, UniformClip, Clamp, UniformClamp }
        public enum NormalMethod { Recalculate, SplineNormals }
        [SerializeField]
        [HideInInspector]
        private bool _baked = false;
        [SerializeField]
        [HideInInspector]
        private bool _markDynamic = true;
        [SerializeField]
        [HideInInspector]
        private float _size = 1f;
        [SerializeField]
        [HideInInspector]
        private Color _color = Color.white;
        [SerializeField]
        [HideInInspector]
        private Vector3 _offset = Vector3.zero;
        [SerializeField]
        [HideInInspector]
        private NormalMethod _normalMethod = NormalMethod.SplineNormals;
        [SerializeField]
        [HideInInspector]
        private bool _calculateTangents = true;
        [SerializeField]
        [HideInInspector]
        private bool _useSplineSize = true;
        [SerializeField]
        [HideInInspector]
        private bool _useSplineColor = true;
        [SerializeField]
        [HideInInspector]
        [Range(-360f, 360f)]
        private float _rotation = 0f;
        [SerializeField]
        [HideInInspector]
        private bool _flipFaces = false;
        [SerializeField]
        [HideInInspector]
        private bool _doubleSided = false;
        [SerializeField]
        [HideInInspector]
        private UVMode _uvMode = UVMode.Clip;
        [SerializeField]
        [HideInInspector]
        private Vector2 _uvScale = Vector2.one;
        [SerializeField]
        [HideInInspector]
        private Vector2 _uvOffset = Vector2.zero;
        [SerializeField]
        [HideInInspector]
        private float _uvRotation = 0f;
        [SerializeField]
        [HideInInspector]
        private UnityEngine.Rendering.IndexFormat _meshIndexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        [SerializeField]
        [HideInInspector]
        private Mesh _bakedMesh;

        [HideInInspector]
        public float colliderUpdateRate = 0.2f;
        protected bool _updateCollider = false;
        protected float _lastUpdateTime = 0f;

        private float _vDist = 0f;
        protected static Vector2 __uvs = Vector2.zero;

        protected virtual string meshName => "Mesh";
        protected TS_Mesh _tsMesh { get; private set; }
        protected Mesh _mesh;

        protected MeshFilter filter;
        protected MeshRenderer meshRenderer;
        protected MeshCollider meshCollider;

#if UNITY_EDITOR

        public void Bake(bool makeStatic, bool lightmapUV)
        {
            if (_mesh == null) return;
            gameObject.isStatic = false;
            UnityEditor.MeshUtility.Optimize(_mesh);
            if (spline != null)
            {
                spline.Unsubscribe(this);
            }
            filter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            filter.hideFlags = meshRenderer.hideFlags = HideFlags.None;
            _bakedMesh = Instantiate(_mesh);
            _bakedMesh.name = meshName + " - Baked";
            if (lightmapUV)
            {
                Unwrapping.GenerateSecondaryUVSet(_bakedMesh);
            }
            filter.sharedMesh = _bakedMesh;
            _mesh = null;
            gameObject.isStatic = makeStatic; 
            _baked = true;
        }

        public void Unbake()
        {
            gameObject.isStatic = false; 
            _baked = false;
            DestroyImmediate(_bakedMesh);
            _bakedMesh = null;
            CreateMesh();
            spline.Subscribe(this);
            Rebuild();
        }

        public override void EditorAwake()
        {
            GetComponents();
            base.EditorAwake();
        }
#endif


        protected override void Awake()
        {
            GetComponents();
            base.Awake();
        }

        protected override void Reset()
        {
            base.Reset();
            GetComponents();
#if UNITY_EDITOR
            bool materialFound = false;
            for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
            {
                if (meshRenderer.sharedMaterials[i] != null)
                {
                    materialFound = true;
                    break;
                }
            }
            if (!materialFound) meshRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
#endif
        }

        private void GetComponents()
        {
            filter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }

        public override void Rebuild()
        {
            if (_baked) return;
            base.Rebuild();
        }

        public override void RebuildImmediate()
        {
            if (_baked) return;
            base.RebuildImmediate();
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
            MeshFilter filter = GetComponent<MeshFilter>();
            MeshRenderer rend = GetComponent<MeshRenderer>();
            if (filter != null)  filter.hideFlags = HideFlags.None;
            if (rend != null)  rend.hideFlags = HideFlags.None;
        }


        public void UpdateCollider()
        {
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filter.sharedMesh;
        }

        protected override void LateRun()
        {
            if (_baked) return;
            base.LateRun();
            if (_updateCollider)
            {
                if (meshCollider != null)
                {
                    if (Time.time - _lastUpdateTime >= colliderUpdateRate)
                    {
                        _lastUpdateTime = Time.time;
                        _updateCollider = false;
                        meshCollider.sharedMesh = filter.sharedMesh;
                    }
                }
            }
        }

        protected override void Build()
        {
            base.Build();
            if (_tsMesh == null || _mesh == null)
            {
                CreateMesh();
            }

            if (sampleCount > 1)
            {
                BuildMesh();
            } else
            {
                ClearMesh();
            }
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            WriteMesh();
        }

        protected virtual void ClearMesh()
        {
            _tsMesh.Clear();
            _mesh.Clear();
        }

        protected virtual void BuildMesh()
        {
            //Logic for mesh generation, automatically called in the Build method
        }

        protected virtual void WriteMesh() 
        {
            MeshUtility.TransformMesh(_tsMesh, trs.worldToLocalMatrix);
            if (_doubleSided)
            {
                MeshUtility.MakeDoublesidedHalf(_tsMesh);
            }
            else if (_flipFaces)
            {
                MeshUtility.FlipFaces(_tsMesh);
            }

            if (_calculateTangents)
            {
                MeshUtility.CalculateTangents(_tsMesh);
            }

            if (_meshIndexFormat == UnityEngine.Rendering.IndexFormat.UInt16 && _tsMesh.vertexCount > UNITY_16_VERTEX_LIMIT)
            {
                Debug.LogError("WARNING: The generated mesh for " + name + " exceeds the maximum vertex count for standard meshes in Unity (" + UNITY_16_VERTEX_LIMIT + "). To create bigger meshes, set the Index Format inside the Vertices foldout to 32.");
            }

            _tsMesh.indexFormat = _meshIndexFormat;

            _tsMesh.WriteMesh(ref _mesh);

            if (_markDynamic)
            {
                _mesh.MarkDynamic();
            }

            if (_normalMethod == 0)
            {
                _mesh.RecalculateNormals();
            }

            if (filter != null)
            {
                filter.sharedMesh = _mesh;
            }
            _updateCollider = true;
        }

        protected virtual void AllocateMesh(int vertexCount, int trisCount)
        {
            if(trisCount < 0)
            {
                trisCount = 0;
            }
            if(vertexCount < 0)
            {
                vertexCount = 0;
            }
            if (_doubleSided)
            {
                vertexCount *= 2;
                trisCount *= 2;
            }
            if (_tsMesh.vertexCount != vertexCount)
            {
                _tsMesh.vertices = new Vector3[vertexCount];
                _tsMesh.normals = new Vector3[vertexCount];
                _tsMesh.tangents = new Vector4[vertexCount];
                _tsMesh.colors = new Color[vertexCount];
                _tsMesh.uv = new Vector2[vertexCount];
            }
            if (_tsMesh.triangles.Length != trisCount)
            {
                _tsMesh.triangles = new int[trisCount];
            }
        }

        protected void ResetUVDistance()
        {
            _vDist = 0f;
            if (uvMode == UVMode.UniformClip)
            {
                _vDist = spline.CalculateLength(0.0, GetSamplePercent(0));
            }
        }

        protected void AddUVDistance(int sampleIndex)
        {
            if (sampleIndex == 0) return;
            SplineSample current = new SplineSample();
            SplineSample last = new SplineSample();
            GetSampleRaw(sampleIndex, ref current);
            GetSampleRaw(sampleIndex - 1, ref last);
            _vDist += Vector3.Distance(current.position, last.position);
        }

        protected void CalculateUVs(double percent, float u)
        {
            __uvs.x = u * _uvScale.x - _uvOffset.x;
            switch (uvMode)
            {
                case UVMode.Clip:  __uvs.y = (float)percent * _uvScale.y - _uvOffset.y; break;
                case UVMode.Clamp: __uvs.y = (float)DMath.InverseLerp(clipFrom, clipTo, percent) * _uvScale.y - _uvOffset.y;  break;
                case UVMode.UniformClamp: __uvs.y = _vDist * _uvScale.y / (float)span - _uvOffset.y; break;
                default: __uvs.y = _vDist * _uvScale.y - _uvOffset.y; break;
            }
        }

        protected float GetBaseSize(SplineSample sample)
        {
            return _useSplineSize? sample.size: 1f;
        }

        protected Color GetBaseColor(SplineSample sample)
        {
            return _useSplineColor ? sample.color : Color.white;
        }

        protected virtual void CreateMesh()
        {
            _tsMesh = new TS_Mesh();
            _mesh = new Mesh();
            _mesh.name = meshName;
            _mesh.indexFormat = _meshIndexFormat;
            _tsMesh.indexFormat = _meshIndexFormat;
            if (_markDynamic)
            {
                _mesh.MarkDynamic();
            }
        }

        private void RefreshMesh()
        {
            if (!Application.isPlaying)
            {
                DestroyImmediate(_mesh);
            } 
            else
            {
                Destroy(_mesh);
            }
            _mesh = null;
            _tsMesh.Clear();
            _tsMesh = null;
            CreateMesh();
        }
    }

  
}
