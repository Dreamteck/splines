namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;
    using System.IO;

    public class BakeMeshWindow : EditorWindow
    {
        public bool isStatic = true;
        public bool copy = false;
        public bool removeComputer = false;
        public bool permanent = false;
        public bool generateLightmapUVs = false;

        MeshFilter filter;
        MeshRenderer renderer;
        MeshGenerator meshGen;
        public enum SaveFormat { MeshAsset, OBJ, Scene }
        SaveFormat format = SaveFormat.MeshAsset;

        public void Init(MeshGenerator generator)
        {
            titleContent = new GUIContent("Bake Mesh");
            meshGen = generator;
            filter = generator.GetComponent<MeshFilter>();
            renderer = generator.GetComponent<MeshRenderer>();
            if (EditorPrefs.HasKey("BakeWindow_isStatic")) isStatic = EditorPrefs.GetBool("BakeWindow_isStatic");
            if (EditorPrefs.HasKey("BakeWindow_generateLightmapUVs")) generateLightmapUVs = EditorPrefs.GetBool("BakeWindow_generateLightmapUVs");
            if (EditorPrefs.HasKey("BakeWindow_copy")) copy = EditorPrefs.GetBool("BakeWindow_copy");
            if (EditorPrefs.HasKey("BakeWindow_removeComputer")) removeComputer = EditorPrefs.GetBool("BakeWindow_removeComputer");
            if (EditorPrefs.HasKey("BakeWindow_permanent")) permanent = EditorPrefs.GetBool("BakeWindow_permanent");
            format = (SaveFormat)EditorPrefs.GetInt("BakeWindow_format", 0);
            minSize = new Vector2(340, 220);
            maxSize = minSize;
        }

        void OnDestroy()
        {
            EditorPrefs.SetBool("BakeWindow_isStatic", isStatic);
            EditorPrefs.SetBool("BakeWindow_generateLightmapUVs", generateLightmapUVs);
            EditorPrefs.SetBool("BakeWindow_copy", copy);
            EditorPrefs.SetBool("BakeWindow_removeComputer", removeComputer);
            EditorPrefs.SetBool("BakeWindow_permanent", permanent);
            EditorPrefs.SetInt("BakeWindow_format", (int)format);
        }

        void OnGUI() {
            format = (SaveFormat)EditorGUILayout.EnumPopup("Save Format", format);
            bool saveMesh = format != SaveFormat.Scene;

            if (format != SaveFormat.Scene) copy = EditorGUILayout.Toggle("Save without baking", copy);
            bool isCopy = format != SaveFormat.Scene && copy;
            switch (format)
            {
                case SaveFormat.Scene: EditorGUILayout.HelpBox("Saves the mesh inside the scene for lightmap", MessageType.Info); break;
                case SaveFormat.MeshAsset: EditorGUILayout.HelpBox("Saves the mesh as an .asset file inside the project. This makes using the mesh in prefabs and across scenes possible.", MessageType.Info); break;
                case SaveFormat.OBJ: EditorGUILayout.HelpBox("Exports the mesh as an OBJ file which can be imported in a third-party modeling application.", MessageType.Info); break;
            }
            EditorGUILayout.Space();

            if (!isCopy)
            {
                isStatic = EditorGUILayout.Toggle("Make Static", isStatic);
                permanent = EditorGUILayout.Toggle("Permanent", permanent);
                generateLightmapUVs = EditorGUILayout.Toggle("Generate Lightmap UVs", generateLightmapUVs);
                if (permanent)
                {
                    removeComputer = EditorGUILayout.Toggle("Remove SplineComputer", removeComputer);
                    if (meshGen.spline.subscriberCount > 1 && !isCopy) EditorGUILayout.HelpBox("WARNING: Removing the SplineComputer from this object will cause other SplineUsers to malfunction!", MessageType.Warning);
                }
            }

            string bakeText = "Bake Mesh";
            if (saveMesh) bakeText = "Bake & Save Mesh";
            if (isCopy) bakeText = "Save Mesh";

            if (GUILayout.Button(bakeText))
            {
                if (permanent)
                {
                    if (!EditorUtility.DisplayDialog("Permanent bake?", "This operation will remove the Mesh Generator. Are you sure you want to continue?", "Yes", "No")) return;
                }
                string savePath = "";
                if (saveMesh)
                {
                    string ext = "asset";
                    if (format == SaveFormat.OBJ) ext = "obj";
                    string meshName = "mesh";
                    if (filter != null) meshName = filter.sharedMesh.name;
                    savePath = EditorUtility.SaveFilePanel("Save " + meshName, Application.dataPath, meshName + "." + ext, ext);
                    if (!Directory.Exists(Path.GetDirectoryName(savePath)) || savePath == "")
                    {
                        EditorUtility.DisplayDialog("Save error", "Invalid save path. Please select a valid save path and try again", "OK");
                        return;
                    }
                    if (format == SaveFormat.OBJ && !copy && !savePath.StartsWith(Application.dataPath))
                    {
                        EditorUtility.DisplayDialog("Save error", "OBJ files can be saved outside of the project folder only when \"Save without baking\" is selected. Please select a directory inside the project in order to save.", "OK");
                        return;
                    }

                    if (format == SaveFormat.MeshAsset && !savePath.StartsWith(Application.dataPath))
                    {
                        EditorUtility.DisplayDialog("Save error", "Asset files cannot be saved outside of the project directory. Please select a path inside the project directory.", "OK");
                        return;
                    }
                }
                Undo.RecordObject(meshGen.gameObject, "Bake mesh");
                if (!isCopy) Bake();
                else
                {
                    MeshUtility.Optimize(filter.sharedMesh);
                    Unwrapping.GenerateSecondaryUVSet(filter.sharedMesh);
                }
                if (saveMesh)
                {
                    SaveMeshFile(savePath);
                }
            }
        }

        void Bake()
        {
            meshGen.Bake(isStatic, generateLightmapUVs);
            EditorUtility.SetDirty(meshGen);
            if (permanent && !copy)
            {
                SplineComputer meshGenComputer = meshGen.spline;
                if (permanent)
                {
                    meshGenComputer.Unsubscribe(meshGen);

                    if (removeComputer && meshGen.transform.IsChildOf(meshGenComputer.transform))
                    {
                        DestroyImmediate(meshGenComputer);
                    }

                    DestroyImmediate(meshGen);
                }
                if (removeComputer && meshGenComputer != null)
                {
                    if (meshGenComputer.GetComponents<Component>().Length == 2)
                    {
                        DestroyImmediate(meshGenComputer.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(meshGenComputer);
                    }
                }
            }
        }

        void SaveMeshFile(string savePath)
        {
            if (format == SaveFormat.Scene) return;
            string relativePath = "";
            if(savePath.StartsWith(Application.dataPath)) relativePath = "Assets" + savePath.Substring(Application.dataPath.Length);

            if (format == SaveFormat.MeshAsset)
            {
                if (copy)
                {
                    Mesh assetMesh = Dreamteck.MeshUtility.Copy(filter.sharedMesh);
                    AssetDatabase.CreateAsset(assetMesh, relativePath);
                } else AssetDatabase.CreateAsset(filter.sharedMesh, relativePath);
            }

            if (format == SaveFormat.OBJ)
            {
                string objString = Dreamteck.MeshUtility.ToOBJString(filter.sharedMesh, renderer.sharedMaterials);
                File.WriteAllText(savePath, objString);
                if (!copy) DestroyImmediate(filter.sharedMesh);
                if (relativePath != "") //Import back the OBJ
                {
                    AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceSynchronousImport);
                    if (!copy) filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(relativePath);
                }
            }
        }
    }
}
