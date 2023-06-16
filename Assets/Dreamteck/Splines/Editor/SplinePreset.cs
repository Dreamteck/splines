namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.IO;

    [System.Serializable]
    public struct S_Vector3
    {
        public float x, y, z;
        public Vector3 vector
        {
            get { return new Vector3(x, y, z); }
            set { }
        }


        public S_Vector3(Vector3 input)
        {
            x = input.x;
            y = input.y;
            z = input.z;
        }
    }
    [System.Serializable]
    public struct S_Color
    {
        public float r, g, b, a;
        public Color color
        {
            get { return new Color(r, g, b, a); }
            set { }
        }
        public S_Color(Color input)
        {
            r = input.r;
            g = input.g;
            b = input.b;
            a = input.a;
        }
    }

    [System.Serializable]
    public class SplinePreset
    {
        [SerializeField]
        private S_Vector3[] points_position = new S_Vector3[0];
        [SerializeField]
        private S_Vector3[] points_tanget = new S_Vector3[0];
        [SerializeField]
        private S_Vector3[] points_tangent2 = new S_Vector3[0];
        [SerializeField]
        private S_Vector3[] points_normal = new S_Vector3[0];
        [SerializeField]
        private S_Color[] points_color = new S_Color[0];
        [SerializeField]
        private float[] points_size = new float[0];
        [SerializeField]
        private SplinePoint.Type[] points_type = new SplinePoint.Type[0];


        [System.NonSerialized]
        protected SplineComputer computer;
        [System.NonSerialized]
        public Vector3 origin = Vector3.zero;

        public bool isClosed = false;
        public string filename = "";
        public string name = "";
        public string description = "";
        public Spline.Type type = Spline.Type.Bezier;
        private static string path = "";

        public SplinePoint[] points
        {
            get
            {
                SplinePoint[] p = new SplinePoint[points_position.Length];
                for (int i = 0; i < p.Length; i++)
                {
                    p[i].type = points_type[i];
                    p[i].position = points_position[i].vector;
                    p[i].tangent = points_tanget[i].vector;
                    p[i].tangent2 = points_tangent2[i].vector;
                    p[i].normal = points_normal[i].vector;
                    p[i].color = points_color[i].color;
                    p[i].size = points_size[i];
                }
                return p;
            }
        }

        public SplinePreset(SerializedSplinePoint[] p, bool closed, Spline.Type t)
        {
            points_position = new S_Vector3[p.Length];
            points_tanget = new S_Vector3[p.Length];
            points_tangent2 = new S_Vector3[p.Length];
            points_normal = new S_Vector3[p.Length];
            points_color = new S_Color[p.Length];
            points_size = new float[p.Length];
            points_type = new SplinePoint.Type[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                points_position[i] = new S_Vector3(p[i].position);
                points_tanget[i] = new S_Vector3(p[i].tangent);
                points_tangent2[i] = new S_Vector3(p[i].tangent2);
                points_normal[i] = new S_Vector3(p[i].normal);
                points_color[i] = new S_Color(p[i].color);
                points_size[i] = p[i].size;
                points_type[i] = p[i].type;
            }
            isClosed = closed;
            type = t;
            path = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
        }

        public void Save(string name)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            FileStream file = File.Create(path + "/" + name + ".jsp");
            byte[] bytes = System.Text.ASCIIEncoding.ASCII.GetBytes(JsonUtility.ToJson(this));
            file.Write(bytes, 0, bytes.Length);
            file.Close();
        }

        public static void Delete(string filename)
        {
            path = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
            if (!Directory.Exists(path))
            {
                Debug.LogError("Directory " + path + " does not exist");
                return;
            }
            File.Delete(path + "/" + filename);
        }

        public static SplinePreset[] LoadAll()
        {
            path = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
            if (!Directory.Exists(path))
            {
                Debug.LogError("Directory " + path + " does not exist");
                return null;
            }
            string[] files = Directory.GetFiles(path, "*.jsp");
            SplinePreset[] presets = new SplinePreset[files.Length];
            for (int i = 0; i < files.Length; i++)
            {

                FileStream file = File.Open(files[i], FileMode.Open);
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, bytes.Length);
                string json = System.Text.ASCIIEncoding.ASCII.GetString(bytes);
                presets[i] = JsonUtility.FromJson<SplinePreset>(json);
                file.Close();
            }
            return presets;
        }
    }
}
