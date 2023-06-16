namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class SplineEditorBase
    {
        public bool open = false;
        public EditorGUIEvents eventModule = null;

        public delegate void UndoHandler(string title);
        public delegate void EmptyHandler();

        public UndoHandler undoHandler;
        public EmptyHandler repaintHandler;

        protected bool gizmosEnabled
        {
            get { return _gizmosEnabled; }
        }

        private bool _gizmosEnabled = true;

        protected readonly SerializedObject serializedObject;

        public SplineEditorBase(SerializedObject serializedObject)
        {
            Load();
            this.serializedObject = serializedObject;
            eventModule = new EditorGUIEvents();
        }

        public virtual void Destroy()
        {
            Save();
        }

        protected virtual void Load()
        {
            open = LoadBool("open");
        }

        protected virtual void Save()
        {
            SaveBool("open", open);
        }

        public virtual void DrawInspector()
        {
            if(SceneView.lastActiveSceneView != null)
            {
#if UNITY_2019_1_OR_NEWER
                _gizmosEnabled = SceneView.lastActiveSceneView.drawGizmos;
#endif
            }
            eventModule.Update(Event.current);
        }

        public virtual void DrawScene(SceneView current)
        {
            eventModule.Update(Event.current);
        }

        protected virtual void RecordUndo(string title)
        {
            if (undoHandler != null) undoHandler(title);
        }

        protected virtual void Repaint()
        {
            if (repaintHandler != null)
            {
                repaintHandler();
            }
        }

        public virtual void UndoRedoPerformed()
        {
            
        }

        protected string GetSaveName(string valueName)
        {
            return GetType().FullName + "." + valueName;
        }

        protected void SaveBool(string variableName, bool value)
        {
            EditorPrefs.SetBool(GetType().ToString() + "." + variableName, value);
        }

        protected void SaveInt(string variableName, int value)
        {
            EditorPrefs.SetInt(GetType().ToString() + "." + variableName, value);
        }

        protected void SaveFloat(string variableName, float value)
        {
            EditorPrefs.SetFloat(GetType().ToString() + "." + variableName, value);
        }

        protected void SaveString(string variableName, string value)
        {
            EditorPrefs.SetString(GetType().ToString() + "." + variableName, value);
        }

        protected bool LoadBool(string variableName, bool defaultValue = false)
        {
            return EditorPrefs.GetBool(GetType().ToString() + "." + variableName, defaultValue);
        }

        protected int LoadInt(string variableName, int defaultValue = 0)
        {
            return EditorPrefs.GetInt(GetType().ToString() + "." + variableName, defaultValue);
        }

        protected float LoadFloat(string variableName, float d = 0f)
        {
            return EditorPrefs.GetFloat(GetType().ToString() + "." + variableName, d);
        }

        protected string LoadString(string variableName)
        {
            return EditorPrefs.GetString(GetType().ToString() + "." + variableName, "");
        }
    }
}
