using System;

namespace MapController
{
    /// <summary>
    /// Metin değerlerini kullanıcının bölgesel ondalık ayarlarını dikkate alarak sayıya dönüştürür.
    /// </summary>
    internal static class Converter
    {
        /// <summary>
        /// Virgül veya nokta ile yazılmış bir değeri <see cref="double"/> türüne dönüştürür.
        /// Geçersiz veya boş değerlerde sıfır döndürür.
        /// </summary>
        public static double ToDouble(string strValidation)
        {
            double value;
            return double.TryParse(
                NormalizeDecimalSeparator(strValidation),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.CurrentCulture,
                out value)
                ? value
                : 0;
        }

        /// <summary>
        /// Virgül veya nokta ile yazılmış bir değeri <see cref="float"/> türüne dönüştürür.
        /// Geçersiz veya boş değerlerde sıfır döndürür.
        /// </summary>
        public static float ToFloat(string strValidation)
        {
            float value;
            return float.TryParse(
                NormalizeDecimalSeparator(strValidation),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.CurrentCulture,
                out value)
                ? value
                : 0;
        }

        private static string NormalizeDecimalSeparator(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // QGIS dosyaları nokta, kullanıcı girişi ise virgül kullanabilir. Ayracı tek biçime getir.
            string decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            return decimalSeparator == "."
                ? value.Replace(",", decimalSeparator)
                : value.Replace(".", decimalSeparator);
        }
    }
}
