using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Dreamteck.Splines
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Dreamteck/Splines/Users/Spline Mesh")]
    public partial class SplineMesh : MeshGenerator
    {
        //Mesh data
        [SerializeField]
        [HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("channels")]
        private List<Channel> _channels = new List<Channel>();
        private bool _useLastResult = false;
        private List<TS_Mesh> _combineMeshes = new List<TS_Mesh>();

        protected override string meshName => "Custom Mesh";

        private Matrix4x4 _vertexMatrix = new Matrix4x4();
        private Matrix4x4 _normalMatrix = new Matrix4x4();
        private SplineSample _lastResult = new SplineSample();

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            for (int i = 0; i < _channels.Count; i++)
            {
                for (int j = 0; j < _channels[i].GetMeshCount(); j++)
                {
                    _channels[i].GetMesh(j).Refresh();
                }
            }
#endif
        }

        protected override void Reset()
        {
            base.Reset();
            AddChannel("Channel 1");
        }

        public void RemoveChannel(int index)
        {
            _channels.RemoveAt(index);
            Rebuild();
        }

        public void SwapChannels(int a, int b)
        {
            if (a < 0 || a >= _channels.Count || b < 0 || b >= _channels.Count) return;
            Channel temp = _channels[b];
            _channels[b] = _channels[a];
            _channels[a] = temp;
            Rebuild();
        }

        public Channel AddChannel(Mesh inputMesh, string name)
        {
            Channel channel = new Channel(name, inputMesh, this);
            _channels.Add(channel);
            return channel;
        }

        public Channel AddChannel(string name)
        {
            Channel channel = new Channel(name, this);
            _channels.Add(channel);
            return channel;
        }

        public int GetChannelCount()
        {
            return _channels.Count;
        }

        public Channel GetChannel(int index)
        {
            return _channels[index];
        }


        protected override void BuildMesh()
        {
            base.BuildMesh();
            Generate();
        }

        private void Generate()
        {
            int meshCount = 0;
            for (int i = 0; i < _channels.Count; i++)
            {
                if (_channels[i].GetMeshCount() == 0) continue;

                if (_channels[i].autoCount)
                {
                    float avgBounds = 0f;
                    for (int j = 0; j < _channels[i].GetMeshCount(); j++)
                    {
                        avgBounds += _channels[i].GetMesh(j).bounds.size.z;
                    }

                    if (_channels[i].GetMeshCount() > 1)
                    {
                        avgBounds /= _channels[i].GetMeshCount();
                    }

                    if (avgBounds > 0f)
                    {
                        float length = CalculateLength(_channels[i].clipFrom, _channels[i].clipTo, false);
                        int newCount = Mathf.RoundToInt(length / avgBounds);
                        if (newCount < 1)
                        {
                            newCount = 1;
                        }
                        _channels[i].count = newCount;
                    }
                }

                meshCount += _channels[i].count;
            }

            if(meshCount == 0)
            {
                _tsMesh.Clear();
                return;
            }

            if (_combineMeshes.Count < meshCount)
            {
                _combineMeshes.AddRange(new TS_Mesh[meshCount - _combineMeshes.Count]);
            }
            else if (_combineMeshes.Count > meshCount)
            {
                _combineMeshes.RemoveRange((_combineMeshes.Count - 1) - (_combineMeshes.Count - meshCount), _combineMeshes.Count - meshCount);
            }

            int combineMeshIndex = 0;
            for (int i = 0; i < _channels.Count; i++)
            {
                if (_channels[i].GetMeshCount() == 0) continue;
                _channels[i].ResetIteration();
                _useLastResult = false;
                double step = 1.0 / _channels[i].count;
                double space = step * _channels[i].spacing * 0.5;
                
                switch (_channels[i].type)
                {
                    case Channel.Type.Extrude:
                        for (int j = 0; j < _channels[i].count; j++)
                        {
                            double from = DMath.Lerp(_channels[i].clipFrom, _channels[i].clipTo, j * step + space);
                            double to = DMath.Lerp(_channels[i].clipFrom, _channels[i].clipTo, j * step + step - space);
                            if (_combineMeshes[combineMeshIndex] == null)
                            {
                                _combineMeshes[combineMeshIndex] = new TS_Mesh();
                            }
                            Extrude(_channels[i], _combineMeshes[combineMeshIndex], from, to);
                            combineMeshIndex++;
                        }
                        if (space == 0f) _useLastResult = true;
                        break;
                    case Channel.Type.Place:
                        for (int j = 0; j < _channels[i].count; j++)
                        {
                            if (_combineMeshes[combineMeshIndex] == null)
                            {
                                _combineMeshes[combineMeshIndex] = new TS_Mesh();
                            }
                            Place(_channels[i], _combineMeshes[combineMeshIndex], DMath.Lerp(_channels[i].clipFrom, _channels[i].clipTo, (double)j / Mathf.Max(_channels[i].count - 1, 1)));
                            combineMeshIndex++;
                        }
                        break;
                   
                }
            }
            _tsMesh.Combine(_combineMeshes);
        }

        private void Place(Channel channel, TS_Mesh target, double percent)
        {
            Channel.MeshDefinition definition = channel.NextMesh();
            if (target == null) target = new TS_Mesh();
            definition.Write(target, channel.overrideMaterialID ? channel.targetMaterialID : -1);
            Vector2 channelOffset = channel.NextRandomOffset();
            Quaternion channelRotation = channel.NextRandomQuaternion();

            var customValues = channel.GetCustomPlaceValues(percent);

            Vector2 finalOffset = channelOffset + customValues.Item1 + new Vector2(offset.x, offset.y);
            Quaternion finalRotation = channelRotation * Quaternion.AngleAxis(rotation, Vector3.forward) * customValues.Item2;
            Vector3 finalScale = channel.NextPlaceScale();

            Evaluate(percent, ref evalResult);
            Vector3 originalNormal = evalResult.up;
            Vector3 originalRight = evalResult.right;
            Vector3 originalDirection = evalResult.forward;
            if (channel.overrideNormal)
            {
                evalResult.forward = Vector3.Cross(evalResult.right, channel.customNormal);
                evalResult.up = channel.customNormal;
            }

            if (!channel.scaleModifier.useClippedPercent)
            {
                UnclipPercent(ref evalResult.percent);
            }
            Vector3 scaleMod = channel.scaleModifier.GetScale(evalResult);
            finalScale.x *= customValues.Item3.x * scaleMod.x;
            finalScale.y *= customValues.Item3.y * scaleMod.y;
            finalScale.z *= customValues.Item3.z * scaleMod.z;

            if (!channel.scaleModifier.useClippedPercent)
            {
                ClipPercent(ref evalResult.percent);
            }

            float resultSize = GetBaseSize(evalResult);
            _vertexMatrix.SetTRS(evalResult.position + originalRight * (finalOffset.x * resultSize) + originalNormal * (finalOffset.y * resultSize) + originalDirection * offset.z, //Position
                evalResult.rotation * finalRotation, //Rotation
                finalScale * resultSize ); //Scale
            _normalMatrix = _vertexMatrix.inverse.transpose;

            for (int i = 0; i < target.vertexCount; i++)
            {
                target.vertices[i] = _vertexMatrix.MultiplyPoint3x4(definition.vertices[i]);
                target.normals[i] = _normalMatrix.MultiplyVector(definition.normals[i]);
            }
            for (int i = 0; i < Mathf.Min(target.colors.Length, definition.colors.Length); i++)
            {
                target.colors[i] = definition.colors[i] * evalResult.color * color;
            }
        }

        private void Extrude(Channel channel, TS_Mesh target, double from, double to)
        {
            Channel.MeshDefinition definition = channel.NextMesh();
            if (target == null) target = new TS_Mesh();
            definition.Write(target, channel.overrideMaterialID ? channel.targetMaterialID : -1);
            Vector2 uv = Vector2.zero;
            Vector3 trsVector = Vector3.zero;

            Vector3 channelOffset = channel.NextRandomOffset();
            Vector3 channelScale = channel.NextRandomScale();
            float channelRotation = channel.NextRandomAngle();

            for (int i = 0; i < definition.vertexGroups.Count; i++)
            {
                if (_useLastResult && i == definition.vertexGroups.Count)
                {
                    evalResult = _lastResult;
                }
                else
                {
                    Evaluate(DMath.Lerp(from, to, definition.vertexGroups[i].percent), ref evalResult);
                }

                Vector3 originalNormal = evalResult.up;
                Vector3 originalRight = evalResult.right;
                Vector3 originalDirection = evalResult.forward;
                if (channel.overrideNormal)
                {
                    evalResult.forward = Vector3.Cross(evalResult.right, channel.customNormal);
                    evalResult.up = channel.customNormal;
                }
                var customValues = channel.GetCustomExtrudeValues(evalResult.percent);
                Vector3 finalOffset = offset + channelOffset + (Vector3)customValues.Item1;
                float finalRotation = rotation + channelRotation + customValues.Item2;
                Vector3 finalScale = channelScale;
                if (!channel.scaleModifier.useClippedPercent)
                {
                    UnclipPercent(ref evalResult.percent);
                }
                Vector2 scaleMod = channel.scaleModifier.GetScale(evalResult);
                if (!channel.scaleModifier.useClippedPercent)
                {
                    ClipPercent(ref evalResult.percent);
                }
                finalScale.x *= customValues.Item3.x * scaleMod.x;
                finalScale.y *= customValues.Item3.y * scaleMod.y;
                finalScale.z = 1f;
                float resultSize = evalResult.size;
                _vertexMatrix.SetTRS(evalResult.position + originalRight * (finalOffset.x * resultSize) + originalNormal * (finalOffset.y * resultSize) + originalDirection * offset.z, //Position
                    evalResult.rotation * Quaternion.AngleAxis(finalRotation, Vector3.forward), //Rotation
                    finalScale * resultSize); //Scale
                _normalMatrix = _vertexMatrix.inverse.transpose;
                if (i == 0)
                {
                    _lastResult = evalResult;
                }

                for (int n = 0; n < definition.vertexGroups[i].ids.Length; n++)
                {
                    int index = definition.vertexGroups[i].ids[n];
                    trsVector = definition.vertices[index];
                    trsVector.z = 0f;
                    target.vertices[index] = _vertexMatrix.MultiplyPoint3x4(trsVector);
                    trsVector = definition.normals[index];
                    target.normals[index] = _normalMatrix.MultiplyVector(trsVector);
                    target.colors[index] = target.colors[index] * evalResult.color * color;
                    if (target.uv.Length > index)
                    {
                        uv = target.uv[index];
                        switch (channel.overrideUVs)
                        {
                            case Channel.UVOverride.ClampU: uv.x = (float)evalResult.percent; break;
                            case Channel.UVOverride.ClampV: uv.y = (float)evalResult.percent; break;
                            case Channel.UVOverride.UniformU: uv.x = CalculateLength(0.0, evalResult.percent); break;
                            case Channel.UVOverride.UniformV: uv.y = CalculateLength(0.0, evalResult.percent); break;
                        }
                        target.uv[index] = new Vector2(uv.x * uvScale.x * channel.uvScale.x, uv.y * uvScale.y * channel.uvScale.y);
                        target.uv[index] += uvOffset + channel.uvOffset;
                    }
                }
            }
        }
    }
}
