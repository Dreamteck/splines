using UnityEngine;
using System.Collections;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Users/Surface Generator")]
    public class SurfaceGenerator : MeshGenerator
    {
        public float expand
        {
            get { return _expand; }
            set
            {
                if (value != _expand)
                {
                    _expand = value;
                    Rebuild();
                }
            }
        }

        public float extrude
        {
            get { return _extrude; }
            set
            {
                if (value != _extrude)
                {
                    _extrude = value;
                    Rebuild();
                }
            }
        }

        public double extrudeClipFrom
        {
            get { return _extrudeFrom; }
            set
            {
                if (value != _extrudeFrom)
                {
                    _extrudeFrom = value;
                    Rebuild();
                }
            }
        }

        public double extrudeClipTo
        {
            get { return _extrudeTo; }
            set
            {
                if (value != _extrudeTo)
                {
                    _extrudeTo = value;
                    Rebuild();
                }
            }
        }

        public Vector2 sideUvScale
        {
            get { return _sideUvScale; }
            set
            {
                if (value != _sideUvScale)
                {
                    _sideUvScale = value;
                    Rebuild();
                }
                else
                {
                    _sideUvScale = value;
                }
            }
        }

        public Vector2 sideUvOffset
        {
            get { return _sideUvOffset; }
            set
            {
                if (value != _sideUvOffset)
                {
                    _sideUvOffset = value;
                    Rebuild();
                }
                else
                {
                    _sideUvOffset = value;
                }
            }
        }

        public float sideUvRotation
        {
            get { return _sideUvRotation; }
            set
            {
                if (value != _sideUvRotation)
                {
                    _sideUvRotation = value;
                    Rebuild();
                }
                else
                {
                    _sideUvRotation = value;
                }
            }
        }

        public SplineComputer extrudeSpline
        {
            get { return _extrudeSpline; }
            set
            {
                if (value != _extrudeSpline)
                {
                    if (_extrudeSpline != null)
                    {
                        _extrudeSpline.Unsubscribe(this);
                    }
                    _extrudeSpline = value;
                    if (value != null)
                    {
                        _extrudeSpline.Subscribe(this);
                    }
                    Rebuild();
                }
            }
        }

        public Vector3 extrudeOffset
        {
            get { return _extrudeOffset; }
            set { 
                if(value != _extrudeOffset)
                {
                    _extrudeOffset = value;
                    Rebuild();
                } 
            }
        }

        public bool uniformUvs
        {
            get { return _uniformUvs; }
            set
            {
                if (value != _uniformUvs)
                {
                    _uniformUvs = value;
                    Rebuild();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private float _expand = 0f;
        [SerializeField]
        [HideInInspector]
        private float _extrude = 0f;
        [SerializeField]
        [HideInInspector]
        private Vector2 _sideUvScale = Vector2.one;
        [SerializeField]
        [HideInInspector]
        private Vector2 _sideUvOffset = Vector2.zero;
        [SerializeField]
        [HideInInspector]
        private float _sideUvRotation = 0f;
        [SerializeField]
        [HideInInspector]
        private SplineComputer _extrudeSpline;
        [SerializeField]
        [HideInInspector]
        private Vector3 _extrudeOffset = Vector3.zero;
        [SerializeField]
        [HideInInspector]
        private SplineSample[] extrudeResults = new SplineSample[0];
        [SerializeField]
        [HideInInspector]
        private Vector3[] identityVertices = new Vector3[0];
        [SerializeField]
        [HideInInspector]
        private Vector3[] identityNormals = new Vector3[0];
        [SerializeField]
        [HideInInspector]
        private Vector2[] projectedVerts = new Vector2[0];
        [SerializeField]
        [HideInInspector]
        private int[] surfaceTris = new int[0];
        [SerializeField]
        [HideInInspector]
        private int[] wallTris = new int[0];

        [SerializeField]
        [HideInInspector]
        private double _extrudeFrom = 0.0;
        [SerializeField]
        [HideInInspector]
        private double _extrudeTo = 1.0;
        [SerializeField]
        [HideInInspector]
        private bool _uniformUvs = false;

        private Vector3 _trsRight = Vector3.right;
        private Vector3 _trsUp = Vector3.up;
        private Vector3 _trsForward = Vector3.forward;

        protected override string meshName => "Surface";

        protected override void Awake()
        {
            base.Awake();
            _trsRight = trs.right;
            _trsUp = trs.up;
            _trsForward = trs.forward;
        }

        protected override void BuildMesh()
        {
            if (spline.pointCount == 0) return;
            base.BuildMesh();
            Generate();
        }

        private void LateUpdate()
        {
            if (multithreaded && trs.hasChanged)
            {
                _trsRight = trs.right;
                _trsUp = trs.up;
                _trsForward = trs.forward;
            }
        }

        public void Generate()
        {
            if (!multithreaded)
            {
                _trsRight = trs.right;
                _trsUp = trs.up;
                _trsForward = trs.forward;
            }
            int surfaceVertexCount = sampleCount;
            if (spline.isClosed) surfaceVertexCount--;
            int vertexCount = surfaceVertexCount;
            bool pathExtrude = false;
            if (_extrudeSpline != null)
            {
                _extrudeSpline.Evaluate(ref extrudeResults, _extrudeFrom, _extrudeTo);
                pathExtrude = extrudeResults.Length > 0;
            } else if(extrudeResults.Length > 0)
            {
                extrudeResults = new SplineSample[0];
            }

            bool simpleExtrude = !pathExtrude && _extrude != 0f;

            if (pathExtrude)
            {
                vertexCount *= 2;
                vertexCount += sampleCount * extrudeResults.Length;
            }
            else if (simpleExtrude)
            {
                vertexCount *= 4;
                vertexCount += 2;
            }
            
            Vector3 center, normal;
            GetProjectedVertices(surfaceVertexCount, out center, out normal);

            bool clockwise = IsClockwise(projectedVerts);
            bool flipCap = false;
            bool flipSide = false;
            if (!clockwise) flipSide = !flipSide;
            if (simpleExtrude && _extrude < 0f)
            {
                flipCap = !flipCap;
                flipSide = !flipSide;
            }

            GenerateSurfaceTris(flipCap);
            int totalTrisCount = surfaceTris.Length;
            if (simpleExtrude)
            {
                totalTrisCount *= 2;
                totalTrisCount += 2 * sampleCount * 2 * 3;
            } else
            {
                totalTrisCount *= 2;
                totalTrisCount += extrudeResults .Length * sampleCount * 2 * 3;
            }
            AllocateMesh(vertexCount, totalTrisCount);
            Vector3 off = _trsRight * offset.x + _trsUp * offset.y + _trsForward * offset.z;
            for (int i = 0; i < surfaceVertexCount; i++)
            {
                GetSample(i, ref evalResult);
                _tsMesh.vertices[i] = evalResult.position + off;
                _tsMesh.normals[i] = evalResult.up;
                _tsMesh.colors[i] = evalResult.color * color;
            }

            #region UVs
            Vector2 min = projectedVerts[0];
            Vector2 max = projectedVerts[0];
            for (int i = 1; i < projectedVerts.Length; i++)
            {
                if (min.x < projectedVerts[i].x) min.x = projectedVerts[i].x;
                if (min.y < projectedVerts[i].y) min.y = projectedVerts[i].y;
                if (max.x > projectedVerts[i].x) max.x = projectedVerts[i].x;
                if (max.y > projectedVerts[i].y) max.y = projectedVerts[i].y;
            }

            for (int i = 0; i < projectedVerts.Length; i++)
            {
                _tsMesh.uv[i].x = Mathf.InverseLerp(max.x, min.x, projectedVerts[i].x) * uvScale.x - uvScale.x * 0.5f + uvOffset.x + 0.5f;
                _tsMesh.uv[i].y = Mathf.InverseLerp(min.y, max.y, projectedVerts[i].y) * uvScale.y - uvScale.y * 0.5f + uvOffset.y + 0.5f;
                _tsMesh.uv[i] = Quaternion.AngleAxis(uvRotation, Vector3.forward) * _tsMesh.uv[i];
            }
            #endregion


            if (flipCap)
            {
                for (int i = 0; i < surfaceVertexCount; i++)
                {
                    _tsMesh.normals[i] *= -1f;
                }
            }

            if (_expand != 0f)
            {
                for (int i = 0; i < surfaceVertexCount; i++)
                {
                    GetSample(i, ref evalResult);
                    _tsMesh.vertices[i] += (clockwise ? -evalResult.right : evalResult.right) * _expand;
                }
            }

            if (pathExtrude)
            {
                GetIdentityVerts(center, normal, clockwise);
                //Generate cap vertices with flipped normals
                for (int i = 0; i < surfaceVertexCount; i++)
                {
                    Vector3 vertexOffset = TransformOffset(extrudeResults[0], _extrudeOffset);
                    _tsMesh.vertices[i + surfaceVertexCount] = extrudeResults[0].position + (extrudeResults[0].rotation * identityVertices[i] + off) + vertexOffset;
                    _tsMesh.normals[i + surfaceVertexCount] = -extrudeResults[0].forward;
                    _tsMesh.colors[i + surfaceVertexCount] = _tsMesh.colors[i] * extrudeResults[0].color;
                    _tsMesh.uv[i + surfaceVertexCount] = new Vector2(1f - _tsMesh.uv[i].x, _tsMesh.uv[i].y);

                    vertexOffset = TransformOffset(extrudeResults[extrudeResults.Length - 1], _extrudeOffset);
                    _tsMesh.vertices[i] = extrudeResults[extrudeResults.Length - 1].position + (extrudeResults[extrudeResults.Length - 1].rotation * identityVertices[i] + off) + vertexOffset;
                    _tsMesh.normals[i] = extrudeResults[extrudeResults.Length - 1].forward;
                    _tsMesh.colors[i] *= extrudeResults[extrudeResults.Length - 1].color;
                }
                //Add wall vertices
                float totalLength = 0f;
                for (int i = 0; i < extrudeResults.Length; i++)
                {
                    if (_uniformUvs && i > 0) totalLength += Vector3.Distance(extrudeResults[i].position, extrudeResults[i - 1].position);
                    int startIndex = surfaceVertexCount * 2 + i * sampleCount;
                    for (int n = 0; n < identityVertices.Length; n++)
                    {
                        Vector3 vertexOffset = TransformOffset(extrudeResults[i], _extrudeOffset);
                        _tsMesh.vertices[startIndex + n] = extrudeResults[i].position + (extrudeResults[i].rotation * identityVertices[n] + off) + vertexOffset;
                        _tsMesh.normals[startIndex + n] = extrudeResults[i].rotation * identityNormals[n];
                        if (_uniformUvs)
                        {
                            _tsMesh.uv[startIndex + n] = new Vector2((float)n / (identityVertices.Length - 1) * _sideUvScale.x + _sideUvOffset.x, totalLength * _sideUvScale.y + _sideUvOffset.y);
                        }
                        else
                        {
                            _tsMesh.uv[startIndex + n] = new Vector2((float)n / (identityVertices.Length - 1) * _sideUvScale.x + _sideUvOffset.x, (float)i / (extrudeResults.Length - 1) * _sideUvScale.y + _sideUvOffset.y);
                        }
                        if (_sideUvRotation != 0f)
                        {
                            _tsMesh.uv[startIndex + n] = Quaternion.AngleAxis(_sideUvRotation, Vector3.forward) * _tsMesh.uv[startIndex + n];
                        }

                        if (clockwise)
                        {
                            _tsMesh.uv[startIndex + n].x = 1f - _tsMesh.uv[startIndex + n].x;
                        }
                    }
                }
                int written = WriteTris(ref surfaceTris, ref _tsMesh.triangles, 0, 0, false);
                written = WriteTris(ref surfaceTris, ref _tsMesh.triangles, surfaceVertexCount, written, true);

                MeshUtility.GeneratePlaneTriangles(ref wallTris, sampleCount - 1, extrudeResults.Length, flipSide, 0, 0, true);
                WriteTris(ref wallTris, ref _tsMesh.triangles, surfaceVertexCount * 2, written, false);
            }
            else if (simpleExtrude)
            {
                //Duplicate cap vertices with flipped normals
                for (int i = 0; i < surfaceVertexCount; i++)
                {
                    _tsMesh.vertices[i + surfaceVertexCount] = _tsMesh.vertices[i];
                    _tsMesh.normals[i + surfaceVertexCount] = -_tsMesh.normals[i];
                    _tsMesh.colors[i + surfaceVertexCount] = _tsMesh.colors[i];
                    _tsMesh.uv[i + surfaceVertexCount] = new Vector2(1f - _tsMesh.uv[i].x, _tsMesh.uv[i].y);
                    _tsMesh.vertices[i] += normal * _extrude;
                }

                //Add wall vertices
                for (int i = 0; i < surfaceVertexCount + 1; i++)
                {
                    int index = i;
                    if (i >= surfaceVertexCount) index = i - surfaceVertexCount;
                    GetSample(index, ref evalResult);
                    _tsMesh.vertices[i + surfaceVertexCount * 2] = _tsMesh.vertices[index] - normal * _extrude;
                    _tsMesh.normals[i + surfaceVertexCount * 2] = clockwise ? -evalResult.right : evalResult.right;
                    _tsMesh.colors[i + surfaceVertexCount * 2] = _tsMesh.colors[index];
                    _tsMesh.uv[i + surfaceVertexCount * 2] = new Vector2((float)i / (surfaceVertexCount - 1) * _sideUvScale.x + _sideUvOffset.x, 0f + _sideUvOffset.y);
                    if (clockwise)
                    {
                        _tsMesh.uv[i + surfaceVertexCount * 2].x = 1f - _tsMesh.uv[i + surfaceVertexCount * 2].x;
                    }

                    int offsetIndex = i + surfaceVertexCount * 3 + 1;
                    _tsMesh.vertices[offsetIndex] = _tsMesh.vertices[index];
                    _tsMesh.normals[offsetIndex] = _tsMesh.normals[i + surfaceVertexCount * 2];
                    _tsMesh.colors[offsetIndex] = _tsMesh.colors[index];
                    if (_uniformUvs)
                    {
                        _tsMesh.uv[offsetIndex] = new Vector2((float)i / surfaceVertexCount * _sideUvScale.x + _sideUvOffset.x, _extrude * _sideUvScale.y + _sideUvOffset.y);
                    }
                    else
                    {
                        _tsMesh.uv[offsetIndex] = new Vector2((float)i / surfaceVertexCount * _sideUvScale.x + _sideUvOffset.x, 1f * _sideUvScale.y + _sideUvOffset.y);
                    }
                    if (_sideUvRotation != 0f)
                    {
                        _tsMesh.uv[offsetIndex] = Quaternion.AngleAxis(_sideUvRotation, Vector3.forward) * _tsMesh.uv[offsetIndex];
                    }
                    if (clockwise)
                    {
                        _tsMesh.uv[offsetIndex].x = 1f - _tsMesh.uv[offsetIndex].x;
                    }
                }
                int written = WriteTris(ref surfaceTris, ref _tsMesh.triangles, 0, 0, false);
                written = WriteTris(ref surfaceTris, ref _tsMesh.triangles, surfaceVertexCount, written, true);

                MeshUtility.GeneratePlaneTriangles(ref wallTris, sampleCount - 1, 2, flipSide, 0, 0, true);
                WriteTris(ref wallTris, ref _tsMesh.triangles, surfaceVertexCount * 2, written, false);
            }
            else
            {
                WriteTris(ref surfaceTris, ref _tsMesh.triangles, 0, 0, false);
            }
        }

        private void GenerateSurfaceTris(bool flip)
        {
            MeshUtility.Triangulate(projectedVerts, ref surfaceTris);
            if (flip) MeshUtility.FlipTriangles(ref surfaceTris);
        }

        private int WriteTris(ref int[] tris, ref int[] target, int vertexOffset, int trisOffset, bool flip)
        {
            for (int i = trisOffset; i < trisOffset + tris.Length; i += 3)
            {
                if (flip)
                {
                    target[i] = tris[i + 2 - trisOffset] + vertexOffset;
                    target[i + 1] = tris[i + 1 - trisOffset] + vertexOffset;
                    target[i + 2] = tris[i - trisOffset] + vertexOffset;
                }
                else
                {
                    target[i] = tris[i - trisOffset] + vertexOffset;
                    target[i + 1] = tris[i + 1 - trisOffset] + vertexOffset;
                    target[i + 2] = tris[i + 2 - trisOffset] + vertexOffset;
                }
            }
            return trisOffset + tris.Length;
        }

        bool IsClockwise(Vector2[] points2D)
        {
            float sum = 0f;
            for (int i = 1; i < points2D.Length; i++)
            {
                Vector2 v1 = points2D[i];
                Vector2 v2 = points2D[(i + 1) % points2D.Length];
                sum += (v2.x - v1.x) * (v2.y + v1.y);
            }
            sum += (points2D[0].x - points2D[points2D.Length - 1].x) * (points2D[0].y + points2D[points2D.Length - 1].y);
            return sum <= 0f;
        }

        void GetIdentityVerts(Vector3 center, Vector3 normal, bool clockwise)
        {
            Quaternion vertsRotation = Quaternion.Inverse(Quaternion.LookRotation(normal));
            if (identityVertices.Length != sampleCount)
            {
                identityVertices = new Vector3[sampleCount];
                identityNormals = new Vector3[sampleCount];
            }
            for (int i = 0; i < sampleCount; i++)
            {
                GetSampleRaw(i, ref evalResult);
                Vector3 right = evalResult.right;
                identityVertices[i] = vertsRotation * (evalResult.position - center + (clockwise ? -right : right) * _expand);
                identityNormals[i] = vertsRotation * (clockwise ? -right : right);
            }
        }

        void GetProjectedVertices(int count, out Vector3 center, out Vector3 normal)
        {
            center = Vector3.zero;
            normal = Vector3.zero;
            Vector3 off = _trsRight * offset.x + _trsUp * offset.y + _trsForward * offset.z;
            for (int i = 0; i < count; i++)
            {
                GetSampleRaw(i, ref evalResult);
                center += evalResult.position + off;
                normal += evalResult.up;
            }
            normal.Normalize();
            center /= count;

            Quaternion rot = Quaternion.LookRotation(normal, Vector3.up);
            Vector3 up = rot * Vector3.up;
            Vector3 right = rot * Vector3.right;
            if (projectedVerts.Length != count) projectedVerts = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                GetSampleRaw(i, ref evalResult);
                Vector3 point = evalResult.position + off - center;
                float projectionPointX = Vector3.Project(point, right).magnitude;
                if (Vector3.Dot(point, right) < 0.0f) projectionPointX *= -1f;
                float projectionPointY = Vector3.Project(point, up).magnitude;
                if (Vector3.Dot(point, up) < 0.0f) projectionPointY *= -1f;
                projectedVerts[i].x = projectionPointX;
                projectedVerts[i].y = projectionPointY;
            }
        }

    }
}
