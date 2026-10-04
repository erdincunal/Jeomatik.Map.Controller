using GeoAPI.CoordinateSystems;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Converters.WellKnownText;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Styles;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace MapController
{
    public partial class Helper
    {
        [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        /// <summary>
        /// Proje dosyasından okunan bir vektör katmanının veri kaynağı, görünürlük ve stil bilgilerini taşır.
        /// </summary>
        public class Layer
        {
            public int Id;
            public int SRID;
            public bool Enabled;
            public string Type;
            public string Name;
            public string DataSource;
            public string CoordinateSystemWkt;
            public Int64 MinScale;
            public Int64 MaxScale;
            public Project Project;
            public Label Label;
            public Theme Theme;
            public Style Style;
            public GeometryType GeometryType;

            public override string ToString()
            {
                return Name;
            }

            /// <summary>
            /// Katman tanımını SharpMap tarafından görüntülenebilen bir <see cref="VectorLayer"/> nesnesine dönüştürür.
            /// Veri kaynağı açılamazsa hata izleme çıktısına yazılır ve <see langword="null"/> döndürülür.
            /// </summary>
            public VectorLayer ToVectorLayer()
            {
                if (!CanCreateVectorLayer())
                {
                    return null;
                }

                try
                {
                    VectorLayer vectorLayer = CreateBaseVectorLayer();
                    ApplyCoordinateTransformation(vectorLayer);
                    return vectorLayer;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Katman '{0}' açılamadı: {1}", Name, ex);
                    return null;
                }
            }

            private bool CanCreateVectorLayer()
            {
                return Style != null && Project != null && !string.IsNullOrWhiteSpace(DataSource);
            }

            private VectorLayer CreateBaseVectorLayer()
            {
                return new VectorLayer(Name)
                {
                    DataSource = new ShapeFile(GetDataSourcePath(), true),
                    SRID = SRID,
                    Enabled = Enabled,
                    Style = CreateVectorStyle(),
                    MinVisible = MaxScale / 1,
                    MaxVisible = MinScale / 1
                };
            }

            private VectorStyle CreateVectorStyle()
            {
                Brush brush = Style.FillColor == Color.Transparent
                    ? Brushes.Transparent
                    : new SolidBrush(Style.FillColor);
                Pen pen = new Pen(Style.OutColor, Style.Size * 3)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Solid,
                    LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                };

                return new VectorStyle
                {
                    PointColor = brush,
                    PointSize = Style.Size * 5,
                    Line = pen,
                    Fill = brush,
                    EnableOutline = true,
                    Outline = pen
                };
            }

            private void ApplyCoordinateTransformation(VectorLayer vectorLayer)
            {
                if (IsWebMercator(SRID))
                {
                    return;
                }

                CoordinateSystemFactory coordinateSystemFactory = new CoordinateSystemFactory();
                CoordinateTransformationFactory transformationFactory = new CoordinateTransformationFactory();
                ICoordinateSystem sourceCoordinateSystem = GetSourceCoordinateSystem(coordinateSystemFactory);
                IProjectedCoordinateSystem targetCoordinateSystem = GetGoogleMercator(coordinateSystemFactory);

                // SharpMap ekran tarafında Web Mercator kullanıyor; veri kaynağını çizim sırasında bu sisteme taşırız.
                vectorLayer.CoordinateTransformation = transformationFactory.CreateFromCoordinateSystems(sourceCoordinateSystem, targetCoordinateSystem);
                vectorLayer.ReverseCoordinateTransformation = transformationFactory.CreateFromCoordinateSystems(targetCoordinateSystem, sourceCoordinateSystem);
            }

            private string GetDataSourcePath()
            {
                if (!DataSource.StartsWith("."))
                {
                    return DataSource;
                }

                // QGIS göreli yollarını proje klasörüne göre çöz; özgün DataSource değerini değiştirme.
                string absolutePath = Path.Combine(Project.Folder, DataSource);
                return Path.GetFullPath(new Uri(absolutePath).LocalPath);
            }

            private ICoordinateSystem GetSourceCoordinateSystem(CoordinateSystemFactory coordinateSystemFactory)
            {
                if (SRID == 4326 || SRID == 0)
                {
                    return GeographicCoordinateSystem.WGS84;
                }

                if (!string.IsNullOrWhiteSpace(CoordinateSystemWkt))
                {
                    ICoordinateSystem coordinateSystem = CoordinateSystemWktReader.Parse(CoordinateSystemWkt) as ICoordinateSystem;
                    if (coordinateSystem != null)
                    {
                        return coordinateSystem;
                    }
                }

                throw new NotSupportedException($"'{Name}' katmanının EPSG:{SRID} koordinat sistemi okunamadı.");
            }

            private static bool IsWebMercator(int srid)
            {
                return srid == 3857 || srid == 900913;
            }

            private string GetDebuggerDisplay()
            {
                return ToString();
            }
        }
    }
}
