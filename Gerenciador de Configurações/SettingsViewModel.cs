using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace TRONIX.Modules
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private AppConfig _config;
        private bool _isAdminLogado = false;
        private string _senhaDigitada;

        // Coleções vinculadas à interface
        public ObservableCollection<string> Desenhistas { get; set; }
        public ObservableCollection<string> Materiais { get; set; }
        public ObservableCollection<string> Pinturas { get; set; }
        public ObservableCollection<string> Banhos { get; set; }
        public ObservableCollection<string> Justificativas { get; set; }

        // Propriedades de Texto (Input do usuário)
        public string NovoDesenhista { get; set; }
        public string NovoMaterial { get; set; }
        public string NovaPintura { get; set; }
        public string NovoBanho { get; set; }
        public string NovaJustificativa { get; set; }

        // Propriedades de Seleção (Para remover)
        public string DesenhistaSelecionado { get; set; }
        public string MaterialSelecionado { get; set; }
        public string PinturaSelecionada { get; set; }
        public string BanhoSelecionado { get; set; }
        public string JustificativaSelecionada { get; set; }

        // Controle de Login
        public bool IsAdminLogado { get => _isAdminLogado; set { _isAdminLogado = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNaoAdmin)); } }
        public bool IsNaoAdmin => !IsAdminLogado;
        public string SenhaDigitada { get => _senhaDigitada; set { _senhaDigitada = value; OnPropertyChanged(); } }

        public int LimiteInatividade
        {
            get => _config.LimiteInatividadeSegundos;
            set { _config.LimiteInatividadeSegundos = value; OnPropertyChanged(); }
        }

        // Comandos
        public ICommand LoginAdminCommand { get; }
        public ICommand SalvarCommand { get; }

        public ICommand AddDesenhistaCommand { get; }
        public ICommand DelDesenhistaCommand { get; }
        public ICommand ImportarDesenhistasCommand { get; }

        public ICommand AddMaterialCommand { get; }
        public ICommand DelMaterialCommand { get; }
        public ICommand ImportarMateriaisCommand { get; }

        public ICommand AddPinturaCommand { get; }
        public ICommand DelPinturaCommand { get; }
        public ICommand ImportarPinturasCommand { get; }

        public ICommand AddBanhoCommand { get; }
        public ICommand DelBanhoCommand { get; }
        public ICommand ImportarBanhosCommand { get; }

        public ICommand AddJustificativaCommand { get; }
        public ICommand DelJustificativaCommand { get; }
        public ICommand ImportarJustificativasCommand { get; }


        public SettingsViewModel()
        {
            _config = ConfigManager.Carregar();

            Desenhistas = new ObservableCollection<string>(_config.Desenhistas);
            Materiais = new ObservableCollection<string>(_config.Materiais);
            Pinturas = new ObservableCollection<string>(_config.Pinturas);
            Banhos = new ObservableCollection<string>(_config.Banhos);
            Justificativas = new ObservableCollection<string>(_config.JustificativasOciosidade);

            LoginAdminCommand = new RelayCommand(FazerLogin);
            SalvarCommand = new RelayCommand(SalvarConfiguracoes);

            // Comandos Desenhistas
            AddDesenhistaCommand = new RelayCommand(() => AdicionarItem(NovoDesenhista, Desenhistas, () => NovoDesenhista = string.Empty, nameof(NovoDesenhista)));
            DelDesenhistaCommand = new RelayCommand(() => RemoverItem(DesenhistaSelecionado, Desenhistas));
            ImportarDesenhistasCommand = new RelayCommand(() => ImportarTxt(Desenhistas));

            // Comandos Materiais
            AddMaterialCommand = new RelayCommand(() => AdicionarItem(NovoMaterial, Materiais, () => NovoMaterial = string.Empty, nameof(NovoMaterial)));
            DelMaterialCommand = new RelayCommand(() => RemoverItem(MaterialSelecionado, Materiais));
            ImportarMateriaisCommand = new RelayCommand(() => ImportarTxt(Materiais));

            // Comandos Pinturas
            AddPinturaCommand = new RelayCommand(() => AdicionarItem(NovaPintura, Pinturas, () => NovaPintura = string.Empty, nameof(NovaPintura)));
            DelPinturaCommand = new RelayCommand(() => RemoverItem(PinturaSelecionada, Pinturas));
            ImportarPinturasCommand = new RelayCommand(() => ImportarTxt(Pinturas));

            // Comandos Banhos
            AddBanhoCommand = new RelayCommand(() => AdicionarItem(NovoBanho, Banhos, () => NovoBanho = string.Empty, nameof(NovoBanho)));
            DelBanhoCommand = new RelayCommand(() => RemoverItem(BanhoSelecionado, Banhos));
            ImportarBanhosCommand = new RelayCommand(() => ImportarTxt(Banhos));

            // Comandos Justificativas
            AddJustificativaCommand = new RelayCommand(() => AdicionarItem(NovaJustificativa, Justificativas, () => NovaJustificativa = string.Empty, nameof(NovaJustificativa)));
            DelJustificativaCommand = new RelayCommand(() => RemoverItem(JustificativaSelecionada, Justificativas));
            ImportarJustificativasCommand = new RelayCommand(() => ImportarTxt(Justificativas));
        }

        private void FazerLogin()
        {
            if (SenhaDigitada == _config.SenhaAdmin)
            {
                IsAdminLogado = true;
                SenhaDigitada = string.Empty;
            }
            else
            {
                MessageBox.Show("Senha incorreta!", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SalvarConfiguracoes()
        {
            _config.Desenhistas = new List<string>(Desenhistas);
            _config.Materiais = new List<string>(Materiais);
            _config.Pinturas = new List<string>(Pinturas);
            _config.Banhos = new List<string>(Banhos);
            _config.JustificativasOciosidade = new List<string>(Justificativas);

            ConfigManager.Salvar(_config);
            MessageBox.Show("Configurações salvas com sucesso!", "TRONIX", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // --- MÉTODOS GENÉRICOS PARA AS LISTAS ---

        private void AdicionarItem(string novoItem, ObservableCollection<string> lista, Action limparCampo, string nomePropriedade)
        {
            if (!string.IsNullOrWhiteSpace(novoItem) && !lista.Contains(novoItem.Trim()))
            {
                lista.Add(novoItem.Trim());
                limparCampo();
                OnPropertyChanged(nomePropriedade);
            }
        }

        private void RemoverItem(string itemSelecionado, ObservableCollection<string> lista)
        {
            if (!string.IsNullOrEmpty(itemSelecionado) && lista.Contains(itemSelecionado))
            {
                lista.Remove(itemSelecionado);
            }
        }

        private void ImportarTxt(ObservableCollection<string> lista)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Importar Lista de Arquivo TXT",
                Filter = "Arquivos de Texto (*.txt)|*.txt|Todos os Arquivos (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string[] linhas = File.ReadAllLines(dialog.FileName);
                    int importados = 0;
                    foreach (var linha in linhas)
                    {
                        string itemLimpo = linha.Trim();
                        if (!string.IsNullOrWhiteSpace(itemLimpo) && !lista.Contains(itemLimpo))
                        {
                            lista.Add(itemLimpo);
                            importados++;
                        }
                    }
                    MessageBox.Show($"{importados} itens importados com sucesso!", "Importação TXT", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao ler o arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}