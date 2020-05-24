namespace mprMepElementsBender.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Helpers;
    using Models;
    using ModPlusAPI.Mvvm;
    using Views;

    /// <summary>
    /// Модель представления категорий
    /// </summary>
    public class CategoriesViewModel
    {
        private readonly CategoriesView _categoriesView;

        /// <summary>
        /// Создает экземпляр класса <see cref="CategoriesViewModel"/>
        /// </summary>
        /// <param name="categoriesView">Окно выбора категорий</param>
        /// <param name="selectedCategories">Список выбранных категорий</param>
        public CategoriesViewModel(CategoriesView categoriesView, List<ElementCategory> selectedCategories)
        {
            _categoriesView = categoriesView;
            SelectedCategories = selectedCategories;
            foreach (var category in SelectedCategories)
            {
                Categories.First(cat => cat.Name == category.Name).IsChecked = true;
            }
        }

        /// <summary>
        /// Событие изменения списка выбранных категорий
        /// </summary>
        public event EventHandler SelectedCategoriesChanged;

        /// <summary>
        /// Команда применения выбранных категорий
        /// </summary>
        public ICommand ApplyCommand => new RelayCommandWithoutParameter(Apply);

        /// <summary>
        /// Команда отмены внесенных изменений
        /// </summary>
        public ICommand CancelCommand => new RelayCommandWithoutParameter(Cancel);

        /// <summary>
        /// Категории элементов
        /// </summary>
        public List<ElementCategory> Categories { get; } = RevitOperationService.GetElementCategories();

        /// <summary>
        /// Выбранные категории элементов
        /// </summary>
        public List<ElementCategory> SelectedCategories { get; set; }

        /// <summary>
        /// Команда применения изменений
        /// </summary>
        private void Apply()
        {
            SelectedCategories = Categories.Where(cat => cat.IsChecked).ToList();
            OnSelectedCategoriesChanged();
            _categoriesView.Close();
        }

        /// <summary>
        /// Команда отмены изменений
        /// </summary>
        private void Cancel()
        {
            _categoriesView.Close();
        }

        /// <summary>
        /// Метод вызова события изменения выбранных категорий элементов
        /// </summary>
        protected virtual void OnSelectedCategoriesChanged()
        {
            SelectedCategoriesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
