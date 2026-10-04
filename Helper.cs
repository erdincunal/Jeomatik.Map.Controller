using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Forms;
using SharpMap.Layers;
using SharpMap.Styles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MapController
{
    /// <summary>
    /// Harita oluşturma, koordinat dönüşümü, katman filtreleme ve arka plan haritası işlemlerini sağlar.
    /// </summary>
    public partial class Helper
    {
        private readonly CoordinateTransformationFactory coordinateTransformationFactory = new CoordinateTransformationFactory();
        private readonly CoordinateSystemFactory coordinateSystemFactory = new CoordinateSystemFactory();
        private readonly ICoordinateSystem CSGoogleMercator;
        private readonly ICoordinateSystem CSWGS84;
        private readonly ICoordinateTransformation TransformationGoogle2WGSGeo;

        /// <summary>
        /// Sık kullanılan Web Mercator/WGS84 koordinat sistemlerini ve dönüşümünü hazırlar.
        /// </summary>
        public Helper()
        {
            CSGoogleMercator = GetGoogleMercator(coordinateSystemFactory);
            CSWGS84 = GeographicCoordinateSystem.WGS84;
            TransformationGoogle2WGSGeo = coordinateTransformationFactory.CreateFromCoordinateSystems(CSGoogleMercator, CSWGS84);
        }

        /// <summary>
        /// Model katmanlarını SharpMap katmanlarına dönüştürerek yeni bir harita oluşturur.
        /// </summary>
        /// <param name="layers">Haritaya eklenecek vektör katmanları.</param>
        /// <param name="tileMap">İsteğe bağlı arka plan karo haritası.</param>
        public Map CreateMap(List<Layer> layers, TileMap tileMap = 0)
        {
            Map map = new Map();
            if (tileMap != 0)
            {
                AddBackgroundLayer(map, tileMap);
            }

            if (layers == null)
            {
                return map;
            }

            foreach (Layer layer in layers)
            {
                AddLayerWithOptionalLabels(map, layer);
            }

            return map;
        }

        /// <summary>
        /// Ondalık dereceyi derece, dakika ve saniye gösterimine dönüştürür.
        /// </summary>
        public static string DMS(double Degree)
        {
            int Deg = (int)Degree;
            int Min = (int)((Degree - Deg) * 60);
            double Sec = ((Degree - Deg) * 60 - Min) * 60;
            return $"{Deg}°{Min}'{Sec:N4}{(char)34}";
        }

        /// <summary>
        /// Web Mercator koordinatını WGS84 coğrafi koordinatına dönüştürür.
        /// </summary>
        /// <returns>Boylam ve enlem değerlerini içeren iki elemanlı dizi.</returns>
        public double[] ConvertCoordinates(double x, double y)
        {
            double[] CursorWGSGeoCoordinates = TransformationGoogle2WGSGeo.MathTransform.Transform(new double[] { x, y });
            return CursorWGSGeoCoordinates;
        }

        #region internal

        internal VectorLayer FilterFeatures(string strLayerName, VectorLayer vectorLayer, Envelope filteredExtent, string[] WhereClause)
        {
            if (vectorLayer == null)
            {
                throw new ArgumentNullException(nameof(vectorLayer));
            }

            if (filteredExtent == null)
            {
                throw new ArgumentNullException(nameof(filteredExtent));
            }

            if (WhereClause == null || WhereClause.Length < 2)
            {
                throw new ArgumentException("Filtre en az bir alan ve değer çifti içermelidir.", nameof(WhereClause));
            }

            VectorLayer selectedLayer = new VectorLayer(strLayerName);
            try
            {
                FeatureDataTable featureDataTable = ReadAllFeatures(vectorLayer);
                if (featureDataTable != null && featureDataTable.Rows.Count > 0)
                {
                    FilterProvider.FilterMethod combinedFilter;
                    GeometryFeatureProvider geometryFeatureProvider = CreateFilteredProvider(featureDataTable, vectorLayer, WhereClause, out combinedFilter);
                    selectedLayer = CreateSelectionLayer(strLayerName, vectorLayer, geometryFeatureProvider);
                    ExpandExtentWithMatches(featureDataTable, filteredExtent, combinedFilter);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return selectedLayer;
        }

        private void AddLayerWithOptionalLabels(Map map, Layer layer)
        {
            VectorLayer vectorLayer = layer.ToVectorLayer();
            if (vectorLayer == null || vectorLayer.DataSource.GetFeatureCount() == 0)
            {
                return;
            }

            map.Layers.Add(vectorLayer);
            map.SRID = vectorLayer.SRID;

            if (layer.Label != null && layer.Label.Visible)
            {
                map.Layers.Add(layer.Label.ToLabelLayer(vectorLayer));
            }
        }

        private static FeatureDataTable ReadAllFeatures(VectorLayer vectorLayer)
        {
            // Shapefile sağlayıcısının tüm kapsamını çekip bellek içi GeometryFeatureProvider üzerinde filtreleriz.
            FeatureDataSet featureDataSet = new FeatureDataSet();
            vectorLayer.DataSource.ExecuteIntersectionQuery(vectorLayer.DataSource.GetExtents(), featureDataSet);
            return featureDataSet.Tables.Count > 0 ? featureDataSet.Tables[0] : null;
        }

        private static GeometryFeatureProvider CreateFilteredProvider(
            FeatureDataTable featureDataTable,
            VectorLayer sourceLayer,
            string[] whereClause,
            out FilterProvider.FilterMethod combinedFilter)
        {
            GeometryFeatureProvider geometryFeatureProvider = new GeometryFeatureProvider(featureDataTable)
            {
                SRID = sourceLayer.SRID
            };

            FilterProvider.FilterMethod providerFilter = geometryFeatureProvider.FilterDelegate;
            combinedFilter = feature => MatchesFilter(feature, whereClause)
                && (providerFilter == null || providerFilter(feature));
            geometryFeatureProvider.FilterDelegate = combinedFilter;
            return geometryFeatureProvider;
        }

        private static VectorLayer CreateSelectionLayer(string layerName, VectorLayer sourceLayer, GeometryFeatureProvider provider)
        {
            return new VectorLayer(layerName, provider)
            {
                CoordinateTransformation = sourceLayer.CoordinateTransformation,
                ReverseCoordinateTransformation = sourceLayer.ReverseCoordinateTransformation,
                MinVisible = sourceLayer.MinVisible,
                MaxVisible = sourceLayer.MaxVisible,
                SRID = sourceLayer.SRID
            };
        }

        private static void ExpandExtentWithMatches(
            FeatureDataTable featureDataTable,
            Envelope filteredExtent,
            FilterProvider.FilterMethod filter)
        {
            // Zoom zarfını sağlayıcının tamamına göre değil, yalnızca eşleşen geometrilere göre kurarız.
            foreach (FeatureDataRow feature in featureDataTable.Rows)
            {
                if (!feature.IsFeatureGeometryNull() && filter(feature))
                {
                    filteredExtent.ExpandToInclude(feature.Geometry.EnvelopeInternal);
                }
            }
        }

        private static bool MatchesFilter(FeatureDataRow feature, string[] filters)
        {
            bool matches = false;
            bool hasCondition = false;

            for (int index = 0; index + 1 < filters.Length; index += 3)
            {
                string columnName = filters[index];
                string expectedValue = filters[index + 1];
                bool currentMatch = feature.Table.Columns.Contains(columnName)
                    && string.Equals(Convert.ToString(feature[columnName]), expectedValue, StringComparison.Ordinal);

                if (!hasCondition)
                {
                    matches = currentMatch;
                    hasCondition = true;
                    continue;
                }

                // Operatör bir önceki alan-değer çiftinden sonra gelir ve iki koşulu birbirine bağlar.
                string logicalOperator = filters[index - 1];
                if (string.Equals(logicalOperator, "OR", StringComparison.OrdinalIgnoreCase))
                {
                    matches = matches || currentMatch;
                }
                else if (string.Equals(logicalOperator, "AND", StringComparison.OrdinalIgnoreCase))
                {
                    matches = matches && currentMatch;
                }
                else
                {
                    throw new ArgumentException($"Geçersiz filtre operatörü: {logicalOperator}");
                }
            }

            return hasCondition && matches;
        }

        internal Envelope TransformEnvelope(Envelope envelope)
        {
            // Sorgu zarfları WGS84, ekran haritası ise Web Mercator koordinatlarında tutulur.
            Envelope newEnvelope = new Envelope();
            ICoordinateTransformation TransformationWGSGeo2Google = WGSGeo2Google();
            double[] NewMin = TransformationWGSGeo2Google.MathTransform.Transform(new double[] { envelope.MinX, envelope.MinY });
            double[] NewMax = TransformationWGSGeo2Google.MathTransform.Transform(new double[] { envelope.MaxX, envelope.MaxY });
            Envelope ThisEnvelope = new Envelope(NewMin[0], NewMax[0], NewMin[1], NewMax[1]);
            newEnvelope.ExpandToInclude(ThisEnvelope);
            return newEnvelope;
        }

        internal void AddVectorLayer(Map map, VectorLayer layer)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }

            // Aynı adlı seçim katmanını yenisiyle değiştirerek üst üste birikmesini önle.
            VectorLayer existingLayer = map.Layers.GetLayerByName(layer.LayerName) as VectorLayer;
            if (existingLayer != null)
            {
                map.Layers.Remove(existingLayer);
            }

            layer.Style = GetLayerStyle();
            map.Layers.Add(layer);
        }
        #endregion

        #region Private

        private static IProjectedCoordinateSystem GetGoogleMercator(CoordinateSystemFactory csFact)
        {
            // Eski ProjNet sürümünde hazır EPSG:3857 tanımı bulunmadığı için projeksiyonu elle kur.
            List<ProjectionParameter> parameters = new List<ProjectionParameter>()
                {
                    new ProjectionParameter("semi_major", 6378137.0),
                    new ProjectionParameter("semi_minor", 6378137.0),
                    new ProjectionParameter("latitude_of_origin", 0.0),
                    new ProjectionParameter("central_meridian", 0.0),
                    new ProjectionParameter("scale_factor", 1.0),
                    new ProjectionParameter("false_easting", 0.0),
                    new ProjectionParameter("false_northing", 0.0)
                };
            IProjection projection = csFact.CreateProjection("Google Mercator", "mercator_1sp", parameters);
            IGeographicCoordinateSystem wgs84 = csFact.CreateGeographicCoordinateSystem("WGS 84", AngularUnit.Degrees, HorizontalDatum.WGS84, PrimeMeridian.Greenwich, new AxisInfo("north", AxisOrientationEnum.North), new AxisInfo("east", AxisOrientationEnum.East));
            IProjectedCoordinateSystem epsg900913 = csFact.CreateProjectedCoordinateSystem("Google Mercator", wgs84, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));
            return epsg900913;
        }

        private ICoordinateTransformation WGSGeo2Google()
        {
            ICoordinateSystem GoogleMercator = GetGoogleMercator(coordinateSystemFactory);
            ICoordinateSystem WGS84 = GeographicCoordinateSystem.WGS84;
            ICoordinateTransformation TransformationWGSGeo2Google = coordinateTransformationFactory.CreateFromCoordinateSystems(WGS84, GoogleMercator);
            return TransformationWGSGeo2Google;
        }

        private VectorStyle GetLayerStyle(Color color, float width)
        {
            Pen pen = new Pen(color, width)
            {
                DashStyle = DashStyle.Solid,
                LineJoin = LineJoin.Round,
            };

            VectorStyle style = new VectorStyle()
            {
                Fill = Brushes.Transparent,
                EnableOutline = true,
                Outline = pen
            };
            return style;
        }

        private VectorStyle GetLayerStyle()
        {
            // Seçim rengi her sorguda değişmemeli; Kamu.Map'in önceki görünümünü koru.
            return GetLayerStyle(Color.IndianRed, 2);
        }

        #endregion

        #region BackgoundLayer

        private void InitializeMapOsm(Map map)
        {
            BruTile.Web.OsmTileSource tileSource = new BruTile.Web.OsmTileSource();
            TileAsyncLayer tileLayer = new TileAsyncLayer(tileSource, "TileLayer - OSM")
            {
                //CoordinateTransformation = coordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, GetEPSG900913(coordinateSystemFactory)),
                //ReverseCoordinateTransformation = coordinateTransformationFactory.CreateFromCoordinateSystems(GetEPSG900913(coordinateSystemFactory), GeographicCoordinateSystem.WGS84)
            };
            AddTileLayer(map, tileLayer);
        }

        private void InitializeMapBing(Map map, BruTile.Web.BingMapType tileMap)
        {
            TileLayer tileLayer = new TileLayer(new BruTile.Web.BingTileSource(BruTile.Web.BingRequest.UrlBingStaging, "", tileMap), "TileLayer - Bing " + Convert.ToString(tileMap));
            AddTileLayer(map, tileLayer);
        }

        private void InitializeMapGoogle(Map map, BruTile.Web.GoogleMapType tileMap)
        {
            BruTile.Web.GoogleRequest req;
            BruTile.ITileSource tileSource;
            TileLayer tileLayer;

            if (tileMap == (BruTile.Web.GoogleMapType.GoogleSatellite | BruTile.Web.GoogleMapType.GoogleLabels))
            {
                // Hibrit görünüm iki ayrı karo katmanı gerektirir: önce uydu, ardından etiketler.
                req = new BruTile.Web.GoogleRequest(BruTile.Web.GoogleMapType.GoogleSatellite);
                tileSource = new BruTile.Web.GoogleTileSource(req);
                tileLayer = new TileLayer(tileSource, $"TileLayer - {Convert.ToString(BruTile.Web.GoogleMapType.GoogleSatellite)}");
                map.Layers.Add(tileLayer);
                req = new BruTile.Web.GoogleRequest(BruTile.Web.GoogleMapType.GoogleLabels);
                tileSource = new BruTile.Web.GoogleTileSource(req);
                tileMap = BruTile.Web.GoogleMapType.GoogleLabels;
            }
            else
            {
                req = new BruTile.Web.GoogleRequest(tileMap);
                tileSource = new BruTile.Web.GoogleTileSource(req);
            }

            tileLayer = new TileLayer(tileSource, $"TileLayer - {Convert.ToString(tileMap)}");
            AddTileLayer(map, tileLayer);
        }

        private void InitializeMapBox(Map map)
        {
            //TileLayer tileLayer;

            string accessToken = Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                System.Diagnostics.Trace.TraceWarning("MapBox katmanı için MAPBOX_ACCESS_TOKEN ortam değişkeni tanımlı değil.");
                return;
            }

            string url = $"https://api.mapbox.com/styles/v1/erdincunal/cklnlzjih4gn617o8shtq5ene/wmts?access_token={Uri.EscapeDataString(accessToken)}"; //dummy is needed because BruTile appends every parameter with ampersand

            BruTile.TileSchema schema = new BruTile.TileSchema
            {
                Srs = "EPSG:3857",
                Format = "image/png",
                Axis = BruTile.AxisDirection.Normal,
                Name = "UTM32",
                Height = 256,
                Width = 256,
                OriginX = 0,
                OriginY = 0,
                Extent = new BruTile.Extent(-20037508.3427892439067364, 20037508.6269290931522846, -20034926.1758808121085167, 20037508.6269291155040264)
            };

            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "STYLES", string.Empty }
            };

            BruTile.Web.WmscRequest request = new BruTile.Web.WmscRequest(new Uri(url), schema, new List<string> { "OSM-WMS" }, new List<string>(), parameters, "1.1.1");
            BruTile.Web.WebTileProvider provider = new BruTile.Web.WebTileProvider(request);
            BruTile.ITileSource tileSource = new BruTile.TileSource(provider, schema);
            TileLayer tileLayer = new TileLayer(tileSource, $"TileLayer ");

            AddTileLayer(map, tileLayer);
        }

        private void AddTileLayer(Map map, TileLayer tileLayer)
        {
            // Denetim aynı anda tek bir arka plan sağlayıcısı gösterecek şekilde tasarlanmıştır.
            map.BackgroundLayer.Clear();
            map.BackgroundLayer.Add(tileLayer);
        }

        private void AddBackgroundLayer(Map map, TileMap tileMap)
        {
            switch (tileMap)
            {
                case TileMap.OSM:
                    InitializeMapOsm(map);
                    break;
                case TileMap.BingMapRoads:
                    InitializeMapBing(map, BruTile.Web.BingMapType.Roads);
                    break;
                case TileMap.BingMapAerial:
                    InitializeMapBing(map, BruTile.Web.BingMapType.Aerial);
                    break;
                case TileMap.BingMapHybrid:
                    InitializeMapBing(map, BruTile.Web.BingMapType.Hybrid);
                    break;
                case TileMap.GoogleMap:
                    InitializeMapGoogle(map, BruTile.Web.GoogleMapType.GoogleMap);
                    break;
                case TileMap.GoogleSatellite:
                    InitializeMapGoogle(map, BruTile.Web.GoogleMapType.GoogleSatellite);
                    break;
                case TileMap.GoogleLabels:
                    InitializeMapGoogle(map, BruTile.Web.GoogleMapType.GoogleLabels);
                    break;
                case TileMap.GoogleSatelliteAndLabels:
                    InitializeMapGoogle(map, BruTile.Web.GoogleMapType.GoogleSatellite | BruTile.Web.GoogleMapType.GoogleLabels);
                    break;
                case TileMap.MapBox:
                    InitializeMapBox(map);
                    break;
                default:
                    break;
            }
        }
        #endregion

        //private void RemoveLayerByName(Map map, string layerName)
        //{
        //    VectorLayer MyLayer = (VectorLayer)map.Layers.GetLayerByName(layerName);
        //    map.Layers.Remove(MyLayer);
        //}

        //private static int GetColumnIndex(VectorLayer SelectedLayer, string ColName)
        //{
        //    // Retrieve the FeatureDataTable from the SelectedLayer
        //    Envelope MyEnvelope = SelectedLayer.DataSource.GetExtents();
        //    FeatureDataSet featureDataSet = new FeatureDataSet();
        //    SelectedLayer.DataSource.ExecuteIntersectionQuery(MyEnvelope, featureDataSet);

        //    // Find the index of the column named "ID"
        //    int columnIndex = featureDataSet.Tables[0].Columns[ColName].Ordinal;
        //    return columnIndex;
        //}

    }
}
