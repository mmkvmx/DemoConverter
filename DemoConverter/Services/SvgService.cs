using DemoConverter.Models;
using System.Globalization;
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
                        seatNode.Attributes.Append(CreateAttribute(xDoc, "place", seat.ToString()));
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

        public void ClearSvgXmlDoc(XmlDocument xDoc, bool clearCss, string customCss = "")
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
        var parent = node.ParentNode;

        // Переносим координаты, если у tspan есть, а у родителя нет
        var tspanX = node.Attributes?["x"]?.Value;
        var tspanY = node.Attributes?["y"]?.Value;

        if (!string.IsNullOrEmpty(tspanX) && parent.Attributes["x"] == null)
        {
            var attrX = xDoc.CreateAttribute("x");
            attrX.Value = tspanX;
            parent.Attributes.Append(attrX);
        }

        if (!string.IsNullOrEmpty(tspanY) && parent.Attributes["y"] == null)
        {
            var attrY = xDoc.CreateAttribute("y");
            attrY.Value = tspanY;
            parent.Attributes.Append(attrY);
        }

        // Собираем текст
        string cleanedText = "";
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
            parent.InsertBefore(newTextNode, node);
        }

        parent.RemoveChild(node);
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
        
        // новый метод с переработанным центрированием и смещением текста внутри
        public void ModifySvg(XmlDocument xDoc, bool updateCircleToRect)
        {

            if (updateCircleToRect)
            {
                ConvertCirclesToRects(xDoc);
            }

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            var rectNodes = xDoc.SelectNodes("//ns:g[@id='seats']//ns:rect", namespaceManager);
            if (rectNodes == null) return;

            foreach (XmlNode rectNode in rectNodes)
            {
                if (rectNode.Attributes == null) continue;

                try
                {
                    double x = GetAttributeValue(rectNode, "x");
                    double y = GetAttributeValue(rectNode, "y");
                    double width = GetAttributeValue(rectNode, "width");

                    double height = GetAttributeValue(rectNode, "height");


                    double newWidth = width;
                    double newHeight = height;

                    double newX = x;
                    double newY = y;

                    double centerX = newX + newWidth / 2;
                    double centerY = newY + newHeight / 2;

                    XmlNode parentNode = rectNode.ParentNode;
                    if (parentNode == null) continue;

                    // *** ОБЪЕДИНЕНИЕ ТЕКСТОВ ***
                    XmlNodeList textNodes = parentNode.SelectNodes(".//ns:text", namespaceManager);
                    if (textNodes != null && textNodes.Count > 1)
                    {
                        MergeTextNodes(textNodes[0], textNodes[1]);
                    }

                    // Работаем с одним (объединённым) текстовым узлом
                    XmlNode textNode = parentNode.SelectSingleNode(".//ns:text", namespaceManager);
                    if (textNode == null) continue;

                    string text = textNode.InnerText.Trim();
                    int length = text.Length;

                    if (textNode.Attributes != null)
                    {
                        if (textNode.Attributes["transform"] != null)
                            textNode.Attributes.RemoveNamedItem("transform");

                        if (textNode.Attributes["dy"] != null)
                            textNode.Attributes.RemoveNamedItem("dy");

                        SetAttributeValue(textNode, "dy", "0.15ex");
                        SetAttributeValue(textNode, "text-anchor", "middle");
                        SetAttributeValue(textNode, "dominant-baseline", "middle");

                        SetAttributeValue(textNode, "x", centerX.ToString("F6", CultureInfo.InvariantCulture));
                        SetAttributeValue(textNode, "y", centerY.ToString("F6", CultureInfo.InvariantCulture));
                        SetAttributeValue(textNode, "fill", "#565C60");
                        SetAttributeValue(textNode, "stroke-opacity", "0");

                        if (length >= 3)
                        {
                            SetAttributeValue(textNode, "textLength", "15");
                            SetAttributeValue(textNode, "lengthAdjust", "spacingAndGlyphs");
                        }
                        else
                        {
                            if (textNode.Attributes["textLength"] != null)
                                textNode.Attributes.RemoveNamedItem("textLength");
                            if (textNode.Attributes["lengthAdjust"] != null)
                                textNode.Attributes.RemoveNamedItem("lengthAdjust");

                        }

                        SetAttributeValue(textNode, "font-family", "Consolas, Courier New, monospace");
                        SetAttributeValue(textNode, "font-size", "9");

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // Применение стилей к надписям
            var captionNodes = xDoc.SelectNodes("//ns:g[@id='adds']//ns:text", namespaceManager);
            if (captionNodes == null) return;

            foreach (XmlNode captionNode in captionNodes)
            {
                if (captionNode.Attributes == null) continue;
                try
                {
                    SetAttributeValue(captionNode, "font-family", "Arial, Inter, Veradana");
                    SetAttributeValue(captionNode, "fill", "#565C60");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void AssignUniqueIds(XmlDocument xDoc)
        {
            int counter = 1;
            XmlNodeList allNodes = xDoc.GetElementsByTagName("*");

            foreach (XmlNode node in allNodes)
            {
                if (node.Attributes != null && node.Attributes["id"] == null)
                {
                    XmlAttribute idAttr = xDoc.CreateAttribute("id");
                    idAttr.Value = $"auto-id-{counter++}";
                    node.Attributes.Append(idAttr);
                }
            }
        }

        // Редактирование, чтобы избавиться от лишних вызовов обработки текстовых доков 
        public void EditPlaces(XmlDocument xDoc, double placeMarginGorizontal, double placeMarginVertical, double placeSizeWidth, double placeSizeHeight, double cornerRadius, bool rectFill, double fontSize, int fontWeigth)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            var rectNodes = xDoc.SelectNodes("//ns:g[@id='places']//ns:rect", namespaceManager);
            if (rectNodes == null) return;

            foreach (XmlNode rectNode in rectNodes)
            {
                if (rectNode.Attributes == null) continue;

                try
                {
                    double x = GetAttributeValue(rectNode, "x");
                    double y = GetAttributeValue(rectNode, "y");
                    double width = GetAttributeValue(rectNode, "width");

                    double height = GetAttributeValue(rectNode, "height");


                    double newWidth = placeSizeWidth > 0 ? placeSizeWidth: width;
                    double newHeight = placeSizeHeight > 0 ? placeSizeHeight : height;

                    double newX = x - placeMarginGorizontal;
                    double newY = y - placeMarginVertical;

                    double centerX = newX + newWidth / 2;
                    double centerY = newY + newHeight / 2;

                    SetAttributeValue(rectNode, "height", newHeight.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "width", newWidth.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "x", newX.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "y", newY.ToString("F6", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "rx", cornerRadius.ToString("F3", CultureInfo.InvariantCulture));
                    if (rectFill)
                    {
                        SetAttributeValue(rectNode, "fill", "#A4E57A");
                    }
                    else
                    {
                        rectNode.Attributes.RemoveNamedItem("fill");
                    }
                        XmlNode parentNode = rectNode.ParentNode;
                    if (parentNode == null) continue;

                    XmlNode textNode = parentNode.SelectSingleNode(".//ns:text", namespaceManager);
                    if (textNode == null) continue;

                    string text = textNode.InnerText.Trim();
                    int length = text.Length;
                    if (textNode.Attributes != null)
                    {
                        SetAttributeValue(textNode, "font-weight", fontWeigth.ToString(CultureInfo.InvariantCulture));
                        SetAttributeValue(textNode, "text-anchor", "middle");
                        SetAttributeValue(textNode, "font-size", $"{fontSize.ToString(CultureInfo.InvariantCulture)}px");
                        SetAttributeValue(textNode, "x", centerX.ToString("F6", CultureInfo.InvariantCulture));
                        SetAttributeValue(textNode, "y", centerY.ToString("F6", CultureInfo.InvariantCulture));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при редактировании мест: {ex.Message}");
                }
            }
        }

        // Сдвиги отдельных элементов
        public void MoveElement(XmlDocument xDoc, string elementId, double dx, double dy)
        {
            if (string.IsNullOrEmpty(elementId))
            {
                Console.WriteLine("Не передан id элемента для сдвига.");
                return;
            }

            var nsmgr = new XmlNamespaceManager(xDoc.NameTable);
            nsmgr.AddNamespace("ns", "http://www.w3.org/2000/svg");

            // Ищем элемент по id
            var node = xDoc.SelectSingleNode($"//*[@id='{elementId}']", nsmgr);
            if (node == null)
            {
                Console.WriteLine($"Элемент с id '{elementId}' не найден.");
                return;
            }

            try
            {
                // Сдвигаем координаты для rect, image, circle, text и т.д.
                if (node.Name == "rect" || node.Name == "image")
                {
                    double x = GetAttributeValue(node, "x");
                    double y = GetAttributeValue(node, "y");
                    SetAttributeValue(node, "x", (x + dx).ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(node, "y", (y + dy).ToString("F3", CultureInfo.InvariantCulture));
                }
                else if (node.Name == "circle")
                {
                    double cx = GetAttributeValue(node, "cx");
                    double cy = GetAttributeValue(node, "cy");
                    SetAttributeValue(node, "cx", (cx + dx).ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(node, "cy", (cy + dy).ToString("F3", CultureInfo.InvariantCulture));
                }
                else if (node.Name == "text")
                {
                    double x = GetAttributeValue(node, "x");
                    double y = GetAttributeValue(node, "y");
                    SetAttributeValue(node, "x", (x + dx).ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(node, "y", (y + dy).ToString("F3", CultureInfo.InvariantCulture));
                }
                // Можно добавить обработку других типов элементов при необходимости
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при перемещении элемента: {ex.Message}");
            }
        }

        // удаление элемента, выбранного на фронте
        public void DeleteXmlElement(XmlDocument xDoc, string elementName)
        {
            if (!string.IsNullOrEmpty(elementName))
            {
                try
                {
                    var nsmgr = new XmlNamespaceManager(xDoc.NameTable);
                    nsmgr.AddNamespace("svg", "http://www.w3.org/2000/svg");

                    var tempDoc = new XmlDocument();
                    tempDoc.LoadXml(elementName);
                    var targetElement = tempDoc.DocumentElement;
                    if (targetElement == null) return;

                    var candidates = xDoc.GetElementsByTagName(targetElement.Name);
                    foreach (XmlNode node in candidates)
                    {
                        if (node.Attributes == null) continue;

                        bool match = true;
                        foreach (XmlAttribute attr in targetElement.Attributes)
                        {
                            var attrInNode = node.Attributes[attr.Name];
                            if (attrInNode == null || attrInNode.Value != attr.Value)
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                        {
                            node.ParentNode?.RemoveChild(node);
                            break; // удалили один — достаточно
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении элемента: {ex.Message}");
                }
            }
        }
        // Метод добавления id надписям и прочему, для удобной работы сдвигов отдельных элементов
        // Метод объединения двух <text> в один
        private void MergeTextNodes(XmlNode firstTextNode, XmlNode secondTextNode)
        {
            if (firstTextNode == null || secondTextNode == null) return;

            string combinedText = firstTextNode.InnerText.Trim() + " " + secondTextNode.InnerText.Trim();
            firstTextNode.InnerText = combinedText;

            XmlNode parent = secondTextNode.ParentNode;
            parent?.RemoveChild(secondTextNode);
        }



        public void ChangeAttributes(XmlDocument xDoc, string attrName, string targetValue, string newValue)
        {
            // Создаём менеджер пространства имён, если в документе используется xmlns
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            // Находим все элементы <g> с id="seats"
            string xpathQuery = $"//ns:g[@{attrName}='{targetValue}']";
            var nodes = xDoc.SelectNodes(xpathQuery, namespaceManager);

            // Если ничего не найдено, выходим
            if (nodes == null || nodes.Count == 0) return;

            foreach (XmlNode node in nodes)
            {
                try
                {
                    // Проверяем наличие атрибутов у текущего узла
                    if (node.Attributes != null)
                    {
                        // Ищем атрибут 
                        XmlAttribute attribute = node.Attributes[attrName];
                        if (attribute != null && attribute.Value == targetValue)
                        {
                            // Меняем значение атрибута 
                            attribute.Value = newValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке узла: {ex.Message}");
                }
            }
        }


        public void ConvertCirclesToRects(XmlDocument xDoc)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            var circleNodes = xDoc.SelectNodes("//ns:g[@id='seats']//ns:circle", namespaceManager);

            if (circleNodes == null) return;

            foreach (XmlNode circleNode in circleNodes)
            {
                if (circleNode.Attributes == null) continue;

                try
                {
                    // Получаем координаты и радиус круга
                    double cx = GetAttributeValue(circleNode, "cx");
                    double cy = GetAttributeValue(circleNode, "cy");
                    double r = GetAttributeValue(circleNode, "r");

                    // Создаем новый элемент <rect>
                    XmlDocument doc = circleNode.OwnerDocument;
                    XmlElement rect = doc.CreateElement("rect", doc.DocumentElement.NamespaceURI);

                    // Вычисляем координаты и размеры прямоугольника (координаты круга - центр, координаты прямоугольника края, поэтому учитываем радиус)
                    double x = cx - r;
                    double y = cy - r;

                    double width = 2 * r;
                    double height = 2 * r;

                    rect.SetAttribute("x", x.ToString("F6", CultureInfo.InvariantCulture));
                    rect.SetAttribute("y", y.ToString("F6", CultureInfo.InvariantCulture));
                    rect.SetAttribute("width", width.ToString("F6", CultureInfo.InvariantCulture));
                    rect.SetAttribute("height", height.ToString("F6", CultureInfo.InvariantCulture));

                    // Копируем остальные атрибуты (например, class, id)
                    foreach (XmlAttribute attr in circleNode.Attributes)
                    {
                        if (!rect.HasAttribute(attr.Name))
                        {
                            rect.SetAttribute(attr.Name, attr.Value);
                        }
                    }

                    // Заменяем <circle> на <rect>
                    XmlNode parent = circleNode.ParentNode;
                    parent.ReplaceChild(rect, circleNode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при преобразовании <circle> в <rect>: {ex.Message}");
                }
            }
        }


        //исправляет ошибку дублирования блоков у СБ, например несколько блоков с id="sectors" 
        public void MergeBlocks(XmlDocument xDoc, IdBlockType idType)
        {
            // Преобразуем enum в строку для использования в XPath
            string idValue = idType.ToString().ToLower(); // Преобразуем в нижний регистр: "sectors" или "seats"

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            // Находим все элементы с заданным id
            XmlNodeList nodes = xDoc.SelectNodes($"//ns:g[@id='{idValue}']", namespaceManager);

            if (nodes == null || nodes.Count == 0)
            {
                Console.WriteLine($"Элементы с id='{idValue}' не найдены.");
                return;
            }

            // Оставляем первый узел как основной
            XmlNode mainNode = nodes[0];

            for (int i = 1; i < nodes.Count; i++)
            {
                XmlNode currentNode = nodes[i];

                // Переносим все дочерние узлы в основной
                foreach (XmlNode childNode in currentNode.ChildNodes)
                {
                    XmlNode importedNode = xDoc.ImportNode(childNode, true);
                    mainNode.AppendChild(importedNode);
                }

                // Удаляем текущую группу
                currentNode.ParentNode.RemoveChild(currentNode);
            }

            Console.WriteLine($"Элементы с id='{idValue}' успешно объединены.");
        }
        public double GetAttributeValue(XmlNode node, string attributeName)
        {
            if (node.Attributes?[attributeName] == null)
            {
                throw new InvalidOperationException($"Атрибут '{attributeName}' отсутствует в узле {node.Name}");
            }

            return double.Parse(node.Attributes[attributeName].Value, CultureInfo.InvariantCulture);
        }

        public void SetAttributeValue(XmlNode node, string attributeName, string value)
        {
            if (node.Attributes?[attributeName] == null)
            {
                var newAttr = node.OwnerDocument.CreateAttribute(attributeName);
                newAttr.Value = value;
                node.Attributes.Append(newAttr);
            }
            else
            {
                node.Attributes[attributeName].Value = value;
            }
        }

    }
}
