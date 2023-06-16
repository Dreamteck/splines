using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Linq;
using Dreamteck.Splines.Primitives;
using System;

namespace Dreamteck.Splines.IO
{
    public class SVG : SplineParser
    {
        public enum Axis { X, Y, Z }
        internal class PathSegment 
        {
            internal Vector3 startTangent = Vector3.zero;
            internal Vector3 endTangent = Vector3.zero;
            internal Vector3 endPoint = Vector3.zero;
            internal enum Type { Cubic, CubicShort, Quadratic, QuadraticShort }

            internal PathSegment(Vector2 s, Vector2 e, Vector2 c)
            {
                startTangent = s;
                endTangent = e;
                endPoint = c;
            }

            internal PathSegment()
            {

            }
        }

        public enum Element { All, Path, Polygon, Ellipse, Rectangle, Line }
        private System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en-US");
        private System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Any;
        List<SplineDefinition> paths = new List<SplineDefinition>();
        List<SplineDefinition> polygons = new List<SplineDefinition>();
        List<SplineDefinition> ellipses = new List<SplineDefinition>();
        List<SplineDefinition> rectangles = new List<SplineDefinition>();
        List<SplineDefinition> lines = new List<SplineDefinition>();

        List<Transformation> transformBuffer = new List<Transformation>();

        public SVG(string filePath)
        {
            if (File.Exists(filePath))
            {
                string ext = Path.GetExtension(filePath).ToLower();
                fileName = Path.GetFileNameWithoutExtension(filePath);
                if (ext != ".svg" && ext != ".xml")
                {
                    Debug.LogError("SVG Parsing ERROR: Wrong format. Please use SVG or XML");
                    return;
                }
                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;
                try
                {
                    doc.Load(filePath);
                }
                catch (XmlException ex)
                {
                    Debug.LogError(ex.Message);
                    return;
                }
                Read(doc);
            }
        }

        public SVG(List<SplineComputer> computers)
        {
            paths = new List<SplineDefinition>(computers.Count);
            for (int i = 0; i < computers.Count; i++)
            {
                if (computers[i] == null) continue;
                Spline spline = new Spline(computers[i].type, computers[i].sampleRate);
                spline.points = computers[i].GetPoints();
                if (spline.type != Spline.Type.Bezier && spline.type != Spline.Type.Linear) spline.CatToBezierTangents();
                if (computers[i].isClosed) spline.Close();
                paths.Add(new SplineDefinition(computers[i].name, spline));
            }
        }

        public void Write(string filePath, Axis ax = Axis.Z)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement svg = doc.CreateElement("svg");
            foreach (SplineDefinition path in paths)
            {
                string elementName = "path";
                string attributeName = "d";
                if (path.type == Spline.Type.Linear)
                {
                    attributeName = "points";
                    if (path.closed) elementName = "polygon";
                    else elementName = "polyline";
                }
                XmlElement splineNode = doc.CreateElement(elementName);
                XmlAttribute splineAttribute = doc.CreateAttribute("id");
                splineAttribute.Value = path.name;
                splineNode.Attributes.Append(splineAttribute);
                splineAttribute = doc.CreateAttribute(attributeName);
                if (path.type == Spline.Type.Linear) splineAttribute.Value = EncodePolygon(path, ax);
                else splineAttribute.Value = EncodePath(path, ax);
                splineNode.Attributes.Append(splineAttribute);
                splineAttribute = doc.CreateAttribute("stroke");
                splineAttribute.Value = "black";
                splineNode.Attributes.Append(splineAttribute);
                splineAttribute = doc.CreateAttribute("stroke-width");
                splineAttribute.Value = "3";
                splineNode.Attributes.Append(splineAttribute);
                splineAttribute = doc.CreateAttribute("fill");
                splineAttribute.Value = "none";
                splineNode.Attributes.Append(splineAttribute);
                svg.AppendChild(splineNode);
            }
            XmlAttribute svgAttribute = doc.CreateAttribute("version");
            svgAttribute.Value = "1.1";
            svg.Attributes.Append(svgAttribute);
            svgAttribute = doc.CreateAttribute("xmlns");
            svgAttribute.Value = "http://www.w3.org/2000/svg";
            svg.Attributes.Append(svgAttribute);
            doc.AppendChild(svg);
            doc.Save(filePath);
        }

