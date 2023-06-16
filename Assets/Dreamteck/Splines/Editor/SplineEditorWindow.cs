namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    public class SplineEditorWindow : EditorWindow
    {
        protected Editor editor;
        protected SplineComputerEditor splineEditor;

        public void Init(Editor e, string inputTitle, Vector2 min, Vector2 max)
        {
            minSize = min;
            maxSize = max;
            Init(e, inputTitle);
        }

        public void Init(Editor e, Vector2 min, Vector2 max)
        {
            minSize = min;
            maxSize = max;
            Init(e);
        }

        public void Init(Editor e, Vector2 size)
        {
            minSize = maxSize = size;
            Init(e);
        }

        public void Init(Editor e, string inputTitle)
        {
            Init(e);
            Title(inputTitle);
        }

        public void Init(Editor e)
        {
            editor = e;
            if (editor is SplineComputerEditor) splineEditor = (SplineComputerEditor)editor;
            else splineEditor = null;
            Title(GetTitle());
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {

        }

        protected virtual string GetTitle()
        {
            return "Spline Editor Window";
        }

        private void Title(string inputTitle)
        {
            titleContent = new GUIContent(inputTitle);
        }
    }
}
