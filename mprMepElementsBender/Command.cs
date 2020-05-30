namespace mprMepElementsBender
{
    using System;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Helpers;
    using ModPlusAPI.Windows;
    using ViewModels;
    using Views;

    /// <inheritdoc />
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (MainView.IsOpen)
                    return Result.Cancelled;

                RevitExternalEventHandler.Init();

                var mainView = new MainView(commandData.Application);
                var viewModel = new MainViewModel(
                    mainView, commandData.Application, new RevitOperationService(commandData.Application));
                mainView.DataContext = viewModel;
                mainView.Closed += (sender, args) => mainView = null;
                mainView.Show();

                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return Result.Failed;
            }
        }
    }
}
