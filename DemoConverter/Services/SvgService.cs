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
        /*
        public void ModifySvg(XmlDocument xDoc, double placeMarginGorizontal, double placeMarginVertical, double placeSizeWidth, double placeSizeHeight, bool updateCircleToRect = false)
        {
            // Параметры для изменения размеров и отступов
            const double cornerRadius = 10.0; // Скругление углов

            // Преобразуем <circle> в <rect>, если это необходимо
            if (updateCircleToRect)
            {
                ConvertCirclesToRects(xDoc, placeMarginGorizontal, placeMarginVertical, placeSizeWidth, placeSizeHeight);
            }

            // Получаем все элементы <rect>, находящиеся внутри <g id="seats">, то есть не все ректы, а только те что места
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
            namespaceManager.AddNamespace("ns", "http://www.w3.org/2000/svg");

            var rectNodes = xDoc.SelectNodes("//ns:g[@id='seats']//ns:rect", namespaceManager);

            if (rectNodes == null) return;

            foreach (XmlNode rectNode in rectNodes)
            {
                if (rectNode.Attributes == null) continue;

                try
                {
                    // Получаем текущие значения атрибутов
                    double x = GetAttributeValue(rectNode, "x");
                    double y = GetAttributeValue(rectNode, "y");
                    double width = GetAttributeValue(rectNode, "width");
                    double height = GetAttributeValue(rectNode, "height");

                    // Вычисляем новые значения для прямоугольника
                    double newX = x - placeMarginGorizontal;
                    double newY = y + placeMarginVertical;
                    double newWidth = width + 2 * placeSizeWidth;
                    double newHeight = height + 2 * placeSizeHeight;


                    // Корректируем позицию текста обозначчающение номер места в зависимости от ширины цифр
                    XmlNode textNode = rectNode.ParentNode?.SelectSingleNode(".//ns:text", namespaceManager);
                    if (textNode != null && textNode.Attributes != null)
                    {
                        if (textNode.InnerText.ToString().Length == 2)
                        {
                            newX += 4;
                        }
                        if (textNode.InnerText.ToString().Length >= 3)
                        {
                            newX += 6;
                        }
                    }



                    // Устанавливаем новые значения атрибутов для прямоугольника
                    SetAttributeValue(rectNode, "x", newX.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "y", newY.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "width", newWidth.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "height", newHeight.ToString("F3", CultureInfo.InvariantCulture));

                    // Добавляем дополнительные атрибуты для прямоугольника
                    //SetAttributeValue(rectNode, "rx", cornerRadius.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "rx", "5.0000");
                    //SetAttributeValue(rectNode, "fill", "#E9ECEE"); //бекграунд места
                    //SetAttributeValue(rectNode, "stroke", "#E9ECEE"); //обводка места
                    //SetAttributeValue(rectNode, "stroke-width", "1");//толщина обводки места



                    // Корректируем позицию текста внутри <g>
                    /*
                    XmlNode textNode = rectNode.ParentNode?.SelectSingleNode(".//ns:text", namespaceManager);
                    if (textNode != null && textNode.Attributes != null)
                    {
                        
                        string transform = textNode.Attributes["transform"]?.Value;
                        string pattern = @"matrix\(1 0 0 1 ([\d\.-]+) ([\d\.-]+)\)";
                        string newTransform = Regex.Replace(transform, pattern, match =>
                        {
                            // Парсим координаты из группы
                            double x = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                            double y = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

                            // Добавляем margin к x и y 
                            y -= 2;
                            if (textNode.InnerText.ToString().Length > 2)
                            {
                                x += placeMarginGorizontal + 2;
                            }
                            else
                            {
                                x += placeMarginGorizontal - 1;
                            }
                            
                            // Возвращаем обновленную строку
                            return $"matrix(1 0 0 1 {x:F3} {y:F3})"; // Формат с четырьмя знаками после запятой
                        });

                        textNode.Attributes["transform"].Value = newTransform.Replace(',', '.');
                        

                        SetAttributeValue(textNode, "fill", "#121212");//цвет цифр
                        SetAttributeValue(textNode, "stroke-opacity", "0");
                        SetAttributeValue(textNode, "font-size", "12px");
                        SetAttributeValue(textNode, "font-family", "Inter, Arial, Verdana");
                        SetAttributeValue(textNode, "font-weight", "800");
                    }

                }
                catch (Exception ex)
                {
                    // Логируем проблему или продолжаем обработку
                    Console.WriteLine(ex.Message);
                }
            }
            
        }
        */

        // новый метод с переработанным центрированием и смещением текста внутри
        public void ModifySvg(XmlDocument xDoc, double placeMarginGorizontal, double placeMarginVertical, double placeSizeWidth, double placeSizeHeight, bool updateCircleToRect)
        {
            const double cornerRadius = 2.0;

            if (updateCircleToRect)
            {
                ConvertCirclesToRects(xDoc, placeMarginGorizontal, placeMarginVertical, placeSizeWidth, placeSizeHeight);
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

                    double newWidth = width + 2 * placeSizeWidth;
                    double newHeight = height + 2 * placeSizeHeight;

                    double newX = x - placeSizeWidth + placeMarginGorizontal;
                    double newY = y - placeSizeHeight + placeMarginVertical;

                    double centerX = newX + newWidth / 2;
                    double centerY = newY + newHeight / 2;

                    // Обновляем атрибуты прямоугольника
                    SetAttributeValue(rectNode, "x", newX.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "y", newY.ToString("F6", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "width", newWidth.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "height", newHeight.ToString("F3", CultureInfo.InvariantCulture));
                    SetAttributeValue(rectNode, "rx", cornerRadius.ToString("F3", CultureInfo.InvariantCulture));

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

                        SetAttributeValue(textNode, "fill", "#121212");
                        SetAttributeValue(textNode, "stroke-opacity", "0");

                        if (length >= 3)
                        {
                            SetAttributeValue(textNode, "textLength", "15");
                            SetAttributeValue(textNode, "lengthAdjust", "spacingAndGlyphs");
                            SetAttributeValue(textNode, "font-weight", "700");
                        }
                        else
                        {
                            if (textNode.Attributes["textLength"] != null)
                                textNode.Attributes.RemoveNamedItem("textLength");
                            if (textNode.Attributes["lengthAdjust"] != null)
                                textNode.Attributes.RemoveNamedItem("lengthAdjust");

                            SetAttributeValue(textNode, "font-weight", "600");
                        }

                        SetAttributeValue(textNode, "font-size", "12px");
                        SetAttributeValue(textNode, "font-family", "Inter, Arial, Verdana");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

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


        public void ConvertCirclesToRects(XmlDocument xDoc, double marginGorizontal, double marginVertical, double placeSizeWidth, double placeSizeHeight)
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
                    double x = cx - r + marginGorizontal;
                    double y = cy - r + marginVertical;

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
