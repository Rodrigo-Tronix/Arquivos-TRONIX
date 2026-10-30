using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TRONIX.Modules.Views;
using TRONIX.Services;
using TRONIX.ViewModels;
using Xarial.XCad.Documents;
using Xarial.XCad.SolidWorks;
using Xarial.XCad.SolidWorks.Documents;

namespace TRONIX.Modules
{
    public class TimeTrackerViewModel : INotifyPropertyChanged
    {
        // [Importações Win32 mantidas...]
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
        [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // Variável de inatividade dinâmica
        private int _limiteInatividadeSegundos = 10;

        private readonly ISwApplication _app;
        private readonly DispatcherTimer _timer;
        private readonly TimeTrackerRepository _repository = new TimeTrackerRepository();
        private readonly Dictionary<string, ProjectSession> _sessoes = new Dictionary<string, ProjectSession>();

        private string _nomeProjetoAtivo;
        private string _codigoProjetoPai;
        private string _tempoFormatado = "00:00:00";
        private string _tempoTotalProjetoFormatado = "00:00:00";
        private string _etapaSelecionada;
        private string _justificativa;

        private bool _precisaJustificarInatividade;
        private DateTime _inicioInatividade = DateTime.Now;

        private static bool _exibindoJanelaBloqueioGlobal;
        private static DateTime _ultimoBloqueioFechadoGlobal = DateTime.MinValue;

        // CACHE DE PERFORMANCE
        private double _totalProjetoSegundosBase = 0;
        private string _ultimoProjetoCalculado = "";

        public TimeTrackerViewModel()
        {
            // LÊ AS CONFIGURAÇÕES DO ARQUIVO JSON
            var config = ConfigManager.Carregar();
            _limiteInatividadeSegundos = config.LimiteInatividadeSegundos;

            try
            {
                _app = SwApplicationFactory.FromProcess(Process.GetCurrentProcess());
                if (_app?.Documents != null) _app.Documents.DocumentActivated += OnDocumentActivated;
            }
            catch { }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => MonitorarEAtualizarTempo();

            EtapasDisponiveis = new ObservableCollection<string> { "Modelagem 3D", "Detalhamento 2D", "Revisão de Projeto", "Lista de Materiais (BOM)", "Alteração Solicitada" };
            HistoricoRecente = new ObservableCollection<SessaoTrabalho>();

            FinalizarProjetoCommand = new RelayCommand(FinalizarProjeto);
            ExportarRelatorioCommand = new RelayCommand(ExportarRelatorio);
            AbrirRenomeadorCigamCommand = new RelayCommand(AbrirRenomeadorCigam);

            AtualizarProjetoAtivo();
            CarregarHistoricoRecente();
            _timer.Start();
        }

        public ObservableCollection<string> EtapasDisponiveis { get; }
        public ObservableCollection<SessaoTrabalho> HistoricoRecente { get; }
        public string NomeProjetoAtivo { get => _nomeProjetoAtivo; set { _nomeProjetoAtivo = value; OnPropertyChanged(); } }
        public string CodigoProjetoPai { get => _codigoProjetoPai; set { _codigoProjetoPai = value; OnPropertyChanged(); RecalcularTempoTotalProjeto(); } }
        public string TempoFormatado { get => _tempoFormatado; set { _tempoFormatado = value; OnPropertyChanged(); } }
        public string TempoTotalProjetoFormatado { get => _tempoTotalProjetoFormatado; set { _tempoTotalProjetoFormatado = value; OnPropertyChanged(); } }
        public string EtapaSelecionada { get => _etapaSelecionada; set { _etapaSelecionada = value; OnPropertyChanged(); } }
        public string Justificativa { get => _justificativa; set { _justificativa = value; OnPropertyChanged(); } }

        public ICommand FinalizarProjetoCommand { get; }
        public ICommand ExportarRelatorioCommand { get; }
        public ICommand AbrirRenomeadorCigamCommand { get; }

        private void AbrirRenomeadorCigam() { var janela = new CigamRenameView { DataContext = new CigamRenameViewModel() }; janela.ShowDialog(); }
        private void PausarTodasSessoes() { foreach (var kvp in _sessoes) { if (kvp.Value.Stopwatch.IsRunning) { kvp.Value.Stopwatch.Stop(); kvp.Value.EmPausa = true; } } }
        private void DefinirEstadoSemArquivoAberto() { NomeProjetoAtivo = "Nenhum arquivo aberto"; CodigoProjetoPai = string.Empty; TempoFormatado = "00:00:00"; TempoTotalProjetoFormatado = "00:00:00"; EtapaSelecionada = null; Justificativa = string.Empty; }

        private void OnDocumentActivated(IXDocument doc)
        {
            PausarTodasSessoes();
            bool janelaMinimizada = doc == null || VerificarSeJanelaMinimizada(doc);

            if (doc != null && !string.IsNullOrEmpty(doc.Title) && !janelaMinimizada)
            {
                NomeProjetoAtivo = doc.Title;
                CodigoProjetoPai = DeterminarCodigoProjetoPai(doc);
                var sessaoAtual = ObterOuCriarSessao(NomeProjetoAtivo);

                sessaoAtual.EmPausa = false;
                sessaoAtual.Sessao.Status = StatusSessao.EmAndamento;
                if (!sessaoAtual.Stopwatch.IsRunning) sessaoAtual.Stopwatch.Start();

                AtualizarTempoTela();
                RecalcularTempoTotalProjeto();
            }
            else DefinirEstadoSemArquivoAberto();
        }

        private void MonitorarEAtualizarTempo()
        {
            IXDocument docAtivo = null;
            try { docAtivo = _app?.Documents?.Active; } catch { }

            bool janelaMinimizada = docAtivo == null || VerificarSeJanelaMinimizada(docAtivo);

            if (docAtivo == null || string.IsNullOrEmpty(docAtivo.Title) || janelaMinimizada)
            {
                PausarTodasSessoes();
                DefinirEstadoSemArquivoAberto();
                _precisaJustificarInatividade = false;
                return;
            }

            if (NomeProjetoAtivo == "Nenhum arquivo aberto" || NomeProjetoAtivo != docAtivo.Title)
            {
                OnDocumentActivated(docAtivo);
                return;
            }

            AtualizarTempoTela();

            if (_exibindoJanelaBloqueioGlobal) return;
            if ((DateTime.Now - _ultimoBloqueioFechadoGlobal).TotalSeconds < 5) return;
            if (!_sessoes.TryGetValue(NomeProjetoAtivo, out var sessaoAtiva)) return;

            bool solidWorksEmFoco = VerificarSeSolidWorksEstaEmFoco();
            double segundosSemEntrada = ObterSegundosInatividadeSistema();

            if (!_precisaJustificarInatividade && solidWorksEmFoco && segundosSemEntrada < _limiteInatividadeSegundos)
            {
                if (!sessaoAtiva.Stopwatch.IsRunning)
                {
                    sessaoAtiva.Stopwatch.Start();
                    sessaoAtiva.EmPausa = false;
                    sessaoAtiva.Sessao.Status = StatusSessao.EmAndamento;
                }
            }

            if (sessaoAtiva.Sessao.Status == StatusSessao.EmAndamento)
            {
                if (segundosSemEntrada >= _limiteInatividadeSegundos || !solidWorksEmFoco)
                {
                    sessaoAtiva.Stopwatch.Stop();
                    sessaoAtiva.EmPausa = true;
                    sessaoAtiva.Sessao.Status = StatusSessao.EmPausa;

                    _inicioInatividade = DateTime.Now.AddSeconds(-Math.Min(segundosSemEntrada, _limiteInatividadeSegundos));
                    _precisaJustificarInatividade = true;

                    SalvarSessaoAtual();
                }
            }
            else if (_precisaJustificarInatividade && solidWorksEmFoco && segundosSemEntrada < _limiteInatividadeSegundos)
            {
                ExibirJanelaJustificativaInatividade(sessaoAtiva);
            }
        }

        private void ExibirJanelaJustificativaInatividade(ProjectSession sessao)
        {
            if (_exibindoJanelaBloqueioGlobal) return;
            _exibindoJanelaBloqueioGlobal = true;
            _precisaJustificarInatividade = false;

            try
            {
                double totalInativoSegundos = (DateTime.Now - _inicioInatividade).TotalSeconds;
                if (sessao.TempoBaseSegundos >= totalInativoSegundos) sessao.TempoBaseSegundos -= totalInativoSegundos;

                var janelaBloqueio = new JustificativaInatividadeWindow(totalInativoSegundos) { Topmost = true };
                try { var helper = new System.Windows.Interop.WindowInteropHelper(janelaBloqueio) { Owner = Process.GetCurrentProcess().MainWindowHandle }; } catch { }

                bool? resultado = janelaBloqueio.ShowDialog();
                _ultimoBloqueioFechadoGlobal = DateTime.Now;

                if (resultado == true)
                {
                    string justificativaOcio = janelaBloqueio.JustificativaInformada;
                    if (!string.IsNullOrEmpty(Justificativa)) Justificativa += $" | [Pausa Ociosa: {justificativaOcio}]";
                    else Justificativa = $"[Pausa Ociosa: {justificativaOcio}]";

                    sessao.Stopwatch.Start();
                    sessao.EmPausa = false;
                    sessao.Sessao.Status = StatusSessao.EmAndamento;
                    SalvarSessaoAtual();
                }
            }
            finally { _exibindoJanelaBloqueioGlobal = false; }
        }

        private bool VerificarSeJanelaMinimizada(IXDocument doc)
        {
            try
            {
                var swApp = _app?.Sw;
                if (swApp == null) return true;
                if (swApp.FrameState == (int)swWindowState_e.swWindowMinimized) return true;

                if (doc is ISwDocument swDoc && swDoc.Model is ModelDoc2 swModel)
                {
                    var activeView = swModel.ActiveView as ModelView;
                    if (activeView != null && activeView.FrameState == (int)swWindowState_e.swWindowMinimized) return true;
                }
            }
            catch { }
            return false;
        }

        private double ObterSegundosInatividadeSistema()
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
            if (GetLastInputInfo(ref lastInput))
            {
                uint envTicks = (uint)System.Environment.TickCount;
                uint diff = envTicks - lastInput.dwTime;
                return diff / 1000.0;
            }
            return 0;
        }