        Vector2 MapPoint(Vector3 original, Axis ax)
        {
            switch (ax)
            {
                case Axis.X: return new Vector2(original.z, -original.y);
                case Axis.Y: return new Vector2(original.x, -original.z);
                case Axis.Z: return new Vector2(original.x, -original.y);
            }
            return original;
        }

        private void Read(XmlDocument doc)
        {
            transformBuffer.Clear();
            Traverse(doc.ChildNodes);
        }

        private void Traverse(XmlNodeList nodes)
        {
            foreach (XmlNode node in nodes)
            {
                int addedTransforms = 0;
                switch (node.Name)
                {
                    case "g": addedTransforms = ParseTransformation(node); break;
                    case "path": addedTransforms = ReadPath(node); break;
                    case "polygon": addedTransforms = ReadPolygon(node, true); break;
                    case "polyline": addedTransforms = ReadPolygon(node, false); break;
                    case "ellipse": addedTransforms = ReadEllipse(node); break;
                    case "circle": addedTransforms = ReadEllipse(node); break;
                    case "line": addedTransforms = ReadLine(node); break;
                    case "rect": addedTransforms = ReadRectangle(node); break;
                }
                Traverse(node.ChildNodes);
                if (addedTransforms > 0) transformBuffer.RemoveRange(transformBuffer.Count - addedTransforms, addedTransforms);
            }
        }

        public List<SplineComputer> CreateSplineComputers(Vector3 position, Quaternion rotation, Element elements = Element.All)
        {
            List<SplineComputer> computers = new List<SplineComputer>();
            if (elements == Element.All || elements == Element.Path)
            {
                foreach (SplineDefinition definition in paths) computers.Add(definition.CreateSplineComputer(position, rotation));
            }
            if (elements == Element.All || elements == Element.Polygon)
            {
                foreach (SplineDefinition definition in polygons) computers.Add(definition.CreateSplineComputer(position, rotation));
            }
            if (elements == Element.All || elements == Element.Ellipse)
            {
                foreach (SplineDefinition definition in ellipses) computers.Add(definition.CreateSplineComputer(position, rotation));
            }
            if (elements == Element.All || elements == Element.Rectangle)
            {
                foreach (SplineDefinition definition in rectangles) computers.Add(definition.CreateSplineComputer(position, rotation));
            }
            if (elements == Element.All || elements == Element.Line)
            {
                foreach (SplineDefinition definition in lines) computers.Add(definition.CreateSplineComputer(position, rotation));
            }
            return computers;
        }

        public List<Spline> CreateSplines(Element elements = Element.All)
        {
            List<Spline> splines = new List<Spline>();
            if (elements == Element.All || elements == Element.Path)
            {
                foreach (SplineDefinition definition in paths) splines.Add(definition.CreateSpline());
            }
            if (elements == Element.All || elements == Element.Polygon)
            {
                foreach (SplineDefinition definition in polygons) splines.Add(definition.CreateSpline());
            }
            if (elements == Element.All || elements == Element.Ellipse)
            {
                foreach (SplineDefinition definition in ellipses) splines.Add(definition.CreateSpline());
            }
            if (elements == Element.All || elements == Element.Rectangle)
            {
                foreach (SplineDefinition definition in rectangles) splines.Add(definition.CreateSpline());
            }
            if (elements == Element.All || elements == Element.Line)
            {
                foreach (SplineDefinition definition in lines) splines.Add(definition.CreateSpline());
            }
            return splines;
        }

