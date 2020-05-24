namespace mprMepElementsBender.Views
{
    /// <summary>
    /// Главное окно плагина
    /// </summary>
    public partial class MainView
    {
        /// <summary>
        /// Создает экземпляр класса <see cref="MainView"/>
        /// </summary>
        public MainView()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetFunctionLocalName(ModPlusConnector.Instance.Name, ModPlusConnector.Instance.LName);
        }
    }
}
