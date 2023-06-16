namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class SplineUserSubEditor
    {
        protected string title = "";
        protected SplineUser user;
        protected SplineUserEditor editor = null;
        public bool alwaysOpen = false;

        public bool isOpen
        {
            get { return foldout || alwaysOpen; }
        }
        bool foldout = false;

        public SplineUserSubEditor(SplineUser user, SplineUserEditor editor)
        {
            this.editor = editor;
            this.user = user;
        }

        public virtual void DrawInspector()
        {
            if (!alwaysOpen)
            {
                foldout = EditorGUILayout.Foldout(foldout, title);
            }
        }

        public virtual void DrawScene()
        {

        }
    }
}
