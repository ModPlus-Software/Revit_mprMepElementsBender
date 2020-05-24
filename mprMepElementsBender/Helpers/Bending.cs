namespace mprMepElementsBender.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Enums;

    /// <summary>
    /// Изгиб
    /// </summary>
    public class Bending
    {
        private const double MovingTolerance = 0.06;

        /// <summary>
        /// Начальная горизонтальная секция
        /// </summary>
        public BendingSection StartHorizontal { get; set; }

        /// <summary>
        /// Начальная наклонная секция
        /// </summary>
        public BendingSection StartInclined { get; set; }

        /// <summary>
        /// Горизонтальная секция
        /// </summary>
        public BendingSection Horizontal { get; set; }

        /// <summary>
        /// Конечная наклонная секция
        /// </summary>
        public BendingSection EndInclined { get; set; }

        /// <summary>
        /// Конечная горизонтальная секция
        /// </summary>
        public BendingSection EndHorizontal { get; set; }

        /// <summary>
        /// Смещение, мм
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Угол, °
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// Выравнивает заданный кабельный лоток относительно предыдущего
        /// </summary>
        public void AlignToStartHorizontalSegment()
        {
            var cs1 = StartHorizontal.End.CoordinateSystem;
            var cs2 = StartInclined.Start.CoordinateSystem;
            var angle = cs2.BasisY.AngleOnPlaneTo(cs1.BasisZ, XYZ.BasisZ);
            var axis = Line.CreateUnbound(StartInclined.Start.Origin, XYZ.BasisZ);
            StartInclined.Element.Location.Rotate(axis, angle);
        }

        /// <summary>
        /// Применяет заданный угол к участку изгиба
        /// </summary>
        public void ApplyAngleToBendingSections()
        {
            var angleToFind = 90 - Angle;
            var sideLengthToFind = Math.Abs(RevitOperationService.ToInches(Offset) * Math.Tan(angleToFind.ToRadians()));

            if (!double.IsInfinity(sideLengthToFind))
            {
                var startMovingVector = StartHorizontal.Start.Origin - StartHorizontal.End.Origin;
                StartHorizontal.End.Origin += startMovingVector / startMovingVector.GetLength() * sideLengthToFind;
                StartInclined.Start.Origin = StartHorizontal.End.Origin;

                var endMovingVector = EndHorizontal.End.Origin - EndHorizontal.Start.Origin;
                EndHorizontal.Start.Origin += endMovingVector / endMovingVector.GetLength() * sideLengthToFind;
                EndInclined.End.Origin = EndHorizontal.Start.Origin;
            }
        }

        /// <summary>
        /// Проверяет положение соединяемых элементов
        /// на соответствие минимальным требованиям для построения соединения
        /// и изменяет изгиб при необходимости
        /// </summary>
        /// <param name="orientation">Ориентация изгибаемого элемента</param>
        public void ApplyMinimalBendingRequirements(Orientation orientation)
        {
            var minimalRadius = StartHorizontal.GetMinimalElbowRadius(orientation);
            if (Math.Abs(minimalRadius) < double.Epsilon
                && StartHorizontal.ElementType != BendingElementType.Pipe)
                return;

            var foundAngle = Angle / 2;
            var minimalSideLength = Math.Abs(2 * minimalRadius * Math.Tan(foundAngle.ToRadians()));
            double additionalHorizontalLength = 0;

            if (StartHorizontal.ElementType == BendingElementType.Duct)
            {
                var elementWidth = StartHorizontal.Element.GetParameterValue(BuiltInParameter.RBS_CURVE_WIDTH_PARAM, true);
                double.TryParse(elementWidth, NumberStyles.Any, CultureInfo.InvariantCulture, out var width);
                additionalHorizontalLength = 2 * width;
            }

            if (StartHorizontal.ElementType == BendingElementType.Conduit)
            {
                var elementDiameter = StartHorizontal.Element.GetParameterValue(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM, true);
                double.TryParse(elementDiameter, NumberStyles.Any, CultureInfo.InvariantCulture, out var diameter);
                minimalSideLength += 2 * diameter;
                additionalHorizontalLength = 2 * diameter;
            }

            if (StartHorizontal.ElementType == BendingElementType.CableTray)
            {
                var elementWidth = StartHorizontal.Element.GetParameterValue(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM, true);
                var elementHeight = StartHorizontal.Element.GetParameterValue(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM, true);
                double.TryParse(elementWidth, NumberStyles.Any, CultureInfo.InvariantCulture, out var width);
                double.TryParse(elementHeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var height);

                if (orientation == Orientation.Vertical)
                {
                    minimalSideLength += 2 * width;
                    additionalHorizontalLength = 2 * width;
                }

                if (orientation == Orientation.Horizontal)
                {
                    minimalSideLength += 2 * height;
                    additionalHorizontalLength = 2 * height;
                }
            }

            if (StartHorizontal.ElementType == BendingElementType.Pipe)
            {
                var elementDiameter = StartHorizontal.Element.GetParameterValue(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, true);
                double.TryParse(elementDiameter, NumberStyles.Any, CultureInfo.InvariantCulture, out var diameter);
                minimalSideLength = Math.Abs(diameter * Math.Tan(foundAngle.ToRadians()));
                additionalHorizontalLength = 2 * diameter;
            }

            if (StartInclined.GetLength() < minimalSideLength)
            {
                var movingDistance = minimalSideLength - StartInclined.GetLength() + MovingTolerance;

                var startMovingVector = StartInclined.End.Origin - StartInclined.Start.Origin;
                startMovingVector = startMovingVector / startMovingVector.GetLength() * movingDistance;

                var endMovingVector = EndInclined.Start.Origin - EndInclined.End.Origin;
                endMovingVector = endMovingVector / endMovingVector.GetLength() * movingDistance;

                var horizontalVector = Horizontal.End.Origin - Horizontal.Start.Origin;
                StartInclined.End.Origin += startMovingVector;
                EndInclined.Start.Origin += endMovingVector;
                Horizontal.Start.Origin = StartInclined.End.Origin;
                Horizontal.End.Origin = EndInclined.Start.Origin;
                var horizontalVectorTransformed = Horizontal.End.Origin - Horizontal.Start.Origin;
                var vectorSumLength = (horizontalVector + horizontalVectorTransformed).GetLength();

                var basePointDistance = Math.Abs(startMovingVector.GetLength() * Math.Cos(Angle.ToRadians())) + additionalHorizontalLength;

                var startBasePointVector = Horizontal.Start.Origin - Horizontal.End.Origin;
                startBasePointVector = startBasePointVector / startBasePointVector.GetLength() * basePointDistance;
                var endBasePointVector = Horizontal.End.Origin - Horizontal.Start.Origin;
                endBasePointVector = endBasePointVector / endBasePointVector.GetLength() * basePointDistance;

                var horizontalStart = StartInclined.End.Origin + startBasePointVector;
                var horizontalEnd = EndInclined.Start.Origin + endBasePointVector;

                if (Math.Round(vectorSumLength, 5) < Math.Round(horizontalVector.GetLength() + horizontalVectorTransformed.GetLength(), 5))
                {
                    MoveHorizontalSection(minimalSideLength, Horizontal.GetLength());
                    ApplyGaps(horizontalStart, horizontalEnd);

                    return;
                }

                if (Horizontal.GetLength() < minimalSideLength)
                {
                    MoveHorizontalSection(minimalSideLength);
                    ApplyGaps(horizontalStart, horizontalEnd);

                    return;
                }

                ApplyGaps(horizontalStart, horizontalEnd);

                return;
            }

            if (Horizontal.GetLength() < minimalSideLength)
                MoveHorizontalSection(minimalSideLength);

            var startVector = Horizontal.Start.Origin - Horizontal.End.Origin;
            var endVector = Horizontal.End.Origin - Horizontal.Start.Origin;
            startVector = startVector / startVector.GetLength() * additionalHorizontalLength / 2;
            endVector = endVector / endVector.GetLength() * additionalHorizontalLength / 2;
            var startPoint = Horizontal.Start.Origin + startVector;
            var endPoint = Horizontal.End.Origin + endVector;

            ApplyGaps(startPoint, endPoint);
        }

        /// <summary>
        /// Применяет минимальные расстояния для горизонтального сегмента
        /// </summary>
        /// <param name="horizontalStart">Начальная точка горизонтального сегмента</param>
        /// <param name="horizontalEnd">Конечная точка горизонтального сегмента</param>
        private void ApplyGaps(XYZ horizontalStart, XYZ horizontalEnd)
        {
            var startMovingVector = new XYZ(horizontalStart.X, horizontalStart.Y, Horizontal.Start.Origin.Z) - Horizontal.Start.Origin;
            var endMovingVector = horizontalEnd - Horizontal.End.Origin;

            Horizontal.Start.Origin = horizontalStart;
            Horizontal.End.Origin = horizontalEnd;

            StartInclined.End.Origin += startMovingVector;
            StartInclined.Start.Origin += startMovingVector;
            StartHorizontal.End.Origin += startMovingVector;

            EndInclined.Start.Origin += endMovingVector;
            EndInclined.End.Origin += endMovingVector;
            EndHorizontal.Start.Origin += endMovingVector;
        }

        /// <summary>
        /// Применяет минимальные допустимые размеры горизонтальной секции соединения
        /// </summary>
        /// <param name="minimalSideLength">Минимальная длина секции</param>
        /// <param name="additionalLength">Дополнительная величина смещения для крайних точек горизонтального сегмента</param>
        private void MoveHorizontalSection(
            double minimalSideLength,
            double additionalLength = 0)
        {
            var movingDistance = ((minimalSideLength - Horizontal.GetLength()) / 2) + MovingTolerance + additionalLength;
            var centerPoint = ((Horizontal.End.Origin - Horizontal.Start.Origin) / 2) + Horizontal.Start.Origin;

            XYZ startMovingVector;
            XYZ endMovingVector;
            if (additionalLength > 0)
            {
                startMovingVector = centerPoint - Horizontal.Start.Origin;
                endMovingVector = centerPoint - Horizontal.End.Origin;
            }
            else
            {
                startMovingVector = Horizontal.Start.Origin - centerPoint;
                endMovingVector = Horizontal.End.Origin - centerPoint;
            }

            startMovingVector = startMovingVector / startMovingVector.GetLength() * movingDistance;
            endMovingVector = endMovingVector / endMovingVector.GetLength() * movingDistance;

            Horizontal.Start.Origin += startMovingVector;
            StartInclined.End.Origin += startMovingVector;
            StartInclined.Start.Origin += startMovingVector;
            StartHorizontal.End.Origin += startMovingVector;

            Horizontal.End.Origin += endMovingVector;
            EndInclined.Start.Origin += endMovingVector;
            EndInclined.End.Origin += endMovingVector;
            EndHorizontal.Start.Origin += endMovingVector;
        }

        /// <summary>
        /// Устанавливает соединительные элементы в местах стыковки
        /// </summary>
        /// <param name="document">Документ Revit</param>
        /// <param name="elbowConnectors">Возвращает список коннекторов соединения</param>
        public void CreateElbows(Document document, out List<Connector> elbowConnectors)
        {
            CreateElbow(document, StartHorizontal.End, StartInclined.Start);
            CreateElbow(document, StartInclined.End, Horizontal.Start);
            CreateElbow(document, Horizontal.End, EndInclined.Start);
            CreateElbow(document, EndInclined.End, EndHorizontal.Start, out elbowConnectors);
        }

        /// <summary>
        /// Создает колено в месте соединения элементов
        /// </summary>
        /// <param name="document">Документ Revit</param>
        /// <param name="one">Первый коннектор</param>
        /// <param name="other">Второй коннектор</param>
        /// <param name="elbowConnectors">Возвращает список коннекторов соединения</param>
        private void CreateElbow(Document document, Connector one, Connector other, out List<Connector> elbowConnectors)
        {
            one.ConnectTo(other);
            var elbowFamily = document.Create.NewElbowFitting(one, other);
            elbowConnectors = elbowFamily.MEPModel.ConnectorManager.Connectors.Cast<Connector>().ToList();
        }

        /// <summary>
        /// Создает колено в месте соединения элементов
        /// </summary>
        /// <param name="document">Документ Revit</param>
        /// <param name="one">Первый коннектор</param>
        /// <param name="other">Второй коннектор</param>
        private void CreateElbow(Document document, Connector one, Connector other)
        {
            one.ConnectTo(other);
            document.Create.NewElbowFitting(one, other);
        }
    }
}