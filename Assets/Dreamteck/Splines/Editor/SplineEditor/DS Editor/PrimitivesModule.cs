namespace Dreamteck.Splines.Editor
{
    using System;
    using UnityEngine;
    using Dreamteck.Splines;
    using Dreamteck.Splines.Primitives;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public class PrimitivesModule : PointTransformModule
    {
        DreamteckSplinesEditor dsEditor = null;
        private PrimitiveEditor[] primitiveEditors;
        private string[] primitiveNames;
        private SplinePreset[] presets;
        private string[] presetNames;
        int mode = 0, selectedPrimitive = 0, selectedPreset = 0;
        bool createPresetMode = false;
        GUIContent[] toolbarContents = new GUIContent[2];
        Dreamteck.Editor.Toolbar toolbar;

        private string savePresetName = "", savePresetDescription = "";

        private bool lastClosed = false;
        private Spline.Type lastType = Spline.Type.Bezier;


        public PrimitivesModule(SplineEditor editor) : base(editor)
        {
            dsEditor = ((DreamteckSplinesEditor)editor);
            toolbarContents[0] = new GUIContent("Primitives", "Procedural Primitives");
            toolbarContents[1] = new GUIContent("Presets", "Saved spline presets");
            toolbar = new Dreamteck.Editor.Toolbar(toolbarContents, toolbarContents);
        }

        public override GUIContent GetIconOff()
        {
            return IconContent("*", "primitives", "Spline Primitives");
        }

        public override GUIContent GetIconOn()
        {
            return IconContent("*", "primitives_on", "Spline Primitives");
        }

        public override void LoadState()
        {
            base.LoadState();
            selectedPrimitive = LoadInt("selectedPrimitive");
            mode = LoadInt("mode");
            createPresetMode = LoadBool("createPresetMode");
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveInt("selectedPrimitive", selectedPrimitive);
            SaveInt("mode", mode);
            SaveBool("createPresetMode", createPresetMode);
        }

        public override void Select()
        {
            base.Select();
            lastClosed = editor.GetSplineClosed();
            lastType = editor.GetSplineType();
            if(mode == 0) LoadPrimitives();
            else if(!createPresetMode) LoadPresets();
        }

        public override void Deselect()
        {
            ApplyDialog();
            base.Deselect();
        }
        
        void ApplyDialog()
        {
            if (!IsDirty()) return;
            if (EditorUtility.DisplayDialog("Unapplied Primitives", "There is an unapplied primitive. Do you want to apply the changes?", "Apply", "Revert"))
            {
                Apply();
            }
            else
            {
                Revert();
            }
        }

        public override void Revert()
        {
            editor.SetSplineType(lastType);
            editor.SetSplineClosed(lastClosed);
            base.Revert();
        }

        protected override void OnDrawInspector()
        {
            EditorGUI.BeginChangeCheck();
            toolbar.Draw(ref mode);
            if (EditorGUI.EndChangeCheck())
            {
                if (mode == 0) LoadPrimitives();
                else if (!createPresetMode) LoadPresets();
                
            }
            if (selectedPoints.Count > 0) ClearSelection();
            if (mode == 0) PrimitivesGUI();
            else PresetsGUI();

            if (IsDirty() && (!createPresetMode || mode == 0))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply")) Apply();
                if (GUILayout.Button("Revert")) Revert();
                EditorGUILayout.EndHorizontal();
            }
        }

        void PrimitivesGUI()
        {
            int last = selectedPrimitive;
            selectedPrimitive = EditorGUILayout.Popup(selectedPrimitive, primitiveNames);
            if (last != selectedPrimitive)
            {
                primitiveEditors[selectedPrimitive].Open(dsEditor);
                primitiveEditors[selectedPrimitive].Update();
                TransformPoints();
            }

            EditorGUI.BeginChangeCheck();
            primitiveEditors[selectedPrimitive].Draw();
            if (EditorGUI.EndChangeCheck())
            {
                TransformPoints();
            }
        }

        void PresetsGUI()
        {
            if (createPresetMode)
            {
                savePresetName = EditorGUILayout.TextField("Preset name", savePresetName);
                EditorGUILayout.LabelField("Description");
                savePresetDescription = EditorGUILayout.TextArea(savePresetDescription);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save"))
                {
                    string lower = savePresetName.ToLower();
                    string noSlashes = lower.Replace('/', '_');
                    noSlashes = noSlashes.Replace('\\', '_');
                    string noSpaces = noSlashes.Replace(' ', '_');
                    SplinePreset preset = new SplinePreset(points, isClosed, splineType);
                    preset.name = savePresetName;
                    preset.description = savePresetDescription;
                    preset.Save(noSpaces);
                    createPresetMode = false;
                    LoadPresets();
                    savePresetName = savePresetDescription = "";
                }
                if (GUILayout.Button("Cancel")) createPresetMode = false;
                EditorGUILayout.EndHorizontal();
                return;
            }
            if (GUILayout.Button("Create New")) createPresetMode = true;
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            selectedPreset = EditorGUILayout.Popup(selectedPreset, presetNames, GUILayout.MaxWidth(Screen.width / 3f));
            if (selectedPreset >= 0 && selectedPreset < presets.Length)
            {
                if (GUILayout.Button("Use"))
                {
                    LoadPreset(selectedPreset);
                }
                if (GUILayout.Button("Delete", GUILayout.MaxWidth(80)))
                {
                    if (EditorUtility.DisplayDialog("Delete Preset", "This will permanently delete the preset file. Continue?", "Yes", "No"))
                    {
                        SplinePreset.Delete(presets[selectedPreset].filename);
                        LoadPresets();
                        if (selectedPreset >= presets.Length) selectedPreset = presets.Length - 1;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
        }

        void TransformPoints()
        {
            for (int i = 0; i < editor.points.Length; i++)
            {
                editor.points[i].position = dsEditor.spline.transform.TransformPoint(editor.points[i].position);
                editor.points[i].tangent = dsEditor.spline.transform.TransformPoint(editor.points[i].tangent);
                editor.points[i].tangent2 = dsEditor.spline.transform.TransformPoint(editor.points[i].tangent2);
                editor.points[i].normal = dsEditor.spline.transform.TransformDirection(editor.points[i].normal);
            }
            RegisterChange();
            SetDirty();
        }

        void LoadPrimitives()
        {
            List<Type> types = FindDerivedClasses.GetAllDerivedClasses(typeof(PrimitiveEditor));
            primitiveEditors = new PrimitiveEditor[types.Count];
            int count = 0;
            primitiveNames = new string[types.Count];
            foreach (Type t in types)
            {
                primitiveEditors[count] = (PrimitiveEditor)Activator.CreateInstance(t);
                primitiveNames[count] = primitiveEditors[count].GetName();
                count++;
            }

            if (selectedPrimitive >= 0 && selectedPrimitive < primitiveEditors.Length)
            {
                ClearSelection();
                primitiveEditors[selectedPrimitive].Open(dsEditor);
                primitiveEditors[selectedPrimitive].Update();
                TransformPoints();
                SetDirty();
            }
        }

        void LoadPresets()
        {
            ApplyDialog();
            presets = SplinePreset.LoadAll();
            presetNames = new string[presets.Length];
            for (int i = 0; i < presets.Length; i++)
            {
                presetNames[i] = presets[i].name;
            }
            ClearSelection();
        }

        void LoadPreset(int index)
        {
            if (index >= 0 && index < presets.Length)
            {
                editor.SetPointsArray(presets[index].points);
                editor.SetSplineClosed(presets[index].isClosed);
                editor.SetSplineType(presets[index].type);
                TransformPoints();
                FramePoints();
            }
        }
    }
}
