using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TRONIX.Modules;
using TRONIX.Services;

namespace TRONIX.ViewModels
{
    public class SessaoExportDTO : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public SessaoTrabalho Sessao { get; set; }
        public string TempoTotalFormatado => TimeSpan.FromSeconds(Sessao.TempoAcumuladoSegundos).ToString(@"hh\:mm\:ss");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ExportViewModel : INotifyPropertyChanged
    {
        // CORREÇÃO: O Repositório é privado e instanciado internamente para salvar/carregar dados
        private readonly TimeTrackerRepository _repository = new TimeTrackerRepository();

        // Mantemos a sessão atual que foi recebida no construtor
        private readonly ProjectSession _sessaoAtual;

        private string _filtroProjeto;
        private string _statusSelecionado = "Todos";

        // CORREÇÃO: Construtor recebe apenas a ProjectSession
        public ExportViewModel(ProjectSession sessaoAtual)
        {
            _sessaoAtual = sessaoAtual;
            _filtroProjeto = sessaoAtual?.Sessao?.NomeDocumento;

            ListaStatus = new ObservableCollection<string> { "Todos", "EmAndamento", "EmPausa", "Finalizada", "Interrompida" };
            SessoesExibidas = new ObservableCollection<SessaoExportDTO>();

            ExportarCSVCommand = new RelayCommand(ExportarCSV);
            CarregarDados();
        }

        public ObservableCollection<SessaoExportDTO> SessoesExibidas { get; }
        public ObservableCollection<string> ListaStatus { get; }

        public string FiltroProjeto
        {
            get => _filtroProjeto;
            set { _filtroProjeto = value; OnPropertyChanged(); CarregarDados(); }
        }

        public string StatusSelecionado
        {
            get => _statusSelecionado;
            set { _statusSelecionado = value; OnPropertyChanged(); CarregarDados(); }
        }

        public ICommand ExportarCSVCommand { get; }

        private void CarregarDados()
        {
            SessoesExibidas.Clear();

            // O repositório agora possui este método corretamente
            var todas = _repository.CarregarTodasSessoes();

            var filtradas = todas.Where(s =>
                (string.IsNullOrEmpty(FiltroProjeto) || s.NomeDocumento.IndexOf(FiltroProjeto, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (StatusSelecionado == "Todos" || s.Status.ToString() == StatusSelecionado)
            );

            foreach (var s in filtradas)
            {
                SessoesExibidas.Add(new SessaoExportDTO { Sessao = s });
            }
        }

        private void ExportarCSV()
        {
            var selecionados = SessoesExibidas.Where(s => s.IsSelected).Select(s => s.Sessao).ToList();
            if (!selecionados.Any())
            {
                MessageBox.Show("Nenhuma sessão selecionada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string caminhoDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Relatorio_Tempo_TRONIX.csv");

            try
            {
                using (var fs = new FileStream(caminhoDestino, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (var sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        sw.WriteLine("Data;Projetista;Documento;Tipo;Etapa;Status;Hora Inicio;Hora Fim;Tempo Gasto;Justificativa");
                        foreach (var s in selecionados)
                        {
                            string tempo = TimeSpan.FromSeconds(s.TempoAcumuladoSegundos).ToString(@"hh\:mm\:ss");
                            string inicio = s.HoraInicio.ToString("dd/MM/yyyy HH:mm:ss");
                            string fim = s.HoraFim.HasValue ? s.HoraFim.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";
                            sw.WriteLine($"{inicio};{s.Projetista};{s.NomeDocumento};{s.TipoDoc};{s.Etapa};{s.Status};{inicio};{fim};{tempo};\"{s.JustificativaFinal}\"");
                        }
                    }
                }
                MessageBox.Show($"Exportação concluída com sucesso!\nSalvo em: {caminhoDestino}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar CSV (Verifique se o arquivo está aberto no Excel): {ex.Message}", "Erro de Acesso", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}