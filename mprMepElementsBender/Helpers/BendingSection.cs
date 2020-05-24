namespace mprMepElementsBender.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Electrical;
    using Enums;
    using Models;

    /// <summary>
    /// Участок изгиба элемента
    /// </summary>
    public class BendingSection
    {
        /// <summary>
        /// Создает экземпляр класса <see cref="BendingSection"/>
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <param name="directionVector">Вектор направления</param>
        public BendingSection(Element element, Vector directionVector)
        {
            Element = element;
            ElementType = element.GetElementType();
            var connectors = Element.GetConnectors().ToList();
            if (Math.Abs((directionVector.Start - connectors.First().Origin).GetLength())
                < Math.Abs((directionVector.End - connectors.First().Origin).GetLength()))
            {
                Start = connectors.First();
                End = connectors.Last();
            }
            else
            {
                Start = connectors.Last();
                End = connectors.First();
            }
        }

        /// <summary>
        /// Начальная точка участка
        /// </summary>
        public Connector Start { get; set; }

        /// <summary>
        /// Конечная точка участка
        /// </summary>
        public Connector End { get; set; }

        /// <summary>
        /// Элемент Revit
        /// </summary>
        public Element Element { get; }

        /// <summary>
        /// Тип элемент Revit
        /// </summary>
        public BendingElementType ElementType { get; }

        /// <summary>
        /// Создает копию заданного изгибаемого элемента
        /// </summary>
        /// <param name="bendingElement">Изгибаемый элемент</param>
        /// <param name="directionVector">Вектор направления</param>
        /// <returns>Копия изгибаемого элемента</returns>
        public static BendingSection Copy(CustomElement bendingElement, Vector directionVector)
        {
            var doc = bendingElement.Element.Document;
            var copiedElement = doc.GetElement(ElementTransformUtils.CopyElement(
                    doc,
                    bendingElement.Element.Id,
                    new XYZ(0, 0, 0))
                .First());

            return new BendingSection(copiedElement, directionVector);
        }

        /// <summary>
        /// Возвращает длину сегмента
        /// </summary>
        /// <returns></returns>
        public double GetLength()
        {
            return (End.Origin - Start.Origin).GetLength();
        }

        /// <summary>
        /// Отсоединяет все коннекторы заданного элемента
        /// </summary>
        public void DetachAllConnectors()
        {
            var neighborConnectorsStart = Start.AllRefs;
            var neighborConnectorsEnd = End.AllRefs;
            foreach (Connector neighborConnector in neighborConnectorsStart)
            {
                if (neighborConnector.ConnectorType != ConnectorType.Logical
                    && !Extensions.Equals(neighborConnector.Origin, End.Origin))
                    Start.DisconnectFrom(neighborConnector);
            }

            foreach (Connector neighborConnector in neighborConnectorsEnd)
            {
                if (neighborConnector.ConnectorType != ConnectorType.Logical
                    && !Extensions.Equals(neighborConnector.Origin, Start.Origin))
                    End.DisconnectFrom(neighborConnector);
            }
        }

        /// <summary>
        /// Возвращает кривую положения изгибаемого элемента
        /// </summary>
        /// <returns>Кривая</returns>
        public Curve GetCurve()
        {
            return Element.GetCurve();
        }

        /// <summary>
        /// Возвращает минимальный радиус изгибаемого элемента
        /// </summary>
        /// <param name="orientation">Ориентация изгибаемого элемента</param>
        /// <returns></returns>
        public double GetMinimalElbowRadius(Orientation orientation)
        {
            switch (ElementType)
            {
                case BendingElementType.Duct:
                    return GetMinimalDuctElbowRadius(orientation);
                case BendingElementType.CableTray:
                    return GetMinimalCableTrayElbowRadius();
                case BendingElementType.Conduit:
                    return GetMinimalConduitElbowRadius();
                case BendingElementType.Undefined:
                    return 0;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Возвращает минимальный диаметр соединительного колена короба
        /// </summary>
        /// <returns>Минимальный диаметр соединительного колена короба</returns>
        private double GetMinimalConduitElbowRadius()
        {
            var sizeSettings = ConduitSizeSettings.GetConduitSizeSettings(Element.Document);
            var iterator = sizeSettings.GetConduitSizeSettingsIterator();
            var conduitSizes = new Dictionary<string, ConduitSizes>();
            while (iterator.MoveNext())
                conduitSizes.Add(iterator.Current.Key, iterator.Current.Value);

            var conduitType = (ConduitType)Element.Document.GetElement(Element.GetTypeId());
            var standard = conduitType.GetParameterValue(BuiltInParameter.CONDUIT_STANDARD_TYPE_PARAM);
            var outerDiameter = Element.GetParameterValue(BuiltInParameter.RBS_CONDUIT_OUTER_DIAM_PARAM, true);
            double.TryParse(outerDiameter, NumberStyles.Any, CultureInfo.InvariantCulture, out var diameter);
            if (conduitSizes.TryGetValue(standard, out var sizes))
            {
                var size = sizes.FirstOrDefault(conduitSize =>
                    Math.Abs(Math.Round(conduitSize.OuterDiameter, 5) - Math.Round(diameter, 5)) < double.Epsilon);
                if (size != null)
                    return size.BendRadius;
            }

            return 0;
        }

        /// <summary>
        /// Возвращает минимальный диаметр соединительного колена кабельного лотка
        /// </summary>
        /// <returns>Минимальный диаметр соединительного колена кабельного лотка</returns>
        private double GetMinimalCableTrayElbowRadius()
        {
            var width = Element.GetParameterValue(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM, true);
            double.TryParse(width, NumberStyles.Any, CultureInfo.InvariantCulture, out var radius);

            return radius;
        }

        /// <summary>
        /// Возвращает минимальный диаметр соединительного колена воздуховода
        /// </summary>
        /// <param name="orientation">Ориентация изгибаемого элемента</param>
        /// <returns>Минимальный диаметр соединительного колена воздуховода</returns>
        private double GetMinimalDuctElbowRadius(Orientation orientation)
        {
            var dimension = string.Empty;
            switch (orientation)
            {
                case Orientation.Undefined:
                    return 0;
                case Orientation.Horizontal:
                    dimension = Element.GetParameterValue(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, true);
                    if (string.IsNullOrEmpty(dimension))
                        return 0;
                    break;
                case Orientation.Vertical:
                    dimension = Element.GetParameterValue(BuiltInParameter.RBS_CURVE_WIDTH_PARAM, true);
                    if (string.IsNullOrEmpty(dimension))
                        return 0;
                    break;
            }

            double.TryParse(dimension, NumberStyles.Any, CultureInfo.InvariantCulture, out var outDimension);

            var elbow = ((MEPCurveType)Element.Document.GetElement(Element.GetTypeId())).Elbow;
            if (elbow == null)
                return outDimension;

            var radiusMultiplier = elbow.GetParameterValue("Radius Multiplier", true);
            double.TryParse(radiusMultiplier, NumberStyles.Any, CultureInfo.InvariantCulture, out var multiplier);

            return outDimension * multiplier;
        }
    }
}