using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ACI318_19Library
{
    public partial class CrossSectionInputControl : UserControl
    {
        public CrossSectionViewModel ViewModel { get; private set; }
        public bool ValidateInputs()
        {
            return ValidationHelper.IsValid(this);
        }

        public CrossSectionInputControl()
        {
            InitializeComponent();
            // create a default ViewModel if none provided
            ViewModel = new CrossSectionViewModel();
            DataContext = ViewModel;

            this.Loaded += (s, e) => Update();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;



            AttachNumericUpDownEvents();

            TensionDataGrid.CellEditEnding += DataGrid_CellEditEnding;
            CompressionDataGrid.CellEditEnding += DataGrid_CellEditEnding;
            TensionDataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;
            CompressionDataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;

            foreach (var layer in ViewModel.TensionRebars)
                layer.PropertyChanged += Layer_PropertyChanged;

            ViewModel.TensionRebars.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (RebarLayerViewModel layer in e.NewItems)
                        layer.PropertyChanged += Layer_PropertyChanged;
            };


            foreach (var layer in ViewModel.CompressionRebars)
                layer.PropertyChanged += Layer_PropertyChanged;

            ViewModel.CompressionRebars.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (RebarLayerViewModel layer in e.NewItems)
                        layer.PropertyChanged += Layer_PropertyChanged;
            };
        }



        public CrossSectionInputControl(CrossSectionViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = ViewModel;

            //ViewModel.TensionRebars.Add(new RebarLayerViewModel("#9", 2, 14.75));
            //ViewModel.TensionRebars.Add(new RebarLayerViewModel("#9", 2, 17.25));

            //ViewModel.CompressionRebars.Add(new RebarLayerViewModel("#6", 2, 2.5));


            Debug.WriteLine("DataContext is null? " + (DataContext == null));
            Debug.WriteLine("TensionRebars count: " + ViewModel.TensionRebars.Count);

            this.Loaded += (s, e) => Update();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            AttachNumericUpDownEvents();

            TensionDataGrid.CellEditEnding += DataGrid_CellEditEnding;
            CompressionDataGrid.CellEditEnding += DataGrid_CellEditEnding;
            TensionDataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;
            CompressionDataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;

            foreach (var layer in ViewModel.TensionRebars)
                layer.PropertyChanged += Layer_PropertyChanged;

            ViewModel.TensionRebars.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (RebarLayerViewModel layer in e.NewItems)
                        layer.PropertyChanged += Layer_PropertyChanged;
            };


            foreach (var layer in ViewModel.CompressionRebars)
                layer.PropertyChanged += Layer_PropertyChanged;

            ViewModel.CompressionRebars.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (RebarLayerViewModel layer in e.NewItems)
                        layer.PropertyChanged += Layer_PropertyChanged;
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

        public bool Validate()
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

            // Rebar layers
            foreach (var layer in ViewModel.TensionRebars)
            {
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
            }

            foreach (var layer in ViewModel.CompressionRebars)
            {
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
            }

            // Optionally show errors in Debug or MessageBox
            if (!isValid)
            {
                Debug.WriteLine("Validation errors:");
                foreach (var err in errors)
                    Debug.WriteLine(" - " + err);
            }

            return isValid;
        }


        public CrossSection GetCrossSection() => ViewModel.ToCrossSection();

        private void BtnAddTension_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RebarLayerInputDialog(RebarCatalog.RebarTable.Keys,
                                                   isTension: true,
                                                   sectionDepth: ViewModel.Height,
                                                   cover: ViewModel.TensionCover);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                ViewModel.AddTensionRebar(dialog.SelectedBarSize, dialog.Count, dialog.Depth);
                Debug.WriteLine($"TensionRebars count: {ViewModel.TensionRebars.Count}");
                TensionDataGrid.Items.Refresh();
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
                ViewModel.AddCompressionRebar(dialog.SelectedBarSize, dialog.Count, dialog.Depth);
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

        public void Update()
        {
            // Clear the design results control
            spResult.Children.Clear();

            // Validate that all the input in the ViewModel is valid, otherwise dont return
            if (ValidateInputs() is false) return;

            // get our new section from the ViewModel
            CrossSection section = GetCrossSection();

            if (section == null)
                return;

            section.Av_barSize = "#4";
            section.ShearSpacing = 12;
            section.StirrupsLegs = 2;

            ShearDesignResultModel shear_model = ShearDesigner.ComputeShearCapacity(section);


            // Perform a moment calculation
            if (section.TensionRebars.Count == 0) return;

            FlexuralDesignResultModel design = FlexuralDesigner.ComputeFlexuralMomentCapacity(section);

            if (design != null)
            {
                FlexureDesignResultControl control = new FlexureDesignResultControl();
                control.Result = design;
                spResult.Children.Add(control);
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
}
