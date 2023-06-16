
namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class Explorer : SplineTool
    {
        GUIStyle normalRow;
        GUIStyle selectedRow;
        List<SplineComputer> sceneSplines = new List<SplineComputer>();
        List<int> selected = new List<int>();
        Vector2 scroll = Vector2.zero;
        bool mouseLeft = false;

        public override string GetName()
        {
            return "Spline Explorer";
        }

        protected override string GetPrefix()
        {
            return "SplineExplorer";
        }

        public override void Open(EditorWindow window)
        {
            base.Open(window);
            normalRow = new GUIStyle(GUI.skin.box);
            normalRow.normal.background = null;
            normalRow.alignment = TextAnchor.MiddleLeft;
            selectedRow = new GUIStyle(normalRow);
            selectedRow.normal.background = SplineEditorGUI.white;
            selectedRow.normal.textColor = SplinePrefs.highlightContentColor;
            GetSceneSplines();
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnScene;
#else
            SceneView.onSceneGUIDelegate += OnScene;
#endif

        }

        public override void Close()
        {
            base.Close();
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnScene;
#else
            SceneView.onSceneGUIDelegate -= OnScene;
#endif

        }

        void OnScene(SceneView current)
        {
            if(selected.Count > 1)
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    if (!sceneSplines[selected[i]].editorAlwaysDraw)
                    {
                        DSSplineDrawer.DrawSplineComputer(sceneSplines[selected[i]]);
                    }
                }
            }
        }

        void GetSceneSplines()
        {
            sceneSplines = new List<SplineComputer>(Resources.FindObjectsOfTypeAll<SplineComputer>());
        }

        public override void Draw(Rect rect)
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0) mouseLeft = true; 
                    break;
                case EventType.MouseUp: if (Event.current.button == 0) mouseLeft = false; break;
            }

            Rect lastRect;
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Width(rect.width), GUILayout.Height(rect.height));
            EditorGUILayout.BeginHorizontal(normalRow);
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(rect.width - 200));
            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel, GUILayout.Width(65));
            EditorGUILayout.LabelField("Draw", EditorStyles.boldLabel, GUILayout.Width(40));
            EditorGUILayout.LabelField("Thickness", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < sceneSplines.Count; i++)
            {
                bool isSelected = selected.Contains(i);
                if (isSelected) GUI.backgroundColor = SplinePrefs.highlightColor;
                
                EditorGUILayout.BeginHorizontal(isSelected ? selectedRow : normalRow);
                EditorGUILayout.LabelField(sceneSplines[i].name, isSelected ? selectedRow : normalRow, GUILayout.Width(rect.width-200));
                GUI.backgroundColor = Color.white;
                Color pathColor = sceneSplines[i].editorPathColor;
                pathColor = EditorGUILayout.ColorField(pathColor, GUILayout.Width(65));
                if(pathColor != sceneSplines[i].editorPathColor)
                {
                    foreach (int index in selected) sceneSplines[index].editorPathColor = pathColor;
                }
                bool alwaysDraw = sceneSplines[i].editorAlwaysDraw;
                alwaysDraw = EditorGUILayout.Toggle(alwaysDraw, GUILayout.Width(40));
                if(alwaysDraw != sceneSplines[i].editorAlwaysDraw)
                {
                    foreach (int index in selected)
                    {
                        if (alwaysDraw)
                        {
                            DSSplineDrawer.RegisterComputer(sceneSplines[index]);
                        }
                        else
                        {
                            DSSplineDrawer.UnregisterComputer(sceneSplines[index]);
                        }
                    }
                }
                bool thickness = sceneSplines[i].editorDrawThickness;
                thickness = EditorGUILayout.Toggle(thickness, GUILayout.Width(40));
                if(thickness != sceneSplines[i].editorDrawThickness)
                {
                    foreach (int index in selected) sceneSplines[index].editorDrawThickness = thickness;
                }
                EditorGUILayout.EndHorizontal();
                lastRect = GUILayoutUtility.GetLastRect();
                if (mouseLeft)
                {
                    if (lastRect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.control)
                        {
                            if (!selected.Contains(i)) selected.Add(i);
                        }
                        else if (selected.Count > 0 && Event.current.shift)
                        {
                            int closest = selected[0];
                            int delta = sceneSplines.Count;
                            for (int j = 0; j < selected.Count; j++)
                            {
                                int d = Mathf.Abs(i - selected[j]);
                                if (d < delta)
                                {
                                    delta = d;
                                    closest = selected[j];
                                }
                            }
                            if (closest < i)
                            {
                                for (int j = closest + 1; j <= i; j++)
                                {
                                    if (selected.Contains(j)) continue;
                                    selected.Add(j);
                                }
                            }
                            else
                            {
                                for (int j = closest - 1; j >= i; j--)
                                {
                                    if (selected.Contains(j)) continue;
                                    selected.Add(j);
                                }
                            }
                        }
                        else selected = new List<int>(new int[] { i });
                        List<GameObject> selectGo = new List<GameObject>();
                        foreach(int index in selected) selectGo.Add(sceneSplines[index].gameObject);
                        Selection.objects = selectGo.ToArray();
                        Repaint();
                    }
                }
            }
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
            EditorGUILayout.EndScrollView();
            if(Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    if (selected.Count > 0) selected = new List<int>(new int[] { selected[0] });
                    else selected[0]++;
                }
                else if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    if (selected.Count > 0) selected = new List<int>(new int[] { selected[selected.Count - 1] });
                    else selected[0]++;
                }
                if (selected.Count == 0) return;
                if (selected[0] < 0) selected[0] = sceneSplines.Count - 1;
                if (selected[0] >= sceneSplines.Count) selected[0] = 0;
                if (sceneSplines.Count > 0) Selection.activeGameObject = sceneSplines[selected[0]].gameObject;
            }
        }
    }
}
