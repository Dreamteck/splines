namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(SplineMesh), true)]
    [CanEditMultipleObjects]
    public class SplineMeshEditor : MeshGenEditor
    {
        private int selectedChannel = -1;
        SplineMesh.Channel renameChannel = null;
        MeshDefinitionWindow definitionWindow = null;
        MeshScaleModifierEditor scaleModifierEditor;

        private Mesh GetMeshFromObject(Object obj)
        {
            SplineMesh user = (SplineMesh)target;
            if (!(obj is GameObject)) return null;
            GameObject gameObj = (GameObject)obj;
            MeshFilter filter = gameObj.GetComponent<MeshFilter>();
            Mesh returnMesh = null;
            if (filter != null && filter.sharedMesh != null) returnMesh = filter.sharedMesh;
            MeshRenderer rend = user.GetComponent<MeshRenderer>();
            if (rend == null) return returnMesh;
            MeshRenderer meshRend = gameObj.GetComponent<MeshRenderer>();
            if (meshRend == null) return returnMesh;
            bool found = false;
            for (int i = 0; i < meshRend.sharedMaterials.Length; i++)
            {
                for (int j = 0; j < rend.sharedMaterials.Length; j++)
                {
                    if(meshRend.sharedMaterials[i] == rend.sharedMaterials[j])
                    {
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                if(EditorUtility.DisplayDialog("New Materials", "The added object has one or more materials which are not refrenced by the renderer. Would you like to add them?", "Yes", "No")) {
                    if(rend.sharedMaterial == AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat"))
                    {
                        if (EditorUtility.DisplayDialog("Replace Material", "The renderer is using the default material. Replace it?", "Yes", "No")) rend.sharedMaterials = new Material[0];
                    }
                    for (int i = 0; i < meshRend.sharedMaterials.Length; i++) AddMaterial(rend, meshRend.sharedMaterials[i]);
                }
            }
            
            return returnMesh;
        }

        void AddMaterial(MeshRenderer target, Material material)
        {
            for (int i = 0; i < target.sharedMaterials.Length; i++)
            {
                if (target.sharedMaterials[i] == material) return;
            }
            Material[] newMaterials = new Material[target.sharedMaterials.Length + 1];
            target.sharedMaterials.CopyTo(newMaterials, 0);
            newMaterials[newMaterials.Length - 1] = material;
            target.sharedMaterials = newMaterials;
        }

        void OnDuplicateChannel(object index)
        {
            SplineMesh extruder = (SplineMesh)target;
            SplineMesh.Channel source = extruder.GetChannel((int)index);
            SplineMesh.Channel newChannel = extruder.AddChannel(source.name);
            source.CopyTo(newChannel);
        }

        void OnRenameChannel(object index)
        {
            SplineMesh extruder = (SplineMesh)target;
            renameChannel = extruder.GetChannel((int)index);
            Repaint();
        }

        void OnDeleteChannel(object index)
        {
            SplineMesh extruder = (SplineMesh)target;
            extruder.RemoveChannel((int)index);
            Repaint();
        }

        void OnMoveChannelUp(object index)
        {
            SplineMesh extruder = (SplineMesh)target;
            extruder.SwapChannels((int)index, ((int)index)-1);
            Repaint();
        }

        void OnMoveChannelDown(object index)
        {
            SplineMesh extruder = (SplineMesh)target;
            extruder.SwapChannels((int)index, ((int)index) + 1);
            Repaint();
        }

        protected override void DuringSceneGUI(SceneView currentSceneView)
        {
            base.DuringSceneGUI(currentSceneView);
            if (scaleModifierEditor != null) scaleModifierEditor.DrawScene();
        }

        protected override void BodyGUI()
        {
            showSize = false;
            showDoubleSided = false;
            showFlipFaces = false;
            base.BodyGUI();

            SplineMesh user = (SplineMesh)target;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Uv Coordinates", EditorStyles.boldLabel);
            user.uvOffset = EditorGUILayout.Vector2Field("UV Offset", user.uvOffset);
            user.uvScale = EditorGUILayout.Vector2Field("UV Scale", user.uvScale);

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Cannot edit channels when multiple objects are selected", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Channels", EditorStyles.boldLabel);
            for (int i = 0; i < user.GetChannelCount(); i++)
            {
                if (ChannelPanel(i))
                {
                    if (Event.current.type == EventType.MouseDown)
                    {
                        Repaint();
                        if (Event.current.button == 0)
                        {
                            if (selectedChannel == i)
                            {
                                selectedChannel = -1;
                            }
                            else
                            {
                                selectedChannel = i;
                                scaleModifierEditor = new MeshScaleModifierEditor(user, this, i);
                                scaleModifierEditor.alwaysOpen = true;
                            }
                        }
                        else if (Event.current.button == 1)
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Rename"), false, OnRenameChannel, i);
                            menu.AddItem(new GUIContent("Duplicate"), false, OnDuplicateChannel, i);
                            if (i == 0) menu.AddDisabledItem(new GUIContent("Move Up"));
                            else menu.AddItem(new GUIContent("Move Up"), false, OnMoveChannelUp, i);
                            if (i == user.GetChannelCount() - 1) menu.AddDisabledItem(new GUIContent("Move Down"));
                            else menu.AddItem(new GUIContent("Move Down"), false, OnMoveChannelDown, i);
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Delete"), false, OnDeleteChannel, i);
                            menu.ShowAsContext();
                        }
                    }
                }
            }
            if (GUILayout.Button("New Channel")) user.AddChannel("Channel " + (user.GetChannelCount() + 1));

            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(user);
        }

        bool ChannelPanel(int channelIndex)
        {
            SplineMesh.Channel channel = ((SplineMesh)target).GetChannel(channelIndex);
            bool open = selectedChannel == channelIndex;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            if (renameChannel == channel && Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                renameChannel = null;
                Repaint();
            }
            if (renameChannel == channel) channel.name = EditorGUILayout.TextField(channel.name);
            else EditorGUILayout.LabelField(channel.name, EditorStyles.boldLabel);
            if (!open)
            {
                GUILayout.EndVertical();
                return GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
            }
            Rect labelRect = GUILayoutUtility.GetLastRect();
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Objects", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 100f;
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < channel.GetMeshCount(); i++) MeshRow(channel, i);
            Object obj = null;
            obj = EditorGUILayout.ObjectField("Add Mesh", obj, typeof(Object), true);
            if (obj != null)
            {
                if (obj is Mesh) channel.AddMesh((Mesh)obj);
                else
                {
                    Mesh m = GetMeshFromObject(obj);
                    if (m != null) channel.AddMesh(m);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            channel.type = (SplineMesh.Channel.Type)EditorGUILayout.EnumPopup("Type", channel.type);
            if (channel.autoCount) EditorGUILayout.TextField("Auto Count: " + channel.count);
            else channel.count = EditorGUILayout.IntField("Count", channel.count);
            channel.autoCount = EditorGUILayout.Toggle("Auto Count", channel.autoCount);
            channel.randomOrder = EditorGUILayout.Toggle("Random Order", channel.randomOrder);
            if (channel.randomOrder) channel.randomSeed = EditorGUILayout.IntField("Seed", channel.randomSeed);
            EditorGUILayout.EndVertical();
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            float clipFrom = (float)channel.clipFrom;
            float clipTo = (float)channel.clipTo;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.MinMaxSlider(new GUIContent("Clip Range:"), ref clipFrom, ref clipTo, 0f, 1f);
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndHorizontal();
            channel.clipFrom = clipFrom;
            channel.clipTo = clipTo;
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(30));
            channel.clipFrom = EditorGUILayout.FloatField((float)channel.clipFrom);
            channel.clipTo = EditorGUILayout.FloatField((float)channel.clipTo);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);

            if(channel.type != SplineMesh.Channel.Type.Place) channel.spacing = EditorGUILayout.Slider("Spacing", (float)channel.spacing, 0f, 1f);

            //Offset
            channel.minOffset = EditorGUILayout.Vector2Field(channel.randomOffset ? "Offset Min" : "Offset", channel.minOffset);
            if(channel.randomOffset) channel.maxOffset = EditorGUILayout.Vector2Field("Offset Max", channel.maxOffset);
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 130f;
            channel.randomOffset = EditorGUILayout.Toggle("Randomize Offset", channel.randomOffset);
            if (channel.randomOffset) channel.offsetSeed = EditorGUILayout.IntField("Seed", channel.offsetSeed);
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            //Rotation
            if (channel.type == SplineMesh.Channel.Type.Extrude)
            {
                Vector3 rot = channel.minRotation;
                rot.z = EditorGUILayout.FloatField(channel.randomRotation ? "Rotation Min" : "Rotation", rot.z);
                channel.minRotation = rot;
                if (channel.randomRotation)
                {
                    rot = channel.maxRotation;
                    rot.z = EditorGUILayout.FloatField("Rotation Max", rot.z);
                    channel.maxRotation = rot;
                }
            }
            else
            {
                channel.minRotation = EditorGUILayout.Vector3Field(channel.randomRotation ? "Rotation Min" : "Rotation", channel.minRotation);
                if (channel.randomRotation) channel.maxRotation = EditorGUILayout.Vector3Field("Rotation Max", channel.maxRotation);
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 130f;
            channel.randomRotation = EditorGUILayout.Toggle("Randomize Rotation", channel.randomRotation);
            if (channel.randomRotation) channel.rotationSeed = EditorGUILayout.IntField("Seed", channel.rotationSeed);
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            //Scale
            if (channel.type == SplineMesh.Channel.Type.Extrude)
            {
                float lastZ = channel.minScale.z;
                Vector3 scale = channel.minScale;
                scale = EditorGUILayout.Vector2Field(channel.randomScale ? "Scale Min" : "Scale", scale);
                scale += Vector3.forward * lastZ;
                channel.minScale = scale;
                if (channel.randomScale)
                {
                    lastZ = channel.maxScale.z;
                    scale = channel.maxScale;
                    scale = EditorGUILayout.Vector2Field("Scale Max", scale);
                    scale += Vector3.forward * lastZ;
                    channel.maxScale = scale;
                }
            }
            else
            {
                channel.minScale = EditorGUILayout.Vector3Field(channel.randomScale ? "Scale Min" : "Scale", channel.minScale);
                if (channel.randomScale) channel.maxScale = EditorGUILayout.Vector3Field("Scale Max", channel.maxScale);
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 130f;
            channel.randomScale = EditorGUILayout.Toggle("Randomize Scale", channel.randomScale);
            if (channel.randomScale) channel.scaleSeed = EditorGUILayout.IntField("Seed", channel.scaleSeed);
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndHorizontal();
            if (channel.randomScale)
            {
                EditorGUI.indentLevel++;
                EditorGUIUtility.labelWidth = 120f;
                channel.uniformRandomScale = EditorGUILayout.Toggle("Uniform", channel.uniformRandomScale);
                EditorGUIUtility.labelWidth = 0f;
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UV Coordinates", EditorStyles.boldLabel);
            channel.uvOffset = EditorGUILayout.Vector2Field("UV Offset", channel.uvOffset);
            channel.uvScale = EditorGUILayout.Vector2Field("UV Scale", channel.uvScale);

            //Override

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Override", EditorStyles.boldLabel);
            channel.overrideNormal = EditorGUILayout.Toggle("Normal", channel.overrideNormal);
            if(channel.overrideNormal) channel.customNormal = EditorGUILayout.Vector3Field("Normal", channel.customNormal);

            if (channel.type == SplineMesh.Channel.Type.Extrude)
            {
                channel.overrideUVs = (SplineMesh.Channel.UVOverride)EditorGUILayout.EnumPopup("UVs", channel.overrideUVs);
                if(channel.overrideUVs != SplineMesh.Channel.UVOverride.None)
                {

                }
            }
            
            channel.overrideMaterialID = EditorGUILayout.Toggle("Material IDs", channel.overrideMaterialID);
            if (channel.overrideMaterialID) channel.targetMaterialID = EditorGUILayout.IntField("Target ID", channel.targetMaterialID);


            if (scaleModifierEditor != null)
            {
                EditorGUILayout.LabelField("Scale Regions", EditorStyles.boldLabel);
                scaleModifierEditor.DrawInspector();
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            return labelRect.Contains(Event.current.mousePosition);
        }

        void OnDuplicateMesh(object mesh)
        {
            MeshLink link = (MeshLink)mesh;
            link.channel.DuplicateMesh(link.index);
        }

        void OnDeleteMesh(object mesh)
        {
            MeshLink link = (MeshLink)mesh;
            link.channel.RemoveMesh(link.index);
            Repaint();
        }

        void OnMoveMeshUp(object mesh)
        {
            MeshLink link = (MeshLink)mesh;
            link.channel.SwapMeshes(link.index, link.index - 1);
            Repaint();
        }

        void OnMoveMeshDown(object mesh)
        {
            MeshLink link = (MeshLink)mesh;
            link.channel.SwapMeshes(link.index, link.index + 1);
            Repaint();
        }

        void MeshRow(SplineMesh.Channel channel, int index)
        {
            SplineMesh.Channel.MeshDefinition definition = channel.GetMesh(index);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);
            if(definition.mesh == null) GUILayout.Box("NULL", EditorStyles.helpBox, GUILayout.MinWidth(200));
            else GUILayout.Box(definition.mesh.name, EditorStyles.helpBox, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();
            Rect rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)){
                if(Event.current.button == 0)
                {
                    definitionWindow = EditorWindow.GetWindow<MeshDefinitionWindow>(true);
                    definitionWindow.Init((SplineMesh)target, definition);
                }   

                if(Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Duplicate"), false, OnDuplicateMesh, new MeshLink(index, channel));
                    if (index == 0) menu.AddDisabledItem(new GUIContent("Move Up"));
                    else menu.AddItem(new GUIContent("Move Up"), false, OnMoveMeshUp, new MeshLink(index, channel));
                    if (index == channel.GetMeshCount() - 1) menu.AddDisabledItem(new GUIContent("Move Down"));
                    else menu.AddItem(new GUIContent("Move Down"), false, OnMoveMeshDown, new MeshLink(index, channel));
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete"), false, OnDeleteMesh, new MeshLink(index, channel));
                    menu.ShowAsContext();
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (definitionWindow != null) definitionWindow.Close();
        }

        internal class MeshLink
        {
            internal int index = 0;
            internal SplineMesh.Channel channel;
            internal MeshLink(int i, SplineMesh.Channel l)
            {
                index = i;
                channel = l;
            }
        }

        public class MeshDefinitionWindow : EditorWindow
        {
            internal SplineMesh.Channel.MeshDefinition definition = null;
            internal SplineMesh extrude = null;

            internal void Init(SplineMesh e, SplineMesh.Channel.MeshDefinition d)
            {
                minSize = new Vector2(482, 180);
                extrude = e;
                definition = d;
                if(definition.mesh != null) titleContent = new GUIContent("Configure " + definition.mesh.name);
                else titleContent = new GUIContent("Configure Mesh");
            }

            private void OnGUI()
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
                definition.mesh = (Mesh)EditorGUILayout.ObjectField(definition.mesh, typeof(Mesh), true);
                definition.mirror = (SplineMesh.Channel.MeshDefinition.MirrorMethod)EditorGUILayout.EnumPopup("Mirror", definition.mirror);
                definition.offset = EditorGUILayout.Vector3Field("Offset", definition.offset);
                definition.rotation = EditorGUILayout.Vector3Field("Rotation", definition.rotation);
                definition.scale = EditorGUILayout.Vector3Field("Scale", definition.scale);
                var spacing = definition.spacing;
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 40;
                EditorGUILayout.LabelField("Spacing");
                spacing.front = EditorGUILayout.FloatField("Front", spacing.front);
                spacing.back = EditorGUILayout.FloatField("Back", spacing.back);
                definition.spacing = spacing;
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Faces", EditorStyles.boldLabel);
                definition.doubleSided = EditorGUILayout.Toggle("Double sided", definition.doubleSided);
                if (definition.doubleSided) definition.flipFaces = false;
                else definition.flipFaces = EditorGUILayout.Toggle("Flip Faces", definition.flipFaces);
                definition.removeInnerFaces = EditorGUILayout.Toggle("Remove Inner Faces", definition.removeInnerFaces);
                EditorGUILayout.LabelField("UVs", EditorStyles.boldLabel);
                definition.uvOffset = EditorGUILayout.Vector2Field("UV Offset", definition.uvOffset);
                definition.uvScale = EditorGUILayout.Vector2Field("UV Scale", definition.uvScale);
                definition.uvRotation = EditorGUILayout.Slider("UV Rotation", definition.uvRotation, -180f, 180f);
                definition.vertexGroupingMargin = EditorGUILayout.FloatField("Vertex Grouping Margin", definition.vertexGroupingMargin);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                if (GUI.changed) extrude.Rebuild();
            }
        }
    }
}
