using DemoConverter.Models;
using System.Xml;
namespace DemoConverter.Services
{
    public interface ISvgService
    {
        public HashSet<int> MarkPlaces(XmlDocument xDoc, List<SbPlace> sbPlaces, string sbSectorId = null);
        public void MarkSectors(XmlDocument xDoc, List<SbSector> sbSectors);
        public void ClearSvgXmlDoc(XmlDocument xDoc, bool clearCss = false, string customCss = "");
    }
}
