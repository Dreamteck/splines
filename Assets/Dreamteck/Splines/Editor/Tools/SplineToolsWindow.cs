namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using UnityEditor;

    public class SplineToolsWindow : EditorWindow
    {
        private static SplineTool[] tools;
        private int toolIndex = -1;
        private Vector2 scroll = Vector2.zero;
        private const float menuWidth = 150f;
        [MenuItem("Window/Dreamteck/Splines/Tools")]
        static void Init()
        {
            SplineToolsWindow window = (SplineToolsWindow)EditorWindow.GetWindow(typeof(SplineToolsWindow));
            window.Show();
        }

        private void Awake()
        {
            titleContent = new GUIContent("Spline Tools");
            name = "Spline tools";
            autoRepaintOnSceneChange = true;

            List<Type> types = FindDerivedClasses.GetAllDerivedClasses(typeof(SplineTool));
            tools = new SplineTool[types.Count];
            int count = 0;
            foreach (Type t in types)
            {
                tools[count] = (SplineTool)Activator.CreateInstance(t);
                count++;
            } 
            if (toolIndex >= 0 && toolIndex < tools.Length) tools[toolIndex].Open(this);
        }

        void OnDestroy()
        {
            if (toolIndex >= 0 && toolIndex < tools.Length) tools[toolIndex].Close();
        }

        void OnGUI()
        {
            if (tools == null) Awake(); 
            GUI.color = new Color(0f, 0f, 0f, 0.15f);
            GUI.DrawTexture(new Rect(0, 0, menuWidth, position.height), SplineEditorGUI.white, ScaleMode.StretchToFill);
            GUI.color = Color.white;
            GUILayout.BeginHorizontal();
            GUILayout.BeginScrollView(scroll, GUILayout.Width(menuWidth), GUILayout.Height(position.height-10));
            if (tools == null) Init();
            SplineEditorGUI.SetHighlightColors(SplinePrefs.highlightColor, SplinePrefs.highlightContentColor);
            for (int i = 0; i < tools.Length; i ++)
            {
                if (SplineEditorGUI.EditorLayoutSelectableButton(new GUIContent(tools[i].GetName()), true, toolIndex == i))
                {
                    if (toolIndex >= 0 && toolIndex < tools.Length) tools[toolIndex].Close();
                    toolIndex = i;
                    if (toolIndex < tools.Length) tools[toolIndex].Open(this);
                }
            }
            GUILayout.EndScrollView();

           
            if(toolIndex >= 0 && toolIndex < tools.Length)
            {
                GUILayout.BeginVertical();
                tools[toolIndex].Draw(new Rect(menuWidth, 0, position.width - menuWidth - 5f, position.height - 10));
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
        
    }
}
