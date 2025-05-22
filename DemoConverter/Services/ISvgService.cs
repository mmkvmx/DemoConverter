using DemoConverter.Models;
using System.Xml;
namespace DemoConverter.Services
{
    public interface ISvgService
    {
        public HashSet<int> MarkPlaces(XmlDocument xDoc, List<SbPlace> sbPlaces, string sbSectorId = null);
        public void MarkSectors(XmlDocument xDoc, List<SbSector> sbSectors);
        public void ClearSvgXmlDoc(XmlDocument xDoc, bool clearCss = false, string customCss = "");
        public void ModifySvg(XmlDocument xDoc, double placeMarginGorizontal, double placeMarginVertical, double placeSizeWidth, double placeSizeHeight, bool updateCircleToRect = false, bool rectFill = false, double cornerRadius = 0, double fontSize = 9, int fontWeight = 600);
        public void EditPlaces(XmlDocument xDoc, double placeMarginGorizontal, double placeMarginVertical, double placeSizeWidth, double placeSizeHeight, double cornerRadius, bool rectFill, double fontSize, int fontWeigth);
        public void DeleteXmlElement(XmlDocument xDoc, string elementName);
        public void ConvertCirclesToRects(XmlDocument xDoc, double marginGorizontal, double marginVertical);
        public double GetAttributeValue(XmlNode node, string attributeName);
        public void SetAttributeValue(XmlNode node, string attributeName, string value);
        void MergeBlocks(XmlDocument xDoc, IdBlockType idType);
        public void ChangeAttributes(XmlDocument xDoc, string attrName, string targetValue, string newValue);
    }
}
public enum IdBlockType
{
    Sectors,
    Seats
}