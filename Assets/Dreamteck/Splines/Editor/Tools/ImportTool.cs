namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using System.IO;
    using Dreamteck.Splines.IO;

    public class ImportExportTool : SplineTool
    {
        private float scaleFactor = 1f;
        private bool alwaysDraw = true;
        private string importPath = "";
        private string exportPath = "";
        List<SplinePoint[]> originalPoints = new List<SplinePoint[]>();
        List<SplineComputer> imported = new List<SplineComputer>();
        List<SplineComputer> exported = new List<SplineComputer>();
        GameObject importedParent = null;

        enum Mode { None, Import, Export }
        enum ExportFormat { SVG, CSV }
        enum Axis { X, Y, Z }

        Mode mode = Mode.None;
        ExportFormat format = ExportFormat.SVG;
        Axis importAxis = Axis.Z;
        Axis exportAxis = Axis.Z;

        bool importOptions = false;

        List<CSV.ColumnType> exportColumns = new List<CSV.ColumnType>();
        List<CSV.ColumnType> importColumns = new List<CSV.ColumnType>();
        bool flatCSV = false;

        public override string GetName()
        {
            return "Import/Export";
        }

        protected override string GetPrefix()
        {
            return "ImportExport";
        }

        public override void Open(EditorWindow window)
        {
            base.Open(window);
            importPath = LoadString("importPath", "");
            exportPath = LoadString("exportPath", "");
            alwaysDraw = LoadBool("alwaysDraw", true);
            flatCSV = LoadBool("flatCSV", false);
            importAxis = (Axis)LoadInt("importAxis", 2);
            exportAxis = (Axis)LoadInt("exportAxis", 2);
            LoadColumns("importColumns", ref importColumns);
            LoadColumns("exportColumns", ref exportColumns);
        }

        void LoadColumns(string name, ref List<CSV.ColumnType> destination)
        {
            string text = LoadString(name, "");
            destination = new List<CSV.ColumnType>();
            if (text == "")
            {
                destination.Add(CSV.ColumnType.Position);
                destination.Add(CSV.ColumnType.Tangent);
                destination.Add(CSV.ColumnType.Tangent2);
                destination.Add(CSV.ColumnType.Normal);
                destination.Add(CSV.ColumnType.Size);
                destination.Add(CSV.ColumnType.Color);
                return;
            }
            string[] elements = text.Split(',');
            foreach (string element in elements)
            {
                int i = 0;
                if (int.TryParse(element, out i)) destination.Add((CSV.ColumnType)i);
            } 
        }

        public override void Close()
        {
            base.Close(); 
            if(importPath != "") SaveString("importPath", Path.GetDirectoryName(importPath));
            if (exportPath != "")  SaveString("exportPath", Path.GetDirectoryName(exportPath));
            string columnString = ""; 
            foreach(CSV.ColumnType col in importColumns)
            {
                if (columnString != "") columnString += ",";
                columnString += ((int)col).ToString();
            }
            SaveString("importColumns", columnString);
            columnString = "";
            foreach (CSV.ColumnType col in exportColumns)
            {
                if (columnString != "") columnString += ",";
                columnString += ((int)col).ToString();
            }
            SaveString("exportColumns", columnString);
            SaveBool("alwaysDraw", alwaysDraw);
            SaveBool("flatCSV", flatCSV);
            SaveInt("importAxis", (int)importAxis);
            SaveInt("exportAxis", (int)exportAxis);

#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnScene;
#else
            SceneView.onSceneGUIDelegate -= OnScene;
#endif

        } 

        protected override void Save()
        {
            base.Save();
            if (importedParent != null)
            {
                Selection.activeGameObject = importedParent;
                importedParent = null;
            } else
            {
                foreach(SplineComputer comp in imported)
                {
                    if(comp != null)
                    {
                        Selection.activeGameObject = comp.gameObject;
                        break;
                    }
                }
            }
            imported.Clear();
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnScene;
#else
            SceneView.onSceneGUIDelegate -= OnScene;
#endif

            mode = Mode.None;
        }

        protected override void Cancel()
        {
            base.Cancel();
            foreach (SplineComputer spline in imported) GameObject.DestroyImmediate(spline.gameObject);
            GameObject.DestroyImmediate(importedParent);
            imported.Clear();
            SceneView.RepaintAll();
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnScene;
#else
            SceneView.onSceneGUIDelegate -= OnScene;
#endif

            mode = Mode.None;
        }

        void CSVColumnUI(List<CSV.ColumnType> columns)
        {
            EditorGUILayout.LabelField("Dataset Columns");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.MaxWidth(30)) && columns.Count > 0) columns.RemoveAt(columns.Count - 1);
            for (int i = 0; i < columns.Count; i++)
            {
                columns[i] = (CSV.ColumnType)EditorGUILayout.EnumPopup(columns[i]);
            }
            if (GUILayout.Button("+", GUILayout.MaxWidth(30)) && columns.Count > 0) columns.Add(CSV.ColumnType.Position);
            EditorGUILayout.EndHorizontal();
        }

        void OnScene(SceneView current)
        {
            for (int i = 0; i < imported.Count; i++)
            {
                DSSplineDrawer.DrawSplineComputer(imported[i]);
            }
        }

        void ImportUI()
        {
            EditorGUI.BeginChangeCheck();
            scaleFactor = EditorGUILayout.FloatField("Scale Factor", scaleFactor);
            importAxis = (Axis)EditorGUILayout.EnumPopup("Facing Axis", importAxis);
            alwaysDraw = EditorGUILayout.Toggle("Always Draw", alwaysDraw);
            if (EditorGUI.EndChangeCheck()) ApplyPoints();
            SaveCancelUI();
        }

        void ExportUI()
        {
            if(exported.Count == 0)
            {
                mode = Mode.None;
                return;
            }
            EditorGUILayout.Space();
            format = (ExportFormat)EditorGUILayout.EnumPopup("Format", format);
            if (format == ExportFormat.SVG)
            {
                exportAxis = (Axis)EditorGUILayout.EnumPopup("Projection Axis", exportAxis);
                EditorGUILayout.HelpBox("The SVG is a 2D vector format so the exported spline will be flattened along the selected axis", MessageType.Info);
            }
            else
            {
                CSVColumnUI(exportColumns);
                flatCSV = EditorGUILayout.Toggle("Flat", flatCSV);
                if(flatCSV) exportAxis = (Axis)EditorGUILayout.EnumPopup("Projection Axis", exportAxis);
                EditorGUILayout.HelpBox("The exported splined will be flattened along the selected axis.", MessageType.Info);

            }

            if (GUILayout.Button("Save File"))
            {
                string extension = "*";
                switch (format)
                {
                    case ExportFormat.SVG: extension = "svg"; break;
                    case ExportFormat.CSV: extension = "csv"; break;
                }
                exportPath = EditorUtility.SaveFilePanel("Export splines", exportPath, "spline", extension);
                if (exportPath != "")
                {
                    if (Directory.Exists(Path.GetDirectoryName(exportPath)))
                    {
                        switch (format)
                        {
                            case ExportFormat.SVG: ExportSVG(exportPath); break;
                            case ExportFormat.CSV: ExportCSV(exportPath); break;
                        }
                    }
                }
            }
        }

        public override void Draw(Rect windowRect)
        {
            if (mode == Mode.Import)
            {
                ImportUI();
            } 
            else
            {
                importOptions = EditorGUILayout.Foldout(importOptions, "Import Options");
                if (importOptions) CSVColumnUI(importColumns);
                if (GUILayout.Button("Import"))
                {
                    importPath = EditorUtility.OpenFilePanel("Browse File", importPath, "svg,csv");
                    if (File.Exists(importPath))
                    {
                        splines.Clear();
                        string ext = Path.GetExtension(importPath).ToLower();
                        switch (ext)
                        {
                            case ".svg": ImportSVG(importPath); break;
                            case ".csv": ImportCSV(importPath); break;
                            case ".xml": ImportSVG(importPath); break;
                        }
                    }
                }
                exported = GetSelectedSplines();
                if (exported.Count == 0) GUI.color = new Color(1f, 1f, 1f, 0.5f);
                if (mode == Mode.Export) ExportUI();
                if (mode != Mode.Export)
                {
                    if (GUILayout.Button("Export") && exported.Count > 0) mode = Mode.Export;
                }
            }
        }

        List<SplineComputer> GetSelectedSplines()
        {
            List<SplineComputer> selected = new List<SplineComputer>();
            foreach(GameObject obj in Selection.gameObjects)
            {
                SplineComputer comp = obj.GetComponent<SplineComputer>();
                if (comp != null) selected.Add(comp);
            }
            return selected;
        }

        void ApplyPoints()
        {
            if (originalPoints.Count != imported.Count) return;
            if (imported.Count == 0) return;
            Quaternion lookRot = Quaternion.identity;
            switch (importAxis)
            {
                case Axis.X: lookRot = Quaternion.LookRotation(Vector3.right); break;
                case Axis.Y: lookRot = Quaternion.LookRotation(Vector3.down); break;
                case Axis.Z: lookRot = Quaternion.LookRotation(Vector3.forward); break;
            }
            for (int i = 0; i < imported.Count; i++)
            {
                SplinePoint[] transformed = new SplinePoint[originalPoints[i].Length];
                originalPoints[i].CopyTo(transformed, 0);
                for (int j = 0; j < transformed.Length; j++)
                {
                    transformed[j].position *= scaleFactor;
                    transformed[j].tangent *= scaleFactor;
                    transformed[j].tangent2 *= scaleFactor;

                    transformed[j].position = lookRot * transformed[j].position;
                    transformed[j].tangent = lookRot * transformed[j].tangent;
                    transformed[j].tangent2 = lookRot * transformed[j].tangent2;
                    transformed[j].normal = lookRot * transformed[j].normal;
                }
                imported[i].SetPoints(transformed);
                if (alwaysDraw)
                {
                    DSSplineDrawer.RegisterComputer(imported[i]);
                }
                else
                {
                    DSSplineDrawer.UnregisterComputer(imported[i]);
                }
            }
            SceneView.RepaintAll();
        }

        void GetImportedPoints()
        {
            foreach (SplineComputer comp in imported)
            {
                if (comp != null)
                {
                    originalPoints.Add(comp.GetPoints(SplineComputer.Space.Local));
                    mode = Mode.Import;
                } else imported.Remove(comp);
            }
        }

        void ImportSVG(string file)
        {
            SVG svg = new SVG(file);
            originalPoints.Clear();
            imported = svg.CreateSplineComputers(Vector3.zero, Quaternion.identity);
            if (imported.Count == 0) return;
            importedParent = new GameObject(svg.name);
            foreach (SplineComputer comp in imported) comp.transform.parent = importedParent.transform;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnScene;
#else
            SceneView.onSceneGUIDelegate += OnScene;
#endif

            GetImportedPoints();
            ApplyPoints();
            promptSave = true;
        }

        void ExportSVG(string file)
        {
            SVG svg = new SVG(exported);
            svg.Write(file, (SVG.Axis)((int)exportAxis));
        }

        void ExportCSV(string file)
        {
            CSV csv = new CSV(exported[0]);
            csv.columns = exportColumns;
            if (flatCSV)
            { 
                switch (exportAxis)
                {
                    case Axis.X: csv.FlatX(); break;
                    case Axis.Y: csv.FlatY(); break;
                    case Axis.Z: csv.FlatZ(); break;
                }
            }
            csv.Write(file);
        }


        void ImportCSV(string file)
        {
            CSV csv = new CSV(file, importColumns);
            originalPoints.Clear();
            imported.Clear();
            imported.Add(csv.CreateSplineComputer(Vector3.zero, Quaternion.identity));
            if (imported.Count == 0) return;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnScene;
#else
            SceneView.onSceneGUIDelegate += OnScene;
#endif

            GetImportedPoints();
            ApplyPoints();
            promptSave = true;
        }
    }
}
