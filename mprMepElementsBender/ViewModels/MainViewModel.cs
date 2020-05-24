namespace mprMepElementsBender.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Helpers;
    using Helpers.Enums;
    using Models;
    using ModPlusAPI.Mvvm;
    using Views;

    /// <summary>
    /// Модель представления главного окна приложения
    /// </summary>
    public class MainViewModel : VmBase
    {
        private readonly string _langItem = ModPlusConnector.Instance.Name;
        private readonly MainView _mainView;
        private readonly RevitOperationService _revitOs;
        private CategoriesViewModel _categoriesViewModel;
        private int _offset;
        private int _angle = 5;

        /// <summary>
        /// Создает экземпляр класса <see cref="MainViewModel"/>
        /// </summary>
        /// <param name="revitOperationService">Сервис работы с документом Revit</param>
        /// <param name="mainView">Главное окно приложения</param>
        public MainViewModel(MainView mainView, RevitOperationService revitOperationService)
        {
            _mainView = mainView;
            _revitOs = revitOperationService;
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
        public ICommand ClearSelectedElementsCommand => new RelayCommandWithoutParameter(ClearSelectedElements);

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
            get => _offset;
            set
            {
                _offset = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Угол изгиба элемента
        /// </summary>
        public int Angle
        {
            get => _angle;
            set
            {
                _angle = value;
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
        public List<CustomElement> AlignmentElements { get; set; } = new List<CustomElement>();

        /// <summary>
        /// Вертикальное направление изгиба
        /// </summary>
        public Direction VerticalBendingDirection { get; set; } = Direction.Up;

        /// <summary>
        /// Горизонтальное направление изгиба
        /// </summary>
        public Direction HorizontalBendingDirection { get; set; } = Direction.Left;

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
            AlignmentElements = _revitOs.SelectElements(SelectedCategories);
            _mainView.SelectAlignElementsBtn.Content = AlignmentElements.Any()
                ? ModPlusAPI.Language.GetItem(_langItem, "m11")
                : ModPlusAPI.Language.GetItem(_langItem, "m10");
        }

        /// <summary>
        /// Удаляет все выбранные элементы выравнивания
        /// </summary>
        private void ClearSelectedElements()
        {
            AlignmentElements.Clear();
            _mainView.SelectAlignElementsBtn.Content = ModPlusAPI.Language.GetItem(_langItem, "m10");
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
                        _revitOs.GetSelectedElements(),
                        AlignmentElements,
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