namespace MapController
{
    public partial class Helper
    {
        /// <summary>
        /// Haritanın arka planında kullanılabilecek çevrimiçi karo sağlayıcılarını belirtir.
        /// </summary>
        public enum TileMap
        {
            OSM,
            BingMapRoads,
            BingMapAerial,
            BingMapHybrid,
            GoogleMap,
            GoogleSatellite,
            GoogleLabels,
            GoogleSatelliteAndLabels,
            MapBox
        }
    }
}
