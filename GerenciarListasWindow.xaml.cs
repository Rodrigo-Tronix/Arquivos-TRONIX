using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace TRONIX.Modules
{
    public partial class GerenciarListasWindow : Window
    {
        private GerenciadorListas.DadosListas _dados;

        public ObservableCollection<string> Desenhistas { get; set; }
        public ObservableCollection<string> Materiais { get; set; }
        public ObservableCollection<string> Pinturas { get; set; }
        public ObservableCollection<string> Banhos { get; set; }

        public GerenciarListasWindow()
        {
            InitializeComponent();
            _dados = GerenciadorListas.Carregar();

            Desenhistas = new ObservableCollection<string>(_dados.Desenhistas);
            Materiais = new ObservableCollection<string>(_dados.Materiais);
            Pinturas = new ObservableCollection<string>(_dados.Pinturas);
            Banhos = new ObservableCollection<string>(_dados.Banhos);

            LstDesenhistas.ItemsSource = Desenhistas;
            LstMateriais.ItemsSource = Materiais;
            LstPinturas.ItemsSource = Pinturas;
            LstBanhos.ItemsSource = Banhos;
        }

        private void AddDesenhista_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtNovoDesenhista.Text) && !Desenhistas.Contains(TxtNovoDesenhista.Text.Trim()))
            {
                Desenhistas.Add(TxtNovoDesenhista.Text.Trim());
                TxtNovoDesenhista.Clear();
            }
        }
        private void DelDesenhista_Click(object sender, RoutedEventArgs e)
        {
            if (LstDesenhistas.SelectedItem is string sel) Desenhistas.Remove(sel);
        }

        private void AddMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtNovoMaterial.Text) && !Materiais.Contains(TxtNovoMaterial.Text.Trim()))
            {
                Materiais.Add(TxtNovoMaterial.Text.Trim());
                TxtNovoMaterial.Clear();
            }
        }
        private void DelMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (LstMateriais.SelectedItem is string sel) Materiais.Remove(sel);
        }

        private void AddPintura_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtNovaPintura.Text) && !Pinturas.Contains(TxtNovaPintura.Text.Trim()))
            {
                Pinturas.Add(TxtNovaPintura.Text.Trim());
                TxtNovaPintura.Clear();
            }
        }
        private void DelPintura_Click(object sender, RoutedEventArgs e)
        {
            if (LstPinturas.SelectedItem is string sel) Pinturas.Remove(sel);
        }

        private void AddBanho_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtNovoBanho.Text) && !Banhos.Contains(TxtNovoBanho.Text.Trim()))
            {
                Banhos.Add(TxtNovoBanho.Text.Trim());
                TxtNovoBanho.Clear();
            }
        }
        private void DelBanho_Click(object sender, RoutedEventArgs e)
        {
            if (LstBanhos.SelectedItem is string sel) Banhos.Remove(sel);
        }

        /// <summary>
        /// Lê a pasta informada e importa itens de DESENHISTA.txt, MATERIA_PRIMA.txt, PINTURA.txt e BANHO.txt
        /// </summary>
        private void ImportarTXT_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Selecione a pasta que contém os arquivos .txt (DESENHISTA, MATERIA_PRIMA, PINTURA, BANHO):";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    int totalImportados = 0;

                    totalImportados += ImportarLinhasParaColecao(Path.Combine(dialog.SelectedPath, "DESENHISTA.txt"), Desenhistas);
                    totalImportados += ImportarLinhasParaColecao(Path.Combine(dialog.SelectedPath, "MATERIA_PRIMA.txt"), Materiais);
                    totalImportados += ImportarLinhasParaColecao(Path.Combine(dialog.SelectedPath, "PINTURA.txt"), Pinturas);
                    totalImportados += ImportarLinhasParaColecao(Path.Combine(dialog.SelectedPath, "BANHO.txt"), Banhos);

                    System.Windows.MessageBox.Show(
                        $"Importação concluída com sucesso!\n\nForam adicionados {totalImportados} novos itens às listas.",
                        "Importação TXT - TRONIX", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private int ImportarLinhasParaColecao(string caminhoTxt, ObservableCollection<string> colecao)
        {
            int novosItens = 0;
            try
            {
                if (File.Exists(caminhoTxt))
                {
                    string[] linhas = File.ReadAllLines(caminhoTxt);
                    foreach (var linha in linhas)
                    {
                        string itemLimpo = linha.Trim();
                        if (!string.IsNullOrWhiteSpace(itemLimpo) && !colecao.Contains(itemLimpo))
                        {
                            colecao.Add(itemLimpo);
                            novosItens++;
                        }
                    }
                }
            }
            catch { }
            return novosItens;
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            _dados.Desenhistas = new System.Collections.Generic.List<string>(Desenhistas);
            _dados.Materiais = new System.Collections.Generic.List<string>(Materiais);
            _dados.Pinturas = new System.Collections.Generic.List<string>(Pinturas);
            _dados.Banhos = new System.Collections.Generic.List<string>(Banhos);

            GerenciadorListas.Salvar(_dados);
            this.DialogResult = true;
            this.Close();
        }
    }
}