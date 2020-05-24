namespace mprMepElementsBender.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Electrical;
    using Autodesk.Revit.DB.Mechanical;
    using Autodesk.Revit.DB.Plumbing;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using Enums;
    using Models;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Сервис работы с документом Revit
    /// </summary>
    public class RevitOperationService
    {
        private readonly string _langItem = ModPlusConnector.Instance.Name;
        private readonly UIApplication _uiApplication;
        private readonly Document _doc;

        /// <summary>
        /// Создает экземпляр класса <see cref="RevitOperationService"/>
        /// </summary>
        /// <param name="uiApplication">Активная сессия пользовательского интерфейса Revit</param>
        public RevitOperationService(UIApplication uiApplication)
        {
            _uiApplication = uiApplication;
            _doc = uiApplication.ActiveUIDocument.Document;
        }

        /// <summary>
        /// Возвращает список категорий элементов Revit
        /// </summary>
        /// <returns>Список категорий элементов Revit</returns>
        public static List<ElementCategory> GetElementCategories()
        {
            return new List<ElementCategory>
            {
                new ElementCategory(typeof(Pipe)),
                new ElementCategory(typeof(Duct)),
                new ElementCategory(typeof(CableTray)),
                new ElementCategory(typeof(Conduit)),
            };
        }

        /// <summary>
        /// Перевод из дюймов в миллиметры
        /// </summary>
        /// <param name="inches">Значение в дюймах</param>
        /// <returns>Значение в миллиметрах</returns>
        public static double ToMillimeters(double inches)
        {
            return UnitUtils.ConvertFromInternalUnits(inches, DisplayUnitType.DUT_MILLIMETERS);
        }

        /// <summary>
        /// Перевод из дюймов в миллиметры
        /// </summary>
        /// <param name="millimeters">Значение в миллиметрах</param>
        /// <returns>Значение в дюймах</returns>
        public static double ToInches(double millimeters)
        {
            return UnitUtils.ConvertToInternalUnits(millimeters, DisplayUnitType.DUT_MILLIMETERS);
        }

        /// <summary>
        /// Получает выбранные элементы модели
        /// </summary>
        /// <returns>Список выбранных элементов модели</returns>
        public List<CustomElement> GetSelectedElements()
        {
            var selectedElementIds = _uiApplication.ActiveUIDocument.Selection.GetElementIds();

            return selectedElementIds
                .Select(elementId => new CustomElement(_doc.GetElement(elementId)))
                .ToList();
        }

        /// <summary>
        /// Инициирует выбор элементов модели
        /// </summary>
        /// <param name="allowedCategories">Список разрешенных категорий</param>
        /// <returns>Список выбранных элементов модели</returns>
        public List<CustomElement> SelectElements(IEnumerable<ElementCategory> allowedCategories)
        {
            try
            {
                var selectedElementRefs = _uiApplication.ActiveUIDocument.Selection.PickObjects(
                    ObjectType.Element,
                    new CategoriesSelectionFilter(allowedCategories),
                    ModPlusAPI.Language.GetItem(_langItem, "m19"));

                _uiApplication.ActiveUIDocument.Selection.SetElementIds(new List<ElementId>());

                return selectedElementRefs
                    .Select(reference =>
                        new CustomElement(_doc.GetElement(reference.ElementId)))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<CustomElement>();
            }
        }

        /// <summary>
        /// Изгибает заданные элементы для получения правильного пересечения
        /// </summary>
        /// <param name="crossingElements">Пересекающие элементы (изгибаемые)</param>
        /// <param name="allElementsToCross">Пересекаемые элементы</param>
        /// <param name="offset">Смещение, мм</param>
        /// <param name="angle">Угол, °</param>
        /// <param name="verticalBendingDirection">Вертикальное направление изгиба</param>
        /// <param name="horizontalBendingDirection">Горизонтальное направление изгиба</param>
        public void BendElements(
            List<CustomElement> crossingElements,
            List<CustomElement> allElementsToCross,
            int offset,
            int angle,
            Direction verticalBendingDirection,
            Direction horizontalBendingDirection)
        {
            var verticalOffset = verticalBendingDirection == Direction.Up ? offset : -offset;
            if (!allElementsToCross.Any())
                MessageBox.Show(ModPlusAPI.Language.GetItem(_langItem, "m21"));

            var passedCrossingElements = new List<CustomElement>();
            var errorElementIds = new List<int>();

            foreach (var crossingElement in crossingElements)
            {
                if (PassCategory(crossingElement))
                    passedCrossingElements.Add(crossingElement);
                else
                    errorElementIds.Add(crossingElement.Element.Id.IntegerValue);
            }

            using (var t = new Transaction(_doc, "aaa"/*ModPlusAPI.Language.GetItem(_langItem, "m20")*/))
            {
                try
                {
                    t.Start();

                    foreach (var crossingElement in passedCrossingElements)
                    {
                        var elementsToCross = GetElementsToCross(crossingElement, allElementsToCross).ToList();
                        if (!elementsToCross.Any())
                            continue;

                        var elementSets = GetElementSets(elementsToCross);
                        var lastCrossingElement = crossingElement;
                        Connector lastElbowConnector = null;
                        foreach (var elementSet in elementSets)
                        {
                            var bending = new Bending { Offset = offset, Angle = angle };
                            var orientation = elementSet.Key;
                            var elementType = elementSet.Value.First().ElementType;
                            var directionVector = GetBendingVector(lastCrossingElement, elementSet.Value);

                            bending.Horizontal = new BendingSection(lastCrossingElement.Element, directionVector);
                            bending.Horizontal.DetachAllConnectors();

                            var horizontalOffset = GetOffsetVector(bending.Horizontal, offset, horizontalBendingDirection);

                            var baseSectionStartPoint = new XYZ(
                                bending.Horizontal.Start.Origin.X,
                                bending.Horizontal.Start.Origin.Y,
                                bending.Horizontal.Start.Origin.Z);
                            var baseSectionEndPoint = new XYZ(
                                bending.Horizontal.End.Origin.X,
                                bending.Horizontal.End.Origin.Y,
                                bending.Horizontal.End.Origin.Z);

                            bending.StartHorizontal = BendingSection.Copy(lastCrossingElement, directionVector);
                            bending.StartHorizontal.End.Origin = new XYZ(directionVector.Start.X, directionVector.Start.Y, baseSectionStartPoint.Z);

                            if (lastElbowConnector != null)
                                bending.StartHorizontal.Start.ConnectTo(lastElbowConnector);

                            bending.StartInclined = BendingSection.Copy(lastCrossingElement, directionVector);
                            bending.StartInclined.Start.Origin = bending.StartHorizontal.End.Origin;

                            if (orientation == Orientation.Horizontal)
                            {
                                bending.StartInclined.End.Origin = new XYZ(
                                    directionVector.Start.X,
                                    directionVector.Start.Y,
                                    baseSectionStartPoint.Z + ToInches(verticalOffset));
                            }
                            else
                            {
                                bending.StartInclined.End.Origin = bending.StartInclined.Start.Origin + horizontalOffset;
                            }

                            if (elementType == BendingElementType.CableTray)
                                bending.AlignToStartHorizontalSegment();

                            bending.EndHorizontal = BendingSection.Copy(lastCrossingElement, directionVector);
                            bending.EndHorizontal.Start.Origin = new XYZ(
                                directionVector.End.X,
                                directionVector.End.Y,
                                baseSectionStartPoint.Z);

                            bending.EndInclined = BendingSection
                                .Copy(lastCrossingElement, directionVector);
                            bending.EndInclined.End.Origin = bending.EndHorizontal.Start.Origin;

                            if (orientation == Orientation.Horizontal)
                            {
                                bending.EndInclined.Start.Origin = new XYZ(
                                    directionVector.End.X,
                                    directionVector.End.Y,
                                    baseSectionEndPoint.Z + ToInches(verticalOffset));
                            }
                            else
                            {
                                bending.EndInclined.Start.Origin = bending.EndInclined.End.Origin + horizontalOffset;
                            }

                            if (orientation == Orientation.Horizontal)
                            {
                                bending.Horizontal.Start.Origin = new XYZ(
                                    directionVector.Start.X,
                                    directionVector.Start.Y,
                                    baseSectionStartPoint.Z + ToInches(verticalOffset));
                                bending.Horizontal.End.Origin = new XYZ(
                                    directionVector.End.X,
                                    directionVector.End.Y,
                                    baseSectionEndPoint.Z + ToInches(verticalOffset));
                            }
                            else
                            {
                                bending.Horizontal.Start.Origin = new XYZ(
                                                                      directionVector.Start.X,
                                                                      directionVector.Start.Y,
                                                                      baseSectionStartPoint.Z) + horizontalOffset;
                                bending.Horizontal.End.Origin = new XYZ(
                                                                    directionVector.End.X,
                                                                    directionVector.End.Y,
                                                                    baseSectionEndPoint.Z) + horizontalOffset;
                            }

                            bending.ApplyAngleToBendingSections();
                            bending.ApplyMinimalBendingRequirements(orientation);
                            bending.CreateElbows(_doc, out var elbowConnectors);

                            lastCrossingElement = new CustomElement(bending.EndHorizontal.Element);
                            lastElbowConnector = GetNearestConnector(bending.EndHorizontal.End, elbowConnectors);
                        }
                    }

                    t.Commit();
                }
                catch (Exception e)
                {
                    ExceptionBox.Show(e);
                }
            }

            if (errorElementIds.Any())
            {
                MessageBox.Show(
                    string.Format(
                        ModPlusAPI.Language.GetItem(_langItem, "m22"),
                        string.Join(", ", errorElementIds)));
            }
        }

        /// <summary>
        /// Проверяет соответствие элемента категории
        /// </summary>
        /// <param name="element">Элемент модели</param>
        private bool PassCategory(CustomElement element)
        {
            var allowedCategoryNames = GetElementCategories()
                .Select(c => c.RevitElementClass.Name);
            return allowedCategoryNames.Contains(Enum.GetName(typeof(BendingElementType), element.ElementType));
        }

        /// <summary>
        /// Возвращает ближайший коннектор соединительного колена
        /// </summary>
        /// <param name="end">Коннектор, относительно которого выполняется сравнение</param>
        /// <param name="elbowConnectors">Список коннекторов соединительного колена</param>
        /// <returns>Ближайший коннектор соединительного колена</returns>
        private Connector GetNearestConnector(IConnector end, List<Connector> elbowConnectors)
        {
            return Math.Abs((end.Origin - elbowConnectors.First().Origin).GetLength())
                   < Math.Abs((end.Origin - elbowConnectors.Last().Origin).GetLength())
                ? elbowConnectors.First()
                : elbowConnectors.Last();
        }

        /// <summary>
        /// Возвращает наборы пересекаемых элементов в зависимости от ориентации
        /// </summary>
        /// <param name="elementsToCross">Пересекаемые элементы</param>
        /// <returns>Наборы пересекаемых элементов в зависимости от ориентации</returns>
        private IEnumerable<KeyValuePair<Orientation, IEnumerable<CustomElement>>> GetElementSets(
            IEnumerable<CustomElement> elementsToCross)
        {
            var results = new List<KeyValuePair<Orientation, IEnumerable<CustomElement>>>();
            var elements = new List<CustomElement>();
            foreach (var element in elementsToCross)
            {
                if (!elements.Any())
                {
                    elements.Add(element);
                    continue;
                }

                if (elements.Last().Orientation == element.Orientation)
                {
                    elements.Add(element);
                    continue;
                }

                results.Add(new KeyValuePair<Orientation, IEnumerable<CustomElement>>(elements.Last().Orientation, elements));
                elements = new List<CustomElement> { element };
            }

            results.Add(new KeyValuePair<Orientation, IEnumerable<CustomElement>>(elements.Last().Orientation, elements));

            return results;
        }

        /// <summary>
        /// Возвращает вектор для перемещения изгибаемого участка
        /// при пересечении вертикально ориентированных элементов
        /// </summary>
        /// <param name="section">Изменяемая секция</param>
        /// <param name="offset">Смещение элементов</param>
        /// <param name="direction">Направление изгиба</param>
        /// <returns>Вектор перемещения</returns>
        private XYZ GetOffsetVector(BendingSection section, int offset, Direction direction)
        {
            var sectionCurve = section.GetCurve();
            var baseVector = sectionCurve.GetEndPoint(1) - sectionCurve.GetEndPoint(0);
            var offsetVector = baseVector / baseVector.GetLength() * ToInches(offset);

            var endPoint = (sectionCurve.GetEndPoint(0) + offsetVector).Rotate(
                sectionCurve.GetEndPoint(0),
                direction == Direction.Left ? -90 : 90);

            return endPoint - sectionCurve.GetEndPoint(0);
        }

        /// <summary>
        /// Возвращает список всех элементов, пересекаемых заданным элементом
        /// </summary>
        /// <param name="crossingElement">Пересекающий элемент</param>
        /// <param name="elementsToCross">Список всех пересекаемых элементов</param>
        /// <returns>Список всех элементов, пересекаемых заданным элементом</returns>
        private IEnumerable<CustomElement> GetElementsToCross(
            CustomElement crossingElement,
            IEnumerable<CustomElement> elementsToCross)
        {
            var crossedElements = new Dictionary<CustomElement, XYZ>();
            var crossingElementVector = new Vector(crossingElement.GetCurve());

            foreach (var elementToCross in elementsToCross)
            {
                if (elementToCross.Orientation == Orientation.Undefined)
                    continue;

                var elementToCrossVector = new Vector(elementToCross.GetCurve());
                if (elementToCross.Orientation == Orientation.Vertical)
                {
                    if (crossingElementVector.IsLieOnVector(elementToCrossVector.Start)
                       && crossingElementVector.IsLieOnVector(elementToCrossVector.End))
                    {
                        crossedElements.Add(
                            elementToCross,
                            new XYZ(elementToCrossVector.Start.X, elementToCrossVector.Start.Y, 0));
                        continue;
                    }
                }

                var intersectionPoint = crossingElementVector.Intersect(elementToCrossVector);
                if (intersectionPoint != null)
                {
                    crossedElements.Add(elementToCross, intersectionPoint);
                }
            }

            var orderedCrossedElements = crossedElements
                .OrderBy(x => (x.Value - crossingElementVector.Start).GetLength())
                .Select(x => x.Key);

            return orderedCrossedElements;
        }

        /// <summary>
        /// Возвращает вектор изгибаемого элемента
        /// </summary>
        /// <param name="elementToBend">Элемент для гибки</param>
        /// <param name="alignmentElements">Элементы выравнивания</param>
        /// <returns>Вектор изгибаемого элемента</returns>
        private Vector GetBendingVector(
            CustomElement elementToBend,
            IEnumerable<CustomElement> alignmentElements)
        {
            var elementVector = new Vector(elementToBend.GetCurve(), true);
            var alignmentElementVectors = alignmentElements
                .ToDictionary(
                    k => k.Element,
                    v => new Vector(v.GetCurve(), true));

            var intersectionPoints = new Dictionary<Element, XYZ>();
            foreach (var alignmentElementVector in alignmentElementVectors)
            {
                var intersectionPoint = elementVector.Intersect(alignmentElementVector.Value);
                if (intersectionPoint != null)
                    intersectionPoints.Add(alignmentElementVector.Key, intersectionPoint);
            }

            if (intersectionPoints.Count == 1)
            {
                var element = intersectionPoints.First().Key;
                var intersectionPoint = intersectionPoints.First().Value;
                var halfWidth = element.GetElementWidth() / 2;
                var firstAdditionalVector = elementVector.Start - intersectionPoint;
                var secondAdditionalVector = elementVector.End - intersectionPoint;
                var startPointVector = firstAdditionalVector / ToMillimeters(firstAdditionalVector.GetLength()) * halfWidth;
                var endPointVector = secondAdditionalVector / ToMillimeters(secondAdditionalVector.GetLength()) * halfWidth;

                return new Vector(intersectionPoint + startPointVector, intersectionPoint + endPointVector);
            }

            if (intersectionPoints.Count == 2)
            {
                return (intersectionPoints.First().Value - elementVector.Start).GetLength()
                       < (intersectionPoints.Last().Value - elementVector.Start).GetLength()
                    ? new Vector(intersectionPoints.First().Value, intersectionPoints.Last().Value)
                    : new Vector(intersectionPoints.Last().Value, intersectionPoints.First().Value);
            }

            var farthestPoints = GetFarthestPoints(intersectionPoints.Values.ToList());

            return (farthestPoints.Key - elementVector.Start).GetLength()
                   < (farthestPoints.Value - elementVector.Start).GetLength()
                ? new Vector(farthestPoints.Key, farthestPoints.Value)
                : new Vector(farthestPoints.Value, farthestPoints.Key);
        }

        /// <summary>
        /// Возвращает пару наиболее удаленных друг от друга точек
        /// (такой подход применен из-за небольшого кол-ва сравниваемых точек)
        /// </summary>
        /// <param name="points">Список точек (на плоскости)</param>
        /// <returns>Пара наиболее удаленных друг от друга точек</returns>
        private KeyValuePair<XYZ, XYZ> GetFarthestPoints(List<XYZ> points)
        {
            var pointsPair = default(KeyValuePair<XYZ, XYZ>);
            var lastLength = default(double);

            for (var i = 0; i < points.Count; i++)
            {
                for (var j = 0; j < points.Count; j++)
                {
                    if (i != j)
                    {
                        var length = Math.Abs((points[j] - points[i]).GetLength());
                        if (length > lastLength)
                        {
                            lastLength = length;
                            pointsPair = new KeyValuePair<XYZ, XYZ>(points[j], points[i]);
                        }
                    }
                }
            }

            return pointsPair;
        }
    }
}