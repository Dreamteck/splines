namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEditor;

    [CustomEditor(typeof(SplineComputer), true)]
    [CanEditMultipleObjects]
    public partial class SplineComputerEditor : Editor 
    {
        public SplineComputer spline;
        public SplineComputer[] splines = new SplineComputer[0];
        public static bool hold = false;

        public int[] pointSelection
        {
            get
            {
                return _selectedPoints.ToArray();
            }
        }

        public int selectedPointsCount
        {
            get { return _selectedPoints.Count; }
            set { }
        }

        protected bool closedOnMirror = false;

        private DreamteckSplinesEditor _pathEditor;
        private ComputerEditor _computerEditor;
        private SplineTriggersEditor _triggersEditor;
        private SplineComputerDebugEditor _debugEditor;
        private bool _rebuildSpline = false;
        private List<int> _selectedPoints = new List<int>();


        [MenuItem("GameObject/3D Object/Spline Computer")]
        private static void NewEmptySpline()
        {
            int count = GameObject.FindObjectsOfType<SplineComputer>().Length;
            string objName = "Spline";
            if (count > 0) objName += " " + count;
            GameObject obj = new GameObject(objName);
            obj.AddComponent<SplineComputer>();
            if (Selection.activeGameObject != null)
            {
                if (EditorUtility.DisplayDialog("Make child?", "Do you want to make the new spline a child of " + Selection.activeGameObject.name + "?", "Yes", "No"))
                {
                    obj.transform.parent = Selection.activeGameObject.transform;
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                }
            }
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/3D Object/Spline Node")]
        private static void NewSplineNode()
        {
            int count = Object.FindObjectsOfType<Node>().Length;
            string objName = "Node";
            if (count > 0) objName += " " + count;
            GameObject obj = new GameObject(objName);
            obj.AddComponent<Node>();
            if(Selection.activeGameObject != null)
            {
                if(EditorUtility.DisplayDialog("Make child?", "Do you want to make the new node a child of " + Selection.activeGameObject.name + "?", "Yes", "No"))
                {
                    obj.transform.parent = Selection.activeGameObject.transform;
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                }
            }
            Selection.activeGameObject = obj;
        }

        public void UndoRedoPerformed()
        {
            _pathEditor.UndoRedoPerformed();
            spline.EditorUpdateConnectedNodes();
            spline.Rebuild();
        }

        private void OnEnable()
        {
            splines = new SplineComputer[targets.Length];
            for (int i = 0; i < splines.Length; i++)
            {
                splines[i] = (SplineComputer)targets[i];
                splines[i].EditorAwake();
                if (splines[i].editorAlwaysDraw)
                {
                    DSSplineDrawer.RegisterComputer(splines[i]);
                }
            }
            spline = splines[0];
            InitializeSplineEditor();
            InitializeComputerEditor();
            _debugEditor = new SplineComputerDebugEditor(spline, serializedObject, _pathEditor);
            _debugEditor.undoHandler += RecordUndo;
            _debugEditor.repaintHandler += OnRepaint;
            _triggersEditor = new SplineTriggersEditor(spline, serializedObject);
            _triggersEditor.undoHandler += RecordUndo;
            _triggersEditor.repaintHandler += OnRepaint;
            hold = false;
#if UNITY_2019_1_OR_NEWER
            SceneView.beforeSceneGui += BeforeSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
#else
            SceneView.onSceneGUIDelegate += BeforeSceneGUI;
            SceneView.onSceneGUIDelegate += DuringSceneGUI;
#endif
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private void BeforeSceneGUI(SceneView current)
        {
            _pathEditor.BeforeSceneGUI(current);

            if (Event.current.type == EventType.MouseUp)
            {
                if (Event.current.button == 0)
                {
                    for (int i = 0; i < splines.Length; i++)
                    {
                        if (splines[i].editorUpdateMode == SplineComputer.EditorUpdateMode.OnMouseUp)
                        {
                            splines[i].RebuildImmediate();
                        }
                    }
                }
            }
        }

        private void InitializeSplineEditor()
        {
            _pathEditor = new DreamteckSplinesEditor(spline, serializedObject);
            _pathEditor.undoHandler = RecordUndo;
            _pathEditor.repaintHandler = OnRepaint;
            _pathEditor.editSpace = (SplineEditor.Space)SplinePrefs.pointEditSpace;
        }

        private void InitializeComputerEditor()
        {
            _computerEditor = new ComputerEditor(splines, serializedObject, _pathEditor);
            _computerEditor.undoHandler = RecordUndo;
            _computerEditor.repaintHandler = OnRepaint;
        }

        private void RecordUndo(string title)
        {
            for (int i = 0; i < splines.Length; i++)
            {
                Undo.RecordObject(splines[i], title);
            }
        }

        private void OnRepaint()
        {
            SceneView.RepaintAll();
            Repaint();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
#if UNITY_2019_1_OR_NEWER
            SceneView.beforeSceneGui -= BeforeSceneGUI;
            SceneView.duringSceneGui -= DuringSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= BeforeSceneGUI;
            SceneView.onSceneGUIDelegate -= DuringSceneGUI;
#endif
            _pathEditor.Destroy();
            _computerEditor.Destroy();
            _debugEditor.Destroy();
            _triggersEditor.Destroy();
        }

        public override void OnInspectorGUI()
        {
            if (_debugEditor.editorUpdateMode == SplineComputer.EditorUpdateMode.OnMouseUp)
            {
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    _rebuildSpline = true;
                }
                if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    _rebuildSpline = true;
                }
            }
            base.OnInspectorGUI();
            spline = (SplineComputer)target;

            if (splines.Length == 1)
            {
                SplineEditorGUI.BeginContainerBox(ref _pathEditor.open, "Edit");
                if (_pathEditor.open)
                {
                    SplineEditor.Space lastSpace = _pathEditor.editSpace;
                    _pathEditor.DrawInspector();
                    if (lastSpace != _pathEditor.editSpace)
                    {
                        SplinePrefs.pointEditSpace = (SplineComputer.Space)_pathEditor.editSpace;
                        SplinePrefs.SavePrefs();
                    }
                }
                else if (_pathEditor.lastEditorTool != Tool.None && Tools.current == Tool.None)
                {
                    Tools.current = _pathEditor.lastEditorTool;
                }
                SplineEditorGUI.EndContainerBox();
            }

            SplineEditorGUI.BeginContainerBox(ref _computerEditor.open, "Spline Computer");
            if (_computerEditor.open)
            {
                _computerEditor.DrawInspector();
            }
            SplineEditorGUI.EndContainerBox();

            if (splines.Length == 1)
            {
                SplineEditorGUI.BeginContainerBox(ref _triggersEditor.open, "Triggers");
                if (_triggersEditor.open) _triggersEditor.DrawInspector();
                SplineEditorGUI.EndContainerBox();
            }

            SplineEditorGUI.BeginContainerBox(ref _debugEditor.open, "Editor Properties");
            if (_debugEditor.open) _debugEditor.DrawInspector();
            SplineEditorGUI.EndContainerBox();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(spline);
            }


            if (Event.current.type == EventType.Layout && _rebuildSpline)
            {
                for (int i = 0; i < splines.Length; i++)
                {
                    if (splines[i].editorUpdateMode == SplineComputer.EditorUpdateMode.OnMouseUp)
                    {
                        splines[i].RebuildImmediate(true);
                    }
                }
                _rebuildSpline = false;
            }

        }

        public bool IsPointSelected(int index)
        {
            return _selectedPoints.Contains(index);
        }

        private void DuringSceneGUI(SceneView currentSceneView)
        {
            _debugEditor.DrawScene(currentSceneView);
            _computerEditor.drawComputer = !(_pathEditor.currentModule is CreatePointModule);
            _computerEditor.drawPivot = _pathEditor.open && spline.editorDrawPivot;
            _computerEditor.DrawScene(currentSceneView);
            if (splines.Length == 1 && _triggersEditor.open) _triggersEditor.DrawScene(currentSceneView);
            if (splines.Length == 1 && _pathEditor.open) _pathEditor.DrawScene(currentSceneView);
        }
    }
}