        private bool VerificarSeSolidWorksEstaEmFoco()
        {
            try
            {
                var swApp = _app?.Sw;
                if (swApp == null) return false;
                if (swApp.FrameState == (int)swWindowState_e.swWindowMinimized) return false;

                IntPtr hwndAtivo = GetForegroundWindow();
                if (hwndAtivo == IntPtr.Zero) return false;

                GetWindowThreadProcessId(hwndAtivo, out uint processIdJanelaAtiva);
                uint processIdSolidWorks = (uint)Process.GetCurrentProcess().Id;
                return processIdJanelaAtiva == processIdSolidWorks;
            }
            catch { return false; }
        }

        // [Métodos DeterminarCodigoProjetoPai, VerificarSePertenceAMontagem, ContemComponenteRecursivo, ExtrairCodigoPimod mantidos...]
        // Para não alongar demais, mantenha os originais da sua versão anterior aqui.
        private string DeterminarCodigoProjetoPai(IXDocument docAtivo)
        {
            if (docAtivo == null) return "AVULSO";
            string codigoProprio = ExtrairCodigoPimod(docAtivo.Title);
            if (!string.IsNullOrEmpty(codigoProprio)) return codigoProprio;

            // (Sua lógica de verificar montagens e histórico anterior...)
            try
            {
                var sessoesAnteriores = _repository.CarregarTodasSessoes(docAtivo.Title);
                var ultimaSessao = sessoesAnteriores.Where(s => !string.IsNullOrEmpty(s.CodigoProjetoPai) && s.CodigoProjetoPai != "AVULSO").OrderByDescending(s => s.UltimoPingAtividade).FirstOrDefault();
                if (ultimaSessao != null) return ultimaSessao.CodigoProjetoPai;
            }
            catch { }
            return "AVULSO";
        }
        private string ExtrairCodigoPimod(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return null;
            if (texto.Contains("PIMOD")) { int index = texto.IndexOf("PIMOD"); if (index >= 0 && texto.Length >= index + 9) return texto.Substring(index, 9); }
            return null;
        }

