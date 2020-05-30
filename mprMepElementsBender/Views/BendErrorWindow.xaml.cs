namespace mprMepElementsBender.Views
{
    using System.Windows;
    using System.Windows.Input;
    using Helpers.Enums;

    /// <summary>
    /// Логика взаимодействия для BendErrorWindow.xaml
    /// </summary>
    public partial class BendErrorWindow
    {
        public BendErrorWindow(string errorMessage)
        {
            InitializeComponent();
            TbErrorMessage.Text = errorMessage;
        }

        /// <summary>
        /// Вариант работы при возникновении ошибки
        /// </summary>
        public OnExceptionVariant OnExceptionVariant { get; private set; }

        private void BtAcceptAndContinue_OnClick(object sender, RoutedEventArgs e)
        {
            OnExceptionVariant = OnExceptionVariant.AcceptAndContinue;
            Close();
        }

        private void BtRejectAndContinue_OnClick(object sender, RoutedEventArgs e)
        {
            OnExceptionVariant = OnExceptionVariant.RejectAndContinue;
            Close();
        }

        private void BtAbortAll_OnClick(object sender, RoutedEventArgs e)
        {
            OnExceptionVariant = OnExceptionVariant.AbortAll;
            Close();
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
