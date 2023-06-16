using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

namespace Dreamteck.Splines
{
    public class UpdateTool : SplineTool
    {
        protected GameObject obj;
        protected ObjectController spawner;
        private string updated = "";

        public override string GetName()
        {
            return "Update Components";
        }

        protected override string GetPrefix()
        {
            return "UpdateTool";
        }

        public override void Draw(Rect windowRect)
        {
            if (GUILayout.Button("Update All Spline Components"))
            {
                updated = "";
                UpdateComputers();
                UpdateNodes();
                UpdateUsers();
            }
            if (GUILayout.Button("Update SplineUsers"))
            {
                updated = "";
                UpdateUsers();
            }
            if (GUILayout.Button("Update MeshGenerators"))
            {
                updated = "";
                UpdateMeshGenerators();
            }
            if (GUILayout.Button("Update SplineComputers"))
            {
                updated = "";
                UpdateComputers();
            }
            if (GUILayout.Button("Update Nodes In Scene"))
            {
                updated = "";
                UpdateNodes();
            }

            EditorGUILayout.Space();
            GUILayout.Label(updated);
        }

        private void UpdateNodes()
        {
            Node[] nodes = GameObject.FindObjectsOfType<Node>();
            EditorUtility.ClearProgressBar();
            for (int i = 0; i < nodes.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Updating nodes", "Updating node " + nodes[i].name, (float)i / (nodes.Length - 1));
                nodes[i].UpdateConnectedComputers();
                EditorUtility.SetDirty(nodes[i]);
                updated += i + " - " + nodes[i].name + System.Environment.NewLine;
            }
            EditorUtility.ClearProgressBar();
            if (nodes.Length == 0) updated += System.Environment.NewLine+"No active Nodes found in the scene.";
        }

        private void UpdateUsers()
        {
            SplineUser[] users = GameObject.FindObjectsOfType<SplineUser>();
            EditorUtility.ClearProgressBar();
            for (int i = 0; i < users.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Updating users", "Updating user " + users[i].name, (float)i/(users.Length-1));
                users[i].Rebuild();
                EditorUtility.SetDirty(users[i]);
                updated += i + " - " + users[i].name + System.Environment.NewLine;
            }
            EditorUtility.ClearProgressBar();
            if (users.Length == 0) updated += System.Environment.NewLine+"No active SplineUsers found in the scene.";
        }

        private void UpdateMeshGenerators()
        {
            MeshGenerator[] users = GameObject.FindObjectsOfType<MeshGenerator>();
            EditorUtility.ClearProgressBar();
            for (int i = 0; i < users.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Updating mesh generators", "Updating generator " + users[i].name, (float)i / (users.Length - 1));
                users[i].Rebuild();
                EditorUtility.SetDirty(users[i]);
                updated += i + " - " + users[i].name + System.Environment.NewLine;
            }
            EditorUtility.ClearProgressBar();
            if (users.Length == 0) updated += System.Environment.NewLine + "No active MeshGenerators found in the scene.";
        }

        private void UpdateComputers()
        {
            SplineComputer[] computers = GameObject.FindObjectsOfType<SplineComputer>();
            EditorUtility.ClearProgressBar();
            for (int i = 0; i < computers.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Updating spline computers", "Updating computer " + computers[i].name, (float)i / (computers.Length - 1));
                computers[i].RebuildImmediate();
                EditorUtility.SetDirty(computers[i]);
                updated += i + " - " + computers[i].name + System.Environment.NewLine;
            }
            EditorUtility.ClearProgressBar();
            if (computers.Length == 0) updated += System.Environment.NewLine+"No active SplineComputers found in the scene.";
        }
    }
}
