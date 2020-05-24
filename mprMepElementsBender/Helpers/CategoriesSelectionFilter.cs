namespace mprMepElementsBender.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI.Selection;
    using Models;

    /// <inheritdoc/>
    public class CategoriesSelectionFilter : ISelectionFilter
    {
        private readonly List<string> _allowedTypeNames = new List<string>();

        /// <summary>
        /// Создает экземпляр класса <see cref="CategoriesSelectionFilter"/>
        /// </summary>
        /// <param name="allowedCategories">Список разрешенных категорий</param>
        public CategoriesSelectionFilter(IEnumerable<ElementCategory> allowedCategories)
        {
            foreach (var category in allowedCategories)
                _allowedTypeNames.Add(category.RevitElementClass.Name);
        }

        /// <inheritdoc/>
        public bool AllowElement(Element elem)
        {
            var elementTypeName = elem.GetType().Name;
            return _allowedTypeNames.Any(name => name == elementTypeName);
        }

        /// <inheritdoc/>
        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}