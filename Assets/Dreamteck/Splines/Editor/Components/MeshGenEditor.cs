namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(MeshGenerator))]
    [CanEditMultipleObjects]
    public class MeshGenEditor : SplineUserEditor
    {
        protected bool showSize = true;
        protected bool showColor = true;
        protected bool showDoubleSided = true;
        protected bool showFlipFaces = true;
        protected bool showRotation = true;
        protected bool showInfo = false;
        protected bool showOffset = true;
        protected bool showTangents = true;
        protected bool showNormalMethod = true;

        private int _framesPassed = 0;
        private bool _commonFoldout = false;

        MeshGenerator[] generators = new MeshGenerator[0];

        BakeMeshWindow bakeWindow = null;

        protected override void DuringSceneGUI(SceneView currentSceneView)
        {
            base.DuringSceneGUI(currentSceneView);
            MeshGenerator generator = (MeshGenerator)target;
            if (Application.isPlaying) return;
            _framesPassed++;
            if(_framesPassed >= 100)
            {
                _framesPassed = 0;
                if (generator != null && generator.GetComponent<MeshCollider>() != null) generator.UpdateCollider();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            generators = new MeshGenerator[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                generators[i] = (MeshGenerator)targets[i];
            }
            MeshGenerator user = (MeshGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            MeshGenerator generator = (MeshGenerator)target;
            if (generator.baked)
            {
                SplineEditorGUI.SetHighlightColors(SplinePrefs.highlightColor, SplinePrefs.highlightContentColor);
                if (SplineEditorGUI.EditorLayoutSelectableButton(new GUIContent("Revert Bake", "Makes the mesh dynamic again and allows editing"), true, true))
                {
                    for (int i = 0; i < generators.Length; i++)
                    {
                        generators[i].Unbake();
                        EditorUtility.SetDirty(generators[i]);
                    }
                }
                return;
            }
            base.OnInspectorGUI();
        }

        protected override void BodyGUI()
        {
            base.BodyGUI();
            MeshGenerator generator = (MeshGenerator)target;
            serializedObject.Update();
            SerializedProperty calculateTangents = serializedObject.FindProperty("_calculateTangents");
            SerializedProperty markDynamic = serializedObject.FindProperty("_markDynamic");
            SerializedProperty size = serializedObject.FindProperty("_size");
            SerializedProperty color = serializedObject.FindProperty("_color");
            SerializedProperty normalMethod = serializedObject.FindProperty("_normalMethod");
            SerializedProperty useSplineSize = serializedObject.FindProperty("_useSplineSize");
            SerializedProperty useSplineColor = serializedObject.FindProperty("_useSplineColor");
            SerializedProperty offset = serializedObject.FindProperty("_offset");
            SerializedProperty rotation = serializedObject.FindProperty("_rotation");
            SerializedProperty flipFaces = serializedObject.FindProperty("_flipFaces");
            SerializedProperty doubleSided = serializedObject.FindProperty("_doubleSided");
            SerializedProperty meshIndexFormat = serializedObject.FindProperty("_meshIndexFormat");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();

            _commonFoldout = EditorGUILayout.Foldout(_commonFoldout, "Common", foldoutHeaderStyle);
            if (_commonFoldout)
            {
                EditorGUI.indentLevel++;
                if (showSize) EditorGUILayout.PropertyField(size, new GUIContent("Size"));
                if (showColor) EditorGUILayout.PropertyField(color, new GUIContent("Color"));
                if (showNormalMethod) EditorGUILayout.PropertyField(normalMethod, new GUIContent("Normal Method"));
                if (showOffset) EditorGUILayout.PropertyField(offset, new GUIContent("Offset"));
                if (showRotation) EditorGUILayout.PropertyField(rotation, new GUIContent("Rotation"));
                if (showTangents) EditorGUILayout.PropertyField(calculateTangents, new GUIContent("Calculate Tangents"));

                EditorGUILayout.PropertyField(useSplineSize, new GUIContent("Use Spline Size"));
                EditorGUILayout.PropertyField(useSplineColor, new GUIContent("Use Spline Color"));
                EditorGUILayout.PropertyField(markDynamic, new GUIContent("Mark Dynamic", "Improves performance in situations where the mesh changes frequently"));
                EditorGUILayout.PropertyField(meshIndexFormat, new GUIContent("Index Format", "Format of the mesh index buffer data. Index buffer can either be 16 bit(supports up to 65535 vertices in a mesh), or 32 bit(supports up to 4 billion vertices).Default index format is 16 bit, since that takes less memory and bandwidth."));
                if(meshIndexFormat.enumValueIndex > 0)
                {
                    EditorGUILayout.HelpBox("Note that GPU support for 32 bit indices is not guaranteed on all platforms; for example Android devices with Mali-400 GPU do not support them.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }

            if (showDoubleSided || showFlipFaces)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Faces", EditorStyles.boldLabel);
                if (showDoubleSided) EditorGUILayout.PropertyField(doubleSided, new GUIContent("Double-sided"));
                if (!generator.doubleSided && showFlipFaces) EditorGUILayout.PropertyField(flipFaces, new GUIContent("Flip Faces"));
            }

            if (generator.GetComponent<MeshCollider>() != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mesh Collider", EditorStyles.boldLabel);
                generator.colliderUpdateRate = EditorGUILayout.FloatField("Collider Update Iterval", generator.colliderUpdateRate);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < generators.Length; i++)
                {
                    generators[i].Rebuild();
                }
            }
        }

        protected override void FooterGUI()
        {
            base.FooterGUI();
            showInfo = EditorGUILayout.Foldout(showInfo, "Info & Components");
            if (showInfo)
            {
                MeshGenerator generator = (MeshGenerator)target;
                MeshFilter filter = generator.GetComponent<MeshFilter>();
                if (filter == null) return;
                MeshRenderer renderer = generator.GetComponent<MeshRenderer>();
                string str = "";
                if (filter.sharedMesh != null) str = "Vertices: " + filter.sharedMesh.vertexCount + "\r\nTriangles: " + (filter.sharedMesh.triangles.Length / 3);
                else str = "No info available";
                EditorGUILayout.HelpBox(str, MessageType.Info);
                bool showFilter = filter.hideFlags == HideFlags.None;
                bool last = showFilter;
                showFilter = EditorGUILayout.Toggle("Show Mesh Filter", showFilter);
                if (last != showFilter)
                {
                    if (showFilter) filter.hideFlags = HideFlags.None;
                    else filter.hideFlags = HideFlags.HideInInspector;
                }
                bool showRenderer = renderer.hideFlags == HideFlags.None;
                last = showRenderer;
                showRenderer = EditorGUILayout.Toggle("Show Mesh Renderer", showRenderer);
                if (last != showRenderer)
                {
                    if (showRenderer) renderer.hideFlags = HideFlags.None;
                    else renderer.hideFlags = HideFlags.HideInInspector;
                }
            }
            if (generators.Length == 1)
            {
                if (GUILayout.Button("Bake Mesh"))
                {
                    MeshGenerator generator = (MeshGenerator)target;
                    bakeWindow = EditorWindow.GetWindow<BakeMeshWindow>();
                    bakeWindow.Init(generator);
                }
            }
        }
        
        protected override void Awake()
        {
            MeshGenerator generator = (MeshGenerator)target;
            MeshRenderer rend = generator.GetComponent<MeshRenderer>();
            if (rend == null) return;
            base.Awake();
        }
        
        protected override void OnDestroy()
        {
            MeshGenerator generator = (MeshGenerator)target;
            base.OnDestroy();
            MeshGenerator gen = (MeshGenerator)target;
            if (gen == null) return;
            if (gen.GetComponent<MeshCollider>() != null) generator.UpdateCollider();
            if (bakeWindow != null) bakeWindow.Close();
        }

        protected override void OnDelete()
        {
            base.OnDelete();
            MeshGenerator generator = (MeshGenerator)target;
            if (generator == null) return;
            MeshFilter filter = generator.GetComponent<MeshFilter>();
            if (filter != null) filter.hideFlags = HideFlags.None;
            MeshRenderer renderer = generator.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.hideFlags = HideFlags.None;
        }

        protected virtual void UVControls(MeshGenerator generator)
        {
            serializedObject.Update();
            SerializedProperty uvMode = serializedObject.FindProperty("_uvMode");
            SerializedProperty uvOffset = serializedObject.FindProperty("_uvOffset");
            SerializedProperty uvRotation = serializedObject.FindProperty("_uvRotation");
            SerializedProperty uvScale = serializedObject.FindProperty("_uvScale");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(uvMode, new GUIContent("UV Mode"));
            EditorGUILayout.PropertyField(uvOffset, new GUIContent("UV Offset"));
            EditorGUILayout.PropertyField(uvRotation, new GUIContent("UV Rotation"));
            EditorGUILayout.PropertyField(uvScale, new GUIContent("UV Scale"));
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
        }
        
    }
}
