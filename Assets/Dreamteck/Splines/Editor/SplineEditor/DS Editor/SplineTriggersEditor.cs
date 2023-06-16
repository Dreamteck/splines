namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class SplineTriggersEditor : SplineEditorBase
    {
        private int selected = -1, selectedGroup = -1;
        private bool renameTrigger = false, renameGroup = false;
        SplineComputer spline;
        SplineTrigger.Type addTriggerType = SplineTrigger.Type.Double;
        private int setDistanceGroup, setDistanceTrigger;

        public SplineTriggersEditor(SplineComputer spline, SerializedObject serializedObject) : base(serializedObject)
        {
            this.spline = spline;
        }

        protected override void Load()
        {
            base.Load();
            addTriggerType = (SplineTrigger.Type)LoadInt("addTriggerType");
        }

        protected override void Save()
        {
            base.Save();
            SaveInt("addTriggerType", (int)addTriggerType);
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < spline.triggerGroups.Length; i++) DrawGroupGUI(i);
            EditorGUILayout.Space();
            if(GUILayout.Button("New Group"))
            {
                RecordUndo("Add Trigger Group");
                TriggerGroup group = new TriggerGroup();
                group.name = "Trigger Group " + (spline.triggerGroups.Length+1);
                ArrayUtility.Add(ref spline.triggerGroups, group);
            }
            EditorGUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
        }

        public override void DrawScene(SceneView current)
        {
            base.DrawScene(current);

            if (spline == null) return;

            for (int i = 0; i < spline.triggerGroups.Length; i++)
            {
                if (!spline.triggerGroups[i].open) continue;
                DrawGroupScene(i);
            }
        }

        void DrawGroupScene(int index)
        {
            TriggerGroup group = spline.triggerGroups[index];
            for (int i = 0; i < group.triggers.Length; i++)
            {
                SplineComputerEditorHandles.SplineSliderGizmo gizmo = SplineComputerEditorHandles.SplineSliderGizmo.DualArrow;
                switch (group.triggers[i].type)
                {
                    case SplineTrigger.Type.Backward: gizmo = SplineComputerEditorHandles.SplineSliderGizmo.BackwardTriangle; break;
                    case SplineTrigger.Type.Forward: gizmo = SplineComputerEditorHandles.SplineSliderGizmo.ForwardTriangle; break;
                    case SplineTrigger.Type.Double: gizmo = SplineComputerEditorHandles.SplineSliderGizmo.DualArrow; break;
                }
                double last = group.triggers[i].position;
                if (SplineComputerEditorHandles.Slider(spline, ref group.triggers[i].position, group.triggers[i].color, group.triggers[i].name, gizmo) || last != group.triggers[i].position)
                {
                    Select(index, i);
                    Repaint();
                }
            }
        }

        void OnSetDistance(float distance)
        {
            SerializedObject serializedObject = new SerializedObject(spline);
            SerializedProperty groups = serializedObject.FindProperty("triggerGroups");
            SerializedProperty groupProperty = groups.GetArrayElementAtIndex(setDistanceGroup);

            SerializedProperty triggersProperty = groupProperty.FindPropertyRelative("triggers");
            SerializedProperty triggerProperty = triggersProperty.GetArrayElementAtIndex(setDistanceTrigger);

            SerializedProperty position = triggerProperty.FindPropertyRelative("position");

            double travel = spline.Travel(0.0, distance, Spline.Direction.Forward);
            position.floatValue = (float)travel;
            serializedObject.ApplyModifiedProperties();
        }

        void DrawGroupGUI(int index)
        {
            TriggerGroup group = spline.triggerGroups[index];
            SerializedObject serializedObject = new SerializedObject(spline);
            SerializedProperty groups = serializedObject.FindProperty("triggerGroups");
            SerializedProperty groupProperty = groups.GetArrayElementAtIndex(index);
            EditorGUI.indentLevel += 2;
            if(selectedGroup == index && renameGroup)
            {
                if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    renameGroup  = false;
                    Repaint();
                }
                group.name = EditorGUILayout.TextField(group.name);
            } else group.open = EditorGUILayout.Foldout(group.open, index + " - " + group.name);
            Rect lastRect = GUILayoutUtility.GetLastRect();
            if(lastRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Rename"), false, delegate { RecordUndo("Rename Trigger Group"); selectedGroup = index; renameGroup = true; renameTrigger = false; Repaint(); });
                menu.AddItem(new GUIContent("Delete"), false, delegate {
                    RecordUndo("Delete Trigger Group");
                    ArrayUtility.RemoveAt(ref spline.triggerGroups, index);
                    Repaint();
                });
                menu.ShowAsContext();
            }
            EditorGUI.indentLevel -= 2;
            if (!group.open) return;

            for (int i = 0; i < group.triggers.Length; i++) DrawTriggerGUI(i, index, groupProperty);
            if (GUI.changed) serializedObject.ApplyModifiedProperties();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Trigger"))
            {
                RecordUndo("Add Trigger");
                SplineTrigger newTrigger = new SplineTrigger(addTriggerType);
                newTrigger.name = "Trigger " + (group.triggers.Length + 1);
                ArrayUtility.Add(ref group.triggers, newTrigger);
            }
            addTriggerType = (SplineTrigger.Type)EditorGUILayout.EnumPopup(addTriggerType);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        void Select(int group, int trigger)
        {
            selected = trigger;
            selectedGroup = group;
            renameTrigger = false;
            renameGroup = false;
            Repaint();
        }

        void DrawTriggerGUI(int index, int groupIndex, SerializedProperty groupProperty)
        {
            bool isSelected = selected == index && selectedGroup == groupIndex;
            TriggerGroup group = spline.triggerGroups[groupIndex];
            SplineTrigger trigger = group.triggers[index];
            SerializedProperty triggersProperty = groupProperty.FindPropertyRelative("triggers");
            SerializedProperty triggerProperty = triggersProperty.GetArrayElementAtIndex(index);
            SerializedProperty eventProperty = triggerProperty.FindPropertyRelative("onCross");
            SerializedProperty positionProperty = triggerProperty.FindPropertyRelative("position");
            SerializedProperty colorProperty = triggerProperty.FindPropertyRelative("color");
            SerializedProperty nameProperty = triggerProperty.FindPropertyRelative("name");
            SerializedProperty enabledProperty = triggerProperty.FindPropertyRelative("enabled");
            SerializedProperty workOnceProperty = triggerProperty.FindPropertyRelative("workOnce");
            SerializedProperty typeProperty = triggerProperty.FindPropertyRelative("type");

            Color col = colorProperty.colorValue;
            if (isSelected) col.a = 1f;
            else col.a = 0.6f;
            GUI.backgroundColor = col;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;
            if (trigger == null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("NULL");
                if (GUILayout.Button("x")) ArrayUtility.RemoveAt(ref group.triggers, index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }


            if (isSelected && renameTrigger)
            {
                if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    renameTrigger = false;
                    Repaint();
                }
                nameProperty.stringValue = EditorGUILayout.TextField(nameProperty.stringValue);
            }
            else
            {
                EditorGUILayout.LabelField(nameProperty.stringValue);
            }

            if (isSelected)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(enabledProperty);
                EditorGUILayout.PropertyField(colorProperty);

                EditorGUILayout.BeginHorizontal();
                positionProperty.floatValue = EditorGUILayout.Slider("Position", positionProperty.floatValue, 0f, 1f);
                if (GUILayout.Button("Set Distance", GUILayout.Width(85)))
                {
                    DistanceWindow w = EditorWindow.GetWindow<DistanceWindow>(true);
                    w.Init(OnSetDistance, spline.CalculateLength());
                    setDistanceGroup = groupIndex;
                    setDistanceTrigger = index;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(typeProperty);
                EditorGUILayout.PropertyField(workOnceProperty);

                EditorGUILayout.PropertyField(eventProperty);
            }
            EditorGUILayout.EndVertical();

            Rect lastRect = GUILayoutUtility.GetLastRect();
            if (lastRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0) Select(groupIndex, index);
                else if (Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Deselect"), false, delegate { Select(-1, -1); });
                    menu.AddItem(new GUIContent("Rename"), false, delegate { Select(groupIndex, index); renameTrigger = true; renameGroup = false; });
                    if (index > 0)
                    {
                        menu.AddItem(new GUIContent("Move Up"), false, delegate {
                            RecordUndo("Move Trigger Up");
                            SplineTrigger temp = group.triggers[index - 1];
                            group.triggers[index - 1] = trigger;
                            group.triggers[index] = temp;
                            selected--;
                            renameTrigger = false;
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Move Up"));
                    }
                    if (index < group.triggers.Length - 1)
                    {
                        menu.AddItem(new GUIContent("Move Down"), false, delegate {
                            RecordUndo("Move Trigger Down");
                            SplineTrigger temp = group.triggers[index + 1];
                            group.triggers[index + 1] = trigger;
                            group.triggers[index] = temp;
                            selected--;
                            renameTrigger = false;
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Move Down"));
                    }

                    menu.AddItem(new GUIContent("Duplicate"), false, delegate {
                        RecordUndo("Duplicate Trigger");
                        SplineTrigger newTrigger = new SplineTrigger(SplineTrigger.Type.Double);
                        newTrigger.color = colorProperty.colorValue;
                        newTrigger.enabled = enabledProperty.boolValue;
                        newTrigger.position = positionProperty.floatValue;
                        newTrigger.type = (SplineTrigger.Type) typeProperty.intValue;
                        newTrigger.name = "Trigger " + (group.triggers.Length + 1);
                        ArrayUtility.Add(ref group.triggers, newTrigger);
                        Select(groupIndex, group.triggers.Length - 1);
                    });
                    menu.AddItem(new GUIContent("Delete"), false, delegate {
                        RecordUndo("Delete Trigger");
                        ArrayUtility.RemoveAt(ref group.triggers, index);
                        Select(-1, -1);
                    });
                    menu.ShowAsContext();
                }
            }
        }
    }
}
