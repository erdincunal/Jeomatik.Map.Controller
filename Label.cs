using SharpMap.Layers;
using SharpMap.Styles;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace MapController
{
    public partial class Helper
    {
        /// <summary>
        /// Bir vektör katmanındaki özniteliklerin harita üzerinde nasıl etiketleneceğini tanımlar.
        /// </summary>
        public class Label
        {
            public string FamilyName;
            public string LabelColumn;
            public bool Visible;
            public Color LabelColor;
            public Color HaloColor;
            public Single LabelSize;
            public Single HaloSize;
            public Int64 MinScale;
            public Int64 MaxScale;
            /// <summary>
            /// Bu ayarlardan, veri kaynağını ve koordinat dönüşümünü ana katmandan alan bir etiket katmanı üretir.
            /// </summary>
            public LabelLayer ToLabelLayer(VectorLayer layer)
            {
                LabelStyle labelStyle = new LabelStyle()
                {
                    ForeColor = LabelColor,
                    Font = string.IsNullOrWhiteSpace(FamilyName)
                        ? new Font(FontFamily.GenericSansSerif, LabelSize)
                        : new Font(FamilyName, LabelSize, FontStyle.Regular),
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Middle,
                    Offset = new PointF(3, 3),
                    Halo = (HaloSize > 0) ? new Pen(HaloColor, HaloSize * 3) : null,
                    CollisionDetection = true,
                };

                LabelLayer labelLayer = new LabelLayer($"{layer.LayerName}~{LabelColumn}")
                {
                    Enabled = true,
                    LabelColumn = LabelColumn,
                    TextRenderingHint = TextRenderingHint.AntiAlias,
                    SmoothingMode = SmoothingMode.AntiAlias,
                    Style = labelStyle,
                    DataSource = layer.DataSource,
                    CoordinateTransformation = layer.CoordinateTransformation,
                    ReverseCoordinateTransformation = layer.ReverseCoordinateTransformation,
                    // Etiket ve kaynak katmanın ölçek aralıklarının kesişimini kullan.
                    MinVisible = MinScale >= layer.MinVisible ? MinScale : layer.MinVisible,
                    MaxVisible = (MaxScale <= layer.MaxVisible && MaxScale != 0) ? MaxScale : layer.MaxVisible,
                    SRID = layer.SRID,
                    LabelFilter = SharpMap.Rendering.LabelCollisionDetection.ThoroughCollisionDetection,
                    MultipartGeometryBehaviour = SharpMap.Layers.LabelLayer.MultipartGeometryBehaviourEnum.Largest,
                    PriorityColumn = LabelColumn,
                };
                return labelLayer;
            }
        }

    }
}
