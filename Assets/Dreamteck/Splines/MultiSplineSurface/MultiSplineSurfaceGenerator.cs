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

        public int iterations
        {
            get { return _iterations; }
            set
            {
                if (value != _iterations)
                {
                    _iterations = value;
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
        [SerializeField]
        [HideInInspector]
        private int _iterations = 1;
        [SerializeField]
        [HideInInspector]
        private bool _automaticNormals = true;
        [SerializeField]
        [HideInInspector]
        private bool _separateMaterialIDs = false;
        [SerializeField]
        [HideInInspector]
        private SplineComputer[] _otherComputers = new SplineComputer[0];

        private int[] tris = new int[0];

        private Spline[] evaluateSplines = new Spline[0];

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
            base.BuildMesh();
            GenerateVertices();
            _tsMesh.subMeshes.Clear();
            if (!_separateMaterialIDs) _tsMesh.triangles = MeshUtility.GeneratePlaneTriangles(sampleCount - 1, (otherComputers.Length * _iterations + otherComputers.Length + 1), flipFaces && !doubleSided);
            else
            {
                tris = MeshUtility.GeneratePlaneTriangles(sampleCount - 1, _iterations + 2, false);
                for (int i = 0; i < _otherComputers.Length; i++)
                {
                    int[] newTris = new int[tris.Length];
                    tris.CopyTo(newTris, 0);
                    _tsMesh.subMeshes.Add(newTris);
                    for (int n = 0; n < _tsMesh.subMeshes[i].Length; n++) _tsMesh.subMeshes[i][n] += i * ((_iterations + 1) * sampleCount);
                }
            }
        }


        void GenerateVertices()
        {
            if (_otherComputers.Length == 0) return;
            if (evaluateSplines.Length != sampleCount)
            {
                evaluateSplines = new Spline[sampleCount];
                for(int i = 0; i < evaluateSplines.Length; i++)
                {
                    //evaluateSplines[i] = new Spline(Spline.Type.Hermite);
                    evaluateSplines[i] = new Spline(Spline.Type.Bezier);
                }
            }
            if (evaluateSplines[0].points.Length != _otherComputers.Length + 1)
            {
                for (int i = 0; i < evaluateSplines.Length; i++) evaluateSplines[i].points = new SplinePoint[_otherComputers.Length+1];
            }

            SplineSample sample = default;

            for(int i = 0; i < sampleCount; i++)
            {
                double percent = (double)i / (sampleCount - 1);
                GetSample(i, ref sample);
                evaluateSplines[i].points[0].position = sample.position;
                if(_automaticNormals) evaluateSplines[i].points[0].normal = Vector3.Cross(sample.forward, sample.right).normalized;
                else evaluateSplines[i].points[0].normal = sample.up;
                evaluateSplines[i].points[0].color = sample.color;
                for (int n = 1; n <= _otherComputers.Length; n++)
                {
                    SplineSample result = _otherComputers[n-1].Evaluate(DMath.Lerp(clipFrom, clipTo, percent));
                    evaluateSplines[i].points[n].position = result.position;
                    if (_automaticNormals) evaluateSplines[i].points[n].normal = Vector3.Cross(result.forward, result.right).normalized;
                    else evaluateSplines[i].points[n].normal = result.up;
                    evaluateSplines[i].points[n].color = result.color;
                }
            }
            
            int vertexCount = (_otherComputers.Length * _iterations + _otherComputers.Length + 1) * evaluateSplines.Length;
            if (_tsMesh.vertexCount != vertexCount)
            {
                _tsMesh.vertices = new Vector3[vertexCount];
                _tsMesh.normals = new Vector3[vertexCount];
                _tsMesh.colors = new Color[vertexCount];
                _tsMesh.uv = new Vector2[vertexCount];
            }
            if (sampleCount == 0) return;
            int vertexIndex = 0;
            int totalPoints = (_otherComputers.Length * _iterations + _otherComputers.Length + 1);
            float xLength = 0f;
            float[] yLengths = new float[0];
            if (_uvWrapMode == UVWrapMode.Uniform || _uvWrapMode == UVWrapMode.UniformY)
            {
                yLengths = new float[evaluateSplines.Length];
                for (int i = 0; i < yLengths.Length; i++) yLengths[i] = 0f;
            }
            Vector3 lastX = Vector3.zero;
            for (int i = 0; i < totalPoints; i++)
            {
                double percent = (double)i / (totalPoints-1);
                for(int n = 0; n < evaluateSplines.Length; n++)
                {
                    SplineSample eval = evaluateSplines[n].Evaluate(percent);
                    if (_uvWrapMode == UVWrapMode.Uniform || _uvWrapMode == UVWrapMode.UniformX)
                    {
                        if (n == 0) xLength = 0f;
                        else xLength += Vector3.Distance(lastX, eval.position);
                        lastX = eval.position;
                    }
                    if (_uvWrapMode == UVWrapMode.Uniform || _uvWrapMode == UVWrapMode.UniformY)
                    {
                        if (i > 0)
                        {
                            Vector3 previousYpos = evaluateSplines[n].EvaluatePosition((double)(i - 1) / (totalPoints - 1));
                            yLengths[n] += Vector3.Distance(eval.position, previousYpos);
                        }
                    }
                    _tsMesh.vertices[vertexIndex] = eval.position;
                    if (_automaticNormals) _tsMesh.normals[vertexIndex] = Vector3.Cross(eval.forward, eval.right).normalized;
                    else _tsMesh.normals[vertexIndex] = eval.up;
                    _tsMesh.colors[vertexIndex] = eval.color * color;
                    switch (_uvWrapMode)
                    {
                        case UVWrapMode.Clamp: _tsMesh.uv[vertexIndex] = new Vector2((float)n / (evaluateSplines.Length - 1)*uvScale.x+uvOffset.x, (float)percent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.UniformX: _tsMesh.uv[vertexIndex] = new Vector2(xLength * uvScale.x + uvOffset.x, (float)percent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.UniformY: _tsMesh.uv[vertexIndex] = new Vector2((float)n / (evaluateSplines.Length - 1) * uvScale.x + uvOffset.x, yLengths[n] * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.Uniform: _tsMesh.uv[vertexIndex] = new Vector2(xLength * uvScale.x + uvOffset.x, yLengths[n] * uvScale.y + uvOffset.y); break;
                    }
                    vertexIndex++;
                }
            }
        }
    }
}
