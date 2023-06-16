using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dreamteck.Splines
{
    [AddComponentMenu("Dreamteck/Splines/Users/Object Controller")]
    public class ObjectController : SplineUser
    {
        [System.Serializable]
        internal class ObjectControl
        {
            public bool isNull
            {
                get
                {
                    return gameObject == null;
                }
            }
            public Transform transform
            {
                get {
                    if (gameObject == null) return null;
                    return gameObject.transform;  
                }
            }
            public GameObject gameObject;
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;
            public bool active = true;

            public Vector3 baseScale = Vector3.one;

            public ObjectControl(GameObject input)
            {
                gameObject = input;
                baseScale = gameObject.transform.localScale;
            }

            public void Destroy()
            {
                if (gameObject == null) return;
                GameObject.Destroy(gameObject);
            }

            public void DestroyImmediate()
            {
                if (gameObject == null) return;
                GameObject.DestroyImmediate(gameObject);
            }

            public void Apply()
            {
                if (gameObject == null) return;
                transform.position = position;
                transform.rotation = rotation;
                transform.localScale = scale;
                gameObject.SetActive(active);
            }

        }

        public enum ObjectMethod { Instantiate, GetChildren }
        public enum Positioning { Stretch, Clip }
        public enum Iteration { Ordered, Random }

        [SerializeField]
        [HideInInspector]
        public GameObject[] objects = new GameObject[0];

        public ObjectMethod objectMethod
        {
            get { return _objectMethod; }
            set
            {
                if (value != _objectMethod)
                {
                    if (value == ObjectMethod.GetChildren)
                    {
                        _objectMethod = value;
                        Spawn();
                    }
                    else _objectMethod = value;
                }
            }
        }

        public int spawnCount
        {
            get { return _spawnCount; }
            set
            {
                if (value != _spawnCount)
                {
                    if (value < 0) value = 0;
                    if (_objectMethod == ObjectMethod.Instantiate)
                    {
                        if (value < _spawnCount)
                        {
                            _spawnCount = value;
                            Remove();
                        }
                        else
                        {
                            _spawnCount = value;
                            Spawn();
                        }
                    }
                    else _spawnCount = value;
                }
            }
        }

        public Positioning objectPositioning
        {
            get { return _objectPositioning; }
            set
            {
                if (value != _objectPositioning)
                {
                    _objectPositioning = value;
                    Rebuild();
                }
            }
        }

        public Iteration iteration
        {
            get { return _iteration; }
            set
            {
                if (value != _iteration)
                {
                    _iteration = value;
                    Rebuild();
                }
            }
        }

#if UNITY_EDITOR
        public bool retainPrefabInstancesInEditor
        {
            get { return _retainPrefabInstancesInEditor; }
            set
            {
                if (value != _retainPrefabInstancesInEditor)
                {
                    _retainPrefabInstancesInEditor = value;
                    Clear();
                    Spawn();
                    Rebuild();
                }
            }
        }
#endif

        public int randomSeed
        {
            get { return _randomSeed; }
            set
            {
                if (value != _randomSeed)
                {
                    _randomSeed = value;
                    Rebuild();
                }
            }
        }

        public Vector3 minOffset
        {
            get { return _minOffset; }
            set
            {
                if (value != _minOffset)
                {
                    _minOffset = value;
                    Rebuild();
                }
            }
        }

        public Vector3 maxOffset
        {
            get { return _maxOffset; }
            set
            {
                if (value != _maxOffset)
                {
                    _maxOffset = value;
                    Rebuild();
                }
            }
        }

        public bool offsetUseWorldCoords
        {
            get { return _offsetUseWorldCoords; }
            set
            {
                if (value != _offsetUseWorldCoords)
                {
                    _offsetUseWorldCoords = value;
                    Rebuild();
                }
            }
        }

        public Vector3 minRotation
        {
            get { return _minRotation; }
            set
            {
                if (value != _minRotation)
                {
                    _minRotation = value;
                    Rebuild();
                }
            }
        }

        public Vector3 maxRotation
        {
            get { return _maxRotation; }
            set
            {
                if (value != _maxRotation)
                {
                    _maxRotation = value;
                    Rebuild();
                }
            }
        }

        public Vector3 rotationOffset
        {
            get { return (_maxRotation+_minRotation)/2f; }
            set
            {
                if (value != _minRotation || value != _maxRotation)
                {
                    _minRotation = _maxRotation = value;
                    Rebuild();
                }
            }
        }

        public Vector3 minScaleMultiplier
        {
            get { return _minScaleMultiplier; }
            set
            {
                if (value != _minScaleMultiplier)
                {
                    _minScaleMultiplier = value;
                    Rebuild();
                }
            }
        }

        public Vector3 maxScaleMultiplier
        {
            get { return _maxScaleMultiplier; }
            set
            {
                if (value != _maxScaleMultiplier)
                {
                    _maxScaleMultiplier = value;
                    Rebuild();
                }
            }
        }

        public bool uniformScaleLerp
        {
            get { return _uniformScaleLerp; }
            set
            {
                if(value != _uniformScaleLerp)
                {
                    _uniformScaleLerp = value;
                    Rebuild();
                }
            }
        }

        public bool shellOffset
        {
            get { return _shellOffset; }
            set
            {
                if (value != _shellOffset)
                {
                    _shellOffset = value;
                    Rebuild();
                }
            }
        }

        public bool applyRotation
        {
            get { return _applyRotation; }
            set
            {
                if (value != _applyRotation)
                {
                    _applyRotation = value;
                    Rebuild();
                }
            }
        }

        public bool rotateByOffset
        {
            get { return _rotateByOffset; }
            set
            {
                if (value != _rotateByOffset)
                {
                    _rotateByOffset = value;
                    Rebuild();
                }
            }
        }

        public bool applyScale
        {
            get { return _applyScale; }
            set
            {
                if (value != _applyScale)
                {
                    _applyScale = value;
                    Rebuild();
                }
            }
        }

        public float evaluateOffset
        {
            get { return _evaluateOffset; }
            set
            {
                if (value != _evaluateOffset)
                {
                    _evaluateOffset = value;
                    Rebuild();
                }
            }
        }

        public float minObjectDistance
        {
            get { return _minObjectDistance; }
            set
            {
                if (value != _minObjectDistance)
                {
                    _minObjectDistance = value;
                    Rebuild();
                }
            }
        }

        public float maxObjectDistance
        {
            get { return _maxObjectDistance; }
            set
            {
                if (value != _maxObjectDistance)
                {
                    _maxObjectDistance = value;
                    Rebuild();
                }
            }
        }

        public ObjectControllerCustomRuleBase customOffsetRule
        {
            get { return _customOffsetRule; }
            set
            {
                if (value != _customOffsetRule)
                {
                    _customOffsetRule = value;
                    Rebuild();
                }
            }
        }

        public ObjectControllerCustomRuleBase customRotationRule
        {
            get { return _customRotationRule; }
            set
            {
                if (value != _customRotationRule)
                {
                    _customRotationRule = value;
                    Rebuild();
                }
            }
        }

        public ObjectControllerCustomRuleBase customScaleRule
        {
            get { return _customScaleRule; }
            set
            {
                if (value != _customScaleRule)
                {
                    _customScaleRule = value;
                    Rebuild();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private float _evaluateOffset = 0f;
        [SerializeField]
        [HideInInspector]
        private int _spawnCount = 0;
#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        private bool _retainPrefabInstancesInEditor = true;
#endif
        [SerializeField]
        [HideInInspector]
        private Positioning _objectPositioning = Positioning.Stretch;
        [SerializeField]
        [HideInInspector]
        private Iteration _iteration = Iteration.Ordered;
        [SerializeField]
        [HideInInspector]
        private int _randomSeed = 1;
        [SerializeField]
        [HideInInspector]
        private Vector3 _minOffset = Vector3.zero;
        [SerializeField]
        [HideInInspector]
        private Vector3 _maxOffset = Vector3.zero;
        [SerializeField]
        [HideInInspector]
        private bool _offsetUseWorldCoords = false;
        [SerializeField]
        [HideInInspector]
        private Vector3 _minRotation = Vector3.zero;
        [SerializeField]
        [HideInInspector]
        private Vector3 _maxRotation = Vector3.zero;
        [SerializeField]
        [HideInInspector]
        private bool _uniformScaleLerp = true;
        [SerializeField]
        [HideInInspector]
        private Vector3 _minScaleMultiplier = Vector3.one;
        [SerializeField]
        [HideInInspector]
        private Vector3 _maxScaleMultiplier = Vector3.one;
        [SerializeField]
        [HideInInspector]
        private bool _shellOffset = false;
        [SerializeField]
        [HideInInspector]
        private bool _applyRotation = true;
        [SerializeField]
        [HideInInspector]
        private bool _rotateByOffset = false;
        [SerializeField]
        [HideInInspector]
        private bool _applyScale = false;
        [SerializeField]
        [HideInInspector]
        private ObjectMethod _objectMethod = ObjectMethod.Instantiate;
        [HideInInspector]
        public bool delayedSpawn = false;
        [HideInInspector]
        public float spawnDelay = 0.1f;
        [SerializeField]
        [HideInInspector]
        private int lastChildCount = 0;
        [SerializeField]
        [HideInInspector]
        private ObjectControl[] spawned = new ObjectControl[0];
        [SerializeField]
        [HideInInspector]
        private bool _useCustomObjectDistance = false;
        [SerializeField]
        [HideInInspector]
        private float _minObjectDistance = 0f;
        [SerializeField]
        [HideInInspector]
        private float _maxObjectDistance = 0f;

        [SerializeField]
        [HideInInspector]
        private ObjectControllerCustomRuleBase _customOffsetRule;

        [SerializeField]
        [HideInInspector]
        private ObjectControllerCustomRuleBase _customRotationRule;

        [SerializeField]
        [HideInInspector]
        private ObjectControllerCustomRuleBase _customScaleRule;

        System.Random offsetRandomizer, shellRandomizer, rotationRandomizer, scaleRandomizer, distanceRandomizer;

        public void Clear()
        {
            for (int i = 0; i < spawned.Length; i++)
            {
                if (spawned[i] == null || spawned[i].transform == null) continue;
                spawned[i].transform.localScale = spawned[i].baseScale;
                if (_objectMethod == ObjectMethod.GetChildren) spawned[i].gameObject.SetActive(false);
                else
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) spawned[i].DestroyImmediate();
                    else spawned[i].Destroy();
#else
                    spawned[i].Destroy();
#endif

                }
            }
            spawned = new ObjectControl[0];
        }

        private void OnValidate()
        {
            if (_spawnCount < 0) _spawnCount = 0;
        }

        private void Remove()
        {
            if (_spawnCount >= spawned.Length) return;
            for (int i = spawned.Length - 1; i >= _spawnCount; i--)
            {
                if (i >= spawned.Length) break;
                if (spawned[i] == null) continue;
                spawned[i].transform.localScale = spawned[i].baseScale;
                if (_objectMethod == ObjectMethod.GetChildren) spawned[i].gameObject.SetActive(false);
                else
                {
                    if (Application.isEditor) spawned[i].DestroyImmediate();
                    else spawned[i].Destroy();

                }
            }
            ObjectControl[] newSpawned = new ObjectControl[_spawnCount];
            for (int i = 0; i < newSpawned.Length; i++)
            {
                newSpawned[i] = spawned[i];
            }
            spawned = newSpawned;
            Rebuild();
        }

        public void GetAll()
        {
            ObjectControl[] newSpawned = new ObjectControl[transform.childCount];
            int index = 0;
            foreach (Transform child in transform)
            {
                if (newSpawned[index] == null)
                {
                    newSpawned[index++] = new ObjectControl(child.gameObject);
                    continue;
                }
                bool found = false;
                for (int i = 0; i < spawned.Length; i++)
                {
                    if (spawned[i].gameObject == child.gameObject)
                    {
                        newSpawned[index++] = spawned[i];
                        found = true;
                        break;
                    }
                }
                if (!found) newSpawned[index++] = new ObjectControl(child.gameObject);
            }
            spawned = newSpawned;
        }

        public void Spawn()
        {
            if (_objectMethod == ObjectMethod.Instantiate)
            {
                if (delayedSpawn && Application.isPlaying)
                {
                    StopCoroutine("InstantiateAllWithDelay");
                    StartCoroutine(InstantiateAllWithDelay());
                }
                else InstantiateAll();
            }
            else GetAll();
            Rebuild();
        }

        protected override void LateRun()
        {
            base.LateRun();
            if (_objectMethod == ObjectMethod.GetChildren && lastChildCount != transform.childCount)
            {
                Spawn();
                lastChildCount = transform.childCount;
            }
        }


        IEnumerator InstantiateAllWithDelay()
        {
            if (spline == null) yield break;
            if (objects.Length == 0) yield break;
            for (int i = spawned.Length; i <= spawnCount; i++)
            {
                InstantiateSingle();
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        private void InstantiateAll()
        {
            if (spline == null) return;
            if (objects.Length == 0) return;
            for (int i = spawned.Length; i < spawnCount; i++) InstantiateSingle();
        }

        private void InstantiateSingle()
        {
            if (objects.Length == 0) return;
            int index = 0;
            if (_iteration == Iteration.Ordered)
            {
                index = spawned.Length - Mathf.FloorToInt(spawned.Length / objects.Length) * objects.Length;
            }
            else index = Random.Range(0, objects.Length);
            if (objects[index] == null) return;

            ObjectControl[] newSpawned = new ObjectControl[spawned.Length + 1];
            spawned.CopyTo(newSpawned, 0);
#if UNITY_EDITOR
            if (!Application.isPlaying && retainPrefabInstancesInEditor)
            {
                GameObject go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(objects[index]);
                go.transform.position = transform.position;
                go.transform.rotation = transform.rotation;
                newSpawned[newSpawned.Length - 1] = new ObjectControl(go);
            } else
            {
                newSpawned[newSpawned.Length - 1] = new ObjectControl((GameObject)Instantiate(objects[index], transform.position, transform.rotation));
            }
#else
            newSpawned[newSpawned.Length - 1] = new ObjectControl((GameObject)Instantiate(objects[index], transform.position, transform.rotation));
#endif
            newSpawned[newSpawned.Length - 1].transform.parent = transform;
            spawned = newSpawned;
        }

        protected override void Build()
        {
            base.Build();
            offsetRandomizer = new System.Random(_randomSeed);
            if(_shellOffset) shellRandomizer = new System.Random(_randomSeed + 1);
            rotationRandomizer = new System.Random(_randomSeed + 2);
            scaleRandomizer = new System.Random(_randomSeed + 3);
            distanceRandomizer = new System.Random(_randomSeed + 4);

            bool hasCustomOffset = _customOffsetRule != null;
            bool hasCustomRotation = _customRotationRule != null;
            bool hasCustomScale = _customScaleRule != null;

            bool randomScaleMultiplier = _minScaleMultiplier != _maxScaleMultiplier;
            double distancePercentAccum = 0.0;
            for (int i = 0; i < spawned.Length; i++)
            {
                if (spawned[i] == null)
                {
                    Clear();
                    Spawn();
                    break;
                }
                float percent = 0f;
                if (spawned.Length > 1)
                {
                    if(!_useCustomObjectDistance)
                    {
                        if (spline.isClosed)
                        {
                            percent = (float)i / spawned.Length;
                        }
                        else
                        {
                            percent = (float)i / (spawned.Length - 1);
                        }
                    } else
                    {
                        percent = (float)distancePercentAccum;
                    }
                }

                percent += _evaluateOffset;
                if (percent > 1f)
                {
                    percent -= 1f;
                }
                else if (percent < 0f)
                {
                    percent += 1f;
                }
                
                if (objectPositioning == Positioning.Clip)
                {
                    spline.Evaluate(percent, ref evalResult);
                }
                else
                {
                    Evaluate(percent, ref evalResult);
                }

                ModifySample(ref evalResult);
                spawned[i].position = evalResult.position;

                if (_applyScale)
                {
                    if (hasCustomScale)
                    {
                        _customScaleRule.SetContext(this, evalResult, i, spawned.Length);
                        spawned[i].scale = _customOffsetRule.GetScale();
                    } 
                    else
                    {
                        Vector3 scale = spawned[i].baseScale * evalResult.size;
                        Vector3 multiplier = _minScaleMultiplier;

                        if (randomScaleMultiplier)
                        {

                            if (_uniformScaleLerp)
                            {
                                multiplier = Vector3.Lerp(new Vector3(_minScaleMultiplier.x, _minScaleMultiplier.y, _minScaleMultiplier.z), new Vector3(_maxScaleMultiplier.x, _maxScaleMultiplier.y, _maxScaleMultiplier.z), (float)scaleRandomizer.NextDouble());
                            }
                            else
                            {
                                multiplier.x = Mathf.Lerp(_minScaleMultiplier.x, _maxScaleMultiplier.x, (float)scaleRandomizer.NextDouble());
                                multiplier.y = Mathf.Lerp(_minScaleMultiplier.y, _maxScaleMultiplier.y, (float)scaleRandomizer.NextDouble());
                                multiplier.z = Mathf.Lerp(_minScaleMultiplier.z, _maxScaleMultiplier.z, (float)scaleRandomizer.NextDouble());
                            }
                        }
                        scale.x *= multiplier.x;
                        scale.y *= multiplier.y;
                        scale.z *= multiplier.z;
                        spawned[i].scale = scale;
                    }
                }
                else
                {
                    spawned[i].scale = spawned[i].baseScale;
                }

                Vector3 right = Vector3.Cross(evalResult.forward, evalResult.up).normalized;

                Vector3 posOffset = _minOffset;
                if (hasCustomOffset)
                {
                    _customOffsetRule.SetContext(this, evalResult, i, spawned.Length);
                    posOffset = _customOffsetRule.GetOffset();
                } 
                else if (_minOffset != _maxOffset)
                {
                    if(_shellOffset)
                    {
                        float x = _maxOffset.x - _minOffset.x;
                        float y = _maxOffset.y - _minOffset.y;
                        float angleInRadians = (float)shellRandomizer.NextDouble() * 360f * Mathf.Deg2Rad;
                        posOffset = new Vector2(0.5f * Mathf.Cos(angleInRadians), 0.5f * Mathf.Sin(angleInRadians));
                        posOffset.x *= x;
                        posOffset.y *= y;
                    } else
                    {
                        float rnd = (float)offsetRandomizer.NextDouble();
                        posOffset.x = Mathf.Lerp(_minOffset.x, _maxOffset.x, rnd);
                        rnd = (float)offsetRandomizer.NextDouble();
                        posOffset.y = Mathf.Lerp(_minOffset.y, _maxOffset.y, rnd);
                        rnd = (float)offsetRandomizer.NextDouble();
                        posOffset.z = Mathf.Lerp(_minOffset.z, _maxOffset.z, rnd);
                    }
                }

                if (_offsetUseWorldCoords)
                {
                    spawned[i].position += posOffset;
                }
                else
                {
                    spawned[i].position += right * posOffset.x * evalResult.size + evalResult.up * posOffset.y * evalResult.size;
                }

                if (_applyRotation)
                {
                    if (hasCustomRotation)
                    {
                        _customRotationRule.SetContext(this, evalResult, i, spawned.Length);
                        spawned[i].rotation = _customRotationRule.GetRotation();
                    }
                    else
                    {
                        Quaternion offsetRot = Quaternion.Euler(Mathf.Lerp(_minRotation.x, _maxRotation.x, (float)rotationRandomizer.NextDouble()), Mathf.Lerp(_minRotation.y, _maxRotation.y, (float)rotationRandomizer.NextDouble()), Mathf.Lerp(_minRotation.z, _maxRotation.z, (float)rotationRandomizer.NextDouble()));
                        if (_rotateByOffset) spawned[i].rotation = Quaternion.LookRotation(evalResult.forward, spawned[i].position - evalResult.position) * offsetRot;
                        else spawned[i].rotation = evalResult.rotation * offsetRot;
                    }
                }

                if (_objectPositioning == Positioning.Clip)
                {
                    if (percent < clipFrom || percent > clipTo) spawned[i].active = false;
                    else spawned[i].active = true;
                }
                if (_useCustomObjectDistance)
                {
                    if (objectPositioning == Positioning.Clip)
                    {
                        distancePercentAccum = spline.Travel(distancePercentAccum, Mathf.Lerp(_minObjectDistance, _maxObjectDistance, (float)distanceRandomizer.NextDouble()));
                    }
                    else
                    {
                        distancePercentAccum = Travel(distancePercentAccum, Mathf.Lerp(_minObjectDistance, _maxObjectDistance, (float)distanceRandomizer.NextDouble()));
                    }
                }
            }
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            for (int i = 0; i < spawned.Length; i++)
            {
                spawned[i].Apply();
            }
        }
    }
}
