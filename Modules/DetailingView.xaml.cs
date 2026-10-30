using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TRONIX.Modules
{
    public partial class DetailingView : UserControl
    {
        public DetailingView()
        {
            InitializeComponent();
            this.DataContext = new DetailingViewModel();
        }

        // ============================================================
        // NAVEGAÇÃO EXCLUSIVA DO COMBOBOX (LISTAS)
        // ============================================================
        private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var cbo = sender as ComboBox;
            if (cbo == null) return;

            // =========================================================
            // TECLA TAB: CONFIRMA, FECHA E AVANÇA
            // =========================================================
            if (e.Key == Key.Tab)
            {
                if (cbo.IsDropDownOpen)
                {
                    // 1. Confirma a seleção
                    ConfirmarSelecao(cbo);

                    // 2. Fecha a lista
                    cbo.IsDropDownOpen = false;

                    // 3. Bloqueia o Tab nativo para não haver conflito com o Popup
                    e.Handled = true;

                    // 4. Move o foco para o próximo campo
                    cbo.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            cbo.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        }),
                        DispatcherPriority.Input);
                }
                // Se a lista estiver fechada, o Tab nativo do WPF funciona sozinho.
                return;
            }

            // =========================================================
            // TECLA ENTER: ABRE OU CONFIRMA
            // =========================================================
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (!cbo.IsDropDownOpen)
                {
                    // 1º ENTER: ABRE A LISTA
                    cbo.IsDropDownOpen = true;
                    e.Handled = true;
                }
                else
                {
                    // 2º ENTER: CONFIRMA, FECHA E AVANÇA
                    ConfirmarSelecao(cbo);
                    cbo.IsDropDownOpen = false;
                    e.Handled = true;

                    cbo.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            cbo.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        }),
                        DispatcherPriority.Input);
                }
                return;
            }

            // =========================================================
            // SETAS ↑ ↓: NAVEGAM ENTRE ITENS OU CAMPOS
            // =========================================================
            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (!cbo.IsDropDownOpen)
                {
                    // Lista fechada: navega entre os campos
                    var direction = e.Key == Key.Down
                        ? FocusNavigationDirection.Down
                        : FocusNavigationDirection.Up;

                    cbo.MoveFocus(new TraversalRequest(direction));
                    e.Handled = true;
                }
                // Se a lista estiver aberta, o WPF navega entre os itens nativamente.
            }
        }

        // Método que força o ComboBox a pegar o item destacado e jogar no campo e no ViewModel
        private void ConfirmarSelecao(ComboBox cbo)
        {
            if (cbo.SelectedItem != null)
            {
                // Força o texto para o item selecionado
                cbo.Text = cbo.SelectedItem.ToString();

                // Força o Binding a atualizar a propriedade "Valor" no ViewModel
                var binding = cbo.GetBindingExpression(ComboBox.TextProperty);
                binding?.UpdateSource();
            }
        }

        // ============================================================
        // NAVEGAÇÃO PARA OS DEMAIS CAMPOS (TEXTBOX, DATEPICKER, ETC)
        // ============================================================
        private void ItemsControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Se o evento veio de dentro de um ComboBox, ignora aqui 
            DependencyObject orig = e.OriginalSource as DependencyObject;
            if (orig != null && (orig is ComboBox || FindParent<ComboBox>(orig) != null))
                return;

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (Keyboard.FocusedElement is ButtonBase)
                    return;

                if (Keyboard.FocusedElement is UIElement uiElemEnter)
                {
                    uiElemEnter.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (Keyboard.FocusedElement is UIElement uiElemArrow)
                {
                    var direction = e.Key == Key.Down
                        ? FocusNavigationDirection.Down
                        : FocusNavigationDirection.Up;

                    uiElemArrow.MoveFocus(new TraversalRequest(direction));
                    e.Handled = true;
                }
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}