namespace Dreamteck.Splines
{
    using UnityEngine;

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Multi Spline Surface Generator")]
    public class MultiSplineSurfaceGenerator : MeshGenerator
    {
        public enum UVWrapMode { Clamp, UniformX, UniformY, Uniform }
        public UVWrapMode uvWrapMode
        {
            get { return _uvWrapMode; }
            set
            {
                if (value != _uvWrapMode)
                {
                    _uvWrapMode = value;
                    Rebuild();
                }
            }
        }

        public int subdivisions
        {
            get { return _subdivisions; }
            set
            {
                if (value != _subdivisions)
                {
                    _subdivisions = value;
                    Rebuild();
                }
            }
        }

        public bool automaticNormals
        {
            get { return _automaticNormals; }
            set
            {
                if (value != _automaticNormals)
                {
                    _automaticNormals = value;
                    Rebuild();
                }
            }
        }

        public bool separateMaterialIDs
        {
            get { return _separateMaterialIDs; }
            set
            {
                if (value != _separateMaterialIDs)
                {
                    _separateMaterialIDs = value;
                    Rebuild();
                }
            }
        }


        public SplineComputer[] otherComputers
        {
            get { return _otherComputers; }
            set
            {
                bool rebuild = false;
                if (value.Length != _otherComputers.Length) rebuild = true;
                else
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (value[i] != _otherComputers[i])
                        {
                            rebuild = true;
                            break;
                        }
                    }
                }
                if (rebuild)
                {
                    _otherComputers = value;
                    Rebuild();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private UVWrapMode _uvWrapMode = UVWrapMode.Clamp;
        [SerializeField, HideInInspector, Min(1)]
        private int _subdivisions = 1;
        [SerializeField]
        [HideInInspector]
        private bool _automaticNormals = true;
        [SerializeField]
        [HideInInspector]
        private bool _separateMaterialIDs = false;
        [SerializeField]
        [HideInInspector]
        private SplineComputer[] _otherComputers = new SplineComputer[0];
        [SerializeField]
        [HideInInspector]
        private Spline[] _splines = new Spline[0];

        private int iterations => _subdivisions * _otherComputers.Length;

        protected override void Awake()
        {
            base.Awake();

            _mesh.name = "multispline_surface";
            for (int i = 0; i < _otherComputers.Length; i++)
            {
                _otherComputers[i].onRebuild -= OnOtherRebuild;
                _otherComputers[i].onRebuild += OnOtherRebuild;
            }
        }

        void OnOtherRebuild()
        {
            RebuildImmediate();
        }

        protected override void Reset()
        {
            base.Reset();
        }


        protected override void BuildMesh()
        {
            if (sampleCount == 0) return;
            if (_otherComputers.Length == 0) return;
            if(_splines.Length != sampleCount)
            {
                _splines = new Spline[sampleCount];
                for (int i = 0; i < _splines.Length; i++)
                {
                    _splines[i] = new Spline(Spline.Type.CatmullRom);
                }
            }

            base.BuildMesh();
            AllocateMesh(sampleCount * (iterations + 1), iterations * (sampleCount-1) * 6);
            _tsMesh.triangles = MeshUtility.GeneratePlaneTriangles(sampleCount - 1, iterations + 1, false);
            GenerateVertices();
            _tsMesh.subMeshes.Clear();

            //if(_separateMaterialIDs)
            //{
            //    tris = MeshUtility.GeneratePlaneTriangles(sampleCount - 1, _iterations + 2, false);
            //    for (int i = 0; i < _otherComputers.Length; i++)
            //    {
            //        int[] newTris = new int[tris.Length];
            //        tris.CopyTo(newTris, 0);
            //        _tsMesh.subMeshes.Add(newTris);
            //        for (int n = 0; n < _tsMesh.subMeshes[i].Length; n++)
            //        {
            //            _tsMesh.subMeshes[i][n] += i * ((_iterations + 1) * sampleCount);
            //        }
            //    }
            //}
        }


        void GenerateVertices()
        {
            if (_otherComputers.Length == 0) return;
            SplineSample sample = default;

            for (int i = 0; i < _otherComputers.Length + 1; i++)
            {
                SplineComputer splineComp = spline;
                if (i > 0)
                {
                    splineComp = _otherComputers[i - 1];
                }

                for (int j = 0; j < sampleCount; j++)
                {
                    if(_splines[j].points.Length != _otherComputers.Length + 1)
                    {
                        _splines[j].points = new SplinePoint[_otherComputers.Length + 1];
                    }
                    
                    double xPercent = (double)j / (sampleCount - 1);
                    if (i > 0)
                    {
                        splineComp.Evaluate(xPercent, ref sample);
                    }
                    else
                    {
                        GetSample(j, ref sample);
                    }

                    _splines[j].points[i].position = sample.position;
                    _splines[j].points[i].normal = sample.up;
                    _splines[j].points[i].color = sample.color;
                }
            }

            for (int x = 0; x < _splines.Length; x++)
            {
                float xPercent = (float)x / (_splines.Length - 1);
                for (int y = 0; y < iterations + 1; y++)
                {
                    float yPercent = (float)y / iterations;
                    int index = x + y * _splines.Length;
                    _splines[x].Evaluate(yPercent, ref sample);
                    _tsMesh.vertices[index] = sample.position;
                    _tsMesh.normals[index] = sample.up;
                    _tsMesh.colors[index] = sample.color;
                    _tsMesh.uv[index] = new Vector2((float)xPercent, (float)yPercent);
                }
            }
        }


        protected override void WriteMesh()
        {
            base.WriteMesh();
            if (_automaticNormals)
            {
                _mesh.RecalculateNormals();
            }
        }

        public static void DrawSpline(Spline spline, Color color, double from = 0.0, double to = 1.0)
        {
            double add = spline.moveStep;
            int iterations = spline.iterations;
            if (iterations <= 0) return;

            Vector3 prevPoint = spline.EvaluatePosition(from);
            for (int i = 1; i < iterations; i++)
            {
                double p = DMath.Lerp(from, to, (double)i / (iterations - 1));
                Debug.DrawLine(prevPoint, spline.EvaluatePosition(p), color, 1f);
                prevPoint = spline.EvaluatePosition(p);
            }
        }


    }
}
