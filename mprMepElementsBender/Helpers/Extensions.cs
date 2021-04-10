namespace mprMepElementsBender.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Electrical;
    using Autodesk.Revit.DB.Mechanical;
    using Autodesk.Revit.DB.Plumbing;
    using Enums;

    /// <summary>
    /// Класс расширений
    /// </summary>
    public static class Extensions
    {
        private const double Tolerance = 0.003;
        private const double OrientationTolerance = 0.01;

        /// <summary>
        /// Преобразует угол в градусах в радианы
        /// </summary>
        /// <param name="degreeAngle">Значение угла в градусах</param>
        /// <returns>Угол в радианах</returns>
        public static double ToRadians(this double degreeAngle)
        {
            return Math.PI / 180 * degreeAngle;
        }

        /// <summary>
        /// Проверяет объекты на равенство
        /// </summary>
        /// <param name="one">Первый элемент сравнения</param>
        /// <param name="other">Второй элемент сравнения</param>
        public static bool Equals(this XYZ one, XYZ other)
        {
            return Math.Abs(one.X - other.X) < Tolerance
                   && Math.Abs(one.Y - other.Y) < Tolerance
                   && Math.Abs(one.Z - other.Z) < Tolerance;
        }

        /// <summary>
        /// Поворот точки вокруг начала координат
        /// </summary>
        /// <param name="point">Точка для поворота</param>
        /// <param name="zero">Точка начала координат</param>
        /// <param name="angle">Угол, °</param>
        /// <returns>Повернутая точка</returns>
        public static XYZ Rotate(this XYZ point, XYZ zero, double angle)
        {
            var x = zero.X +
                    ((point.X - zero.X) * Math.Cos(angle.ToRadians()))
                    - ((point.Y - zero.Y) * Math.Sin(angle.ToRadians()));

            var y = zero.Y +
                    ((point.Y - zero.Y) * Math.Cos(angle.ToRadians()))
                    + ((point.X - zero.X) * Math.Sin(angle.ToRadians()));

            return new XYZ(x, y, point.Z);
        }

        /// <summary>
        /// Возвращает ширину заданного элемента
        /// (для построения вектора изгиба
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <returns>Ширина заданного элемента</returns>
        public static double GetElementWidth(this Element element)
        {
            var widthParameterNames = new List<BuiltInParameter>
            {
                BuiltInParameter.RBS_PIPE_DIAMETER_PARAM,
                BuiltInParameter.RBS_CURVE_WIDTH_PARAM,
                BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM,
                BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM
            };

            foreach (var parameterName in widthParameterNames)
            {
                var elementWidth = element.GetParameterValue(parameterName);
                if (string.IsNullOrEmpty(elementWidth))
                    continue;

                double.TryParse(elementWidth, NumberStyles.Any, CultureInfo.InvariantCulture, out var width);

                return width;
            }

            return default;
        }

        /// <summary>
        /// Определяет ориентацию элемента в модели
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <returns>Ориентация элемента в модели</returns>
        public static Orientation GetOrientation(this Element element)
        {
            var curve = element.GetCurve();
            var vector = curve.GetEndPoint(1) - curve.GetEndPoint(0);
            var basis = XYZ.BasisZ;
            var dotProduct = vector.DotProduct(basis);
            if (Math.Abs(vector.GetLength() * basis.GetLength()) - Math.Abs(dotProduct) < OrientationTolerance)
                return Orientation.Vertical;

            return Math.Abs(curve.GetEndPoint(0).Z - curve.GetEndPoint(1).Z) < OrientationTolerance
                ? Orientation.Horizontal
                : Orientation.Undefined;
        }

        /// <summary>
        /// Возвращает тип изгибаемого элемента
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <returns>Тип изгибаемого элемента</returns>
        public static BendingElementType GetElementType(this Element element)
        {
            switch (element.GetType().Name)
            {
                case nameof(Pipe):
                    return BendingElementType.Pipe;
                case nameof(Duct):
                    return BendingElementType.Duct;
                case nameof(CableTray):
                    return BendingElementType.CableTray;
                case nameof(Conduit):
                    return BendingElementType.Conduit;
                default:
                    return BendingElementType.Undefined;
            }
        }

        /// <summary>
        /// Возвращает кривую положения элемента
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <returns>Кривая</returns>
        public static Curve GetCurve(this Element element)
        {
            return ((LocationCurve)element.Location).Curve;
        }

        /// <summary>
        /// Возвращает значение заданного параметра элемента
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <param name="parameter">Получаемый параметр</param>
        /// <param name="asDouble">Получить значение AsDouble</param>
        /// <returns>Значение заданного параметра</returns>
        public static string GetParameterValue(this Element element, object parameter, bool asDouble = false)
        {
            if (parameter == null)
                return string.Empty;

            Parameter foundParameter = null;
            switch (parameter)
            {
                case BuiltInParameter builtInParameter:
                    foundParameter = element.get_Parameter(builtInParameter);
                    break;
                case string parameterName:
                    foundParameter = element.LookupParameter(parameterName);
                    break;
            }

            if (foundParameter == null)
                return string.Empty;

            switch (foundParameter.StorageType)
            {
                case StorageType.Double:
                    return asDouble
                        ? foundParameter.AsDouble().ToString(CultureInfo.InvariantCulture)
#if R2017 || R2018 || R2019 || R2020
                        : foundParameter.AsValueString(new FormatOptions(DisplayUnitType.DUT_MILLIMETERS));
#else
                        : foundParameter.AsValueString(new FormatOptions(UnitTypeId.Millimeters));
#endif

                case StorageType.Integer:
                    return foundParameter.AsInteger().ToString(CultureInfo.InvariantCulture);
                case StorageType.String:
                    return foundParameter.AsString();
                case StorageType.ElementId:
                    return foundParameter.AsValueString();
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Возвращает коннекторы заданного элемента
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        /// <returns>Пара коннекторов элемента</returns>
        public static IEnumerable<Connector> GetConnectors(this Element element)
        {
            var mepCurve = (MEPCurve)element;
            foreach (Connector connector in mepCurve.ConnectorManager.Connectors)
                yield return connector;
        }
    }
}