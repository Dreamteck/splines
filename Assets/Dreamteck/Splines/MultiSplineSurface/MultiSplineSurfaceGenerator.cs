using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Dreamteck.Splines
{
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
            mesh.name = "multispline_surface";
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
            if (clippedSamples.Length == 0) return;
            if (_otherComputers.Length == 0) return;
            base.BuildMesh();
            GenerateVertices();
            tsMesh.subMeshes.Clear();
            if (!_separateMaterialIDs) tsMesh.triangles = MeshUtility.GeneratePlaneTriangles(clippedSamples.Length - 1, (otherComputers.Length * _iterations + otherComputers.Length + 1), flipFaces && !doubleSided);
            else
            {
                tris = MeshUtility.GeneratePlaneTriangles(clippedSamples.Length - 1, _iterations + 2, false);
                for (int i = 0; i < _otherComputers.Length; i++)
                {
                    int[] newTris = new int[tris.Length];
                    tris.CopyTo(newTris, 0);
                    tsMesh.subMeshes.Add(newTris);
                    for (int n = 0; n < tsMesh.subMeshes[i].Length; n++) tsMesh.subMeshes[i][n] += i * ((_iterations + 1) * clippedSamples.Length);
                }
            }
        }


        void GenerateVertices()
        {
            if (_otherComputers.Length == 0) return;
            if (evaluateSplines.Length != clippedSamples.Length)
            {
                evaluateSplines = new Spline[clippedSamples.Length];
                for(int i = 0; i < evaluateSplines.Length; i++)
                {
                    evaluateSplines[i] = new Spline(Spline.Type.Hermite);
                }
            }
            if (evaluateSplines[0].points.Length != _otherComputers.Length + 1)
            {
                for (int i = 0; i < evaluateSplines.Length; i++) evaluateSplines[i].points = new SplinePoint[_otherComputers.Length+1];
            }
            for(int i = 0; i < clippedSamples.Length; i++)
            {
                double percent = (double)i / (clippedSamples.Length-1);
                evaluateSplines[i].points[0].position = clippedSamples[i].position;
                if(_automaticNormals) evaluateSplines[i].points[0].normal = Vector3.Cross(clippedSamples[i].direction, clippedSamples[i].right).normalized;
                else evaluateSplines[i].points[0].normal = clippedSamples[i].normal;
                evaluateSplines[i].points[0].color = clippedSamples[i].color;
                for (int n = 1; n <= _otherComputers.Length; n++)
                {
                    SplineSample result = _otherComputers[n-1].Evaluate(DMath.Lerp(clipFrom, clipTo, percent));
                    evaluateSplines[i].points[n].position = result.position;
                    if (_automaticNormals) evaluateSplines[i].points[n].normal = Vector3.Cross(result.direction, result.right).normalized;
                    else evaluateSplines[i].points[n].normal = result.normal;
                    evaluateSplines[i].points[n].color = result.color;
                }
            }
            
            int vertexCount = (_otherComputers.Length * _iterations + _otherComputers.Length + 1) * evaluateSplines.Length;
            if (tsMesh.vertexCount != vertexCount)
            {
                tsMesh.vertices = new Vector3[vertexCount];
                tsMesh.normals = new Vector3[vertexCount];
                tsMesh.colors = new Color[vertexCount];
                tsMesh.uv = new Vector2[vertexCount];
            }
            if (clippedSamples.Length == 0) return;
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
                    tsMesh.vertices[vertexIndex] = eval.position;
                    if (_automaticNormals) tsMesh.normals[vertexIndex] = Vector3.Cross(eval.direction, eval.right).normalized;
                    else tsMesh.normals[vertexIndex] = eval.normal;
                    tsMesh.colors[vertexIndex] = eval.color * color;
                    switch (_uvWrapMode)
                    {
                        case UVWrapMode.Clamp: tsMesh.uv[vertexIndex] = new Vector2((float)n / (evaluateSplines.Length - 1)*uvScale.x+uvOffset.x, (float)percent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.UniformX: tsMesh.uv[vertexIndex] = new Vector2(xLength * uvScale.x + uvOffset.x, (float)percent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.UniformY: tsMesh.uv[vertexIndex] = new Vector2((float)n / (evaluateSplines.Length - 1) * uvScale.x + uvOffset.x, yLengths[n] * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.Uniform: tsMesh.uv[vertexIndex] = new Vector2(xLength * uvScale.x + uvOffset.x, yLengths[n] * uvScale.y + uvOffset.y); break;
                    }
                    vertexIndex++;
                }
            }
        }
    }
}
