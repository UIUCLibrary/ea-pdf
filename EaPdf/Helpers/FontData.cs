using RoyT.TrueType.Helpers;
using RoyT.TrueType.Tables.Name;
using RoyT.TrueType;
using System.Globalization;
using System.Text.RegularExpressions;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class FontData
    {
        public string Family { get; set; } = string.Empty;
        public string Subfamily { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public FontHelper.BaseFontFamily BaseFamily { get; set; }
        public long FileSize { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fontFolder"></param>
        /// <param name="baseFontMapping"></param>
        /// <returns></returns>
        public static List<FontData> GetList(string fontFolder, Dictionary<Regex, FontHelper.BaseFontFamily> baseFontMapping)
        {
            List<FontData> fontDataList = new();

            var ttfFiles = Directory.GetFiles(fontFolder, "*.ttf", SearchOption.AllDirectories);

            foreach (var ttfFile in ttfFiles)
            {
                var font = TrueTypeFont.FromFile(ttfFile);
                var family = NameHelper.GetName(NameId.FontFamilyName, CultureInfo.CurrentCulture, font);
                var subfamily = NameHelper.GetName(NameId.FontSubfamilyName, CultureInfo.CurrentCulture, font);

                var style = FontHelper.GuessFontStyle(font);
                var weight = FontHelper.GuessFontWeight(font);
                var bas = FontHelper.GuessBaseFontFamily(font, baseFontMapping);

                var fontData = new FontData
                {
                    Family = family,
                    Subfamily = subfamily,
                    Style = style,
                    Weight = weight,
                    Path = ttfFile,
                    BaseFamily = bas,
                    FileSize = new FileInfo(ttfFile).Length
                };

                fontDataList.Add(fontData);
            }

            return fontDataList;
        }


    }
}