        private int ReadRectangle(XmlNode rectNode)
        {
            float x = 0f, y = 0f, w = 0f, h = 0f, rx = -1f, ry = -1f;
            string attribute = GetAttributeContent(rectNode, "x");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out x);
            attribute = GetAttributeContent(rectNode, "y");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out y);
            attribute = GetAttributeContent(rectNode, "width");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out w);
            attribute = GetAttributeContent(rectNode, "height");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out h);
            attribute = GetAttributeContent(rectNode, "rx");
            if (attribute != "ERROR") float.TryParse(attribute, style, culture, out rx);
            attribute = GetAttributeContent(rectNode, "ry");
            if (attribute != "ERROR") float.TryParse(attribute, style, culture, out ry);
            else ry = rx;
            string elementName = GetAttributeContent(rectNode, "id");

            if (rx == -1f && ry == -1f)
            {
                Rectangle rect = new Rectangle();
                rect.offset = new Vector2(x + w / 2f, -y - h / 2f);
                rect.size = new Vector2(w, h);
                if (elementName == "ERROR") elementName = fileName + "_rectangle" + (rectangles.Count + 1);
                buffer = new SplineDefinition(elementName, rect.CreateSpline());
            }
            else
            {
                RoundedRectangle rect = new RoundedRectangle();
                rect.offset = new Vector2(x + w / 2f, -y - h / 2f);
                rect.size = new Vector2(w, h);
                rect.xRadius = rx;
                rect.yRadius = ry;
                if (elementName == "ERROR") elementName = fileName + "_roundedRectangle" + (rectangles.Count + 1);
                buffer = new SplineDefinition(elementName, rect.CreateSpline());
            }
            int addedTransforms = ParseTransformation(rectNode);
            WriteBufferTo(rectangles);
            return addedTransforms;
        }

        private int ReadLine(XmlNode lineNode)
        {
            float startX = 0f, startY = 0f, endX = 0f, endY = 0f;
            string attribute = GetAttributeContent(lineNode, "x1");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out startX);
            attribute = GetAttributeContent(lineNode, "y1");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out startY);
            attribute = GetAttributeContent(lineNode, "x2");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out endX);
            attribute = GetAttributeContent(lineNode, "y2");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out endY);
            string elementName = GetAttributeContent(lineNode, "id");
            if (elementName == "ERROR") elementName = fileName + "_line" + (ellipses.Count + 1);
            buffer = new SplineDefinition(elementName, Spline.Type.Linear);
            buffer.position = new Vector2(startX, -startY);
            buffer.CreateLinear();
            buffer.position = new Vector2(endX, -endY);
            buffer.CreateLinear();
            int addedTransforms = ParseTransformation(lineNode);
            WriteBufferTo(lines);
            return addedTransforms;
        }

        private int ReadEllipse(XmlNode ellipseNode)
        {
            float x = 0f, y = 0f, rx = 0f, ry = 0f;
            string attribute = GetAttributeContent(ellipseNode, "cx");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out x);
            attribute = GetAttributeContent(ellipseNode, "cy");
            if (attribute == "ERROR") return 0;
            float.TryParse(attribute, style, culture, out y);
            attribute = GetAttributeContent(ellipseNode, "r");
            string shapeName = "circle";
            if (attribute == "ERROR") //It might be an ellipse
            {
                shapeName = "ellipse";
                attribute = GetAttributeContent(ellipseNode, "rx");
                if (attribute == "ERROR") return 0;
                float.TryParse(attribute, style, culture, out rx);
                attribute = GetAttributeContent(ellipseNode, "ry");
                if (attribute == "ERROR") return 0;
            }
            else //Nope, it's a circle
            {
                float.TryParse(attribute, style, culture, out rx);
                ry = rx;
            }
            float.TryParse(attribute, style, culture, out ry);
            Ellipse ellipse = new Ellipse();
            ellipse.offset = new Vector2(x, -y);
            ellipse.xRadius = rx;
            ellipse.yRadius = ry;

            string elementName = GetAttributeContent(ellipseNode, "id");
            if (elementName == "ERROR") elementName = fileName + "_" + shapeName + (ellipses.Count + 1);
            buffer = new SplineDefinition(elementName, ellipse.CreateSpline());
            int addedTransforms = ParseTransformation(ellipseNode);
            WriteBufferTo(ellipses);
            return addedTransforms;
        }

        private int ReadPolygon(XmlNode polyNode, bool closed)
        {
            string contents = GetAttributeContent(polyNode, "points");
            if (contents == "ERROR") return 0;
            List<float> coords = ParseFloatArray(contents);
            if (coords.Count % 2 != 0)
            {
                Debug.LogWarning("There is an error with one of the polygon shapes.");
                return 0;
            }
            string elementName = GetAttributeContent(polyNode, "id");
            if (elementName == "ERROR") elementName = fileName + (closed ? "_polygon " : "_polyline") + (polygons.Count + 1);
            buffer = new SplineDefinition(elementName, Spline.Type.Linear);
            int count = coords.Count / 2;
            for (int i = 0; i < count; i++)
            {
                buffer.position = new Vector2(coords[0 + 2 * i], -coords[1 + 2 * i]);
                buffer.CreateLinear();
            }
            if (closed)
            {
                buffer.CreateClosingPoint();
                buffer.closed = true;
            }
            int addedTransforms = ParseTransformation(polyNode);
            WriteBufferTo(polygons);
            return addedTransforms;
        }

        private int ParseTransformation(XmlNode node)
        {
            string transformAttribute = GetAttributeContent(node, "transform");
            if (transformAttribute == "ERROR") return 0;
            List<Transformation> trs = ParseTransformations(transformAttribute);
            transformBuffer.AddRange(trs);
            return trs.Count;
        }

        private List<Transformation> ParseTransformations(string transformContent)
        {
            List<Transformation> trs = new List<Transformation>();
            MatchCollection matches = Regex.Matches(transformContent.ToLower(), @"(?<function>translate|rotate|scale|skewx|skewy|matrix)\s*\((\s*(?<param>-?\s*\d+(\.\d+)?)\s*\,*\s*)+\)");
            foreach (Match match in matches)
            {
                if (match.Groups["function"].Success)
                {
                    CaptureCollection parameters = match.Groups["param"].Captures;
                    switch (match.Groups["function"].Value)
                    {
                        case "translate":
                            if (parameters.Count < 2) break;
                            trs.Add(new Translate(new Vector2(float.Parse(parameters[0].Value), float.Parse(parameters[1].Value))));
                            break;
                        case "rotate":
                            if (parameters.Count < 1) break;
                            trs.Add(new Rotate(float.Parse(parameters[0].Value)));
                            break;
                        case "scale":
                            if (parameters.Count < 2) break;
                            trs.Add(new Scale(new Vector2(float.Parse(parameters[0].Value), float.Parse(parameters[1].Value))));
                            break;
                        case "skewx":
                            if (parameters.Count < 1) break;
                            trs.Add(new SkewX(float.Parse(parameters[0].Value)));
                            break;
                        case "skewy":
                            if (parameters.Count < 1) break;
                            trs.Add(new SkewY(float.Parse(parameters[0].Value)));
                            break;
                        case "matrix":
                            if (parameters.Count < 6) break;
                            trs.Add(new MatrixTransform(float.Parse(parameters[0].Value), float.Parse(parameters[1].Value), float.Parse(parameters[2].Value), float.Parse(parameters[3].Value), float.Parse(parameters[4].Value), float.Parse(parameters[5].Value)));
                            break;
                    }
                }
            }
            return trs;
        }

        private int ReadPath(XmlNode pathNode)
        {
            string contents = GetAttributeContent(pathNode, "d");
            if (contents == "ERROR") return 0;
            string elementName = GetAttributeContent(pathNode, "id");
            if (elementName == "ERROR") elementName = fileName + "_path " + (paths.Count + 1);
            IEnumerable<string> tokens = Regex.Split(contents, @"(?=[A-Za-z])").Where(t => !string.IsNullOrEmpty(t));
            int numSplines = 0;
            foreach (string token in tokens)
            {
                char cmd = token.Substring(0, 1).Single();
                switch (cmd)
                {
                    case 'M':
                        PathStart(elementName, token, false);
                        ++numSplines;
                        break;
                    case 'm':
                        PathStart(elementName, token, true);
                        ++numSplines;
                        break;
                    case 'Z':
                        PathClose();
                        break;
                    case 'z':
                        PathClose();
                        break;
                    case 'L':
                        PathLineTo(token, false);
                        break;
                    case 'l':
                        PathLineTo(token, true);
                        break;
                    case 'H':
                        PathHorizontalLineTo(token, false);
                        break;
                    case 'h':
                        PathHorizontalLineTo(token, true);
                        break;
                    case 'V':
                        PathVerticalLineTo(token, false);
                        break;
                    case 'v':
                        PathVerticalLineTo(token, true);
                        break;
                    case 'C':
                        PathCurveTo(token, PathSegment.Type.Cubic, false);
                        break;
                    case 'c':
                        PathCurveTo(token, PathSegment.Type.Cubic, true);
                        break;
                    case 'S':
                        PathCurveTo(token, PathSegment.Type.CubicShort, false);
                        break;
                    case 's':
                        PathCurveTo(token, PathSegment.Type.CubicShort, true);
                        break;
                    case 'Q':
                        PathCurveTo(token, PathSegment.Type.Quadratic, false);
                        break;
                    case 'q':
                        PathCurveTo(token, PathSegment.Type.Quadratic, true);
                        break;
                    case 'T':
                        PathCurveTo(token, PathSegment.Type.QuadraticShort, false);
                        break;
                    case 't':
                        PathCurveTo(token, PathSegment.Type.QuadraticShort, true);
                        break;
                    case 'A':
                        PathArcTo(token, false);
                        break;
                    case 'a':
                        PathArcTo(token, true);
                        break;
                }
            }
            if (buffer != null) WriteBufferTo(paths);
            int addedTransforms = ParseTransformation(pathNode);
            for (int i = paths.Count - 1; i >= paths.Count - numSplines; --i) paths[i].Transform(transformBuffer);
            return addedTransforms;
        }

        private void PathStart(string name, string coords, bool relative)
        {
            if (buffer != null) WriteBufferTo(paths);
            buffer = new SplineDefinition(name, Spline.Type.Bezier);
            if (relative) buffer.position = paths.Last().GetLastPoint().position;
            Vector2[] vectors = ParseVector2(coords);
            foreach (Vector3 vector in vectors)
            {
                if (relative) buffer.position += vector;
                else buffer.position = vector;
                buffer.CreateLinear();
            }
        }

        private void PathClose()
        {
            buffer.CreateClosingPoint();
            buffer.closed = true;
        }

        private void PathLineTo(string coords, bool relative)
        {
            Vector2[] vectors = ParseVector2(coords);
            foreach (Vector3 vector in vectors)
            {
                if (relative) buffer.position += vector;
                else buffer.position = vector;
                buffer.CreateLinear();
            }
        }

        private void PathHorizontalLineTo(string coords, bool relative)
        {
            float[] floats = ParseFloat(coords);
            foreach (float f in floats)
            {
                if (relative) buffer.position.x += f;
                else buffer.position.x = f;
                buffer.CreateLinear();
            }
        }

        private void PathVerticalLineTo(string coords, bool relative)
        {
            float[] floats = ParseFloat(coords);
            foreach (float f in floats)
            {
                if (relative) buffer.position.y -= f;
                else buffer.position.y = -f;
                buffer.CreateLinear();
            }
        }

        private void PathCurveTo(string coords, PathSegment.Type type, bool relative)
        {
            PathSegment[] segment = ParsePathSegment(coords, type);
            for (int i = 0; i < segment.Length; i++)
            {
                SplinePoint p = buffer.GetLastPoint();
                p.type = SplinePoint.Type.Broken;

                //Get the control points
                Vector3 startPoint = p.position;
                Vector3 endPoint = segment[i].endPoint;
                Vector3 startTangent = segment[i].startTangent;
                Vector3 endTangent = segment[i].endTangent;

                switch (type)
                {
                    case PathSegment.Type.CubicShort: startTangent = startPoint - p.tangent; break;
                    case PathSegment.Type.Quadratic:
                        buffer.tangent = segment[i].startTangent;
                        startTangent = startPoint + 2f / 3f * (buffer.tangent - startPoint);
                        endTangent = endPoint + 2f / 3f * (buffer.tangent - endPoint);
                        break;
                    case PathSegment.Type.QuadraticShort:
                        Vector3 reflection = startPoint + (startPoint - buffer.tangent);
                        startTangent = startPoint + 2f / 3f * (reflection - startPoint);
                        endTangent = endPoint + 2f / 3f * (reflection - endPoint);
                        break;
                }

                if (type == PathSegment.Type.CubicShort || type == PathSegment.Type.QuadraticShort) p.type = SplinePoint.Type.SmoothMirrored; //Smooth the previous point
                else
                {
                    if (relative) p.SetTangent2Position(startPoint + startTangent);
                    else p.SetTangent2Position(startTangent);
                }
                buffer.SetLastPoint(p);
                if (relative)
                {
                    buffer.position += endPoint;
                    buffer.tangent = startPoint + endTangent;
                }
                else
                {
                    buffer.position = endPoint;
                    buffer.tangent = endTangent;
                }
                buffer.CreateBroken();
            }
        }

        private void PathArcTo(string coords, bool relative)
        {
            // Get Arc Arguments
            float[] floats = ParseFloat(coords);
            float rx = floats[0];
            float ry = floats[1];
            float rotation = floats[2] * Mathf.Deg2Rad;
            bool largeArc = floats[3] > 0.5f;
            bool sweep = floats[4] > 0.5f;
            float x = floats[5];
            float y = floats[6];

            // Set the last buffer point to broken
            SplinePoint p = buffer.GetLastPoint();
            p.type = SplinePoint.Type.Broken;

            // Get the start and end point. Note: Flip Y, as the Ellipse Parameters calculation assumes positive Y values
            Vector3 sp = p.position;
            sp.y *= -1;
            Vector3 ep = new Vector3(x, y, 0);
            if (relative) ep += sp;

            Vector2 c;
            float theta1;
            float sweepTheta;
            float adjustedRx;
            float adjustedRy;

            // Get the Ellipse Parameters
            CalculateEllipseParams(sp, ep, rotation, rx, ry, largeArc, sweep,
                out c, out theta1, out sweepTheta, out adjustedRx, out adjustedRy);

            // Flip the center Y back
            c.y *= -1;

            // Generate the ellipse primitive. Note: Rotated by -90 degrees and flipped, so first point starts at +X and goes clockwise
            Ellipse ellipse = new Ellipse();
            ellipse.offset = c;
            ellipse.rotation = new Vector3(0, 0, -90 - rotation * Mathf.Rad2Deg);
            ellipse.xRadius = adjustedRx;
            ellipse.yRadius = adjustedRy;
            var ellipseSpline = ellipse.CreateSpline();
            var esp = ellipseSpline.points;

            
            var tmpp = esp[1];
            esp[1] = esp[3];
            esp[3] = tmpp;
            for (int i = 0; i < esp.Length; i++)
            {
                FlipTangents(ref esp[i]);
            }

            // Find the percentages to sample the ellipse at
            var startP = theta1 / (Mathf.PI * 2);
            startP = ModP(startP, 1f);
            var sweepP = sweepTheta / (Mathf.PI * 2);
            var endP = startP + sweepP;

            var percentages = GetArcSegmentPercentages(startP, endP);

            // Sample the ellipse and add points to buffer
            for (int i = 1; i < percentages.Length; ++i)
            {
                double pp = percentages[i - 1];
                double pc = percentages[i];
                double pr = pc - pp;
                int sgn = Math.Sign(pr); // This sign indicates a reversed range if < 0; The sampled tangents need to be flipped if this is the case
                pr *= sgn;
                if (pr < 0.0001d) continue; // Sample error margin
                double d = 0.75d / pr;
                pc = ModP(pc, 1d);
                pp = ModP(pp, 1d);

                // New point in buffer
                Vector3 posc = Vector3.zero, tanc = Vector3.zero, tanp = Vector3.zero;
                ellipseSpline.EvaluatePosition(pc, ref posc);
                ellipseSpline.EvaluateTangent(pc, ref tanc);
                tanc *= sgn;
                tanc /= (float)d;
                buffer.position = posc;
                buffer.tangent = posc - tanc; 

                // Modify tangent2 of last point in buffer
                ellipseSpline.EvaluateTangent(pp, ref tanp);
                tanp *= sgn;
                tanp /= (float)d;
                p = buffer.GetLastPoint();
                p.type = SplinePoint.Type.Broken;
                p.SetTangent2Position(p.position + tanp);
                buffer.SetLastPoint(p);

                buffer.CreateBroken();
            }
        }

        private void FlipTangents(ref SplinePoint point)
        {
            var tmpt = point.tangent;
            point.tangent = point.tangent2;
            point.tangent2 = tmpt;
        }

        private void CalculateEllipseParams(Vector2 p0, Vector2 p1, float phi, float rx, float ry, bool fa, bool fs, out Vector2 c, out float theta1, out float sweepTheta, out float adjustedRx, out float adjustedRy)
        {
            // From https://observablehq.com/@toja/ellipse-and-elliptical-arc-conversion
            float sinPhi = Mathf.Sin(phi);
            float cosPhi = Mathf.Cos(phi);

            float x = cosPhi * (p0.x - p1.x) / 2 + sinPhi * (p0.y - p1.y) / 2;
            float y = -sinPhi * (p0.x - p1.x) / 2 + cosPhi * (p0.y - p1.y) / 2;

            float px = x * x, py = y * y, prx = rx * rx, pry = ry * ry;

            rx = Mathf.Abs(rx);
            ry = Mathf.Abs(ry);
            float l = px / prx + py / pry;
            if (l > 1)
            {
                float sqrtl = Mathf.Sqrt(l);
                rx = sqrtl * rx;
                ry = sqrtl * ry;
                prx = rx * rx;
                pry = ry * ry;
            }
            adjustedRx = rx;
            adjustedRy = ry;

            float sign = fa == fs ? -1 : 1;
            float m = Mathf.Sqrt((prx * pry - prx * py - pry * px) / (prx * py + pry * px)) * sign;

            float ccx = m * (rx * y) / ry;
            float ccy = m * (-ry * x) / rx;

            c = new Vector2(
                cosPhi * ccx - sinPhi * ccy + (p0.x + p1.x) / 2,
                sinPhi * ccx + cosPhi * ccy + (p0.y + p1.y) / 2
            );

            theta1 = VectorAngle(new Vector2(1, 0), new Vector2((x - ccx) / rx, (y - ccy) / ry));
            sweepTheta = VectorAngle(new Vector2((x - ccx) / rx, (y - ccy) / ry), new Vector2((-x - ccx) / rx, (-y - ccy) / ry));
            sweepTheta *= Mathf.Rad2Deg;
            sweepTheta %= 360;

            if (!fs && sweepTheta > 0) sweepTheta -= 360;
            if (fs && sweepTheta < 0) sweepTheta += 360;
            sweepTheta *= Mathf.Deg2Rad;
        }

        private double[] GetArcSegmentPercentages(double start, double end)
        {
            List<double> percentages = new List<double>();
            bool swap = start > end;
            if (swap) { double tmp = start; start = end; end = tmp; }

            percentages.Add(start);
            double rsM = Math.Ceiling(start * 4d) * 0.25d;
            if (rsM > end)
            {
                percentages.Add(end);
                return ReturnPercentage(swap, percentages);
            }
            else if (start < rsM)
            {
                percentages.Add(rsM);
            }

            double rem = rsM + 0.25d;
            for (; rem <= end; rem += 0.25d)
            {
                percentages.Add(rem);
            }
            rem -= 0.25d;
            if (rem < end)
            {
                percentages.Add(end);
            }

            return ReturnPercentage(swap, percentages);
        }

        private double[] ReturnPercentage(bool swap, List<double> percentages)
        {
            var ret = new double[percentages.Count];
            for (int i = 0; i < percentages.Count; ++i)
            {
                var r = swap ? percentages.Count - 1 - i : i;
                var p = percentages[r];
                ret[i] = p;
            }
            return ret;
        }

        private float VectorAngle(Vector2 u, Vector2 v)
        {
            float sign = u.x * v.y - u.y * v.x < 0 ? -1 : 1;
            float ua = Mathf.Sqrt(u.x * u.x + u.y * u.y);
            float va = Mathf.Sqrt(v.x * v.x + v.y * v.y);
            float dot = u.x * v.x + u.y * v.y;
            return sign * Mathf.Acos(dot / (ua * va));
        }

        private float ModP(float f, float div)
        {
            return ((f % div) + div) % div;
        }

        private double ModP(double d, double div)
        {
            return ((d % div) + div) % div;
        }

        private void WriteBufferTo(List<SplineDefinition> list)
        {
            buffer.Transform(transformBuffer);
            list.Add(buffer);
            buffer = null;
        }

        private PathSegment[] ParsePathSegment(string coord, PathSegment.Type type)
        {
            List<float> list = ParseFloatArray(coord.Substring(1));
            int count = 0;
            switch (type)
            {
                case PathSegment.Type.Cubic: count = list.Count / 6; break;
                case PathSegment.Type.Quadratic: count = list.Count / 4; break;
                case PathSegment.Type.CubicShort: count = list.Count / 4; break;
                case PathSegment.Type.QuadraticShort: count = list.Count / 2; break;
            }

            if (count == 0)
            {
                Debug.Log("Error in " + coord + " " + type);
                return new PathSegment[] { new PathSegment() };
            }
            PathSegment[] data = new PathSegment[count];
            for (int i = 0; i < count; i++)
            {
                switch (type)
                {
                    case PathSegment.Type.Cubic: data[i] = new PathSegment(new Vector2(list[0 + 6 * i], -list[1 + 6 * i]), new Vector2(list[2 + 6 * i], -list[3 + 6 * i]), new Vector2(list[4 + 6 * i], -list[5 + 6 * i])); break;
                    case PathSegment.Type.Quadratic: data[i] = new PathSegment(new Vector2(list[0 + 4 * i], -list[1 + 4 * i]), Vector2.zero, new Vector2(list[2 + 4 * i], -list[3 + 4 * i])); break;
                    case PathSegment.Type.CubicShort: data[i] = new PathSegment(Vector2.zero, new Vector2(list[0 + 4 * i], -list[1 + 4 * i]), new Vector2(list[2 + 4 * i], -list[3 + 4 * i])); break;
                    case PathSegment.Type.QuadraticShort: data[i] = new PathSegment(Vector2.zero, Vector2.zero, new Vector2(list[0 + 4 * i], -list[1 + 4 * i])); break;
                }
            }
            return data;
        }

        private string EncodePath(SplineDefinition definition, Axis ax)
        {
            string text = "M";
            for (int i = 0; i < definition.pointCount; i++)
            {
                SplinePoint p = definition.points[i];
                Vector3 tangent = MapPoint(p.tangent, ax);
                Vector3 position = MapPoint(p.position, ax);
                if (i == 0) text += position.x + "," + position.y;
                else
                {
                    SplinePoint lp = definition.points[i - 1];
                    Vector3 tangent2 = MapPoint(lp.tangent2, ax);
                    text += "C" + tangent2.x + "," + tangent2.y + "," + tangent.x + "," + tangent.y + "," + position.x + "," + position.y;
                }
            }
            if (definition.closed) text += "z";
            return text;
        }

        private string EncodePolygon(SplineDefinition definition, Axis ax)
        {
            string text = "";
            for (int i = 0; i < definition.pointCount; i++)
            {
                Vector3 position = MapPoint(definition.points[i].position, ax);
                if (text != "") text += ",";
                text += position.x + "," + position.y;
            }
            return text;
        }

        private string GetAttributeContent(XmlNode node, string attributeName)
        {
            for (int j = 0; j < node.Attributes.Count; j++)
            {
                if (node.Attributes[j].Name == attributeName) return node.Attributes[j].InnerText;
            }
            return "ERROR";
        }

    }
}
