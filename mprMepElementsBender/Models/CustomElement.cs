namespace mprMepElementsBender.Models
{
    using Autodesk.Revit.DB;
    using Helpers;
    using Helpers.Enums;

    /// <summary>
    /// Элемент модели
    /// </summary>
    public class CustomElement
    {
        /// <summary>
        /// Создает экземпляр класса <see cref="CustomElement"/>
        /// </summary>
        /// <param name="element">Элемент Revit</param>
        public CustomElement(Element element)
        {
            Element = element;
            ElementType = element.GetElementType();
            Orientation = element.GetOrientation();
        }

        /// <summary>
        /// Элемент Revit
        /// </summary>
        public Element Element { get; }

        /// <summary>
        /// Тип изгибаемого элемента
        /// </summary>
        public BendingElementType ElementType { get; }

        /// <summary>
        /// Ориентация элемента в модели
        /// </summary>
        public Orientation Orientation { get; }

        /// <summary>
        /// Возвращает кривую положения изгибаемого элемента
        /// </summary>
        /// <returns>Кривая</returns>
        public Curve GetCurve()
        {
            return Element.GetCurve();
        }
    }
}