namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(SplineMorph))]
    public class SplineMorphEditor : Editor
    {
        private string addName = "";
        bool rename = false;
        int selected = -1;

        SplineMorph morph;

        private void OnEnable()
        {
            morph = (SplineMorph)target;
            GetAddName();
        }

        void GetAddName()
        {
            addName = "Channel " + morph.GetChannelCount();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Undo.RecordObject(morph, "Edit Morph");
            morph.spline = (SplineComputer)EditorGUILayout.ObjectField("Spline", morph.spline, typeof(SplineComputer), true);
            morph.space = (SplineComputer.Space)EditorGUILayout.EnumPopup("Space", morph.space);
            morph.cycle = EditorGUILayout.Toggle("Runtime Cycle", morph.cycle);
            if (morph.cycle)
            {
                EditorGUI.indentLevel++;
                morph.cycleMode = (SplineMorph.CycleMode)EditorGUILayout.EnumPopup("Cycle Wrap", morph.cycleMode);
                morph.cycleUpdateMode = (SplineMorph.UpdateMode)EditorGUILayout.EnumPopup("Update Mode", morph.cycleUpdateMode);
                morph.cycleDuration = EditorGUILayout.FloatField("Cycle Duration", morph.cycleDuration);
                EditorGUI.indentLevel--;
            }

            int channelCount = morph.GetChannelCount();
            if (channelCount > 0)
            {
                if(morph.spline == null)
                {
                    EditorGUILayout.HelpBox("No spline assigned.", MessageType.Error);
                    return;
                }
                if (morph.GetSnapshot(0).Length != morph.spline.pointCount)
                {
                    EditorGUILayout.HelpBox("Recorded morphs require the spline to have " + morph.GetSnapshot(0).Length + ". The spline has " + morph.spline.pointCount, MessageType.Error);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Clear morph states"))
                    {
                        if (EditorUtility.DisplayDialog("Clear morph states?", "Do you want to clear all morph states?", "Yes", "No"))
                        {
                            morph.Clear();
                        }
                    }
                    string str = "Reduce";
                    if (morph.GetSnapshot(0).Length > morph.spline.pointCount) str = "Increase";
                    if (GUILayout.Button(str + " spline points"))
                    {
                        if (EditorUtility.DisplayDialog(str + " spline points?", "Do you want to " + str + " the spline points?", "Yes", "No"))
                        {
                            morph.spline.SetPoints(morph.GetSnapshot(0), SplineComputer.Space.Local);
                        }
                    }

                    if (GUILayout.Button("Update Morph States"))
                    {
                        if (EditorUtility.DisplayDialog("Update morph states?", "This will add or delete the needed spline points to all morph states", "Yes", "No"))
                        {
                            for (int i = 0; i < morph.GetChannelCount(); i++)
                            {
                                var points = morph.GetSnapshot(i);
                                while (points.Length < morph.spline.pointCount)
                                {
                                    Dreamteck.ArrayUtility.Add(ref points, new SplinePoint());
                                }

                                while (points.Length > morph.spline.pointCount)
                                {
                                    Dreamteck.ArrayUtility.RemoveAt(ref points, points.Length-1);
                                }

                                morph.SetSnapshot(i, points);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    return;
                }
            }

            for (int i = 0; i < channelCount; i++) DrawChannel(i);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(40)))
            {
                morph.AddChannel(addName);
                GetAddName();
            }
            addName = EditorGUILayout.TextField(addName);
            
            EditorGUILayout.EndHorizontal();
            if (GUI.changed) SceneView.RepaintAll();
        }

        void DrawChannel(int index)
        {
            SplineMorph.Channel channel = morph.GetChannel(index);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;
            if (selected == index && rename)
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return) rename = false;
                channel.name = EditorGUILayout.TextField(channel.name);
            }
            else if (index > 0)
            {
                float weight = morph.GetWeight(index);
                float lastWeight = weight;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("●", "Capture Snapshot"), GUILayout.Width(22f))) morph.CaptureSnapshot(index);
                EditorGUILayout.LabelField(channel.name, GUILayout.Width(EditorGUIUtility.labelWidth));
                weight = EditorGUILayout.Slider(weight, 0f, 1f);
                EditorGUILayout.EndHorizontal();
                if (lastWeight != weight) morph.SetWeight(index, weight);
                SplineMorph.Channel.Interpolation lastInterpolation = channel.interpolation;
                channel.interpolation = (SplineMorph.Channel.Interpolation)EditorGUILayout.EnumPopup("Interpolation", channel.interpolation);
                if (lastInterpolation != channel.interpolation) morph.UpdateMorph();

                channel.curve = EditorGUILayout.CurveField("Curve", channel.curve);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("●", "Capture Snapshot"), GUILayout.Width(22f))) morph.CaptureSnapshot(index);
                GUILayout.Label(channel.name);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            Rect last = GUILayoutUtility.GetLastRect();
            if (last.Contains(Event.current.mousePosition))
            {
                if(Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.button == 0)
                    {
                        rename = false;
                        selected = -1;
                        Repaint();
                    }
                    if (Event.current.button == 1)
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Rename"), false, delegate { rename = true; selected = index; });
                        menu.AddItem(new GUIContent("Delete"), false, delegate
                        {
                            morph.SetWeight(index, 0f);
                            morph.RemoveChannel(index);
                            selected = -1;
                            GetAddName();
                        });
                        menu.ShowAsContext();
                    }
                }
            }
        }
    }
}
