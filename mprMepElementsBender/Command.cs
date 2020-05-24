﻿namespace mprMepElementsBender
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
        private MainView _mainView;

        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                RevitExternalEventHandler.Init();
                if (_mainView == null)
                {
                    _mainView = new MainView();
                    var viewModel = new MainViewModel(_mainView, new RevitOperationService(commandData.Application));
                    _mainView.DataContext = viewModel;
                    _mainView.Closed += (sender, args) => _mainView = null;
                    _mainView.Show();

                    return Result.Succeeded;
                }

                _mainView.Activate();
                _mainView.Focus();
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
