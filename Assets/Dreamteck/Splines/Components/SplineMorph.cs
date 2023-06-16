namespace Dreamteck.Splines
{
    using UnityEngine;
    [AddComponentMenu("Dreamteck/Splines/Morph")]
    public class SplineMorph : MonoBehaviour
    {
        [HideInInspector]
        public SplineComputer.Space space = SplineComputer.Space.Local;
        [HideInInspector]
        public bool cycle = false;
        public enum CycleMode {Default, Loop, PingPong}
        public enum UpdateMode {Update, FixedUpdate, LateUpdate}
        [HideInInspector]
        public CycleMode cycleMode = CycleMode.Default;
        [HideInInspector]
        public UpdateMode cycleUpdateMode = UpdateMode.Update;
        [HideInInspector]
        public float cycleDuration = 1f;
        public SplineComputer spline
        {
            get { return _spline; }
            set
            {
                if (Application.isPlaying)
                {
                    if(channels.Length > 0 && value.pointCount != channels[0].points.Length)
                    {
                        value.SetPoints(channels[0].points, space);
                    }
                }
                _spline = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        private SplineComputer _spline;
        private SplinePoint[] points = new SplinePoint[0];
        private float cycleValue = 0f;
        private short cycleDirection = 1;


        [System.Serializable]
        public class Channel
        {
            public enum Interpolation { Linear, Spherical }
            [SerializeField]
            internal SplinePoint[] points = new SplinePoint[0];
            [SerializeField]
            internal float percent = 1f;
            public string name = "";
            public AnimationCurve curve;
            public Interpolation interpolation = Interpolation.Linear;
        }
        [HideInInspector]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("morphStates")]
        private Channel[] channels = new Channel[0];

        private void Reset()
        {
            spline = GetComponent<SplineComputer>();
        }

        private void Update()
        {
            if (cycleUpdateMode == UpdateMode.Update) RunUpdate();
        }

        private void FixedUpdate()
        {
            if (cycleUpdateMode == UpdateMode.FixedUpdate) RunUpdate();
        }

        private void LateUpdate()
        {
            if (cycleUpdateMode == UpdateMode.LateUpdate) RunUpdate();
        }

        void RunUpdate()
        {
            if (!cycle) return;
            if (cycleMode != CycleMode.PingPong) cycleDirection = 1;
            cycleValue += Time.deltaTime / cycleDuration * cycleDirection;
            switch (cycleMode)
            {
                case CycleMode.Default:
                    if (cycleValue > 1f) cycleValue = 1f;
                    break;
                case CycleMode.Loop:
                    if (cycleValue > 1f) cycleValue -= Mathf.Floor(cycleValue);
                    break;
                case CycleMode.PingPong:
                    if (cycleValue > 1f)
                    {
                        cycleValue = 1f - (cycleValue - Mathf.Floor(cycleValue));
                        cycleDirection = -1;
                    } else if (cycleValue < 0f)
                    {
                        cycleValue = -cycleValue - Mathf.Floor(-cycleValue);
                        cycleDirection = 1;
                    }
                    break;
            }
            SetWeight(cycleValue, cycleMode == CycleMode.Loop);
        }

        public void SetCycle(float value)
        {
            cycleValue = Mathf.Clamp01(value);
        }

        public void SetWeight(int index, float weight)
        {
            channels[index].percent = Mathf.Clamp01(weight);
            UpdateMorph();
        }

        public void SetWeight(string name, float weight)
        {
            int index = GetChannelIndex(name);
            channels[index].percent = Mathf.Clamp01(weight);
            UpdateMorph();
        }

        public void SetWeight(float percent, bool loop = false)
        {
            float channelValue = percent * (loop ? channels.Length : channels.Length - 1);
            for (int i = 0; i < channels.Length; i++)
            {
                float delta = Mathf.Abs(i - channelValue);
                if (delta > 1f)
                {
                    SetWeight(i, 0f);
                }
                else
                {
                    if (channelValue <= i)
                    {
                        SetWeight(i, 1f - (i - channelValue));
                    }
                    else
                    {
                        SetWeight(i, 1f - (channelValue - i));
                    }
                }
            }
            if (loop && channelValue >= channels.Length - 1)
            {
                SetWeight(0, channelValue - (channels.Length - 1));
            }
        }

        public void CaptureSnapshot(string name)
        {
            CaptureSnapshot(GetChannelIndex(name));
        }

        public void CaptureSnapshot(int index)
        {
            if (_spline == null) return;
            if ((channels.Length > 0 && _spline.pointCount != channels[0].points.Length && index != 0))
            {
                Debug.LogError("Point count must be the same as " + _spline.pointCount);
                return;
            }
            channels[index].points = _spline.GetPoints(space);
            UpdateMorph();
        }

        public void Clear()
        {
            channels = new Channel[0];
        }

        public SplinePoint[] GetSnapshot(int index)
        {
            return channels[index].points;
        }
        public void SetSnapshot(int index, SplinePoint[] points)
        {
            channels[index].points = points;
        }


        public SplinePoint[] GetSnapshot(string name)
        {
            int index = GetChannelIndex(name);
            return channels[index].points;
        }

        public float GetWeight(int index)
        {
            return channels[index].percent;
        }

        public float GetWeight(string name)
        {
            int index = GetChannelIndex(name);
            return channels[index].percent;
        }

        public void AddChannel(string name)
        {
            if (_spline == null) return;
            if (channels.Length > 0 && _spline.pointCount != channels[0].points.Length)
            {
                Debug.LogError("Point count must be the same as " + channels[0].points.Length);
                return;
            }
            Channel newMorph = new Channel();
            newMorph.points = _spline.GetPoints(space);
            newMorph.name = name;
            newMorph.curve = new AnimationCurve();
            newMorph.curve.AddKey(new Keyframe(0, 0, 0, 1));
            newMorph.curve.AddKey(new Keyframe(1, 1, 1, 0));
            ArrayUtility.Add(ref channels, newMorph);
            UpdateMorph();
        }

        public void RemoveChannel(string name)
        {
            int index = GetChannelIndex(name);
            RemoveChannel(index);
        }

        public void RemoveChannel(int index)
        {
            if (index < 0 || index >= channels.Length) return;
            Channel[] newStates = new Channel[channels.Length - 1];
            for (int i = 0; i < channels.Length; i++)
            {
                if (i == index) continue;
                else if (i < index) newStates[i] = channels[i];
                else if (i >= index) newStates[i - 1] = channels[i];
            }
            channels = newStates;
            UpdateMorph();
        }

        private int GetChannelIndex(string name)
        {
            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i].name == name)
                {
                    return i;
                }
            }
            Debug.Log("Channel not found " + name);
            return 0;
        }

        public int GetChannelCount()
        {
            if (channels == null) return 0;
            return channels.Length;
        }

        public Channel GetChannel(int index)
        {
            return channels[index];
        }

        public Channel GetChannel(string name)
        {
            return channels[GetChannelIndex(name)];
        }

        public void UpdateMorph()
        {
            if (_spline == null) return;
            if (channels.Length == 0) return;
            if(points.Length != channels[0].points.Length)
            {
                points = new SplinePoint[channels[0].points.Length];
            }

            for (int i = 0; i < channels.Length; i++)
            {
                for (int j = 0; j < points.Length; j++)
                {
                    if(i == 0)
                    {
                        points[j] = channels[0].points[j];
                        continue;
                    }

                    float percent = channels[i].curve.Evaluate(channels[i].percent);
                    if (channels[i].interpolation == Channel.Interpolation.Linear)
                    {
                        points[j].position += (channels[i].points[j].position - channels[0].points[j].position) * percent;
                        points[j].tangent += (channels[i].points[j].tangent - channels[0].points[j].tangent) * percent;
                        points[j].tangent2 += (channels[i].points[j].tangent2 - channels[0].points[j].tangent2) * percent;
                        points[j].normal += (channels[i].points[j].normal - channels[0].points[j].normal) * percent;
                    } else
                    {
                        points[j].position = Vector3.Slerp(points[j].position, points[j].position + (channels[i].points[j].position - channels[0].points[j].position), percent);
                        points[j].tangent = Vector3.Slerp(points[j].tangent, points[j].tangent + (channels[i].points[j].tangent - channels[0].points[j].tangent), percent);
                        points[j].tangent2 = Vector3.Slerp(points[j].tangent2, points[j].tangent2 + (channels[i].points[j].tangent2 - channels[0].points[j].tangent2), percent);
                        points[j].normal = Vector3.Slerp(points[j].normal, points[j].normal + (channels[i].points[j].normal - channels[0].points[j].normal), percent);
                    }

                    points[j].color += (channels[i].points[j].color - channels[0].points[j].color) * percent;
                    points[j].size += (channels[i].points[j].size - channels[0].points[j].size) * percent;

                    if(points[j].type == SplinePoint.Type.SmoothMirrored) points[j].type = channels[i].points[j].type;
                    else if(points[j].type == SplinePoint.Type.SmoothFree)
                    {
                        if (channels[i].points[j].type == SplinePoint.Type.Broken) points[j].type = SplinePoint.Type.Broken;
                    }
                }
            }

            for (int i = 0; i < points.Length; i++)
            {
                points[i].normal.Normalize();
            }
            _spline.SetPoints(points, space);
        }
    }
}
