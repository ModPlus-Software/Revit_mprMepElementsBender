namespace mprMepElementsBender.Views
{
    /// <summary>
    /// Окно выбора категорий
    /// </summary>
    public partial class CategoriesView
    {
        /// <summary>
        /// Создает экземпляр класса <see cref="CategoriesView"/>
        /// </summary>
        public CategoriesView()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(ModPlusConnector.Instance.Name, "m3");
        }
    }
}
