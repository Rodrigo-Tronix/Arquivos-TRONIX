using System.Windows;
using System.Windows.Controls;

namespace TRONIX.Modules
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            this.DataContext = new SettingsViewModel();
        }

        // Evento para contornar a limitação de segurança do PasswordBox
        private void TxtSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SettingsViewModel vm)
            {
                vm.SenhaDigitada = TxtSenha.Password;
            }
        }
    }
}