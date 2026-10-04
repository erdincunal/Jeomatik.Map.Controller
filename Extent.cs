using GeoAPI.Geometries;

namespace MapController
{
    public partial class Helper
    {
        /// <summary>
        /// Bir harita alanının minimum ve maksimum koordinatlarını taşır.
        /// </summary>
        public class Extent
        {
            public double XMin;
            public double YMin;
            public double XMax;
            public double YMax;
            public Extent(double xmin, double ymin, double xmax, double ymax)
            {
                XMin = xmin;
                YMin = ymin;
                XMax = xmax;
                YMax = ymax;
            }
            /// <summary>
            /// Alanı SharpMap/GeoAPI tarafından kullanılan zarf nesnesine dönüştürür.
            /// </summary>
            public Envelope ToEnvelope()
            {
                Envelope envelope = new Envelope(XMin, XMax, YMin, YMax);
                return envelope;
            }
        }
    }
}
