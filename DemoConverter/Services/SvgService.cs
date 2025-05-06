using DemoConverter.Models;
using System.Text.RegularExpressions;
using System.Xml;

namespace DemoConverter.Services
{
    public class SvgService : ISvgService
    {
        // Метод добавления новых атрибутов каждому <g>
        private XmlAttribute CreateAttribute(XmlDocument doc, string name, string value)
        {
            var attr = doc.CreateAttribute(name);
            attr.Value = value;
            return attr;
        }

        // Ищем по id соответсвие в SVG с SbPlaces, меняем родной id на uniqueId, добавляем атрибуты места, ряда и сектора
        public HashSet<int> MarkPlaces(XmlDocument xDoc, List<SbPlace> sbPlaces, string sbSectorId = null)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            XmlNodeList seatNodes = xDoc.SelectNodes("//ns:g[@id='seats']/ns:g", namespaceManager);

            if (seatNodes == null || seatNodes.Count == 0)
            {
                throw new Exception("Список узлов пуст");
            }

            var existSectors = new HashSet<int>(); // уникальные SectorId

            foreach (var place in sbPlaces) 
            {
                int placeId = place.Id;
                int sectorId = place.SectorId;
                int? row = place.Row;
                int seat = place.Seat;

                if (sbSectorId != null && sectorId != int.Parse(sbSectorId))
                {
                    continue;
                }
                string uniqueId = $"{sectorId}_{(row.HasValue ? row.ToString() : "")}_{seat}";

                foreach (XmlNode seatNode in seatNodes)
                {
                    var idAttr = seatNode.Attributes?["id"]?.Value;

                    if (idAttr != null &&
                        idAttr.StartsWith("s") &&
                        idAttr.Substring(1) == placeId.ToString())
                    {
                        seatNode.Attributes["id"].Value = uniqueId;
                        seatNode.Attributes.Append(CreateAttribute(xDoc, "sectorAlias", sectorId.ToString()));
                        seatNode.Attributes.Append(CreateAttribute(xDoc, "row", row.HasValue ? row.ToString() : "")); // если row пустой, добавляем пустую строку
                        seatNode.Attributes.Append(CreateAttribute(xDoc, "place", place.ToString()));
                        seatNode.Attributes.Append(CreateAttribute(xDoc, "class", "place"));
                        existSectors.Add(sectorId);
                    }
                }
            }
            return existSectors;
        }

        public void MarkSectors(XmlDocument xDoc, List<SbSector> sbSectors)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            // Поиск родителя с id="sectors"
            var sectorNods = xDoc.SelectNodes("//ns:g[@id='sectors']/ns:g", namespaceManager);

            if (sbSectors == null) return;

