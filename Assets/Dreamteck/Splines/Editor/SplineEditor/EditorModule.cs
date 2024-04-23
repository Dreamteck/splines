namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class EditorModule
    {
        protected string prefPrefix = "";

        private bool _changed = false;

        public bool hasChanged { get { return _changed; } }

        protected SceneView _currentSceneView;


        protected void RegisterChange()
        {
            _changed = true;
        }

        public virtual void Select()
        {
            LoadState();
        }

        public virtual void Deselect()
        {
            SaveState();
        }

        public virtual void BeforeSceneDraw(SceneView current)
        {
            _currentSceneView = current;
        }

        public void DrawScene()
        {
            _changed = false;
            OnDrawScene();
        }

        protected virtual void OnDrawScene()
        {
        }

        public void DrawInspector()
        {
            _changed = false;
            OnDrawInspector();
        }

        protected virtual void OnDrawInspector()
        {
        }

        public virtual GUIContent GetIconOff()
        {
            return new GUIContent("OFF", "Point Module Off");
        }

        public virtual GUIContent GetIconOn()
        {
            return new GUIContent("ON", "Point Module On");
        }

        protected virtual void RecordUndo(string title)
        {
        }

        protected virtual void Repaint()
        {
        }

        protected void SaveBool(string variableName, bool value)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            EditorPrefs.SetBool(prefPrefix + "." + variableName, value);
        }

        protected void SaveInt(string variableName, int value)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            EditorPrefs.SetInt(prefPrefix + "." + variableName, value);
        }

        protected void SaveFloat(string variableName, float value)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            EditorPrefs.SetFloat(prefPrefix + "." + variableName, value);
        }

        protected void SaveString(string variableName, string value)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            EditorPrefs.SetString(prefPrefix + "." + variableName, value);
        }

        protected bool LoadBool(string variableName)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            return EditorPrefs.GetBool(prefPrefix + "." + variableName, false);
        }

        protected int LoadInt(string variableName, int defaultValue = 0)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            return EditorPrefs.GetInt(prefPrefix + "." + variableName, defaultValue);
        }

        protected float LoadFloat(string variableName, float d = 0f)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            return EditorPrefs.GetFloat(prefPrefix + "." + variableName, d);
        }

        protected string LoadString(string variableName)
        {
            if (prefPrefix == "") prefPrefix = GetType().ToString();
            return EditorPrefs.GetString(prefPrefix + "." + variableName, "");
        }

        public virtual void SaveState()
        {

        }

        public virtual void LoadState()
        {

        }

        internal static GUIContent IconContent(string title, string iconName, string description)
        {
            GUIContent content = new GUIContent(title, description);
            if (EditorGUIUtility.isProSkin)
            {
                iconName += "_dark";
            }
            Texture2D tex = ResourceUtility.EditorLoadTexture("Splines/Editor/Icons", iconName);
            if (tex != null)
            {
                content.image = tex;
                content.text = "";
            }
            return content;
        }
    }
}
