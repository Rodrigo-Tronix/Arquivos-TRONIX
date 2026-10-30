using System.Windows;
using System.Windows.Controls;

namespace TRONIX.Modules
{
    public partial class JustificativaInatividadeWindow : Window
    {
        public string JustificativaInformada { get; private set; }
        public double SegundosInativo { get; private set; }

        public JustificativaInatividadeWindow(double segundosInativo)
        {
            InitializeComponent();
            SegundosInativo = segundosInativo;
            CarregarMotivos();
        }

        private void CarregarMotivos()
        {
            // 1. Carrega as configurações salvas pelo Administrador
            var config = ConfigManager.Carregar();

            // 2. Adiciona a lista dinâmica ao ComboBox
            if (config.JustificativasOciosidade != null)
            {
                foreach (var motivo in config.JustificativasOciosidade)
                {
                    if (!string.IsNullOrWhiteSpace(motivo))
                    {
                        CboMotivos.Items.Add(motivo);
                    }
                }
            }

            // 3. Adiciona a opção de texto livre no final, como padrão de segurança
            CboMotivos.Items.Add("Outro Motivo (Especificar Abaixo)");
        }

        private void CboMotivos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Como agora adicionamos strings diretas, checamos se é string
            if (CboMotivos.SelectedItem is string texto)
            {
                if (texto != "Outro Motivo (Especificar Abaixo)")
                {
                    TxtJustificativa.Text = texto;
                    TxtErro.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TxtJustificativa.Clear();
                    TxtJustificativa.Focus();
                }
            }
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) btn.IsEnabled = false;

            if (string.IsNullOrWhiteSpace(TxtJustificativa.Text))
            {
                TxtErro.Visibility = Visibility.Visible;
                TxtJustificativa.Focus();

                if (sender is Button b) b.IsEnabled = true;
                return;
            }

            TxtErro.Visibility = Visibility.Collapsed;
            JustificativaInformada = TxtJustificativa.Text.Trim();

            this.DialogResult = true;
        }
    }
}