            foreach (var sbSector in sbSectors)
            {
                int sectorId = sbSector.Id;

                // Формируем уникальный идентификатор
                // Каждый сектор имеет уникальный идентификатор в пределах зала sectorAlias. 
                string uniqueId = sectorId.ToString();

                foreach (XmlNode sector in sectorNods)
                {
                    // Проверяем, соответствует ли текущий узел нужному шаблону
                    if (sector.Attributes != null &&
                    sector.Attributes["id"]?.Value.StartsWith("a") == true &&
                    sector.Attributes["id"].Value.Substring(1) == sectorId.ToString())
                    {
                        // Обновляем атрибуты узла
                        var attributes = sector.Attributes;

                        if (attributes != null)
                        {
                            attributes["id"].Value = uniqueId;
                        }
                    }
                }
            }
        }

        public void ClearSvgXmlDoc(XmlDocument xDoc, bool clearCss = false, string customCss = "")
        {
            // добавляем к тегу svg  id="hall_scheme"
            XmlNodeList svgElementList = xDoc.GetElementsByTagName("svg");
            if (svgElementList.Count > 0)
            {
                // Заменяем значение атрибута id на "hall_scheme"
                XmlAttribute idAttr = svgElementList[0].Attributes["id"];
                if (idAttr != null)
                {
                    idAttr.Value = "hall_scheme";
                }
                xDoc.InnerXml = xDoc.OuterXml;
            }

            // удаляем что-то типа <defs id="..." />
            XmlNodeList nodesToRemoveList = xDoc.GetElementsByTagName("defs");
            if (nodesToRemoveList.Count > 0)
            {
                nodesToRemoveList[0].ParentNode?.RemoveChild(nodesToRemoveList[0]);
                xDoc.InnerXml = xDoc.OuterXml;
            }


            // удаляем теги, оставленные програмами-редакторами
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);

            namespaceManager.AddNamespace("sodipodi", "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd");
            namespaceManager.AddNamespace("inkscape", "http://www.inkscape.org/namespaces/inkscape");
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            nodesToRemoveList = xDoc.SelectNodes("//sodipodi:namedview | //inkscape:grid", namespaceManager);

            if (nodesToRemoveList != null)
            {
                foreach (XmlNode node in nodesToRemoveList)
                {
                    node.ParentNode?.RemoveChild(node);
                }
                xDoc.InnerXml = xDoc.OuterXml;
            }


            // Удаляем colorscheme
            //namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            nodesToRemoveList = xDoc.SelectNodes("//ns:g[@id='icolorscheme']", namespaceManager);
            if (nodesToRemoveList != null && nodesToRemoveList.Count > 0)
            {
                var nodesToRemove = new List<XmlNode>();
                foreach (XmlNode node in nodesToRemoveList)
                {
                    nodesToRemove.Add(node);
                }

                foreach (XmlNode node in nodesToRemove)
                {
                    node.ParentNode?.RemoveChild(node);
                }
            }
            xDoc.InnerXml = xDoc.OuterXml;


            // Удаляем tspan, но сохраняем их содержимое (без пробелов и переносов)
            nodesToRemoveList = xDoc.SelectNodes("//ns:tspan", namespaceManager);

            if (nodesToRemoveList != null && nodesToRemoveList.Count > 0)
            {
                var nodesToReplace = new List<XmlNode>();

                foreach (XmlNode node in nodesToRemoveList)
                {
                    nodesToReplace.Add(node);
                }

                foreach (XmlNode node in nodesToReplace)
                {
                    if (node.ParentNode != null)
                    {
                        string cleanedText = "";

                        // Собираем весь текст из tspan, удаляя лишние пробелы, табы и переносы строк
                        foreach (XmlNode child in node.ChildNodes)
                        {
                            if (child.NodeType == XmlNodeType.Text)
                            {
                                cleanedText += child.InnerText.Trim().Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("  ", "");
                            }
                        }

                        if (!string.IsNullOrEmpty(cleanedText))
                        {
                            XmlText newTextNode = xDoc.CreateTextNode(cleanedText);
                            node.ParentNode.InsertBefore(newTextNode, node);
                        }

                        node.ParentNode.RemoveChild(node);
                    }
                }
            }
            xDoc.InnerXml = xDoc.OuterXml;


            // удаляем все стили
            if (clearCss)
            {
                nodesToRemoveList = xDoc.GetElementsByTagName("style");
                if (nodesToRemoveList != null && nodesToRemoveList.Count > 0)
                {
                    // Копируем узлы в отдельный список
                    var nodesToRemove = new List<XmlNode>();
                    foreach (XmlNode node in nodesToRemoveList)
                    {
                        nodesToRemove.Add(node);
                    }

                    // Удаляем узлы
                    foreach (XmlNode node in nodesToRemove)
                    {
                        node.ParentNode?.RemoveChild(node);
                    }

                    // Обновляем документ
                    xDoc.InnerXml = xDoc.OuterXml;
                }


                // Список атрибутов для удаления
                // вырезаем все иннер стили и имена классов, при этом оставляем class="place" и class="sector"
                // вырезаем атрибуты с цветами
                var attributesToRemove = new[]
                {
                    "style", "font-size", "font-family", "class",
                    "sodipodi:docname", "inkscape:version",
                    "xmlns:inkscape", "xmlns:sodipodi",
                    "fill"
                };

                void RemoveAttributes(XmlNode node)
                {
                    if (node.Attributes != null)
                    {
                        foreach (var attr in attributesToRemove)
                        {
                            // Проверяем, чтобы оставить class="place" и class="sector"
                            if (attr == "class")
                            {
                                var classValue = node.Attributes[attr]?.Value;
                                if (!string.IsNullOrEmpty(classValue) && (classValue == "place" || classValue == "sector"))
                                    continue;
                            }

                            // Удаляем атрибут, если он существует
                            if (node.Attributes[attr] != null)
                                node.Attributes.RemoveNamedItem(attr);
                        }
                    }

                    // Обрабатываем дочерние узлы
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        RemoveAttributes(child);
                    }
                }

                // Запускаем очистку с корневого узла
                RemoveAttributes(xDoc.DocumentElement);


                // удалем все нестандартные для SVG аттрибуты
                void ProcessNode(XmlNode node)
                {
                    // Определяем стандартное пространство имен SVG
                    string svgNamespace = "http://www.w3.org/2000/svg";

                    // Удаляем все атрибуты, которые не относятся к стандартному пространству имен SVG
                    if (node.Attributes != null)
                    {
                        var attributesToRemove = new List<XmlAttribute>();

                        foreach (XmlAttribute attr in node.Attributes)
                        {
                            if (attr.NamespaceURI != svgNamespace && !string.IsNullOrEmpty(attr.NamespaceURI))
                            {
                                attributesToRemove.Add(attr);
                            }
                        }

                        foreach (var attr in attributesToRemove)
                        {
                            node.Attributes.Remove(attr);
                        }
                    }

                    // Обрабатываем дочерние узлы
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        ProcessNode(childNode);
                    }
                }

                // добавляем CSS-стили для отображения элементов 
                if (!string.IsNullOrEmpty(customCss))
                {
                    // Создаем элемент <style>
                    XmlElement styleElement = xDoc.CreateElement("style", xDoc.DocumentElement.NamespaceURI);
                    styleElement.SetAttribute("type", "text/css");

                    // Создаем содержимое CDATA с переданным CSS-контентом
                    XmlCDataSection cdataSection = xDoc.CreateCDataSection(customCss);

                    // Добавляем CDATA в элемент <style>
                    styleElement.AppendChild(cdataSection);

                    // Добавляем <style> как первый дочерний элемент <svg>
                    if (xDoc.DocumentElement != null)
                    {
                        xDoc.DocumentElement.InsertBefore(styleElement, xDoc.DocumentElement.FirstChild);
                    }
                }

            }

            // удаляем пустые строки
            string emptyLinePattern = @"^\s*\r?\n|\r?\n(?!\s*\S)";
            xDoc.InnerXml = Regex.Replace(xDoc.InnerXml, emptyLinePattern, "", RegexOptions.Multiline);

        }


    }
}
