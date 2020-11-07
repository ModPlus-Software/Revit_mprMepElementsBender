namespace mprMepElementsBender.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;
    using Helpers;
    using Helpers.Enums;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;
    using Views;

    /// <summary>
    /// Модель представления главного окна приложения
    /// </summary>
    public class MainViewModel : VmBase
    {
        private readonly MainView _mainView;
        private readonly RevitOperationService _revitOs;
        private CategoriesViewModel _categoriesViewModel;

        /// <summary>
        /// Создает экземпляр класса <see cref="MainViewModel"/>
        /// </summary>
        /// <param name="uiApplication"><see cref="UIApplication"/></param>
        /// <param name="revitOperationService">Сервис работы с документом Revit</param>
        /// <param name="mainView">Главное окно приложения</param>
        public MainViewModel(MainView mainView, UIApplication uiApplication, RevitOperationService revitOperationService)
        {
            _mainView = mainView;
            _revitOs = revitOperationService;
            IntersectedElements = new ObservableCollection<CustomElement>();
            IntersectedElements.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(IsEnabledBend));
            ElementsToBend = new ObservableCollection<CustomElement>();
            ElementsToBend.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(IsEnabledBend));
            uiApplication.Idling += UiApplicationOnIdling;
        }

        /// <summary>
        /// Команда вызова окна выбора категорий элементов модели
        /// </summary>
        public ICommand SelectCategoriesCommand => new RelayCommandWithoutParameter(SelectCategories);

        /// <summary>
        /// Команда выбора элементов модели
        /// </summary>
        public ICommand SelectElementsCommand => new RelayCommandWithoutParameter(SelectElements);

        /// <summary>
        /// Команда удаления всех элементов выравнивания
        /// </summary>
        public ICommand ClearSelectedElementsCommand =>
            new RelayCommandWithoutParameter(() => IntersectedElements.Clear());

        /// <summary>
        /// Команда обработки выбранного документа Revit
        /// </summary>
        public ICommand BendCommand => new RelayCommandWithoutParameter(Bend);

        /// <summary>
        /// Команда выбора направления изгиба
        /// </summary>
        public ICommand BendingDirectionCommand => new RelayCommand<string>(SetBendingDirection);

        /// <summary>
        /// Смещение элемента
        /// </summary>
        public int Offset
        {
            get => int.TryParse(UserConfigFile.GetValue(nameof(Offset)), out var offset) ? offset : 200;
            set
            {
                UserConfigFile.SetValue(nameof(Offset), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Угол изгиба элемента
        /// </summary>
        public int Angle
        {
            get => int.TryParse(UserConfigFile.GetValue(nameof(Angle)), out var offset) ? offset : 5;
            set
            {
                UserConfigFile.SetValue(nameof(Angle), value.ToString(), true);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Выбранные категории элементов
        /// </summary>
        public List<ElementCategory> SelectedCategories { get; set; } = RevitOperationService.GetElementCategories();

        /// <summary>
        /// Выбранные элементы выравнивания
        /// </summary>
        public ObservableCollection<CustomElement> IntersectedElements { get; }

        /// <summary>
        /// Элементы, которые будут загибаться
        /// </summary>
        public ObservableCollection<CustomElement> ElementsToBend { get; }

        /// <summary>
        /// Вертикальное направление изгиба
        /// </summary>
        public Direction VerticalBendingDirection { get; set; } = Direction.Up;

        /// <summary>
        /// Горизонтальное направление изгиба
        /// </summary>
        public Direction HorizontalBendingDirection { get; set; } = Direction.Left;

        /// <summary>
        /// Доступность запуска процесса создания огибаний
        /// </summary>
        public bool IsEnabledBend => IntersectedElements.Any() && ElementsToBend.Any();

        private void UiApplicationOnIdling(object sender, IdlingEventArgs e)
        {
            if (sender is UIApplication uiApplication)
            {
                ElementsToBend.Clear();

                var selectedElementIds = uiApplication.ActiveUIDocument.Selection.GetElementIds();

                foreach (var customElement in selectedElementIds
                    .Select(elementId => new CustomElement(uiApplication.ActiveUIDocument.Document.GetElement(elementId))))
                {
                    if (IntersectedElements.Any(el => el.Id == customElement.Id))
                        continue;

                    ElementsToBend.Add(customElement);
                }
            }
        }

        /// <summary>
        /// Метод выбора категорий элементов модели
        /// </summary>
        private void SelectCategories()
        {
            var categoriesView = new CategoriesView();
            _categoriesViewModel = new CategoriesViewModel(categoriesView, SelectedCategories);
            _categoriesViewModel.SelectedCategoriesChanged += OnSelectedCategoriesChanged;
            categoriesView.DataContext = _categoriesViewModel;
            categoriesView.ShowDialog();
        }

        /// <summary>
        /// Выбор элементов выравнивания при изгибе
        /// </summary>
        private void SelectElements()
        {
            try
            {
                var addedByPreSelection = false;
                var preSelectedElements = _revitOs.GetPreSelectedElements(SelectedCategories);
                foreach (var customElement in preSelectedElements)
                {
                    if (IntersectedElements.Any(e => e.Id == customElement.Id))
                        continue;
                    IntersectedElements.Add(customElement);
                    addedByPreSelection = true;
                }

                if (!addedByPreSelection)
                {
                    _mainView.Hide();
                    var newElements = _revitOs.SelectElements(SelectedCategories);
                    foreach (var customElement in newElements)
                    {
                        if (IntersectedElements.Any(e => e.Id == customElement.Id))
                            continue;
                        IntersectedElements.Add(customElement);
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // ignore
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }

            _mainView.Show();
        }

        /// <summary>
        /// Выполняет изгиб выбранных элементов
        /// </summary>
        private void Bend()
        {
            RevitExternalEventHandler.Instance.Run(
                () =>
                {
                    _revitOs.BendElements(
                        ElementsToBend,
                        IntersectedElements,
                        Offset,
                        Angle,
                        VerticalBendingDirection,
                        HorizontalBendingDirection);
                }, true);
        }

        /// <summary>
        /// Устанавливает направления изгиба элемента
        /// </summary>
        /// <param name="directionName">Имя направления</param>
        private void SetBendingDirection(string directionName)
        {
            switch (directionName)
            {
                case "Left":
                    HorizontalBendingDirection = Direction.Left;
                    break;
                case "Right":
                    HorizontalBendingDirection = Direction.Right;
                    break;
                case "Up":
                    VerticalBendingDirection = Direction.Up;
                    break;
                case "Down":
                    VerticalBendingDirection = Direction.Down;
                    break;
            }
        }

        /// <summary>
        /// Метод обработки события изменения выбранных категорий элементов Revit
        /// </summary>
        private void OnSelectedCategoriesChanged(object sender, EventArgs e)
        {
            SelectedCategories = _categoriesViewModel.SelectedCategories;
        }
    }
}