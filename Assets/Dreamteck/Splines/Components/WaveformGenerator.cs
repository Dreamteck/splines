using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Users/Waveform Generator")]
    public class WaveformGenerator : MeshGenerator
    {
        public enum Axis { X, Y, Z }
        public enum Space { World, Local }
        public enum UVWrapMode { Clamp, UniformX, UniformY, Uniform }

        public Axis axis
        {
            get { return _axis; }
            set
            {
                if (value != _axis)
                {
                    _axis = value;
                    Rebuild();
                }
            }
        }

        public bool symmetry
        {
            get { return _symmetry; }
            set
            {
                if (value != _symmetry)
                {
                    _symmetry = value;
                    Rebuild();
                }
            }
        }

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

        public int slices
        {
            get { return _slices; }
            set
            {
                if (value != _slices)
                {
                    if (value < 1) value = 1;
                    _slices = value;
                    Rebuild();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private Axis _axis = Axis.Y;
        [SerializeField]
        [HideInInspector]
        private bool _symmetry = false;
        [SerializeField]
        [HideInInspector]
        private UVWrapMode _uvWrapMode = UVWrapMode.Clamp;
        [SerializeField]
        [HideInInspector]
        private int _slices = 1;

        protected override string meshName => "Waveform";

        protected override void BuildMesh()
        {
            base.BuildMesh();
            Generate();
        }

        protected override void Build()
        {
            base.Build();
        }

        protected override void LateRun()
        {
            base.LateRun();
        }

        private void Generate()
        {
            int vertexCount = sampleCount * (_slices + 1);
            AllocateMesh(vertexCount, _slices * (sampleCount - 1) * 6);
            int vertIndex = 0;
            float avgTop = 0f;
            float totalLength = 0f;
            Vector3 computerPosition = spline.position;
            Vector3 normal = spline.TransformDirection(Vector3.right);
            switch (_axis)
            {
                case Axis.Y: normal = spline.TransformDirection(Vector3.up); break;
                case Axis.Z: normal = spline.TransformDirection(Vector3.forward); break;
            }

            Vector3 lastPosition = Vector3.zero;
            for (int i = 0; i < sampleCount; i++)
            {
                GetSample(i, ref evalResult);
                float resultSize = GetBaseSize(evalResult);
                Vector3 samplePosition = evalResult.position;
                Vector3 localSamplePosition = spline.InverseTransformPoint(samplePosition);
                Vector3 bottomPosition = localSamplePosition;
                Vector3 sampleDirection = evalResult.forward;
                Vector3 sampleNormal = evalResult.up;

                float heightPercent = 1f;
                if (_uvWrapMode == UVWrapMode.UniformX || _uvWrapMode == UVWrapMode.Uniform)
                {
                    if (i > 0)
                    {
                        totalLength += Vector3.Distance(evalResult.position, lastPosition);
                    }
                }
                switch (_axis)
                {
                    case Axis.X: bottomPosition.x = _symmetry ? -localSamplePosition.x : 0f;  heightPercent = uvScale.y * Mathf.Abs(localSamplePosition.x); avgTop += localSamplePosition.x; break;
                    case Axis.Y: bottomPosition.y = _symmetry ? -localSamplePosition.y : 0f;  heightPercent = uvScale.y * Mathf.Abs(localSamplePosition.y); avgTop += localSamplePosition.y; break;
                    case Axis.Z: bottomPosition.z = _symmetry ? -localSamplePosition.z : 0f;  heightPercent = uvScale.y * Mathf.Abs(localSamplePosition.z); avgTop += localSamplePosition.z; break;
                }
                bottomPosition = spline.TransformPoint(bottomPosition);
                Vector3 right = Vector3.Cross(normal, sampleDirection).normalized;
                Vector3 offsetRight = Vector3.Cross(sampleNormal, sampleDirection);
                
                for (int n = 0; n < _slices + 1; n++)
                {
                    float slicePercent = ((float)n / _slices);
                    _tsMesh.vertices[vertIndex] = Vector3.Lerp(bottomPosition, samplePosition, slicePercent) + normal * (offset.y * resultSize) + offsetRight * (offset.x * resultSize);
                    _tsMesh.normals[vertIndex] = right;
                    switch (_uvWrapMode)
                    {
                        case UVWrapMode.Clamp: _tsMesh.uv[vertIndex] = new Vector2((float)evalResult.percent * uvScale.x + uvOffset.x, slicePercent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.UniformX: _tsMesh.uv[vertIndex] = new Vector2(totalLength * uvScale.x + uvOffset.x, slicePercent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.UniformY: _tsMesh.uv[vertIndex] = new Vector2((float)evalResult.percent * uvScale.x + uvOffset.x, heightPercent * slicePercent * uvScale.y + uvOffset.y); break;
                        case UVWrapMode.Uniform: _tsMesh.uv[vertIndex] = new Vector2(totalLength * uvScale.x + uvOffset.x, heightPercent * slicePercent * uvScale.y + uvOffset.y); break;
                    }
                    _tsMesh.colors[vertIndex] = GetBaseColor(evalResult) * color;
                    vertIndex++;
                }
                lastPosition = evalResult.position;
            }
            if (sampleCount > 0) avgTop /= sampleCount;
            MeshUtility.GeneratePlaneTriangles(ref _tsMesh.triangles, _slices, sampleCount, avgTop < 0f);
        }
    }
}