        private ProjectSession ObterOuCriarSessao(string nomeProjeto)
        {
            if (string.IsNullOrEmpty(nomeProjeto)) return null;
            if (!_sessoes.TryGetValue(nomeProjeto, out var sessao))
            {
                var ultimasSessoes = _repository.CarregarTodasSessoes(nomeProjeto);
                var sessaoExistente = ultimasSessoes.OrderByDescending(s => s.HoraInicio).FirstOrDefault(s => s.Status != StatusSessao.Finalizada);

                sessao = new ProjectSession
                {
                    EtapaSelecionada = sessaoExistente?.Etapa ?? EtapasDisponiveis.FirstOrDefault(),
                    Justificativa = sessaoExistente?.JustificativaFinal ?? string.Empty,
                    TempoBaseSegundos = sessaoExistente?.TempoAcumuladoSegundos ?? 0,
                    Sessao = sessaoExistente ?? new SessaoTrabalho { NomeDocumento = nomeProjeto, CodigoProjetoPai = CodigoProjetoPai, HoraInicio = DateTime.Now, Status = StatusSessao.EmAndamento }
                };
                if (sessao.Sessao.Status == StatusSessao.EmAndamento) sessao.Stopwatch.Start();
                _sessoes[nomeProjeto] = sessao;
            }
            return sessao;
        }

        private void AtualizarProjetoAtivo() { var doc = _app?.Documents.Active; OnDocumentActivated(doc); }

        private void AtualizarTempoTela()
        {
            if (!string.IsNullOrEmpty(NomeProjetoAtivo) && NomeProjetoAtivo != "Nenhum arquivo aberto" && _sessoes.TryGetValue(NomeProjetoAtivo, out var sessao))
            {
                double totalSegundosItem = sessao.TempoBaseSegundos + sessao.Stopwatch.Elapsed.TotalSeconds;
                TempoFormatado = TimeSpan.FromSeconds(totalSegundosItem).ToString(@"hh\:mm\:ss");
                sessao.Sessao.UltimoPingAtividade = DateTime.Now;
                RecalcularTempoTotalProjeto();
            }
            else TempoFormatado = "00:00:00";
        }

