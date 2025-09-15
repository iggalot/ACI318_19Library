using ACI318_19Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ACI318_19Library
{
    public partial class ConcreteCrossSectionInputControl : UserControl
    {
        public ConcreteCrossSectionViewModel ViewModel { get; private set; }
        public bool ValidateInputs()
        {
            return ValidationHelper.IsValid(this);
        }

        public ConcreteCrossSectionInputControl()
        {
            InitializeComponent();
            InitializeWithViewModel(new ConcreteCrossSectionViewModel());
        }

        public ConcreteCrossSectionInputControl(ConcreteCrossSectionViewModel vm)
        {
            InitializeComponent();
            InitializeWithViewModel(vm);
        }

        private void InitializeWithViewModel(ConcreteCrossSectionViewModel vm)
        {
            ViewModel = vm;
            DataContext = ViewModel;

            this.Loaded += (s, e) => Update();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            AttachNumericUpDownEvents();

            // Hook DataGrids
            HookGrid(TensionDataGrid);
            HookGrid(CompressionDataGrid);
            HookGrid(StirrupDataGrid);

            // Hook rebar collections
            HookRebarCollection(ViewModel.TensionRebars);
            HookRebarCollection(ViewModel.CompressionRebars);
            HookRebarCollection(ViewModel.StirrupRebars);
        }

        private void HookGrid(DataGrid grid)
        {
            grid.CellEditEnding += DataGrid_CellEditEnding;
            grid.CurrentCellChanged += DataGrid_CurrentCellChanged;
        }

        private void HookRebarCollection<T>(ObservableCollection<T> collection) where T : INotifyPropertyChanged
        {
            foreach (var item in collection)
                item.PropertyChanged += Layer_PropertyChanged;

            collection.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (INotifyPropertyChanged item in e.NewItems)
                        item.PropertyChanged += Layer_PropertyChanged;
            };
        }

        private void AttachNumericUpDownEvents()
        {
            foreach (var child in FindVisualChildren<NumericUpDown>(this))
            {
                child.ValueChanged += NumericUpDown_ValueChanged;
            }
        }

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Update();
        }

        private void DataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null) return;

            // Commit any pending edit
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);

            // Now update your diagram
            Update();
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Force the binding to update immediately
            if (e.EditingElement is TextBox tb)
            {
                var binding = tb.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();
            }

            // Update the control after the edit
            Dispatcher.BeginInvoke(new Action(() => Update()));
        }


        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Only react to relevant properties
            if (e.PropertyName == nameof(ViewModel.Width) ||
                e.PropertyName == nameof(ViewModel.Height) ||
                e.PropertyName == nameof(ViewModel.TensionCover) ||
                e.PropertyName == nameof(ViewModel.CompressionCover) ||
                e.PropertyName == nameof(ViewModel.SideCover) ||
                e.PropertyName == nameof(ViewModel.ClearSpacing) ||
                e.PropertyName == nameof(ViewModel.Fck_psi) ||
                e.PropertyName == nameof(ViewModel.Fy_psi))
            {
                Update();
            }
        }

        public bool ValidateCrossSection()
        {
            bool isValid = true;
            var errors = new List<string>();

            // Width and Height
            if (ViewModel.Width <= 0)
            {
                isValid = false;
                errors.Add("Width must be greater than 0.");
            }
            if (ViewModel.Height <= 0)
            {
                isValid = false;
                errors.Add("Height must be greater than 0.");
            }

            // Covers
            if (ViewModel.TensionCover < 0.5)
            {
                isValid = false;
                errors.Add("Tension cover must be at least 0.5 in.");
            }
            if (ViewModel.CompressionCover < 0.5)
            {
                isValid = false;
                errors.Add("Compression cover must be at least 0.5 in.");
            }
            if (ViewModel.SideCover < 0.5)
            {
                isValid = false;
                errors.Add("Side cover must be at least 0.5 in.");
            }

            // Clear spacing
            if (ViewModel.ClearSpacing < 0.5)
            {
                isValid = false;
                errors.Add("Clear spacing must be at least 0.5 in.");
            }

            // Material properties
            if (ViewModel.Fck_psi < 2000 || ViewModel.Fck_psi > 10000)
            {
                isValid = false;
                errors.Add("f'c must be between 2000 and 10000 psi.");
            }
            if (ViewModel.Fy_psi < 40000 || ViewModel.Fy_psi > 120000)
            {
                isValid = false;
                errors.Add("fy must be between 40,000 and 120,000 psi.");
            }

            if (ValidateRebars(ref errors, ViewModel) is false)
                isValid = false;

            // Optionally show errors in Debug or MessageBox
            if (!isValid)
            {
                String str = string.Empty;
                str += "\nValidation errors:";

                foreach (var err in errors)
                {
                    str += "\n - " + err;

                }

                Debug.WriteLine(str);
                MessageBox.Show(str);
            }

            return isValid;
        }

        private bool ValidateRebars(ref List<string> errors, ConcreteCrossSectionViewModel model)
        {
            bool isValid = true;

            // Rebar layers
            foreach (var layer in model.TensionRebars)
            {
                if (RebarCatalog.RebarTable.ContainsKey(layer.BarSize) is false)
                {
                    isValid = false;
                    errors.Add($"Tension bar size {layer.BarSize} is not in RebarCatalog.  Did you forget '#' on the size designation?");
                }
                if (layer.Qty <= 0)
                {
                    isValid = false;
                    errors.Add($"Tension rebar layer {layer.BarSize} has invalid quantity.");
                }
                if (layer.DepthFromTop <= 0 || layer.DepthFromTop >= ViewModel.Height)
                {
                    isValid = false;
                    errors.Add($"Tension rebar layer {layer.BarSize} has invalid depth.");
                }

                if (FlexuralDesigner.RebarSpacingHorizontalIsValid(model.ToCrossSection(), layer.BarSize, layer.Qty) is false)
                {
                    isValid = false;
                    errors.Add($"Tension rebar layer: ({layer.Qty})-{layer.BarSize} does not fit in layer.");
                }

                if (isValid is false) return false;  // return and stop checking 
            }

            foreach (var layer in model.CompressionRebars)
            {
                if (RebarCatalog.RebarTable.ContainsKey(layer.BarSize) is false)
                {
                    isValid = false;
                    errors.Add($"Compression bar size {layer.BarSize} is not in RebarCatalog.  Did you forget '#' on the size designation?");
                }
                if (layer.Qty <= 0)
                {
                    isValid = false;
                    errors.Add($"Compression rebar layer {layer.BarSize} has invalid quantity.");
                }
                if (layer.DepthFromTop <= 0 || layer.DepthFromTop >= ViewModel.Height)
                {
                    isValid = false;
                    errors.Add($"Compression rebar layer {layer.BarSize} has invalid depth.");
                }

                if (FlexuralDesigner.RebarSpacingHorizontalIsValid(model.ToCrossSection(), layer.BarSize, layer.Qty) is false)
                {
                    isValid = false;
                    errors.Add($"Compression rebar layer: ({layer.Qty})-{layer.BarSize} does not fit in layer.");
                }

                if (isValid is false) return false;  // return and stop checking 
            }

            foreach (var layer in model.StirrupRebars)
            {
                if (RebarCatalog.RebarTable.ContainsKey(layer.BarSize) is false)
                {
                    isValid = false;
                    errors.Add($"Stirrup bar size {layer.BarSize} is not in RebarCatalog.  Did you forget '#' on the size designation?");
                }
                if (layer.NumShearLegs <= 0)
                {
                    isValid = false;
                    errors.Add($"Stirrup rebar layer {layer.BarSize} has invalid quantity.");
                }
                if (layer.Spacing <= 0)
                {
                    isValid = false;
                    errors.Add($"Stirrup rebar layer {layer.Spacing} has invalid spacing.");
                }
                if (layer.StartPos < 0)
                {
                    isValid = false;
                    errors.Add($"Stirrup rebar layer {layer.StartPos} has invalid starting position.");
                }
                if (layer.EndPos <= layer.StartPos)
                {
                    isValid = false;
                    errors.Add($"Stirrup rebar layer {layer.EndPos} cannot be less than or equal to the starting position.");
                }

                if (isValid is false) return false;  // return and stop checking 
            }

            return isValid;
        }

        public ConcreteCrossSection GetCrossSection() => ViewModel.ToCrossSection();

        private void BtnAddTension_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RebarLayerInputDialog(RebarCatalog.RebarTable.Keys,
                                                   isTension: true,
                                                   sectionDepth: ViewModel.Height,
                                                   cover: ViewModel.TensionCover);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                bool isValid = true;
                List<string> errors = new List<string>();

                // create a new view model and add the bars so we can validate it
                ConcreteCrossSectionViewModel model = new ConcreteCrossSectionViewModel(ViewModel.ToCrossSection());
                model.AddTensionRebar(dialog.SelectedBarSize, dialog.Count, dialog.Depth);

                if (ValidateRebars(ref errors, model) is false)
                    isValid = false;

                if (isValid)
                {
                    ViewModel.AddTensionRebar(dialog.SelectedBarSize, dialog.Count, dialog.Depth);
                    Debug.WriteLine($"TensionRebars count: {ViewModel.TensionRebars.Count}");
                    TensionDataGrid.Items.Refresh();
                } else
                {
                    string str = string.Empty;
                    foreach(var err in errors)
                    {
                        str += "\n" + err;
                    }
                    Debug.WriteLine(str);
                    MessageBox.Show(str);
                }
            }
            Update();
        }

        private void BtnAddComp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RebarLayerInputDialog(RebarCatalog.RebarTable.Keys,
                                                   isTension: false,
                                                   sectionDepth: ViewModel.Height,
                                                   cover: ViewModel.CompressionCover);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                bool isValid = true;
                List<string> errors = new List<string>();

                // create a new view model and add the bars so we can validate it
                ConcreteCrossSectionViewModel model = new ConcreteCrossSectionViewModel(ViewModel.ToCrossSection());
                model.AddCompressionRebar(dialog.SelectedBarSize, dialog.Count, dialog.Depth);

                if (ValidateRebars(ref errors, model) is false)
                    isValid = false;

                if (isValid)
                {
                    ViewModel.AddCompressionRebar(dialog.SelectedBarSize, dialog.Count, dialog.Depth);
                    CompressionDataGrid.Items.Refresh();
                }
                else
                {
                    string str = string.Empty;
                    foreach (var err in errors)
                    {
                        str += "\n" + err;
                    }
                    Debug.WriteLine(str);
                    MessageBox.Show(str);
                }
            }
            Update();
        }

        private void BtnAddStirrups_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StirrupRebarLayerInputDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                bool isValid = true;
                List<string> errors = new List<string>();

                // create a new view model and add the bars so we can validate it
                ConcreteCrossSectionViewModel model = new ConcreteCrossSectionViewModel(ViewModel.ToCrossSection());
                model.AddStirrupRebar(dialog.SelectedBarSize, dialog.NumShearLegs, dialog.Spacing, dialog.StartPos, dialog.EndPos);

                if (ValidateRebars(ref errors, model) is false)
                    isValid = false;

                // actually add it to our view model
                if (isValid)
                {
                    ViewModel.AddStirrupRebar(dialog.SelectedBarSize, dialog.NumShearLegs, dialog.Spacing, dialog.StartPos, dialog.EndPos);
                    StirrupDataGrid.Items.Refresh();
                }
                else
                {
                    string str = string.Empty;
                    foreach (var err in errors)
                    {
                        str += "\n" + err;
                    }
                    Debug.WriteLine(str);
                    MessageBox.Show(str);
                }
            }
            Update();
        }


        private void BtnRemoveTension_Click(object sender, RoutedEventArgs e)
        {
            if (TensionDataGrid.SelectedItem is RebarLayerViewModel layer)
                ViewModel.RemoveTensionRebar(layer);

            Update();
        }

        private void BtnRemoveComp_Click(object sender, RoutedEventArgs e)
        {
            if (CompressionDataGrid.SelectedItem is RebarLayerViewModel layer)
                ViewModel.RemoveCompressionRebar(layer);

            Update();
        }

        private void BtnRemoveStirrups_Click(object sender, RoutedEventArgs e)
        {
            if (StirrupDataGrid.SelectedItem is StirrupRebarLayerViewModel layer)
                ViewModel.RemoveStirrupRebar(layer);

            Update();
        }

        public void Update()
        {
            // Clear the design results control
            spResult.Children.Clear();
            spShearResult.Children.Clear();

            // ValidateCrossSection that all the input in the ViewModel is valid, otherwise dont return
            if (ValidateInputs() is false) return;
            if (ValidateCrossSection() is false) return;

            // get our new section from the ViewModel
            ConcreteCrossSection section = GetCrossSection();

            if (section == null)
                return;

            // Perform a moment calculation
            if (section.TensionRebars.Count == 0) return;
            {
                // perform flexure and shear calculations
                FlexuralDesignResultModel design = FlexuralDesigner.ComputeFlexuralMomentCapacity(section);
                ShearDesigner.ComputeShearCapacity(section, ref design);


                // add the flexure result control
                FlexureDesignResultControl control = new FlexureDesignResultControl();
                spResult.Children.Add(control);
                if (control != null)
                {
                    control.Result = design;
                }

                // add the shear result control
                ShearDesignResultControl shear_control = new ShearDesignResultControl();
                spShearResult.Children.Add(shear_control);
                if (shear_control != null)
                {
                    shear_control.Result = design;
                }
            }
        }

        public static class ValidationHelper
        {
            public static bool IsValid(DependencyObject node)
            {
                // Check this node
                if (Validation.GetHasError(node))
                {
                    return false;
                }

                // Check children
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(node, i);
                    if (!IsValid(child))
                        return false;
                }

                return true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Force validation immediately
            var textBox = sender as TextBox;
            BindingExpression be = textBox.GetBindingExpression(TextBox.TextProperty);
            be?.UpdateSource();

            // Optionally run your overall control validation
            bool allValid = ValidationHelper.IsValid(this);
        }


        private void NumericUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            spResult.Children.Clear();
            // Only update if all NumericUpDowns are valid
            if (AreAllNumericInputsValid())
                Update();
        }

        private bool AreAllNumericInputsValid()
        {
            foreach (var numUpDown in FindVisualChildren<NumericUpDown>(this))
            {
                if (!numUpDown.IsValid)
                    return false;
            }
            return true;
        }

        // Helper function to enumerate children recursively
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T t)
                    yield return t;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }

    public class CrossSectionRowValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            // `value` is the row item (e.g. TensionRebarViewModel)
            var row = value as object;

            // Find the DataContext (your VM) from Application.Current.Windows
            var control = Application.Current.Windows.OfType<Window>()
                .SelectMany(w => FindVisualChildren<ConcreteCrossSectionInputControl>(w))
                .FirstOrDefault();

            if (control == null)
                return ValidationResult.ValidResult;

            if (!control.ValidateCrossSection())
                return new ValidationResult(false, "Rebar validation failed.");

            return ValidationResult.ValidResult;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }

}
