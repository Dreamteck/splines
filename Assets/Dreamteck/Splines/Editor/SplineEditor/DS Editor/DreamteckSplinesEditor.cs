namespace Dreamteck.Splines.Editor
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class DreamteckSplinesEditor : SplineEditor
    {
        public SplineComputer spline = null;

        public bool splineChanged
        {
            get { return _splineChanged; }
        }

        private Transform _transform;
        private DSCreatePointModule _createPointModule = null;
        private Dreamteck.Editor.Toolbar _nodesToolbar;
        private bool _splineChanged = false;

        private List<Vector3> _triggerWorldPositions = new List<Vector3>();


        protected override string editorName { get { return "DreamteckSplines"; } }

        public DreamteckSplinesEditor(SplineComputer splineComputer, SerializedObject splineHolder) : base (splineComputer.transform.localToWorldMatrix, splineHolder, "_spline")
        {
            spline = splineComputer;
            _transform = spline.transform;
            evaluate = spline.Evaluate;
            evaluateAtPoint = spline.Evaluate;
            evaluatePosition = spline.EvaluatePosition;
            calculateLength = spline.CalculateLength;
            travel = spline.Travel;
            undoHandler = HandleUndo;
            mainModule.onBeforeDeleteSelectedPoints += OnBeforeDeleteSelectedPoints;
            mainModule.onDuplicatePoint += OnDuplicatePoint;
            if (spline.isNewlyCreated)
            {
                if (SplinePrefs.startInCreationMode)
                {
                    open = true;
                    editMode = true;
                    ToggleModule(0);
                }
                spline.isNewlyCreated = false;
            }
            GUIContent[] nodeToolbarContents = new GUIContent[3];
            nodeToolbarContents[0] = new GUIContent("Select");
            nodeToolbarContents[1] = new GUIContent("Delete");
            nodeToolbarContents[2] = new GUIContent("Disconnect");
            _nodesToolbar = new Dreamteck.Editor.Toolbar(nodeToolbarContents);
        }

        protected override void Load()
        {
            pointOperations.Add(new PointOperation { name = "Center To Transform", action = delegate { CenterSelection(); } });
            pointOperations.Add(new PointOperation { name = "Move Transform To", action = delegate { MoveTransformToSelection(); } });
            base.Load();
        }

        private void OnDuplicatePoint(int[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                spline.ShiftNodes(points[i], spline.pointCount - 1, 1);
            }
        }

        public override void DrawInspector()
        {
            drawColor = spline.editorPathColor;
            is2D = spline.is2D;
            base.DrawInspector();
        }

        public override void DrawScene(SceneView current)
        {
            if (spline == null) return;

            drawColor = spline.editorPathColor;
            is2D = spline.is2D;
            base.DrawScene(current);
        }

        public void CacheTriggerPositions()
        {
            _triggerWorldPositions.Clear();
            LoopTriggerProperties((trigger) =>
            {
                SerializedProperty positionProperty = trigger.FindPropertyRelative("position");
                _triggerWorldPositions.Add(spline.EvaluatePosition(positionProperty.floatValue));
            });
        }

        public void WriteTriggerPositions()
        {
            SplineSample projectSample = new SplineSample();
            int index = 0;
            LoopTriggerProperties((trigger) =>
            {
                spline.Project(_triggerWorldPositions[index], ref projectSample);
                SerializedProperty positionProperty = trigger.FindPropertyRelative("position");
                positionProperty.floatValue = (float)projectSample.percent;
                index++;
            });
            _serializedObject.ApplyModifiedProperties();
        }

        private void OnBeforeDeleteSelectedPoints()
        {
            CacheTriggerPositions();
            string nodeString = "";
            List <Node> deleteNodes = new List<Node>();
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                Node node = spline.GetNode(selectedPoints[i]);
                if (node)
                {
                    spline.DisconnectNode(selectedPoints[i]);
                    if (node.GetConnections().Length == 0)
                    {
                        deleteNodes.Add(node);
                        if (nodeString != "") nodeString += ", ";
                        string trimmed = node.name.Trim();
                        if (nodeString.Length + trimmed.Length > 80) nodeString += "...";
                        else nodeString += node.name.Trim();
                    }
                }
            }

            if (deleteNodes.Count > 0)
            {
                string message = "The following nodes:\r\n" + nodeString + "\r\n were only connected to the currently selected points. Would you like to remove them from the scene?";
                if (EditorUtility.DisplayDialog("Remove nodes?", message, "Yes", "No"))
                {
                    for (int i = 0; i < deleteNodes.Count; i++)
                    {
                        Undo.DestroyObjectImmediate(deleteNodes[i].gameObject);
                    }
                }
            }

            int min = spline.pointCount - 1;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (selectedPoints[i] < min)
                {
                    min = selectedPoints[i];
                }
            }
        }

        protected override void PointMenu()
        {
            base.PointMenu();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Nodes", GUILayout.MaxWidth(200f));
            int nodesCount = 0;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if(spline.GetNode(selectedPoints[i]) != null)
                {
                    nodesCount ++;
                }
            }

            if (nodesCount > 0)
            {
                int option = -1;
                _nodesToolbar.center = false;
                _nodesToolbar.Draw(ref option);
                if(option == 0)
                {
                    List<Node> nodeList = new List<Node>();
                    for (int i = 0; i < selectedPoints.Count; i++)
                    {
                        Node node = spline.GetNode(selectedPoints[i]);
                        if(node != null)
                        {
                            nodeList.Add(node);
                        }
                    }
                    Selection.objects = nodeList.ToArray();
                }

                if(option == 1)
                {
                    for (int i = 0; i < selectedPoints.Count; i++)
                    {
                        bool delete = true;
                        Node node = spline.GetNode(selectedPoints[i]);
                        if(node.GetConnections().Length > 1)
                        {
                            if(!EditorUtility.DisplayDialog("Delete Node",
                                "Node " + node.name + " has multiple connections. Are you sure you want to completely remove it?", "Yes", "No"))
                            {
                                delete = false;
                            }
                        }
                        if (delete)
                        {
                            Undo.RegisterCompleteObjectUndo(spline, "Delete Node");
                            Undo.DestroyObjectImmediate(node.gameObject);
                            spline.DisconnectNode(selectedPoints[i]);
                            EditorUtility.SetDirty(spline);
                        }
                    }
                }
                if (option == 2)
                {
                    for (int i = 0; i < selectedPoints.Count; i++)
                    {
                        Undo.RegisterCompleteObjectUndo(spline, "Disconnect Node");
                        spline.DisconnectNode(selectedPoints[i]);
                        EditorUtility.SetDirty(spline);
                    }
                }
            } else
            {
                if(GUILayout.Button(selectedPoints.Count == 1 ? "Add Node to Point" : "Add Nodes to Points"))
                {
                    for (int i = 0; i < selectedPoints.Count; i++)
                    {
                        SplineSample sample = spline.Evaluate(selectedPoints[i]);
                        GameObject go = new GameObject(spline.name + "_Node_" + (spline.GetNodes().Count+1));
                        go.transform.parent = spline.transform;
                        go.transform.position = sample.position;
                        if (spline.is2D)
                        {
                            go.transform.rotation = sample.rotation * Quaternion.Euler(90, -90, 0);
                        }
                        else
                        {
                            go.transform.rotation = sample.rotation;
                        }
                        Node node = go.AddComponent<Node>();
                        Undo.RegisterCreatedObjectUndo(go, "Create Node");
                        Undo.RegisterCompleteObjectUndo(spline, "Create Node");
                        spline.ConnectNode(node, selectedPoints[i]);
                    }
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
        protected override void OnModuleList(List<PointModule> list)
        {
            _createPointModule = new DSCreatePointModule(this);
            list.Add(_createPointModule);
            list.Add(new DeletePointModule(this));
            list.Add(new PointMoveModule(this));
            list.Add(new PointRotateModule(this));
            list.Add(new PointScaleModule(this));
            list.Add(new PointNormalModule(this));
            list.Add(new PointMirrorModule(this));
            list.Add(new PrimitivesModule(this));
        }

        public override void Destroy()
        {
            base.Destroy();
            if(spline != null)
            {
                spline.RebuildImmediate();
            }
        }

        public override void BeforeSceneGUI(SceneView current)
        {
            for (int i = 0; i < moduleCount; i++)
            {
                SetupModule(GetModule(i));
            }
            SetupModule(mainModule);
            _createPointModule.createPointColor = SplinePrefs.createPointColor;
            _createPointModule.createPointSize = SplinePrefs.createPointSize;
            base.BeforeSceneGUI(current);
        }

        public override void DeletePoint(int index)
        {
            CacheTriggerPositions();
            Dictionary<int, Node> nodes = new Dictionary<int, Node>();
            foreach(var node in spline.GetNodes())
            {
                if(node.Key > index)
                {
                    spline.DisconnectNode(node.Key);
                    nodes.Add(node.Key - 1, node.Value);
                }
            }
            var nodesProperty = _serializedObject.FindProperty("_nodes");
            for (int i = 0; i < nodesProperty.arraySize; i++)
            {
                var indexProperty = nodesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("pointIndex");
                if(indexProperty.intValue > index)
                {
                    nodesProperty.DeleteArrayElementAtIndex(i);
                    i--;
                }
            }
            InverseTransformPoints();
            _pointsProperty.DeleteArrayElementAtIndex(index);

            foreach (var node in nodes)
            {
                spline.ConnectNode(node.Value, node.Key);
                nodesProperty.arraySize = nodesProperty.arraySize + 1;
                var lastProperty = nodesProperty.GetArrayElementAtIndex(nodesProperty.arraySize - 1);
                var lastnodeProperty = lastProperty.FindPropertyRelative("node");
                var lastIndexProperty = lastProperty.FindPropertyRelative("pointIndex");
                lastnodeProperty.objectReferenceValue = node.Value;
                lastIndexProperty.intValue = node.Key;
            }

            _serializedObject.ApplyModifiedProperties();
            GetPointsFromSpline();
            spline.Rebuild(true);
            WriteTriggerPositions();
        }

        public override void GetPointsFromSpline()
        {
            base.GetPointsFromSpline();

            if (_serializedObject.FindProperty("_space").enumValueIndex == (int)SplineComputer.Space.Local)
            {
                TransformPoints();
            }
        }

        public override void ApplyModifiedProperties(bool forceAllUpdate = false)
        {
            if (_serializedObject.FindProperty("_space").enumValueIndex == (int)SplineComputer.Space.Local)
            {
                InverseTransformPoints();
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].changed || forceAllUpdate)
                {
                    spline.EditorSetPointDirty(i);
                }
            }

            _splineChanged = true;

            if (spline.isClosed && points.Length < 3)
            {
                SetSplineClosed(false);
            }

            _serializedObject.FindProperty("_is2D").boolValue = is2D;

            base.ApplyModifiedProperties(forceAllUpdate);

            spline.EditorUpdateConnectedNodes();

            if (_serializedObject.FindProperty("editorUpdateMode").enumValueIndex == (int)SplineComputer.EditorUpdateMode.Default)
            {
                spline.RebuildImmediate(true, forceAllUpdate);
            }
            GetPointsFromSpline();
        }

        public override void SetSplineClosed(bool closed)
        {
            base.SetSplineClosed(closed);
            if (closed)
            {
                spline.Close();
            }
            else
            {
                if (selectedPoints.Count > 0)
                {
                    spline.Break(selectedPoints[selectedPoints.Count - 1]);
                }
                else
                {
                    spline.Break();
                }
            }
        }

        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();
            spline.RebuildImmediate(true, true);
        }

        private void TransformPoints()
        {
            _matrix = spline.transform.localToWorldMatrix;
            for (int i = 0; i < points.Length; i++)
            {
                bool changed = points[i].changed;
                points[i].position = _matrix.MultiplyPoint3x4(points[i].position);
                points[i].tangent = _matrix.MultiplyPoint3x4(points[i].tangent);
                points[i].tangent2 = _matrix.MultiplyPoint3x4(points[i].tangent2);
                points[i].normal = _matrix.MultiplyVector(points[i].normal);
                points[i].changed = changed;
            }
        }

        private void InverseTransformPoints()
        {
            _matrix = spline.transform.localToWorldMatrix;
            Matrix4x4 invMatrix = _matrix.inverse;
            for (int i = 0; i < points.Length; i++)
            {
                bool changed = points[i].changed;
                points[i].position = invMatrix.MultiplyPoint3x4(points[i].position);
                points[i].tangent = invMatrix.MultiplyPoint3x4(points[i].tangent);
                points[i].tangent2 = invMatrix.MultiplyPoint3x4(points[i].tangent2);
                points[i].normal = invMatrix.MultiplyVector(points[i].normal);
                points[i].changed = changed;
            }
        }

        public override void SetPreviewPoints(SplinePoint[] points)
        {
            spline.SetPoints(points);
        }

        private void HandleUndo(string title)
        {
            Undo.RecordObject(spline, title);
        }

        public void MoveTransformToSelection()
        {
            Undo.RecordObject(_transform, "Move Transform To");
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avg += points[selectedPoints[i]].position;
            }
            avg /= selectedPoints.Count;
            _transform.position = avg;
            ApplyModifiedProperties(true);
            ResetCurrentModule();
        }

        public void CenterSelection()
        {
            RecordUndo("Center Selection");
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avg += points[selectedPoints[i]].position;
            }
            avg /= selectedPoints.Count;
            Vector3 delta = _transform.position - avg;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                points[selectedPoints[i]].SetPosition(points[selectedPoints[i]].position + delta);
            }
            ApplyModifiedProperties(true);
            ResetCurrentModule();
        }

        private void SetupModule(PointModule module)
        {
            module.duplicationDirection = SplinePrefs.duplicationDirection;
            module.highlightColor = SplinePrefs.highlightColor;
            module.showPointNumbers = SplinePrefs.showPointNumbers;
        }
    }
}
