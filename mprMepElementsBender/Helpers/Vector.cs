namespace mprMepElementsBender.Helpers
{
    using System;
    using Autodesk.Revit.DB;

    /// <summary>
    /// Вектор
    /// </summary>
    public class Vector
    {
        private const double CrossingTolerance = 0.03;

        /// <summary>
        /// Создает экземпляр класса <see cref="Vector"/>
        /// </summary>
        /// <param name="start">Начальная точка</param>
        /// <param name="end">Конечная точка</param>
        /// <param name="ignoreHeight">Указывает, игнорировать ли координату Z при создании вектора</param>
        public Vector(XYZ start, XYZ end, bool ignoreHeight = false)
        {
            if (ignoreHeight)
            {
                Start = new XYZ(start.X, start.Y, 0);
                End = new XYZ(end.X, end.Y, 0);
            }
            else
            {
                Start = start;
                End = end;
            }

            Instance = End - Start;
            Length = Instance.GetLength();
        }

        /// <summary>
        /// Создает экземпляр класса <see cref="Vector"/>
        /// </summary>
        /// <param name="curve">Кривая</param>
        /// <param name="ignoreHeight">Указывает, игнорировать ли координату Z при создании вектора</param>
        public Vector(Curve curve, bool ignoreHeight = false)
        {
            var start = curve.GetEndPoint(0);
            var end = curve.GetEndPoint(1);

            if (ignoreHeight)
            {
                Start = new XYZ(start.X, start.Y, 0);
                End = new XYZ(end.X, end.Y, 0);
            }
            else
            {
                Start = start;
                End = end;
            }

            Instance = End - Start;
            Length = Instance.GetLength();
        }

        /// <summary>
        /// Начальная точка вектора
        /// </summary>
        public XYZ Start { get; }

        /// <summary>
        /// Конечная точка вектора
        /// </summary>
        public XYZ End { get; }

        /// <summary>
        /// Экземпляр вектора
        /// </summary>
        public XYZ Instance { get; }

        /// <summary>
        /// Длина вектора
        /// </summary>
        public double Length { get; }

        /// <summary>
        /// Возвращает точку пересечения с заданным вектором
        /// </summary>
        /// <param name="vector">Вектор</param>
        /// <returns></returns>
        public XYZ Intersect(Vector vector)
        {
            if (IsLieOnVector(vector.Start)
                && IsLieOnVector(vector.End))
                return new XYZ(vector.Start.X, vector.Start.Y, 0);

            var a1 = End.Y - Start.Y;
            var b1 = Start.X - End.X;
            var c1 = (a1 * Start.X) + (b1 * Start.Y);

            var a2 = vector.End.Y - vector.Start.Y;
            var b2 = vector.Start.X - vector.End.X;
            var c2 = (a2 * vector.Start.X) + (b2 * vector.Start.Y);

            var delta = (a1 * b2) - (a2 * b1);

            if (Math.Abs(delta) < 0.003)
                return null;

            var x = ((b2 * c1) - (b1 * c2)) / delta;
            var y = ((a1 * c2) - (a2 * c1)) / delta;

            var intersectionPoint = new XYZ(x, y, 0);
            if (IsLieOnVector(intersectionPoint)
                && vector.IsLieOnVector(intersectionPoint))
            {
                return intersectionPoint;
            }

            return null;
        }

        /// <summary>
        /// Проверяет, лежит ли точка на векторе
        /// </summary>
        /// <param name="point">Проверяемая точка</param>
        /// <param name="checkHeight">Проверять высоту</param>
        public bool IsLieOnVector(XYZ point, bool checkHeight = false)
        {
            var lowerX = Math.Round(Math.Min(Start.X, End.X), 5);
            var upperX = Math.Round(Math.Max(Start.X, End.X), 5);
            var lowerY = Math.Round(Math.Min(Start.Y, End.Y), 5);
            var upperY = Math.Round(Math.Max(Start.Y, End.Y), 5);
            var lowerZ = Math.Round(Math.Min(Start.Z, End.Z), 5);
            var upperZ = Math.Round(Math.Max(Start.Z, End.Z), 5);
            var x = Math.Round(point.X, 5);
            var y = Math.Round(point.Y, 5);
            var z = Math.Round(point.Z, 5);

            if (checkHeight)
            {
                return (x >= lowerX || Math.Abs(x - lowerX) < CrossingTolerance)
                       && (x <= upperX || Math.Abs(x - upperX) < CrossingTolerance)
                       && (y >= lowerY || Math.Abs(y - lowerY) < CrossingTolerance)
                       && (y <= upperY || Math.Abs(y - upperY) < CrossingTolerance)
                       && (z >= lowerZ || Math.Abs(z - lowerZ) < CrossingTolerance)
                       && (z <= upperZ || Math.Abs(z - upperZ) < CrossingTolerance);
            }

            return (x >= lowerX || Math.Abs(x - lowerX) < CrossingTolerance)
                   && (x <= upperX || Math.Abs(x - upperX) < CrossingTolerance)
                   && (y >= lowerY || Math.Abs(y - lowerY) < CrossingTolerance)
                   && (y <= upperY || Math.Abs(y - upperY) < CrossingTolerance);
        }
    }
}