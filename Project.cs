using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace MapController
{
    public partial class Helper
    {
        /// <summary>
        /// QGIS QGS/QGZ proje dosyasından koordinat sistemi, görünüm alanı ve vektör katmanlarını okur.
        /// </summary>
        public class Project
        {
            private const int DefaultSrid = 4326;
            private const long DefaultMinScale = 0;
            private const long DefaultMaxScale = 100000000;
            private const float DefaultOpacity = 1;
            private static readonly string[] DefaultFillColor = { "-1", "0", "0", "255" };
            private static readonly string[] DefaultOutlineColor = { "0", "0", "0", "255" };

            public Project()
            {
            }

            /// <summary>
            /// Verilen QGS veya sıkıştırılmış QGZ proje dosyasını açıp içeriğini okur.
            /// </summary>
            public Project(string fileName)
            {
                Folder = Path.GetDirectoryName(fileName);
                string extension = Path.GetExtension(fileName).ToUpperInvariant();
                if (extension != ".QGS" && extension != ".QGZ")
                {
                    throw new NotSupportedException("Yalnızca QGIS QGS ve QGZ proje dosyaları desteklenir.");
                }

                FileName = extension == ".QGZ" ? OpenZipFile(fileName) : fileName;
                if (FileName != null)
                {
                    Read();
                }
            }

            public string Name { get; set; }
            public string Description { get; set; }
            public string FileName { get; set; }
            public string Folder { get; set; }
            public int SRID { get; set; }
            public string CoordinateSystemWkt { get; set; }
            public Extent Extent { get; set; }
            public List<Layer> Layers;

            public override string ToString()
            {
                return Name;
            }

            private string OpenZipFile(string zipFileName)
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), "MapController", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectory);
                string tempFileName = Path.Combine(tempDirectory, $"{Path.GetFileNameWithoutExtension(zipFileName)}.qgs");

                using (ZipArchive zip = ZipFile.Open(zipFileName, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry qgsEntry = zip.Entries
                        .FirstOrDefault(entry => entry.Name.EndsWith(".qgs", StringComparison.OrdinalIgnoreCase));
                    if (qgsEntry == null)
                    {
                        throw new InvalidDataException("QGZ arşivi içinde QGS proje dosyası bulunamadı.");
                    }

                    qgsEntry.ExtractToFile(tempFileName, true);
                }

                return tempFileName;
            }

            private void Read()
            {
                Layers = new List<Layer>();
                XDocument xDocument = XDocument.Load(FileName);
                XElement root = xDocument.Root ?? throw new InvalidDataException("QGIS proje dosyası boş veya geçersiz.");

                ReadProjectHeader(root);
                Extent = ReadExtent(root.Element("mapcanvas")?.Element("extent"));
                ReadLayers(root);
            }

            private void ReadProjectHeader(XElement root)
            {
                Name = ReadString(root, "title");
                if (string.IsNullOrWhiteSpace(Name))
                {
                    Name = Path.GetFileNameWithoutExtension(FileName);
                }

                XElement spatialRef = root.Element("projectCrs")?.Element("spatialrefsys");
                SRID = ReadInt(spatialRef, "srid", DefaultSrid);
                CoordinateSystemWkt = ReadString(spatialRef, "srtext");
            }

            private Extent ReadExtent(XElement extentElement)
            {
                if (extentElement == null)
                {
                    return null;
                }

                double xmin = Converter.ToDouble(ReadString(extentElement, "xmin"));
                double ymin = Converter.ToDouble(ReadString(extentElement, "ymin"));
                double xmax = Converter.ToDouble(ReadString(extentElement, "xmax"));
                double ymax = Converter.ToDouble(ReadString(extentElement, "ymax"));
                return new Extent(xmin, ymin, xmax, ymax);
            }

            private void ReadLayers(XElement root)
            {
                foreach (XElement layerElement in root.Descendants("maplayer").Where(IsVectorLayer))
                {
                    Layers.Add(CreateLayer(layerElement));
                }
            }

            private Layer CreateLayer(XElement layerElement)
            {
                XElement spatialRef = layerElement.Element("srs")?.Element("spatialrefsys");
                Label label = IsLabelEnabled(layerElement) ? ReadLabel(layerElement) : new Label();

                return new Layer
                {
                    Name = ReadString(layerElement, "layername"),
                    DataSource = ReadString(layerElement, "datasource"),
                    MinScale = ReadLongAttribute(layerElement, "minScale", DefaultMinScale),
                    MaxScale = ReadLongAttribute(layerElement, "maxScale", DefaultMaxScale),
                    SRID = ReadInt(spatialRef, "srid", SRID),
                    CoordinateSystemWkt = ReadString(spatialRef, "srtext", CoordinateSystemWkt),
                    Enabled = true,
                    GeometryType = ReadGeometryType(layerElement),
                    Style = ReadStyle(layerElement, ReadGeometryType(layerElement)),
                    Label = label,
                    Project = CreateLayerProject()
                };
            }

            private Project CreateLayerProject()
            {
                return new Project
                {
                    FileName = FileName,
                    Folder = Folder,
                    SRID = SRID,
                    CoordinateSystemWkt = CoordinateSystemWkt,
                    Extent = Extent
                };
            }

            private static bool IsVectorLayer(XElement layerElement)
            {
                // Raster katmanlar bu denetimin vektör sorgulama ve stil akışının dışında kalır.
                return string.Equals(ReadAttribute(layerElement, "type"), "vector", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsLabelEnabled(XElement layerElement)
            {
                return ReadAttribute(layerElement, "labelsEnabled") == "1";
            }

            private static GeometryType ReadGeometryType(XElement layerElement)
            {
                GeometryType geometryType;
                Enum.TryParse(ReadAttribute(layerElement, "geometry"), out geometryType);
                return geometryType;
            }

            private static Style ReadStyle(XElement layerElement, GeometryType geometryType)
            {
                Dictionary<string, string> options = ReadSymbolOptions(layerElement);
                string[] fillColor = (string[])DefaultFillColor.Clone();
                string[] outlineColor = (string[])DefaultOutlineColor.Clone();
                float symbolSize = 1;
                float layerOpacity = ReadFloatElement(layerElement, "layerOpacity", DefaultOpacity);

                ApplyGeometryStyleOptions(geometryType, options, ref fillColor, ref outlineColor, ref symbolSize);

                return new Style
                {
                    Size = symbolSize,
                    FillColor = fillColor[0] == "-1" ? Color.Transparent : GetColor(fillColor, layerOpacity),
                    OutColor = GetColor(outlineColor, layerOpacity)
                };
            }

            private static Dictionary<string, string> ReadSymbolOptions(XElement layerElement)
            {
                XElement optionRoot = layerElement.Element("renderer-v2")?
                    .Element("symbols")?
                    .Element("symbol")?
                    .Element("layer")?
                    .Element("Option");

                Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (optionRoot == null)
                {
                    return options;
                }

                foreach (XElement option in optionRoot.Elements())
                {
                    string name = ReadAttribute(option, "name");
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        options[name] = ReadAttribute(option, "value");
                    }
                }

                return options;
            }

            private static void ApplyGeometryStyleOptions(
                GeometryType geometryType,
                Dictionary<string, string> options,
                ref string[] fillColor,
                ref string[] outlineColor,
                ref float symbolSize)
            {
                switch (geometryType)
                {
                    case GeometryType.Point:
                        outlineColor = ReadColorOption(options, "outline_color", outlineColor);
                        fillColor = ReadColorOption(options, "color", fillColor);
                        symbolSize = ReadFloatOption(options, "size", symbolSize);
                        break;
                    case GeometryType.Line:
                        outlineColor = ReadColorOption(options, "line_color", outlineColor);
                        symbolSize = ReadFloatOption(options, "line_width", symbolSize);
                        break;
                    case GeometryType.Polygon:
                        outlineColor = ReadColorOption(options, "outline_color", outlineColor);
                        outlineColor = ReadColorOption(options, "line_color", outlineColor);
                        fillColor = ReadColorOption(options, "color", fillColor);
                        symbolSize = ReadFloatOption(options, "line_width", symbolSize);
                        symbolSize = ReadFloatOption(options, "size", symbolSize);
                        break;
                }
            }

            private static string[] ReadColorOption(Dictionary<string, string> options, string key, string[] defaultValue)
            {
                string value;
                return options.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)
                    ? value.Split((char)',')
                    : defaultValue;
            }

            private static Label ReadLabel(XElement layerElement)
            {
                XElement settings = layerElement.Element("labeling")?.Element("settings");
                XElement textStyle = settings?.Element("text-style");
                XElement rendering = settings?.Element("rendering");
                XElement textBuffer = textStyle?.Element("text-buffer");

                string fieldName = ReadAttribute(textStyle, "fieldName");
                string[] textColor = ReadAttribute(textStyle, "textColor", "255,0,0,255").Split((char)',');
                string[] bufferColor = ReadAttribute(textBuffer, "bufferColor", "255,0,0,255").Split((char)',');
                float textOpacity = ReadFloatAttribute(textStyle, "textOpacity", DefaultOpacity);

                return new Label
                {
                    Visible = !string.IsNullOrWhiteSpace(fieldName),
                    FamilyName = ReadAttribute(textStyle, "fontFamily", "Arial"),
                    MinScale = ReadLongAttribute(rendering, "scaleMin", DefaultMinScale),
                    MaxScale = ReadLongAttribute(rendering, "scaleMax", DefaultMaxScale),
                    LabelColumn = fieldName,
                    LabelSize = ReadFloatAttribute(textStyle, "fontSize", 10),
                    LabelColor = GetColor(textColor, textOpacity),
                    HaloSize = ReadFloatAttribute(textBuffer, "bufferSize", 1),
                    HaloColor = GetColor(bufferColor, textOpacity)
                };
            }

            private static Color GetColor(string[] colorParts, float opacity)
            {
                try
                {
                    Int16 alpha = Convert.ToInt16(Convert.ToInt16(colorParts[3]) * opacity);
                    Int16 red = Convert.ToInt16(colorParts[0]);
                    Int16 green = Convert.ToInt16(colorParts[1]);
                    Int16 blue = Convert.ToInt16(colorParts[2]);
                    return Color.FromArgb(alpha, red, green, blue);
                }
                catch (Exception)
                {
                    return Color.Pink;
                }
            }

            private static string ReadString(XElement element, string name, string defaultValue = "")
            {
                return element?.Element(name)?.Value ?? defaultValue;
            }

            private static string ReadAttribute(XElement element, string name, string defaultValue = "")
            {
                return element?.Attribute(name)?.Value ?? defaultValue;
            }

            private static int ReadInt(XElement element, string name, int defaultValue)
            {
                int value;
                return int.TryParse(ReadString(element, name), out value) ? value : defaultValue;
            }

            private static long ReadLongAttribute(XElement element, string name, long defaultValue)
            {
                long value;
                return long.TryParse(ReadAttribute(element, name), out value) ? value : defaultValue;
            }

            private static float ReadFloatElement(XElement element, string name, float defaultValue)
            {
                string value = ReadString(element, name);
                return string.IsNullOrWhiteSpace(value) ? defaultValue : Converter.ToFloat(value);
            }

            private static float ReadFloatAttribute(XElement element, string name, float defaultValue)
            {
                string value = ReadAttribute(element, name);
                return string.IsNullOrWhiteSpace(value) ? defaultValue : Converter.ToFloat(value);
            }

            private static float ReadFloatOption(Dictionary<string, string> options, string key, float defaultValue)
            {
                string value;
                return options.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)
                    ? Converter.ToFloat(value)
                    : defaultValue;
            }
        }
    }
}