        // === AQUI ESTÁ A CORREÇÃO DE PERFORMANCE ===
        private void RecalcularTempoTotalProjeto()
        {
            if (string.IsNullOrEmpty(CodigoProjetoPai) || CodigoProjetoPai == "AVULSO")
            {
                TempoTotalProjetoFormatado = TempoFormatado;
                return;
            }

            // Só consulta o banco se o projeto mudou (Cache em memória)
            if (_ultimoProjetoCalculado != CodigoProjetoPai)
            {
                var sessoesDoProjeto = _repository.CarregarSessoesPorProjetoPai(CodigoProjetoPai);
                _totalProjetoSegundosBase = 0;

                foreach (var s in sessoesDoProjeto)
                {
                    // Ignora a sessão atual em memória (para não duplicar)
                    if (!string.IsNullOrEmpty(NomeProjetoAtivo) && s.NomeDocumento == NomeProjetoAtivo && s.Status != StatusSessao.Finalizada) continue;
                    _totalProjetoSegundosBase += s.TempoAcumuladoSegundos;
                }
                _ultimoProjetoCalculado = CodigoProjetoPai;
            }

            // Adiciona o tempo da sessão ativa que está em memória
            double tempoSessaoAtual = 0;
            if (!string.IsNullOrEmpty(NomeProjetoAtivo) && NomeProjetoAtivo != "Nenhum arquivo aberto" && _sessoes.TryGetValue(NomeProjetoAtivo, out var sessaoAtual))
            {
                tempoSessaoAtual = sessaoAtual.TempoBaseSegundos + sessaoAtual.Stopwatch.Elapsed.TotalSeconds;
            }

            TempoTotalProjetoFormatado = TimeSpan.FromSeconds(_totalProjetoSegundosBase + tempoSessaoAtual).ToString(@"hh\:mm\:ss");
        }

        private void CarregarHistoricoRecente() { HistoricoRecente.Clear(); foreach (var item in _repository.CarregarHistoricoRecente(10)) HistoricoRecente.Add(item); }

        private void FinalizarProjeto()
        {
            if (string.IsNullOrEmpty(NomeProjetoAtivo) || NomeProjetoAtivo == "Nenhum arquivo aberto" || !_sessoes.TryGetValue(NomeProjetoAtivo, out var sessao)) return;
            if (MessageBox.Show($"Deseja marcar o item '{NomeProjetoAtivo}' como FINALIZADO?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                sessao.Stopwatch.Stop();
                sessao.Sessao.HoraFim = DateTime.Now;
                sessao.Sessao.Status = StatusSessao.Finalizada;
                sessao.Sessao.Etapa = EtapaSelecionada;
                sessao.Sessao.CodigoProjetoPai = CodigoProjetoPai;
                sessao.Sessao.TempoAcumuladoSegundos = sessao.TempoBaseSegundos + sessao.Stopwatch.Elapsed.TotalSeconds;
                sessao.Sessao.JustificativaFinal = Justificativa;
                _repository.SalvarSessaoAtomico(sessao.Sessao);
                CarregarHistoricoRecente();
                _ultimoProjetoCalculado = ""; // Força recalcular o total do projeto na próxima
                MessageBox.Show("Sessão finalizada!", "Concluído", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void SalvarSessaoAtual()
        {
            if (!string.IsNullOrEmpty(NomeProjetoAtivo) && NomeProjetoAtivo != "Nenhum arquivo aberto" && _sessoes.TryGetValue(NomeProjetoAtivo, out var sessao))
            {
                if (sessao.Sessao.Status != StatusSessao.Finalizada)
                {
                    sessao.Sessao.TempoAcumuladoSegundos = sessao.TempoBaseSegundos + sessao.Stopwatch.Elapsed.TotalSeconds;
                    sessao.Sessao.CodigoProjetoPai = CodigoProjetoPai;
                    sessao.Sessao.Etapa = EtapaSelecionada;
                    sessao.Sessao.JustificativaFinal = Justificativa;
                    _repository.SalvarSessaoAtomico(sessao.Sessao);
                    CarregarHistoricoRecente();
                }
            }
        }

        private void ExportarRelatorio()
        {
            SalvarSessaoAtual();
            if (!string.IsNullOrEmpty(NomeProjetoAtivo) && _sessoes.TryGetValue(NomeProjetoAtivo, out var sessaoAtual))
            {
                var exportVM = new ExportViewModel(sessaoAtual);
                var janelaExportacao = new Modules.Views.ExportView { DataContext = exportVM };
                janelaExportacao.ShowDialog();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}