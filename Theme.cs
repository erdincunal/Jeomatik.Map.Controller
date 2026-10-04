using System.Drawing;

namespace MapController
{
    public partial class Helper
    {
        /// <summary>
        /// Bir öznitelik sütununa bağlı tematik gösterim ayarlarını tanımlar.
        /// </summary>
        public class Theme
        {
            public string ColumnName;
            public Layer ThematicLayer;
            public bool Open;
            public Color OutColor;
            public Color FillColor;
            public float LineWidth;
        }
    }
}
