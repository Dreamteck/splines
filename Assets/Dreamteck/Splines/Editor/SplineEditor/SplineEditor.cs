namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using Dreamteck.Editor;
    using UnityEngine;
    using UnityEditor;
    using Dreamteck.Splines;

    public class SplineEditor : SplineEditorBase
    {
        public enum Space { World, Local };
        public bool editMode = false;
        protected Matrix4x4 _matrix;
        protected virtual string editorName { get { return "SplineEditor"; } }

        public bool is2D = false;
        public Color drawColor = Color.white;

        public MainPointModule mainModule;
        public SerializedSplinePoint[] points = new SerializedSplinePoint[0];
        public List<int> selectedPoints = new List<int>();
        public Tool lastEditorTool = Tool.None;
        public Space editSpace = Space.World;

        public delegate void SplineEvaluation(double percent, ref SplineSample result);
        public delegate void SplinePointEvaluation(int pointIndex, ref SplineSample result);
        public delegate Vector3 SplineEvaluatePosition(double percent);
        public delegate float SplineCalculateLength(double from, double to);
        public delegate double SplineTravel(double start, float distance, Spline.Direction direction);

        public SplineEvaluation evaluate;
        public SplinePointEvaluation evaluateAtPoint;
        public SplineEvaluatePosition evaluatePosition;
        public SplineCalculateLength calculateLength;
        public SplineTravel travel;
        public EmptyHandler selectionChangeHandler;

        public int moduleCount
        {
            get { return _modules.Length; }
        }

        public PointModule currentModule
        {
            get
            {
                if (_module < 0 || _module >= _modules.Length) return null;
                else return _modules[_module];
            }
        }

        protected List<PointOperation> pointOperations = new List<PointOperation>();
        protected Vector2 lastClickPoint = Vector2.zero;
        protected GUIContent[] toolContents = new GUIContent[0], toolContentsSelected = new GUIContent[0];
        protected bool pointToolsToggle = false;
        protected Toolbar toolbar;
        protected SplineSample evalResult = new SplineSample();
        protected SerializedProperty _splineProperty { get; private set; }
        protected SerializedProperty _pointsProperty { get; private set; }
        protected SerializedProperty _typeProperty { get; private set; }
        protected SerializedProperty _sampleRateProperty { get; private set; }
        protected SerializedProperty _closedProperty { get; private set; }
        protected string splinePropertyName
        {
            get
            {
                if (_customSplinePropertyName != "") return _customSplinePropertyName;
                return "_spline";
            }
        }

        private int _module = -1, _selectModule = -1, _loadedModuleIndex = -1;
        private PointModule[] _modules = new PointModule[0];
        private string[] _pointOperationStrings = new string[0];
        private float _editLabelAlpha = 0f;
        private Vector2 _editLabelPosition = Vector2.zero;
        private float lastEmptyClickTime = 0f;
        private int _selectedPointOperation = 0;
        private bool _emptyClick = false;
        private string _customSplinePropertyName = "";

        public Matrix4x4 matrix
        {
            get { return _matrix; }
        }

        public SplineEditor(Matrix4x4 transformMatrix, SerializedObject splineHolder, string customSplinePropertyName) : base(splineHolder)
        {
            _customSplinePropertyName = customSplinePropertyName;
            Initialize(transformMatrix, splineHolder);
        }


        public SplineEditor(Matrix4x4 transformMatrix, SerializedObject splineHolder) : base(splineHolder)
        {
            Initialize(transformMatrix, splineHolder);
        }

        private void Initialize(Matrix4x4 transformMatrix, SerializedObject splineHolder)
        {
            _matrix = transformMatrix;
            string[] serializedPath = splinePropertyName.Split('/');
            foreach (var element in serializedPath)
            {
                if (_splineProperty == null)
                {
                    _splineProperty = serializedObject.FindProperty(element);
                    continue;
                }
                int i = 0;
                if (int.TryParse(element, out i))
                {
                    _splineProperty = _splineProperty.GetArrayElementAtIndex(i);
                }
                else
                {
                    _splineProperty = _splineProperty.FindPropertyRelative(element);
                }
            }

            GetSerializedProperteis();
            mainModule = new MainPointModule(this);
            mainModule.onSelectionChanged += OnSelectionChanged;
            List<PointModule> moduleList = new List<PointModule>();
            OnModuleList(moduleList);
            _modules = moduleList.ToArray();
            toolContents = new GUIContent[_modules.Length];
            toolContentsSelected = new GUIContent[_modules.Length];
            for (int i = 0; i < _modules.Length; i++)
            {
                _modules[i].onSelectionChanged += OnSelectionChanged;
                toolContents[i] = _modules[i].GetIconOff();
                toolContentsSelected[i] = _modules[i].GetIconOn();
            }
            toolbar = new Toolbar(toolContents, toolContentsSelected, 35f);

            pointOperations.Add(new PointOperation { name = "Flat X", action = delegate { FlatSelection(0); } });
            pointOperations.Add(new PointOperation { name = "Flat Y", action = delegate { FlatSelection(1); } });
            pointOperations.Add(new PointOperation { name = "Flat Z", action = delegate { FlatSelection(2); } });
            pointOperations.Add(new PointOperation { name = "Mirror X", action = delegate { MirrorSelection(0); } });
            pointOperations.Add(new PointOperation { name = "Mirror Y", action = delegate { MirrorSelection(1); } });
            pointOperations.Add(new PointOperation { name = "Mirror Z", action = delegate { MirrorSelection(2); } });
            pointOperations.Add(new PointOperation { name = "Distribute Evenly", action = delegate { DistributeEvenly(); } });
            pointOperations.Add(new PointOperation { name = "Auto Bezier Tangents", action = delegate { AutoTangents(); } });
            pointOperations.Add(new PointOperation { name = "Swap Bezier Tangents", action = delegate { SwapTangents(); } });
            pointOperations.Add(new PointOperation { name = "Flip Bezier Tangents", action = delegate { FlipTangents(); } });
            pointOperations.Add(new PointOperation { name = "Flip First Bezier Tangent", action = delegate { FlipFirstTangent(); } });
            pointOperations.Add(new PointOperation { name = "Flip Seconds Bezier Tangent", action = delegate { FlipSecondTangent(); } });

            _pointOperationStrings = new string[pointOperations.Count];
            for (int i = 0; i < pointOperations.Count; i++)
            {
                _pointOperationStrings[i] = pointOperations[i].name;
            }
            if (_selectedPointOperation >= _pointOperationStrings.Length || _selectedPointOperation < 0)
            {
                _selectedPointOperation = 0;
            }
        }

        protected virtual void GetSerializedProperteis()
        {
            _pointsProperty = _splineProperty.FindPropertyRelative("points");
            _typeProperty = _splineProperty.FindPropertyRelative("type");
            _sampleRateProperty = _splineProperty.FindPropertyRelative("sampleRate");
            _closedProperty = _splineProperty.FindPropertyRelative("closed");
        }

        public PointModule GetModule(int index)
        {
            return _modules[index];
        }

        public override void UndoRedoPerformed()
        {
            GetPointsFromSpline();
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if(selectedPoints[i] >= points.Length)
                {
                    selectedPoints.RemoveAt(i);
                    i--;
                }
            }
            ResetCurrentModule();
        }

        protected virtual void OnModuleList(List<PointModule> list)
        {
            list.Add(new CreatePointModule(this));
            list.Add(new DeletePointModule(this));
            list.Add(new PointMoveModule(this));
            list.Add(new PointRotateModule(this));
            list.Add(new PointScaleModule(this));
            list.Add(new PointNormalModule(this));
            list.Add(new PointMirrorModule(this));
        }

        public virtual void GetPointsFromSpline()
        {
            serializedObject.Update();
            if (points.Length != _pointsProperty.arraySize)
            {
                points = new SerializedSplinePoint[_pointsProperty.arraySize];
            }

            for (int i = 0; i < _pointsProperty.arraySize; i++)
            {
                points[i] = new SerializedSplinePoint(_pointsProperty.GetArrayElementAtIndex(i));
            }
        }

        public virtual void ApplyModifiedProperties(bool forceAllUpdate = false)
        {
            serializedObject.ApplyModifiedProperties();
        }

        public virtual void SetPreviewPoints(SplinePoint[] points)
        {

        }

        public SplinePoint[] GetPointsArray()
        {
            SplinePoint[] p = new SplinePoint[points.Length];
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = points[i].CreateSplinePoint();
            }
            return p;
        }

        public void SetPointsArray(SplinePoint[] input)
        {
            SetPointsCount(input.Length);
            for (int i = 0; i < points.Length; i++)
            {
                points[i].SetPoint(input[i]);
            }
        }

        public void SetPointsCount(int count)
        {
            _pointsProperty.arraySize = count;
            serializedObject.ApplyModifiedProperties();
            GetPointsFromSpline();
        }

        public virtual void DeletePoint(int index)
        {
            _pointsProperty.DeleteArrayElementAtIndex(index);
            ApplyModifiedProperties(true);
            GetPointsFromSpline();
        }

        public void AddPointAt(int index)
        {
            _pointsProperty.InsertArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            if (points.Length != _pointsProperty.arraySize)
            {
                points = new SerializedSplinePoint[_pointsProperty.arraySize];
            }

            for (int i = 0; i < _pointsProperty.arraySize; i++)
            {
                points[i] = new SerializedSplinePoint(_pointsProperty.GetArrayElementAtIndex(i));
            }

            ApplyModifiedProperties(true);
        }

        public override void Destroy()
        {
            base.Destroy();
            mainModule.Deselect();
            if (currentModule != null) currentModule.Deselect();
            if(lastEditorTool != Tool.None && Tools.current == Tool.None) Tools.current = lastEditorTool;
        }

        public virtual void SetSplineClosed(bool closed)
        {
            if (points.Length < 3)
            {
                closed = false;
            }
            _closedProperty.boolValue = closed;
        }

        public virtual void SetSplineType(Spline.Type type)
        {
            _typeProperty.enumValueIndex = (int)type;
        }

        public virtual void SetSplineSampleRate(int rate)
        {
            if (rate < 2) rate = 2;
            _sampleRateProperty.intValue = rate;
        }

        public virtual bool GetSplineClosed()
        {
            return _closedProperty.boolValue;
        }

        public virtual int GetSplineSampleRate()
        {
            return _sampleRateProperty.intValue;
        }

        public virtual Spline.Type GetSplineType()
        {
            return (Spline.Type)_typeProperty.enumValueIndex;
        }

        void OnSelectionChanged()
        {
            ResetCurrentModule();
            Repaint();
            if (selectionChangeHandler != null) selectionChangeHandler();
        }

        protected override void Save()
        {
            base.Save();
            EditorPrefs.SetBool(GetSaveName("editMode"), editMode);
            EditorPrefs.SetBool(GetSaveName("pointToolsToggle"), pointToolsToggle);
            EditorPrefs.SetInt(GetSaveName("selectedPointOperation"), _selectedPointOperation);
        }

        protected override void Load()
        {
            base.Load();
            editMode = EditorPrefs.GetBool(GetSaveName("editMode"), false);
            pointToolsToggle = EditorPrefs.GetBool(GetSaveName("pointToolsToggle"), false);
            _selectedPointOperation = EditorPrefs.GetInt(GetSaveName("selectedPointOperation"), 0);
        }

        private void HandleEditModeToggle()
        {
            if(Event.current.type == EventType.KeyDown)
            {
                if (editMode && Event.current.keyCode == KeyCode.Escape)
                {
                    if(_module >= 0)
                    {
                        UntoggleCurrentModule();
                        Repaint();
                    } else
                    {
                        editMode = false;
                        Repaint();
                    }
                }
                if (Event.current.control && Event.current.keyCode == KeyCode.E) {
                    editMode = !editMode;
                    Repaint();
                }
            }
        }

        public override void DrawInspector()
        {
            GetPointsFromSpline();
            HandleEditModeToggle();
            base.DrawInspector();
            if (editMode)
            {
                if (!gizmosEnabled)
                {
                    EditorGUILayout.HelpBox("Gizmos are disabled in the scene view. Enable Gizmos in the scene view for the spline editor to work.", MessageType.Error);
                }
                EditorGUILayout.Space();
                DrawToolMenu();
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                if (currentModule != null)
                {
                    currentModule.DrawInspector();
                    if (currentModule.hasChanged)
                    {
                        ApplyModifiedProperties();
                    }
                }
                DreamteckEditorGUI.DrawSeparator();
                PointPanel();
                if (EditorGUI.EndChangeCheck()) ResetCurrentModule();
            } else
            {
                if (GUILayout.Button("Edit"))
                {
                    editMode = true;
                }
            }
        }

        void DrawToolMenu()
        {
            EditorGUILayout.BeginHorizontal();
            if (_loadedModuleIndex >= 0)
            {
                ToggleModule(_loadedModuleIndex);
                _loadedModuleIndex = -1;
            }
            _selectModule = _module;
            EditorGUI.BeginChangeCheck();
            toolbar.Draw(ref _selectModule);
            if (EditorGUI.EndChangeCheck())
            {
                ToggleModule(_selectModule);
            }

            EditorGUILayout.EndHorizontal();
        }

        protected virtual void PointPanel()
        {
            if (points.Length == 0)
            {
                EditorGUILayout.LabelField("No control points available.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            mainModule.DrawInspector();
            if (mainModule.hasChanged)
            {
                ApplyModifiedProperties();
            }
            if (selectedPoints.Count > 0 && points.Length > 0)
            {
                PointMenu();
            }
        }

        public virtual void BeforeSceneGUI(SceneView current)
        {
            mainModule.BeforeSceneDraw(current);
            if (_module >= 0 && _module < _modules.Length)
            {
                _modules[_module].BeforeSceneDraw(current);
            }
        }

        public override void DrawScene(SceneView current)
        {
            GetPointsFromSpline();
            HandleEditModeToggle();
            if (!editMode)
            {
                return;
            }
            base.DrawScene(current);
            Event e = Event.current;
            if (Tools.current != Tool.None)
            {
                lastEditorTool = Tools.current;
                Tools.current = Tool.None;
            }
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (e.GetTypeForControl(controlID) == EventType.Layout) HandleUtility.AddDefaultControl(controlID);

            if (eventModule.mouseLeftDown) lastClickPoint = e.mousePosition;
            EditorGUI.BeginChangeCheck();
            mainModule.DrawScene();
            if (mainModule.hasChanged)
            {
                ApplyModifiedProperties();
            }
            if (currentModule != null)
            {
                currentModule.DrawScene();
                if (currentModule.hasChanged)
                {
                    ApplyModifiedProperties();
                }
                if (currentModule is CreatePointModule)
                {
                    if (eventModule.mouseLeftDown && eventModule.mouseRight)
                    {
                        GUIUtility.hotControl = -1;
                        ApplyModifiedProperties(true);
                        ToggleModule(0);
                    }
                }
            }
            if(eventModule.mouseLeftDown) _emptyClick = GUIUtility.hotControl == 0;

            if (_emptyClick)
            {
                if (eventModule.mouseLeft && !mainModule.isDragging && Vector2.Distance(lastClickPoint, e.mousePosition) >= mainModule.minimumRectSize && !eventModule.alt)
                {
                    mainModule.StartDrag(lastClickPoint);
                    _emptyClick = false;
                }
            }

            if (eventModule.mouseLeftUp)
            {
                if (mainModule.isDragging) mainModule.FinishDrag();
                else
                {
                    if (_emptyClick && !eventModule.alt)
                    {
                        if(selectedPoints.Count > 0) mainModule.ClearSelection();
                        else if(editMode)
                        {
                            if (Time.realtimeSinceStartup - lastEmptyClickTime <= 0.3f)
                            {
                                editMode = false;
                            }
                            else
                            {
                                _editLabelAlpha = 1f;
                                _editLabelPosition = e.mousePosition;
                                lastEmptyClickTime = Time.realtimeSinceStartup;
                            }
                        }
                    }
                }
            }


            if (!eventModule.mouseRight && !eventModule.mouseLeft && e.type == EventType.KeyDown && !e.control)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Q:
                        if (_module == 0) ToggleModule(1);
                        else ToggleModule(0);
                        e.Use(); break;
                    case KeyCode.W: ToggleModule(2); e.Use(); break;
                    case KeyCode.E: ToggleModule(3); e.Use(); break;
                    case KeyCode.R: ToggleModule(4); e.Use(); break;
                    case KeyCode.T: ToggleModule(5); e.Use(); break;
                    case KeyCode.Y: ToggleModule(6); e.Use(); break;
                }
            }

            if(_editLabelAlpha > 0f)
            {
                Handles.BeginGUI();
                GUI.contentColor = new Color(1f, 1f, 1f, _editLabelAlpha);
                DreamteckEditorGUI.Label(new Rect(_editLabelPosition, new Vector2(140, 50)), "Click Again To Exit");
                Handles.EndGUI();
                _editLabelAlpha = Mathf.MoveTowards(_editLabelAlpha, 0f, Time.deltaTime * 0.05f);
                Repaint();
            }
        }

        public void ToggleModule(int index)
        {
            Tools.current = Tool.None;
            if (currentModule != null) currentModule.Deselect();
            if (index == _module) _module = -1;
            else
            {
                _module = index;
                ResetCurrentModule();
                currentModule.Select();
                if (currentModule.hasChanged)
                {
                    ApplyModifiedProperties(true);
                }
            }
            Repaint();
        }

        public void UntoggleCurrentModule()
        {
            if (currentModule != null) currentModule.Deselect();
            _module = -1;
            Repaint();
        }


        protected virtual void PointMenu()
        {
            //Otherwise show the editing menu + the point selection menu
            Vector3 avgPos = Vector3.zero;
            Vector3 avgTan = Vector3.zero;
            Vector3 avgTan2 = Vector3.zero;
            Vector3 avgNormal = Vector3.zero;
            float avgSize = 0f;
            Color avgColor = Color.clear;

            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avgPos += points[selectedPoints[i]].position;
                avgNormal += points[selectedPoints[i]].normal;
                avgSize += points[selectedPoints[i]].size;
                avgTan += points[selectedPoints[i]].tangent;
                avgTan2 += points[selectedPoints[i]].tangent2;
                avgColor += points[selectedPoints[i]].color;
            }

            avgPos /= selectedPoints.Count;
            avgTan /= selectedPoints.Count;
            avgTan2 /= selectedPoints.Count;
            avgSize /= selectedPoints.Count;
            avgColor /= selectedPoints.Count;
            avgNormal.Normalize();

            SplinePoint avgPoint = new SplinePoint(avgPos, avgPos);
            avgPoint.tangent = avgTan;
            avgPoint.tangent2 = avgTan2;
            avgPoint.size = avgSize;
            avgPoint.color = avgColor;
            avgPoint.type = points[selectedPoints[0]].type;
            SplinePoint.Type lastType = avgPoint.type;

            avgPoint.normal = avgNormal;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Point Operations");

            EditorGUILayout.BeginVertical();

            _selectedPointOperation = EditorGUILayout.Popup(_selectedPointOperation, _pointOperationStrings);
            if (GUILayout.Button("Apply"))
            {
                pointOperations[_selectedPointOperation].action.Invoke();
                ApplyModifiedProperties();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            editSpace = (Space)EditorGUILayout.EnumPopup("Coordinate Space", editSpace);
            bool isBezier = _typeProperty.enumValueIndex == (int)Spline.Type.Bezier;
            if (isBezier)
            {
                if (is2D)
                {
                    avgPoint.SetTangentPosition(TransformedPositionField2D("Tangent 1", avgPoint.tangent));
                    avgPoint.SetTangent2Position(TransformedPositionField2D("Tangent 2", avgPoint.tangent2));
                }
                else
                {
                    avgPoint.SetTangentPosition(TransformedPositionField("Tangent 1", avgPoint.tangent));
                    avgPoint.SetTangent2Position(TransformedPositionField("Tangent 2", avgPoint.tangent2));
                }
            }
            if (is2D)
            {
                avgPoint.SetPosition(TransformedPositionField2D("Position", avgPoint.position));
            }
            else
            {
                avgPoint.SetPosition(TransformedPositionField("Position", avgPoint.position));
            }

            if (!is2D)
            {
                avgPoint.normal = TransformedVectorField("Normal", avgPoint.normal);
            }
            avgPoint.size = EditorGUILayout.FloatField("Size", avgPoint.size);
            avgPoint.color = EditorGUILayout.ColorField("Color", avgPoint.color);
            if (isBezier)
            {
                avgPoint.type = (SplinePoint.Type)EditorGUILayout.EnumPopup("Point Type", avgPoint.type);
            }

            if (!EditorGUI.EndChangeCheck()) return;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                points[selectedPoints[i]].SetPosition(GetChangedVector(avgPos, avgPoint.position, points[selectedPoints[i]].position));
                points[selectedPoints[i]].normal = GetChangedVector(avgNormal, avgPoint.normal, points[selectedPoints[i]].normal);

                if (isBezier)
                {
                    points[selectedPoints[i]].SetTangentPosition(GetChangedVector(avgTan, avgPoint.tangent, points[selectedPoints[i]].tangent));
                    points[selectedPoints[i]].SetTangent2Position(GetChangedVector(avgTan2, avgPoint.tangent2, points[selectedPoints[i]].tangent2));
                }
                if (avgPoint.size != avgSize) points[selectedPoints[i]].size = avgPoint.size;
                if (avgColor != avgPoint.color) points[selectedPoints[i]].color = avgPoint.color;
                if (lastType != avgPoint.type) points[selectedPoints[i]].type = avgPoint.type;
            }
            ApplyModifiedProperties();
        }

        Vector3 GetChangedVector(Vector3 oldVector, Vector3 newVector, Vector3 original)
        {
            if (!Mathf.Approximately(oldVector.x, newVector.x)) original.x = newVector.x;
            if (!Mathf.Approximately(oldVector.y, newVector.y)) original.y = newVector.y;
            if (!Mathf.Approximately(oldVector.z, newVector.z)) original.z = newVector.z;
            return original;
        }

        Vector3 TransformedPositionField(string title, Vector3 worldPoint)
        {
            Vector3 pos = worldPoint;
            if (editSpace == Space.Local) pos = _matrix.inverse.MultiplyPoint3x4(worldPoint);
            pos = EditorGUILayout.Vector3Field(title, pos);
            if (editSpace == Space.Local) pos = _matrix.MultiplyPoint3x4(pos);
            return pos;
        }

        Vector3 TransformedVectorField(string title, Vector3 worldPoint)
        {
            Vector3 vector = worldPoint;
            if (editSpace == Space.Local) vector = _matrix.inverse.MultiplyVector(worldPoint);
            vector = EditorGUILayout.Vector3Field(title, vector);
            if (editSpace == Space.Local) vector = _matrix.MultiplyVector(vector);
            return vector;
        }

        Vector2 TransformedPositionField2D(string title, Vector3 worldPoint)
        {
            Vector2 pos = worldPoint;
            if (editSpace == Space.Local) pos = _matrix.inverse.MultiplyPoint3x4(worldPoint);
            pos = EditorGUILayout.Vector2Field(title, pos);
            if (editSpace == Space.Local) pos = _matrix.MultiplyPoint3x4(pos);
            return pos;
        }

        public void FlatSelection(int axis)
        {
            Vector3 avg = Vector3.zero;
            bool flatTangent = false;
            bool flatPosition = true;
            if (_typeProperty.enumValueIndex == (int)Spline.Type.Bezier)
            {
                switch (EditorUtility.DisplayDialogComplex("Flat Bezier", "How do you want to flat the selected Bezier points?", "Points Only", "Tangens Only", "Everything"))
                {
                    case 0: flatTangent = false; flatPosition = true; break;
                    case 1: flatTangent = true; flatPosition = false; break;
                    case 2: flatTangent = true; flatPosition = true; break;
                }
            }
            RecordUndo("Flat Selection");
            if (flatPosition)
            {
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    avg += points[selectedPoints[i]].position;
                }
                avg /= selectedPoints.Count;
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    Vector3 pos = points[selectedPoints[i]].position;
                    Vector3 nor = points[selectedPoints[i]].normal;
                    switch (axis)
                    {
                        case 0: pos.x = avg.x; nor.x = 0f; break;
                        case 1: pos.y = avg.y; nor.y = 0f; break;
                        case 2: pos.z = avg.z; nor.z = 0f; break;
                    }
                    points[selectedPoints[i]].normal = nor.normalized;
                    if (points[selectedPoints[i]].normal == Vector3.zero) points[selectedPoints[i]].normal = Vector3.up;
                    points[selectedPoints[i]].SetPosition(pos);
                    if (flatTangent)
                    {
                        Vector3 tan = points[selectedPoints[i]].tangent;
                        Vector3 tan2 = points[selectedPoints[i]].tangent2;
                        switch (axis)
                        {
                            case 0: tan.x = avg.x; tan2.x = avg.x; break;
                            case 1: tan.y = avg.y; tan2.y = avg.y; break;
                            case 2: tan.z = avg.z; tan2.z = avg.z; break;
                        }
                        points[selectedPoints[i]].SetTangentPosition(tan);
                        points[selectedPoints[i]].SetTangent2Position(tan2);
                    }
                }
            }
            else
            {
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    Vector3 tan = points[selectedPoints[i]].tangent;
                    Vector3 tan2 = points[selectedPoints[i]].tangent2;
                    Vector3 pos = points[selectedPoints[i]].position;
                    switch (axis)
                    {
                        case 0: tan.x = pos.x; tan2.x = pos.x; break;
                        case 1: tan.y = pos.y; tan2.y = pos.y; break;
                        case 2: tan.z = pos.z; tan2.z = pos.z; break;
                    }
                    points[selectedPoints[i]].SetTangentPosition(tan);
                    points[selectedPoints[i]].SetTangent2Position(tan2);
                }
            }
            ResetCurrentModule();
        }

        public void MirrorSelection(int axis)
        {
            bool mirrorTangents = false;
            if (_typeProperty.enumValueIndex == (int)Spline.Type.Bezier)
            {
                if (EditorUtility.DisplayDialog("Mirror tangents", "Do you want to mirror the tangents too ?", "Yes", "No")) mirrorTangents = true;
            }
            float min = 0f, max = 0f;
            switch (axis)
            {
                case 0: min = max = points[selectedPoints[0]].position.x; break;
                case 1: min = max = points[selectedPoints[0]].position.y; break;
                case 2: min = max = points[selectedPoints[0]].position.z; break;
            }
            RecordUndo("Mirror Selection");
            if (mirrorTangents)
            {
                float value = 0f;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[0]].tangent.x; break;
                    case 1: value = points[selectedPoints[0]].tangent.y; break;
                    case 2: value = points[selectedPoints[0]].tangent.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[0]].tangent2.x; break;
                    case 1: value = points[selectedPoints[0]].tangent2.y; break;
                    case 2: value = points[selectedPoints[0]].tangent2.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
            }
            for (int i = 1; i < selectedPoints.Count; i++)
            {
                float value = 0f;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[i]].position.x; break;
                    case 1: value = points[selectedPoints[i]].position.y; break;
                    case 2: value = points[selectedPoints[i]].position.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
                if (mirrorTangents)
                {
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent.x; break;
                        case 1: value = points[selectedPoints[i]].tangent.y; break;
                        case 2: value = points[selectedPoints[i]].tangent.z; break;
                    }
                    if (value < min) min = value;
                    if (value > max) max = value;
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent2.x; break;
                        case 1: value = points[selectedPoints[i]].tangent2.y; break;
                        case 2: value = points[selectedPoints[i]].tangent2.z; break;
                    }
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }

            for (int i = 0; i < selectedPoints.Count; i++)
            {
                float value = 0f;
                if (mirrorTangents)
                {
                    //Point position
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].position.x; break;
                        case 1: value = points[selectedPoints[i]].position.y; break;
                        case 2: value = points[selectedPoints[i]].position.z; break;
                    }
                    float percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    Vector3 pos = points[selectedPoints[i]].position;
                    switch (axis)
                    {
                        case 0: pos.x = value; break;
                        case 1: pos.y = value; break;
                        case 2: pos.z = value; break;
                    }
                    points[selectedPoints[i]].position = pos;
                    //Tangent 1
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent.x; break;
                        case 1: value = points[selectedPoints[i]].tangent.y; break;
                        case 2: value = points[selectedPoints[i]].tangent.z; break;
                    }
                    percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    Vector3 tan = points[selectedPoints[i]].tangent;
                    switch (axis)
                    {
                        case 0: tan.x = value; break;
                        case 1: tan.y = value; break;
                        case 2: tan.z = value; break;
                    }
                    points[selectedPoints[i]].tangent = tan;
                    //Tangent 2
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent2.x; break;
                        case 1: value = points[selectedPoints[i]].tangent2.y; break;
                        case 2: value = points[selectedPoints[i]].tangent2.z; break;
                    }
                    percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    tan = points[selectedPoints[i]].tangent2;
                    switch (axis)
                    {
                        case 0: tan.x = value; break;
                        case 1: tan.y = value; break;
                        case 2: tan.z = value; break;
                    }
                    points[selectedPoints[i]].tangent2 = tan;
                }
                else
                {
                    Vector3 pos = points[selectedPoints[i]].position;
                    switch (axis)
                    {
                        case 0: value = pos.x; break;
                        case 1: value = pos.y; break;
                        case 2: value = pos.z; break;
                    }
                    float percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: pos.x = value; break;
                        case 1: pos.y = value; break;
                        case 2: pos.z = value; break;
                    }
                    points[selectedPoints[i]].SetPosition(pos);
                }
                //Normal
                Vector3 nor = points[selectedPoints[i]].normal;
                switch (axis)
                {
                    case 0: nor.x *= -1f; break;
                    case 1: nor.y *= -1f; break;
                    case 2: nor.z *= -1f; break;
                }
                points[selectedPoints[i]].normal = nor.normalized;
            }
            ResetCurrentModule();
        }

        public void DistributeEvenly()
        {
            if (selectedPoints.Count < 3) return;
            RecordUndo("Distribute Evenly");
            int min = points.Length-1, max = 0;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (selectedPoints[i] < min) min = selectedPoints[i];
                if (selectedPoints[i] > max) max = selectedPoints[i];
            }
            double minPercent = (double)min / (points.Length - 1);
            double maxPercent = (double)max / (points.Length - 1);
            float length = calculateLength(minPercent, maxPercent);
            float step = length / (max - min);
            SplineSample evalResult = new SplineSample();
            evaluate(minPercent, ref evalResult);
            for (int i = min + 1; i < max; i++)
            {
                double percent = travel(evalResult.percent, step, Spline.Direction.Forward);
                evaluate(percent, ref evalResult);
                points[i].SetPosition(evalResult.position);
            }
            ResetCurrentModule();
        }

        public void LoopTriggerProperties(System.Action<SerializedProperty> onTrigger)
        {
            SerializedProperty triggerGroups = serializedObject.FindProperty("triggerGroups");

            for (int i = 0; i < triggerGroups.arraySize; i++)
            {
                SerializedProperty triggers = triggerGroups.GetArrayElementAtIndex(i).FindPropertyRelative("triggers");
                for (int j = 0; j < triggers.arraySize; j++)
                {
                    SerializedProperty trigger = triggers.GetArrayElementAtIndex(j);
                    onTrigger.Invoke(trigger);
                }
            }
        }

        public void AutoTangents()
        {
            RecordUndo("Auto Tangents");
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                int index = selectedPoints[i];
                Vector3 prevPos = points[index].position, forwardPos = points[index].position;
                if(index == 0 && points.Length > 1)
                {
                    prevPos = points[0].position + (points[0].position - points[1].position);
                } else prevPos = points[index - 1].position;
                if (index == points.Length-1 && points.Length > 1)
                {
                    forwardPos = points[points.Length-1].position + (points[points.Length - 1].position - points[points.Length - 2].position);
                }
                else forwardPos = points[index + 1].position;
                Vector3 delta = (forwardPos - prevPos) / 2f;
                points[index].tangent = points[index].position - delta / 3f;
                points[index].tangent2 = points[index].position + delta / 3f;
            }
            ResetCurrentModule();
        }

        public void SwapTangents()
        {
            RecordUndo("Swap Tangents");
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                int index = selectedPoints[i];
                Vector3 tempTangent = points[index].tangent;
                points[index].tangent = points[index].tangent2;
                points[index].tangent2 = tempTangent;
            }
            ResetCurrentModule();
        }

        public void FlipTangents()
        {
            RecordUndo("Flip Tangents");
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                int index = selectedPoints[i];
                points[index].tangent = points[index].position + (points[index].position - points[index].tangent);
                points[index].tangent2 = points[index].position + (points[index].position - points[index].tangent2);
            }
            ResetCurrentModule();
        }

        public void FlipFirstTangent()
        {
            RecordUndo("Flip First Tangent");
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                int index = selectedPoints[i];
                points[index].tangent2 = points[index].position + (points[index].position - points[index].tangent2);
            }
            ResetCurrentModule();
        }

        public void FlipSecondTangent()
        {
            RecordUndo("Flip Second Tangent");
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                int index = selectedPoints[i];
                points[index].tangent = points[index].position + (points[index].position - points[index].tangent);
            }
            ResetCurrentModule();
        }

        protected void ResetCurrentModule()
        {
            if (_module < 0 || _module >= _modules.Length) return;
            _modules[_module].Reset();
        }

        public class PointOperation
        {
            public string name = "";
            public System.Action action;
        }
    }
}
