using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Dreamteck.Splines
{
    public class LevelTerrainTool : SplineTool
    {
        public override string GetName()
        {
            return "Level Terrain";
        }

        protected override string GetPrefix()
        {
            return "LevelTerrainTool";
        }

        public struct Point
        {
            public int x;
            public int y;

            public Vector2 vector
            {
                get { return new Vector2(x, y); }
                set {
                    x = (int)value.x;
                    y = (int)value.y;
                }
            }

            public Point(int newX, int newY)
            {
                x = newX;
                y = newY;
            }

            public Point(Vector2 input)
            {
                x = Mathf.RoundToInt(input.x);
                y = Mathf.RoundToInt(input.y);
            }
        }

        public class TerrainPaintPoint
        {
            public Point leftPoint;
            public Point rightPoint;
            public float leftHeight = 0f;
            public float rightHeight = 0f;
            public Point center;
            public float floatDiameter = 0f;

            public float GetHeight(float percent)
            {
                return Mathf.Lerp(leftHeight, rightHeight, percent);
            }
        }

        public float size = 1f;
        public int feather = 1;
        public float offset = 0f;
        public float clipFrom = 0f;
        public float clipTo = 1f;
        private float[,] heights = null;
        private Texture2D brushPreview = null;
        private Texture2D basePreview = null;
        private Texture2D drawPreview = null;

        private float maxDrawHeight = 0f;


        private bool init = false;

        Terrain terrain = null;


        void GetSplinesAndTerrain()
        {
            if(splines.Count == 0) GetSplines();
            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                if (terrain == null)  terrain = Selection.gameObjects[i].GetComponent<Terrain>();
            }

            Terrain[] terrains = GameObject.FindObjectsOfType<Terrain>();
            if(terrains.Length == 1)
            {
                //if there is only one terrain in the scene, automatically select it
                terrain = terrains[0];
            }
        }

        void OnGUI()
        {
           // Draw();
        }

        public override void Open(EditorWindow window)
        {
            base.Open(window);
            GetSplinesAndTerrain();
        }

        public override void Close()
        {
            base.Close();
            if (promptSave)
            {
                if (EditorUtility.DisplayDialog("Apply changes?", "Changes to the terrain have been made. Do you want to keep them?", "Yes", "No"))
                {
                    SaveChanges();
                }
                else RevertToBase();
            }
        }

        public override void Draw(Rect windowRect)
        {
            base.Draw(windowRect);

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
            if (EditorGUI.EndChangeCheck())
            {
                heights = null;
            }

            if (splines.Count == 0) EditorGUILayout.HelpBox("No spline selected! Select an object with a SplineComputer component.", MessageType.Warning);
            if (terrain == null) EditorGUILayout.HelpBox("No terrain selected! You need to select a terrain.", MessageType.Warning);
            if (splines.Count == 0 || terrain == null) return;
            if (!init)
            {
                init = true;
                brushPreview = GenerateBrushThumbnail();
            }
            if (heights == null)
            {
                GetBase();
            }
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            float lastSize = size;
            size = EditorGUILayout.FloatField("Brush radius", size);
            if (size < 0f) size = 0f;
            if(lastSize != size) brushPreview = GenerateBrushThumbnail();
            int lastBlur = feather;
            int maxFeatherCount = Mathf.Max(heights.GetLength(0)/64, 2);
            feather = EditorGUILayout.IntSlider("Feather", feather, 0, maxFeatherCount);
            if (lastBlur != feather) brushPreview = GenerateBrushThumbnail();
            GUILayout.EndVertical();
            GUILayout.Box("", GUILayout.Width(64), GUILayout.Height(64));
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(rect, brushPreview);
            GUILayout.EndHorizontal();
            offset = EditorGUILayout.FloatField("Height offset", offset);
            EditorGUILayout.MinMaxSlider(new GUIContent("Spline range"), ref clipFrom, ref clipTo, 0f, 1f);
            if (GUILayout.Button("Level")) CarveTerrain();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Terrain heightmap:");
            GUILayout.Label("Path heightmap:");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Box("", GUILayout.Width((windowRect.width-10)/2), GUILayout.Height((windowRect.width - 10) / 2));
            rect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(rect, basePreview);
            GUILayout.Box("", GUILayout.Width((windowRect.width - 10) / 2), GUILayout.Height((windowRect.width - 10) / 2));
            rect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(rect, drawPreview);
            GUILayout.EndHorizontal();

            if (promptSave)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Revert")) RevertToBase();
                if (GUILayout.Button("Apply")) SaveChanges();
                GUILayout.EndHorizontal();
            }
        }

        void OnFocus()
        {
            GetSplinesAndTerrain();
            if (promptSave)
            {
                bool isChanged = false;
                float[,] newHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
                if (newHeights.GetLength(0) != heights.GetLength(0) || newHeights.GetLength(1) != heights.GetLength(1))
                {
                    isChanged = true;
                } else {
                    for (int x = 0; x < heights.GetLength(0); x++)
                    {
                        for (int y = 0; y < heights.GetLength(1); y++)
                        {
                            if (heights[x,y] != newHeights[x, y])
                            {
                                isChanged = true;
                                break;
                            }
                        }
                    }
                }
                if (isChanged)
                {
                    if (EditorUtility.DisplayDialog("Preserve terrain ?", "The terrain has been edited from outside. Do you want to load the new height data? \r\n WARNING: Doing so will apply your leveled data to the terrain.", "Yes", "No"))
                    {
                        GetBase();
                    }
                }
            }
          
        }

        void OnLostFocus()
        {
           // RevertToBase();
        }

        void CarveTerrain()
        {
            float[,] drawLayer = new float[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];
            float[,] alphaLayer = new float[terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution];
            Undo.RecordObject(terrain, "Carve");
            for (int i = 0; i < splines.Count; i++)
            {
                PaintHeightMap(terrain, splines[i], ref drawLayer, ref alphaLayer);
            }

            float[,] blurLayer = new float[drawLayer.GetLength(0), drawLayer.GetLength(1)];
            GaussBlur(ref drawLayer, ref blurLayer, feather);
            float[,] blurAlphaLayer = new float[drawLayer.GetLength(0), drawLayer.GetLength(1)];
            GaussBlur(ref alphaLayer, ref blurAlphaLayer, feather);
            float[,] finalLayer = new float[drawLayer.GetLength(0), drawLayer.GetLength(1)];


            Color[] pixels = drawPreview.GetPixels();


            drawPreview = new Texture2D(drawLayer.GetLength(0), drawLayer.GetLength(1));
            for (int x = 0; x < drawLayer.GetLength(0); x++)
            {
                for (int y = 0; y < drawLayer.GetLength(1); y++)
                {
                    finalLayer[x, y] = Mathf.Lerp(heights[x, y], blurLayer[x, y], blurAlphaLayer[x,y]);
                    pixels[x * drawPreview.width + y] = Color.Lerp(Color.black, Color.white, blurLayer[x, y]/maxDrawHeight*blurAlphaLayer[x,y]);
                } 
            }
            terrain.terrainData.SetHeights(0, 0, finalLayer);
            drawPreview.SetPixels(pixels);
            drawPreview.Apply();
        }

        Texture2D GenerateBrushThumbnail()
        {
            Texture2D tex = new Texture2D(65, 65, TextureFormat.RGB24, false);
            Color[] colors = tex.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }
            //Get the brush size, compared to the blur amount
            int hmSize = ToHeightmapSize(size);
            float percent = 1f;
            if(hmSize > 0) percent = Mathf.Clamp01((float)(feather* feather) / hmSize);
            int r = Mathf.RoundToInt(30 * (1f - percent));
            int center = 32;
            for (int x = center - 30; x <= center; x++)
            {
                for (int y = center - 30; y <= center; y++)
                {
                    float value = (x - center) * (x - center) + (y - center) * (y - center);
                    int xSym = center - (x - center);
                    int ySym = center - (y - center);

                    if (value <= r * r)
                    {
                        colors[x * tex.width + y] = Color.black;
                        colors[xSym * tex.width + y] = Color.black;
                        colors[x * tex.width + ySym] = Color.black;
                        colors[xSym * tex.width + ySym] = Color.black;
                    } else 
                    if (value <= 30 * 30 && value > r*r)
                    {
                        float rr = r * r;
                        float val = value - rr;
                        float div = 30 * 30 - rr;
                        float alpha = Mathf.Clamp01(val / div);
                        //Debug.Log(val + "/" + div + " = " + alpha);
                        Color col = Color.Lerp(Color.black, Color.white, alpha);
                        colors[x * tex.width + y] = col;
                        colors[xSym * tex.width + y] = col;
                        colors[x * tex.width + ySym] = col;
                        colors[xSym * tex.width + ySym] = col;
                    }
                    if (value <= 30 * 30 && value >= 29 * 29)
                    {
                        Color col = Color.Lerp(Color.gray, Color.white, 1f-percent);
                        colors[x * tex.width + y] = col;
                        colors[xSym * tex.width + y] = col;
                        colors[x * tex.width + ySym] = col;
                        colors[xSym * tex.width + ySym] = col;
                    }
                }
            }


            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        void GetBase()
        {
            GetSplinesAndTerrain();
            if (terrain == null) return;
            heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
            basePreview = new Texture2D(heights.GetLength(0), heights.GetLength(1));
            drawPreview = new Texture2D(heights.GetLength(0), heights.GetLength(1));
            Color[] pixels = new Color[basePreview.width * basePreview.height];
            Color[] blackPixels = new Color[basePreview.width * basePreview.height];
            float maxHeight = 0f;
            for (int x = 0; x < basePreview.width; x++)
            {
                for(int y = 0; y < basePreview.height; y++)
                {
                    if (heights[x, y] > maxHeight) maxHeight = heights[x, y];
                    pixels[x * basePreview.width + y] = Color.Lerp(Color.black, Color.white, heights[x, y]);
                    blackPixels[x * basePreview.width + y] = Color.black;
                }
            }
            if(maxHeight > 0f)
            {
                for (int x = 0; x < basePreview.width; x++)
                {
                    for (int y = 0; y < basePreview.height; y++)
                    {
                        pixels[x * basePreview.width + y] /= maxHeight;
                    }
                }
            }
            basePreview.SetPixels(pixels);
            basePreview.Apply();
            drawPreview.SetPixels(blackPixels);
            drawPreview.Apply();
            promptSave = false;
        }

        void SaveChanges()
        {
            GetBase();
        }

        void RevertToBase()
        {
            if (terrain == null) return;
            terrain.terrainData.SetHeights(0, 0, heights);
            heights = null;
        }

        void PaintHeightMap(Terrain terrain, SplineComputer computer, ref float[,] drawLayer, ref float[,] alphaLayer)
        {
            if (heights == null) GetBase();
            SplineSample[] results = new SplineSample[computer.iterations];
            computer.Evaluate(ref results, clipFrom, clipTo);
            Draw(results, ref drawLayer, ref alphaLayer);          
        }


        int ToHeightmapSize(float value)
        {
            float avgSize = (terrain.terrainData.size.x + terrain.terrainData.size.z) / 2f;
            int result = Mathf.RoundToInt(value / avgSize * terrain.terrainData.heightmapResolution);
            return result;
        }

        Point ToHeightmapCoords(Vector3 pos)
        {
            Vector3 terrainPos = pos - terrain.transform.position;
            terrainPos.x /= terrain.terrainData.size.x;
            terrainPos.z /= terrain.terrainData.size.z;
            terrainPos.x = Mathf.Clamp01(terrainPos.x);
            terrainPos.z = Mathf.Clamp01(terrainPos.z);
            int x = Mathf.RoundToInt(terrainPos.z * terrain.terrainData.heightmapResolution);
            int y = Mathf.RoundToInt(terrainPos.x * terrain.terrainData.heightmapResolution);
            return new Point(x, y);
        }

        float ToHeightmapValue(float y)
        {
            float terrainHeight = y - terrain.transform.position.y;
            terrainHeight /= terrain.terrainData.size.y;
            return terrainHeight;
        }

        void PaintSegment(TerrainPaintPoint fromPoint, TerrainPaintPoint toPoint, ref float[,] layer, ref float[,] alphaLayer, bool writeAlpha = true, bool overWriteHeight = true)
        {
            //Flip the points if the forward one has a bigger radius so the lerp can work well
            if (Vector2.Distance(fromPoint.leftPoint.vector, fromPoint.rightPoint.vector) < Vector2.Distance(toPoint.leftPoint.vector, toPoint.rightPoint.vector))
            {
                TerrainPaintPoint temp = fromPoint;
                fromPoint = toPoint;
                toPoint = temp;
            }

            List<Point> drawn = new List<Point>();
            Vector2 currentPosition = fromPoint.leftPoint.vector;
            Vector2 fromRight = fromPoint.rightPoint.vector;

            float alphaStartPercent = 0f;
            float alphaEndPercent = 1f;
            if(feather > 0)
            {
                currentPosition += (fromPoint.leftPoint.vector - fromPoint.center.vector).normalized * feather * 4f;
                fromRight += (fromPoint.rightPoint.vector - fromPoint.center.vector).normalized * feather * 4f;
                float span = (fromPoint.leftPoint.vector - fromPoint.rightPoint.vector).magnitude / (fromRight - currentPosition).magnitude;
                float rest = (1f - span) / 2f;
                alphaStartPercent = rest;
                alphaEndPercent = 1f - rest;
            }
            float armLength = Vector2.Distance(currentPosition, fromRight);
            if (armLength < 1f) return;
            while (true)
            {
                float armDistance = Vector2.Distance(currentPosition, fromRight);
                float armPercent = 1f-armDistance / armLength;
                //This can be optimized, take it outside of the cycle
                Point fromPos = new Point(currentPosition);
                Vector2 leftvector = toPoint.leftPoint.vector;
                Vector2 rightVector = toPoint.rightPoint.vector;
                if (feather > 0)
                {
                    leftvector += (toPoint.leftPoint.vector - toPoint.center.vector).normalized * feather * 4f;
                    rightVector += (toPoint.rightPoint.vector - toPoint.center.vector).normalized * feather * 4f;
                }
                Vector2 toArm = Vector2.Lerp(leftvector, rightVector, armPercent);

                Point toPos = new Point(toArm);
                int dx = Mathf.Abs(toPos.x - fromPos.x), sx = fromPos.x < toPos.x ? 1 : -1;
                int dy = -Mathf.Abs(toPos.y - fromPos.y), sy = fromPos.y < toPos.y ? 1 : -1;
                int err = dx + dy, e2;
                Point current = fromPos;
                Vector2 target = new Vector2(toPos.x - fromPos.x, toPos.y - fromPos.y);
                
                float fromHeight = fromPoint.GetHeight(armPercent);
                float toHeight = toPoint.GetHeight(armPercent);
                while (true)
                {
                    if (current.x >= 0 && current.x < layer.GetLength(0) && current.y >= 0 && current.y < layer.GetLength(1))
                    {
                        if (overWriteHeight || layer[current.x, current.y] == 0f)
                        {
                            if (!ContainsPoint(ref drawn, current))
                            {
                                Vector2 currentDist = new Vector2(current.x - fromPos.x, current.y - fromPos.y);
                                float positionPercent = Mathf.Clamp01(currentDist.magnitude / target.magnitude);
                                float height = Mathf.Lerp(fromHeight, toHeight, positionPercent);
                                float alphaValue = 0f;
                                if (armPercent >= alphaStartPercent && armPercent <= alphaEndPercent) alphaValue = 1f;
                                if (writeAlpha) Plot(current.x, current.y, height, alphaValue, ref alphaLayer, ref layer);
                                else Plot(current.x, current.y, height, alphaLayer[current.x, current.y], ref alphaLayer, ref layer);
                                drawn.Add(current);
                            }
                        }
                    }
                    if (current.x == toPos.x && current.y == toPos.y) break;
                    e2 = 2 * err;
                    if (e2 > dy)
                    {
                        err += dy;
                        current.x += sx;
                    } else if (e2 < dx)
                    {
                        err += dx;
                        current.y += sy;
                    }
                }
                if (currentPosition == fromRight) break;
                currentPosition = Vector2.MoveTowards(currentPosition, fromRight, 1f);
            }
         }

        private bool ContainsPoint(ref List<Point> list, Point point)
        {
            for(int i = 0; i < list.Count; i++)
            {
                if (list[i].x == point.x && list[i].y == point.y) return true;
            }
            return false;
        }

        void Draw(SplineSample[] points, ref float[,] drawLayer, ref float[,] alphaLayer)
        {
            List<SplineSample> selectedPoints = new List<SplineSample>();
            Point last = new Point();
            //Filter out points that are too close to each other
            for (int i = 0; i < points.Length; i++)
            {
                Point current = ToHeightmapCoords(points[i].position + points[i].up * offset);
                if (i == 0 || i == points.Length-1)
                {
                    last = new Point(current.x, current.y);
                    selectedPoints.Add(points[i]);
                } else if (Vector2.Distance(new Vector2(current.x, current.y), new Vector2(last.x, last.y)) >= 1.5f)
                {
                    selectedPoints.Add(points[i]);
                    last = new Point(current.x, current.y);
                }
            }
            if (selectedPoints.Count <= 1) return;
            TerrainPaintPoint[] paintPoints = new TerrainPaintPoint[selectedPoints.Count];
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                ConvertToPaintPoint(selectedPoints[i], ref paintPoints[i]);
            }
            //Paint the points
            for (int i = 0; i < paintPoints.Length - 1; i++)
            {
                promptSave = true;
                PaintSegment(paintPoints[i], paintPoints[i + 1], ref drawLayer, ref alphaLayer);
            }

            SplineSample exResult = selectedPoints[0];
            exResult.position += exResult.position - selectedPoints[1].position;
            TerrainPaintPoint exPoint = null;
            ConvertToPaintPoint(exResult, ref exPoint);
            PaintSegment(paintPoints[0], exPoint, ref drawLayer, ref alphaLayer, false, false);

            exResult = selectedPoints[selectedPoints.Count-1];
            exResult.position += exResult.position - selectedPoints[selectedPoints.Count - 2].position;
            ConvertToPaintPoint(exResult, ref exPoint);
            PaintSegment(paintPoints[paintPoints.Length-1], exPoint, ref drawLayer, ref alphaLayer, false, false);
            //Extrapolate the ending and the begining
        }

        TerrainPaintPoint ConvertToPaintPoint(SplineSample result, ref TerrainPaintPoint paintPoint)
        {
            paintPoint = new TerrainPaintPoint();
            Vector3 right = -Vector3.Cross(result.forward, result.up).normalized * size * 0.5f * result.size;
            Vector3 leftPoint = result.position - right + result.up * offset;
            Vector3 rightPoint = result.position + right + result.up * offset;
            paintPoint.center = ToHeightmapCoords(result.position + result.up * offset);
            paintPoint.leftPoint = ToHeightmapCoords(leftPoint);
            paintPoint.rightPoint = ToHeightmapCoords(rightPoint);
            paintPoint.leftHeight = ToHeightmapValue(leftPoint.y);
            paintPoint.rightHeight = ToHeightmapValue(rightPoint.y);
            paintPoint.floatDiameter = Vector2.Distance(new Vector2(leftPoint.x, leftPoint.z), new Vector2(rightPoint.x, rightPoint.z));
            if (paintPoint.leftHeight > maxDrawHeight) maxDrawHeight = paintPoint.leftHeight;
            if (paintPoint.rightHeight > maxDrawHeight) maxDrawHeight = paintPoint.rightHeight;
            return paintPoint;
        }


        Point Project(Point fromPoint, Point toPoint, int x, int y)
        {
            Vector2 dir = toPoint.vector - fromPoint.vector;
            Vector2 point = new Vector2(x, y);
            dir.Normalize();
            Vector2 v = point - fromPoint.vector;
            float d = Vector2.Dot(v, dir);
            return new Point(fromPoint.vector + dir * d);
        }

        void GaussBlur(ref float[,] source, ref float[,] target, int r)
        {
            int w = source.GetLength(0);
            int h = source.GetLength(1);
            int[] bxs = GBGetBoxes(r, 3);
            float[] flatSource = new float[source.GetLength(0) * source.GetLength(1)];
            float[] flatTarget = new float[source.GetLength(0) * source.GetLength(1)];
            for (int x = 0; x < source.GetLength(0); x++)
            {
                for (int y = 0; y < source.GetLength(1); y++)
                {
                    if (r == 0) target[x, y] = source[x, y];
                    else flatSource[x * source.GetLength(0) + y] = source[x, y];
                }
            }
            if (r == 0) return;
            BoxBlur(ref flatSource, ref flatTarget, w, h, (bxs[0] - 1) / 2);
            BoxBlur(ref flatTarget, ref flatSource, w, h, (bxs[1] - 1) / 2);
            BoxBlur(ref flatSource, ref flatTarget, w, h, (bxs[2] - 1) / 2);

            for (int i = 0; i < flatSource.Length; i++)
            {
                int x = Mathf.FloorToInt(i / source.GetLength(0));
                int y = i - x * source.GetLength(0);
                target[x, y] = flatTarget[i];
            }
        }

        int[] GBGetBoxes(int sigma, int n)
        {
            float wIdeal = Mathf.Sqrt((12 * sigma * sigma / n) + 1);
            int wl = Mathf.FloorToInt(wIdeal); if (wl % 2 == 0) wl--;
            int wu = wl + 2;

            float mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            float m = Mathf.Round(mIdeal);

            int[] sizes = new int[n];
            for (int i = 0; i < n; i++) sizes[i] = i < m ? wl : wu;
            return sizes;
        }

        void BoxBlur(ref float[] source, ref float[] target, int w, int h, int r)
        {
            for (int i = 0; i < source.Length; i++) target[i] = source[i];
            HorizontalBlur(ref target, ref source, w, h, r);
            VerticalBlur(ref source, ref target, w, h, r);
        }

        void HorizontalBlur(ref float[] source, ref float[] target, int w, int h, int r)
        {
            float iarr = 1f / (r*2f + 1f);
            for (int i = 0; i < h; i++)
            {
                int ti = i * w, li = ti, ri = ti + r;
                float fv = source[ti], lv = source[ti + w - 1], val = (r + 1) * fv;
                for (int j = 0; j < r; j++) val += source[ti + j];
                for (int j = 0; j <= r; j++) { val += source[ri++] - fv; target[ti++] = val * iarr; }
                for (int j = r + 1; j < w - r; j++) { val += source[ri++] - source[li++]; target[ti++] = val * iarr; }
                for (int j = w - r; j < w; j++) { val += lv - source[li++]; target[ti++] = val * iarr; }
            }
        }

        void VerticalBlur(ref float[] source, ref float[] target, int w, int h, int r)
        {
            float iarr = 1f / (r * 2f + 1f);
            for (int i = 0; i < w; i++)
            {
                int ti = i, li = ti, ri = ti + r * w;
                float fv = source[ti], lv = source[ti + w * (h - 1)], val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j * w];
                for (var j = 0; j <= r; j++) { val += source[ri] - fv; target[ti] = val * iarr; ri += w; ti += w; }
                for (var j = r + 1; j < h - r; j++) { val += source[ri] - source[li]; target[ti] = val * iarr; li += w; ri += w; ti += w; }
                for (var j = h - r; j < h; j++) { val += lv - source[li]; target[ti] = val * iarr; li += w; ti += w; }
            }
        }


        private void Plot(int x, int y, float value, float alpha, ref float[,] alphaTarget, ref float[,] target)
        {
            if (x < 0 || x >= target.GetLength(0)) return;
            if (y < 0 || y >= target.GetLength(1)) return;
            if (value > target[x, y])
            {
                target[x, y] = value;
                alphaTarget[x, y] = alpha;
            }
        }
    }
}
