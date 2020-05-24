namespace mprMepElementsBender.Models
{
    using System;

    /// <summary>
    /// Категория элементов
    /// </summary>
    public class ElementCategory
    {
        private readonly string _langItem = ModPlusConnector.Instance.Name;

        /// <summary>
        /// Создает экземпляр класса <see cref="ElementCategory"/>
        /// </summary>
        /// <param name="type">Класс элементов Revit</param>
        public ElementCategory(Type type)
        {
            Name = SetName(type);
            RevitElementClass = type;
        }

        /// <summary>
        /// Имя категории
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Тип элемента Revit
        /// </summary>
        public Type RevitElementClass { get; }

        /// <summary>
        /// Указывает, выбрана ли категория
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// Возвращает имя категории
        /// </summary>
        /// <param name="type">Тип элемента</param>
        /// <returns>Имя категории</returns>
        private string SetName(Type type)
        {
            switch (type.Name)
            {
                case "Duct":
                    return ModPlusAPI.Language.GetItem(_langItem, "m15");
                case "Pipe":
                    return ModPlusAPI.Language.GetItem(_langItem, "m16");
                case "Conduit":
                    return ModPlusAPI.Language.GetItem(_langItem, "m17");
                case "CableTray":
                    return ModPlusAPI.Language.GetItem(_langItem, "m18");
                default:
                    return string.Empty;
            }
        }
    }
}