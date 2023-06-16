namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class ComputerEditorModule : EditorModule
    {
        protected SplineComputer spline;
        public SplineEditorBase.UndoHandler undoHandler;
        public EmptySplineHandler repaintHandler;

        public ComputerEditorModule(SplineComputer spline)
        {
            this.spline = spline;
        }

        protected override void RecordUndo(string title)
        {
            base.RecordUndo(title);
            if (undoHandler != null) undoHandler(title);
        }

        protected override void Repaint()
        {
            base.Repaint();
            if (repaintHandler != null) repaintHandler();
        }
    }
}
