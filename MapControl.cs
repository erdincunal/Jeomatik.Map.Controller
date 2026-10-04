using GeoAPI.Geometries;
using SharpMap;
using SharpMap.Data;
using SharpMap.Forms;
using SharpMap.Layers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace MapController
{
    /// <summary>
    /// SharpMap haritasını barındıran, sorgulama ve görünüm işlemlerini dışarı açan kullanıcı denetimidir.
    /// </summary>
    public partial class MapControl : UserControl
    {
        private const string SelectionLayerName = "~SELECTED~";
        private readonly Helper helper = new Helper();

        public MapControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Proje dosyasını MapController modelleriyle okuyup kanvasa yükler.
        /// QGS ve sıkıştırılmış QGZ proje dosyaları desteklenir.
        /// </summary>
        public Helper.Project LoadProject(string projectFile, Helper.TileMap tileMap = 0)
        {
            if (string.IsNullOrWhiteSpace(projectFile))
            {
                throw new ArgumentException("Harita proje dosyası boş olamaz.", nameof(projectFile));
            }

            SetBusy(true);
            try
            {
                Helper.Project project = new Helper.Project(projectFile);
                mapBox.Map = helper.CreateMap(project.Layers, tileMap);

                if (project.Extent != null)
                {
                    mapBox.Map.ZoomToBox(project.Extent.ToEnvelope());
                }
                else if (mapBox.Map.Layers.Count > 0)
                {
                    mapBox.Map.ZoomToExtents();
                }

                mapBox.Refresh();
                return project;
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>Kanvastaki haritanın tüm katmanlarını kapsayan zarfı döndürür.</summary>
        public Envelope GetMapExtents()
        {
            return mapBox.Map.Layers.Count == 0 ? null : mapBox.Map.GetExtents();
        }

        /// <summary>Bilgi aracını etkinleştirir.</summary>
        public void ActivateQueryTool()
        {
            mapBox.ActiveTool = MapBox.Tools.QueryBox;
        }

        /// <summary>Etkin harita aracını kapatır.</summary>
        public void CancelActiveTool()
        {
            mapBox.ActiveTool = MapBox.Tools.None;
        }

        /// <summary>Bilgi aracının etkin olup olmadığını belirtir.</summary>
        public bool IsQueryToolActive => mapBox.ActiveTool == MapBox.Tools.QueryBox;

        /// <summary>
        /// Ekran konumunun altında kalan, belirtilen katmana ait öznitelikleri döndürür.
        /// Katman veya eşleşen kayıt yoksa <see langword="null"/> döner.
        /// </summary>
        public FeatureDataTable QueryLayerAt(string layerName, Point imageLocation)
        {
            if (string.IsNullOrWhiteSpace(layerName))
            {
                throw new ArgumentException("Katman adı boş olamaz.", nameof(layerName));
            }

            ICanQueryLayer queryLayer = FindVectorLayer(layerName) as ICanQueryLayer;
            if (queryLayer == null)
            {
                return null;
            }

            Coordinate worldPoint = mapBox.Map.ImageToWorld(imageLocation);
            FeatureDataSet result = new FeatureDataSet();
            queryLayer.ExecuteIntersectionQuery(new Envelope(worldPoint.CoordinateValue), result);
            return result.Tables.Count == 0 ? null : result.Tables[0];
        }

        /// <summary>Önceki sorgunun oluşturduğu geçici seçim katmanını kaldırır.</summary>
        public void ClearSelection()
        {
            VectorLayer selectedLayer = FindVectorLayer(SelectionLayerName);
            if (selectedLayer != null)
            {
                mapBox.Map.Layers.Remove(selectedLayer);
                mapBox.Refresh();
            }
        }

        /// <summary>Harita kanvasını yeniden çizer.</summary>
        public void RefreshMap()
        {
            mapBox.Refresh();
        }

        /// <summary>
        /// QGIS katmanını ve ona bağlı etiket katmanlarını birlikte görünür veya gizli yapar.
        /// </summary>
        public void SetLayerVisibility(string layerName, bool visible)
        {
            if (string.IsNullOrWhiteSpace(layerName))
            {
                return;
            }

            foreach (ILayer layer in mapBox.Map.Layers)
            {
                bool isSourceLayer = string.Equals(layer.LayerName, layerName, StringComparison.OrdinalIgnoreCase);
                bool isLabelLayer = layer.LayerName.StartsWith(layerName + "~", StringComparison.OrdinalIgnoreCase);
                if (isSourceLayer || isLabelLayer)
                {
                    layer.Enabled = visible;
                }
            }

            mapBox.Refresh();
        }

        #region Harita olayları

        /// <summary>Harita üzerindeki dünya koordinatıyla birlikte fare basma olayını bildirir.</summary>
        public new event MouseDownEventHandler MouseDown;
        public delegate void MouseDownEventHandler(Coordinate worldPos, MouseEventArgs e);
        private void MapBox1_MouseDown(Coordinate worldPos, MouseEventArgs e)
        {
            MouseEventArgs mouseEventArgs = new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta);
            MouseDown?.Invoke(worldPos, mouseEventArgs);
        }

        /// <summary>Harita üzerindeki dünya koordinatıyla birlikte fare bırakma olayını bildirir.</summary>
        public new event MouseUpEventHandler MouseUp;
        public delegate void MouseUpEventHandler(Coordinate worldPos, MouseEventArgs e);
        private void MapBox1_MouseUp(Coordinate worldPos, MouseEventArgs e)
        {
            MouseEventArgs mouseEventArgs = new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta);
            MouseUp?.Invoke(worldPos, mouseEventArgs);
        }

        /// <summary>Harita üzerindeki dünya koordinatıyla birlikte fare hareketini bildirir.</summary>
        public new event MouseMoveEventHandler MouseMove;
        public delegate void MouseMoveEventHandler(Coordinate worldPos, MouseEventArgs e);
        private void MapBox1_MouseMove(Coordinate worldPos, MouseEventArgs e)
        {
            MouseEventArgs mouseEventArgs = new MouseEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta);
            MouseMove?.Invoke(worldPos, mouseEventArgs);
        }

        #endregion

        private void MapBox1_SizeChanged(object sender, EventArgs e)
        {
            mapBox.Refresh();
        }

        /// <summary>
        /// Haritanın mevcut görüntüsünü PNG dosyası olarak kaydeder.
        /// </summary>
        /// <param name="filename">Oluşturulacak PNG dosyasının yolu.</param>
        public void SavePNG(string filename)
        {
            try
            {
                mapBox.Image.Save(filename, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Harita görünümünü verilen koordinat zarfını kapsayacak şekilde yakınlaştırır.
        /// </summary>
        public void ZoomWindow(Envelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            try
            {
                if (mapBox.Map.Layers.Count > 0)
                {
                    RunBusyMapOperation(() =>
                    {
                        mapBox.Map.ZoomToBox(envelope);
                        mapBox.Refresh();
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Belirtilen vektör katmanını alan-değer koşullarıyla filtreler ve sonucu seçili katman olarak gösterir.
        /// </summary>
        /// <param name="strVectorLayerName">Sorgulanacak vektör katmanının adı.</param>
        /// <param name="filterParams">
        /// Alan, değer ve sonraki koşulla bağlantı operatöründen oluşan dizi
        /// (örnek: <c>IL, Ankara, AND, ILCE, Cankaya</c>).
        /// </param>
        public void RunQuery(string strVectorLayerName, List<string> filterParams)
        {
            if (string.IsNullOrWhiteSpace(strVectorLayerName))
            {
                throw new ArgumentException("Katman adı boş olamaz.", nameof(strVectorLayerName));
            }

            if (filterParams == null)
            {
                throw new ArgumentNullException(nameof(filterParams));
            }

            try
            {
                RunBusyMapOperation(() =>
                {
                    VectorLayer sourceLayer = FindVectorLayer(strVectorLayerName);
                    if (sourceLayer == null)
                    {
                        throw new InvalidOperationException($"'{strVectorLayerName}' adlı vektör katmanı bulunamadı.");
                    }

                    Envelope envelope = new Envelope();
                    VectorLayer selectedFeatureLayer = helper.FilterFeatures(SelectionLayerName, sourceLayer, envelope, filterParams.ToArray());
                    helper.AddVectorLayer(mapBox.Map, selectedFeatureLayer);

                    if (!envelope.IsNull)
                    {
                        mapBox.Map.ZoomToBox(helper.TransformEnvelope(envelope));
                    }

                    mapBox.Refresh();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunBusyMapOperation(Action operation)
        {
            Application.DoEvents();
            SetBusy(true);
            try
            {
                operation();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool isBusy)
        {
            // Hata oluşsa bile çağıran finally bloğu imleç ve ilerleme durumunu birlikte kapatır.
            Application.UseWaitCursor = isBusy;
            mapBox.ShowProgressUpdate = isBusy;
        }

        private VectorLayer FindVectorLayer(string layerName)
        {
            // QGIS katman adlarının büyük/küçük harf kullanımı projeden projeye değişebilir.
            return mapBox.Map.Layers
                .OfType<VectorLayer>()
                .FirstOrDefault(layer => string.Equals(layer.LayerName, layerName, StringComparison.OrdinalIgnoreCase));
        }

    }
}
