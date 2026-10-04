using System;
using System.Collections.Generic;
using System.Drawing;

namespace MapController
{
    public partial class Helper
    {
        /// <summary>
        /// Bir vektör katmanının dolgu, sınır ve sembol boyutu ayarlarını taşır.
        /// </summary>
        public class Style
        {
            public Color OutColor;
            public Color FillColor;
            public Single Size;
            public List<Theme> Themes;
        }
    }
}
