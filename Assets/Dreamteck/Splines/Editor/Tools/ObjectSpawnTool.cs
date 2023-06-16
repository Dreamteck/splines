namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using System.IO;

    public class ObjectSpawnTool : SplineTool
    {
        internal class SpawnCollection
        {
            public class SpawnObject
            {
                public GameObject instance;
                public GameObject source;

                public SpawnObject(GameObject instance, GameObject source)
                {
                    this.instance = instance;
                    this.source = source;
                }
            }

            internal SplineComputer spline;
            internal List<SpawnObject> objects = new List<SpawnObject>();

            internal void Clear()
            {
                for (int i = 0; i < objects.Count; i++) Object.DestroyImmediate(objects[i].instance);
                objects.Clear();
            }

            internal void Spawn(GameObject obj, Vector3 position, Quaternion rotation)
            {
                GameObject go = null;
                bool isPrefab = PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab;

                if (isPrefab) go = (GameObject)PrefabUtility.InstantiatePrefab(obj);
                else go = Object.Instantiate(obj, position, rotation);
                go.transform.parent = spline.transform;
                objects.Add(new SpawnObject(go, obj));
            }

            internal void Destroy(int index)
            {
                Object.DestroyImmediate(objects[index].instance);
                objects.RemoveAt(index);
            }

            internal SpawnCollection(SplineComputer spline)
            {
                this.spline = spline;
            }
        }

        internal List<SpawnCollection> collections = new List<SpawnCollection>();
        double clipFrom = 0.0, clipTo = 1.0;
        List<GameObject> objects = new List<GameObject>();
        int spawnCount = 1;
        enum Iteration { Ordered, Random }
        Iteration iteration = Iteration.Ordered;
        private int offsetSeed = 0;
        private int rotationSeed = 0;
        private int scaleSeed = 0;
        private int orderSeed = 0;
        private float positionOffset = 0f;
        private Vector2 randomSize = Vector2.one;
        private Vector2 offset = Vector2.zero;
        private Vector3 minRotationOffset = Vector3.zero;
        private Vector3 maxRotationOffset = Vector3.zero;
        private Vector3 minScaleMultiplier = Vector3.one;
        private Vector3 maxScaleMultiplier = Vector3.one;
        private bool randomizeOffset = false;
        private bool useRandomOffsetRotation = false;
        private bool shellOffset = true;
        private bool applyRotation = true;
        private bool applyScale = false;
        bool uniform = false;

        SplineSample result = new SplineSample();

        System.Random orderRandom, offsetRandom, rotationRandom, scaleRandom;

        public override string GetName()
        {
            return "Spawn Objects";
        }

        protected override string GetPrefix()
        {
            return "ObjectSpawnTool";
        }

        public override void Close()
        {
            base.Close();
            for (int i = 0; i < splines.Count; i++) splines[i].onRebuild -= Rebuild;
            if (promptSave)
            {
                if (EditorUtility.DisplayDialog("Save changes?", "You are about to close the Object Spawn Tool, do you want to save the generated objects?", "Yes", "No")) Save();
                else Cancel();
            }
            else Cancel();
            promptSave = false;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnScene;
#else
            SceneView.onSceneGUIDelegate -= OnScene;
#endif

        }

        public override void Open(EditorWindow window)
        {
            base.Open(window);
            GetSplines();
            collections.Clear();
            for (int i = 0; i < splines.Count; i++)
            {
                collections.Add(new SpawnCollection(splines[i]));
                splines[i].onRebuild += Rebuild;
            }
            Rebuild();
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnScene;
#else
            SceneView.onSceneGUIDelegate += OnScene;
#endif

        }

        void OnScene(SceneView current)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                if (collections[i].spline != null) DSSplineDrawer.DrawSplineComputer(collections[i].spline);
            }
        }

        protected override void OnSplineAdded(SplineComputer spline)
        {
            base.OnSplineAdded(spline);
            collections.Add(new SpawnCollection(spline));
            spline.onRebuild += Rebuild;
            Rebuild();
        }

        protected override void OnSplineRemoved(SplineComputer spline)
        {
            base.OnSplineRemoved(spline);
            for (int i = 0; i < collections.Count; i++)
            {
                if (collections[i].spline == spline)
                {
                    collections[i].Clear();
                    collections.RemoveAt(i);
                    spline.onRebuild -= Rebuild;
                    Rebuild();
                    return;
                }
            }
        }

        public override void Draw(Rect windowRect)
        {
            base.Draw(windowRect);
            if (splines.Count == 0)
            {
                EditorGUILayout.HelpBox("No spline selected! Select an object with a SplineComputer component.", MessageType.Warning);
                return;
            }
            EditorGUI.BeginChangeCheck();
            ClipUI(ref clipFrom, ref clipTo);
            uniform = EditorGUILayout.Toggle("Uniform Samples", uniform);
            EditorGUILayout.Space();
            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.fieldWidth = 0;

            EditorGUILayout.BeginVertical();
            for (int i = 0; i < objects.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                objects[i] = (GameObject)EditorGUILayout.ObjectField(objects[i], typeof(GameObject), true);
                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    objects.RemoveAt(i);
                    i--;
                    Rebuild();
                    Repaint();
                    continue;
                }
                if (i > 0)
                {
                    if (GUILayout.Button("▲", GUILayout.Width(20)))
                    {
                        GameObject temp = objects[i - 1];
                        objects[i - 1] = objects[i];
                        objects[i] = temp;
                        Rebuild();
                    }
                }
                if (i < objects.Count - 1)
                {
                    if (GUILayout.Button("▼", GUILayout.Width(20)))
                    {
                        GameObject temp = objects[i + 1];
                        objects[i + 1] = objects[i];
                        objects[i] = temp;
                        Rebuild();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            GameObject newObj = null;
            newObj = (GameObject)EditorGUILayout.ObjectField("Add Object", newObj, typeof(GameObject), true);
            if (newObj != null)
            {
                objects.Add(newObj);
                Rebuild();
            }
            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
            bool hasObj = false;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    hasObj = true;
                    break;
                }
            }

            if (hasObj) spawnCount = EditorGUILayout.IntField("Spawn count", spawnCount);
            else spawnCount = 0;
            iteration = (Iteration)EditorGUILayout.EnumPopup("Iteration", iteration);
            if (iteration == Iteration.Random) orderSeed = EditorGUILayout.IntField("Order Seed", orderSeed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
            applyRotation = EditorGUILayout.Toggle("Apply Rotation", applyRotation);
            if (applyRotation)
            {
                EditorGUI.indentLevel++;
                minRotationOffset = EditorGUILayout.Vector3Field("Min. Rotation Offset", minRotationOffset);
                maxRotationOffset = EditorGUILayout.Vector3Field("Max. Rotation Offset", maxRotationOffset);
                rotationSeed = EditorGUILayout.IntField("Rotation Seed", rotationSeed);
                EditorGUI.indentLevel--;
            }
            applyScale = EditorGUILayout.Toggle("Apply Scale", applyScale);
            if (applyScale)
            {
                EditorGUI.indentLevel++;
                minScaleMultiplier = EditorGUILayout.Vector3Field("Min. Scale Multiplier", minScaleMultiplier);
                maxScaleMultiplier = EditorGUILayout.Vector3Field("Max. Scale Multiplier", maxScaleMultiplier);
                scaleSeed = EditorGUILayout.IntField("Scale Seed", scaleSeed);
                EditorGUI.indentLevel--;
            }

            positionOffset = EditorGUILayout.Slider("Evaluate Offset", positionOffset, -1f, 1f);

            offset = EditorGUILayout.Vector2Field("Offset", offset);
            randomizeOffset = EditorGUILayout.Toggle("Randomize Offset", randomizeOffset);
            if (randomizeOffset)
            {
                randomSize = EditorGUILayout.Vector2Field("Size", randomSize);
                offsetSeed = EditorGUILayout.IntField("Offset Seed", offsetSeed);
                shellOffset = EditorGUILayout.Toggle("Shell", shellOffset);
                useRandomOffsetRotation = EditorGUILayout.Toggle("Apply offset rotation", useRandomOffsetRotation);
            }

            if (EditorGUI.EndChangeCheck())
            {
                promptSave = true;
                Rebuild();
            }

            EditorGUILayout.BeginHorizontal();
            if(collections.Count > 0)
            {
                if (GUILayout.Button("Save"))
                {
                    Save();
                }
                if (GUILayout.Button("Cancel"))
                {
                    Cancel();
                }
            } else
            {
                if (GUILayout.Button("New")) Open(windowInstance);
            }
            EditorGUILayout.EndHorizontal();
        }

        protected override void Save()
        {
            base.Save();
            //register created object undo for each object in collections
            collections.Clear();
            //Set scene dirty
        }

        protected override void Cancel()
        {
            base.Cancel();
            foreach (SpawnCollection collection in collections) collection.Clear();
            collections.Clear();
        }

        void InitializeRandomization()
        {
            orderRandom = new System.Random(orderSeed);
            if (randomizeOffset) offsetRandom = new System.Random(offsetSeed);
            if(applyRotation) rotationRandom = new System.Random(rotationSeed);
            if(applyScale) scaleRandom = new System.Random(scaleSeed);
        }

        protected override void Rebuild()
        {
            base.Rebuild();
            if (objects.Count == 0) return;
            InitializeRandomization();
            foreach (SpawnCollection c in collections)
            {
                if(c == null) continue;
                if (c.spline == null || spawnCount <= 0)
                {
                    c.Clear();
                    continue;
                }
                HandleCollection(c);
            }
        }

        void HandleCollection(SpawnCollection collection)
        {
            collection.Clear();
            if (collection.spline == null) return;
            while(collection.objects.Count > spawnCount && collection.objects.Count >= 0) collection.Destroy(collection.objects.Count - 1);
            int orderIndex = 0;
            while (collection.objects.Count < spawnCount)
            {
                switch (iteration)
                {
                    case Iteration.Ordered:
                        collection.Spawn(objects[orderIndex], Vector3.zero, Quaternion.identity);
                        orderIndex++;
                        if (orderIndex >= objects.Count) orderIndex = 0;
                        break;
                    case Iteration.Random:
                        collection.Spawn(objects[orderRandom.Next(objects.Count)], Vector3.zero, Quaternion.identity);
                        break;
                }
            }
            float splineLength = 0f;
            if (uniform) splineLength = collection.spline.CalculateLength() * (float)(clipTo - clipFrom);
            for (int i = 0; i < spawnCount; i++)
            {
                double percent = 0.0;
                if(spawnCount > 1) percent = (double)i / (spawnCount - 1);
                double evaluate = 0.0;
                if (uniform) evaluate = collection.spline.Travel(clipFrom, splineLength * (float)percent, Spline.Direction.Forward);
                else evaluate = DMath.Lerp(clipFrom, clipTo, percent);
                //Handle uniform splines
                evaluate += positionOffset;
                if (evaluate > 1f) evaluate -= 1f;
                else if (evaluate < 0f) evaluate += 1f;
                collection.spline.Evaluate(evaluate, ref result);
                HandleObject(collection.objects[i]);
            }
        }

        void HandleObject(SpawnCollection.SpawnObject obj)
        {
            Transform instanceTransform = obj.instance.transform;
            Transform sourceTransform = obj.source.transform;
            Vector3 right = result.right;
            instanceTransform.position = result.position;
            instanceTransform.position += -right * offset.x + result.up * offset.y;
            Quaternion offsetRot = Quaternion.Euler(minRotationOffset);

            if (applyRotation)
            {
                offsetRot = Quaternion.Euler(Mathf.Lerp(minRotationOffset.x, maxRotationOffset.x, (float)rotationRandom.NextDouble()), Mathf.Lerp(minRotationOffset.y, maxRotationOffset.y, (float)rotationRandom.NextDouble()), Mathf.Lerp(minRotationOffset.z, maxRotationOffset.z, (float)rotationRandom.NextDouble()));
                instanceTransform.rotation = result.rotation * offsetRot;
            }

            if (randomizeOffset)
            {
                float distance = (float)offsetRandom.NextDouble();
                float angleInRadians = (float)offsetRandom.NextDouble() * 360f * Mathf.Deg2Rad;
                Vector2 randomCircle = new Vector2(distance * Mathf.Cos(angleInRadians), distance * Mathf.Sin(angleInRadians));
                if (shellOffset) randomCircle.Normalize();
                else randomCircle = Vector2.ClampMagnitude(randomCircle, 1f);
                instanceTransform.position += randomCircle.x * right * randomSize.x * result.size * 0.5f + randomCircle.y * result.up * randomSize.y * result.size * 0.5f;
                if (useRandomOffsetRotation) instanceTransform.rotation = Quaternion.LookRotation(result.forward, instanceTransform.position - result.position) * offsetRot;
            }

            if (applyScale)
            {
                Vector3 scale = sourceTransform.localScale * result.size;
                scale.x *= Mathf.Lerp(minScaleMultiplier.x, maxScaleMultiplier.x, (float)scaleRandom.NextDouble());
                scale.y *= Mathf.Lerp(minScaleMultiplier.y, maxScaleMultiplier.y, (float)scaleRandom.NextDouble());
                scale.z *= Mathf.Lerp(minScaleMultiplier.z, maxScaleMultiplier.z, (float)scaleRandom.NextDouble());
                instanceTransform.localScale = scale;
            } else instanceTransform.localScale = sourceTransform.localScale;
        }
    }
}
