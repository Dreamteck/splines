using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace Dreamteck.Splines
{
    public class CatenaryTool : SplineTool
    {
        protected GameObject obj;
        protected ObjectController spawner;
        private float _sag = 0f;
        private float _minSagDistance = 0f;
        private float _maxSagDistance = 10f;
        private Dictionary<SplineComputer, SplinePoint[]> _editSplines = new Dictionary<SplineComputer, SplinePoint[]>();

        public override string GetName()
        {
            return "Catenary Tool";
        }

        protected override string GetPrefix()
        {
            return "CatenaryTool";
        }

        public override void Open(EditorWindow window)
        {
            base.Open(window);
            _sag = EditorPrefs.GetFloat("DreamteckSplines.CatenaryTool._sag", 0f);
            _minSagDistance = EditorPrefs.GetFloat("DreamteckSplines.CatenaryTool._minSagDistance", 0f);
            _maxSagDistance = EditorPrefs.GetFloat("DreamteckSplines.CatenaryTool._maxSagDistance", 10f);
        }

        public override void Close()
        {
            base.Close();
            EditorPrefs.SetFloat("DreamteckSplines.CatenaryTool._sag", _sag);
            EditorPrefs.SetFloat("DreamteckSplines.CatenaryTool._minSagDistance", _minSagDistance);
            EditorPrefs.SetFloat("DreamteckSplines.CatenaryTool._maxSagDistance", _minSagDistance);
        }

        public override void Draw(Rect windowRect)
        {
            base.Draw(windowRect);
            if(_editSplines.Keys.Count == 0 && splines.Count > 0)
            {
                if(GUILayout.Button("Convert Selected"))
                {
                    ConvertSelected();
                }
            } else
            {
                EditorGUI.BeginChangeCheck();
                _sag = EditorGUILayout.FloatField("Sag", _sag);
                _minSagDistance = EditorGUILayout.FloatField("Min Distance", _minSagDistance);
                _maxSagDistance = EditorGUILayout.FloatField("Max Distance", _maxSagDistance);

                var keys = _editSplines.Keys;
                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                    foreach (var key in keys)
                    {
                        for (int i = 0; i < key.pointCount; i++)
                        {
                            ModifyPoint(key, i);
                        }
                        key.SetPoints(_editSplines[key]);
                    }
                }
                
                if (GUILayout.Button("Apply"))
                {
                    foreach (var key in keys)
                    {
                        EditorUtility.SetDirty(key);
                    }
                    _editSplines.Clear();
                }
            }
        }

        private void ModifyPoint(SplineComputer spline, int index)
        {
            var current = _editSplines[spline][index];
            if(index > 0)
            {
                var previous = _editSplines[spline][index - 1];
                Vector3 prevDirection = (previous.position - current.position)/3f;
                float sagAmount = Mathf.InverseLerp(_minSagDistance, _maxSagDistance, prevDirection.magnitude) * _sag;
                current.SetTangentPosition(current.position + prevDirection + Vector3.down * sagAmount);
            }

            if(index < _editSplines[spline].Length - 1)
            {
                var next = _editSplines[spline][index + 1];
                Vector3 nextDirection = (next.position - current.position) / 3f;
                float sagAmount = Mathf.InverseLerp(_minSagDistance, _maxSagDistance, nextDirection.magnitude) * _sag;
                current.SetTangent2Position(current.position + nextDirection + Vector3.down * sagAmount);
            }
            _editSplines[spline][index] = current;
        }

        private void ConvertSelected()
        {
            _editSplines.Clear();
            foreach(var spline in splines)
            {
                var points = spline.GetPoints();
                for (int i = 0; i < points.Length; i++)
                {
                    points[i].type = SplinePoint.Type.Broken;
                }
                spline.type = Spline.Type.Bezier;
                _editSplines.Add(spline, points);
            }
        }
    }
}
