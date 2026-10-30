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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TRONIX.Services;
using TRONIX.ViewModels;
using Xarial.XCad.Documents;
using Xarial.XCad.Documents.Extensions;
using Xarial.XCad.SolidWorks;
using Xarial.XCad.SolidWorks.Documents;

namespace TRONIX.Modules
{
    public class CigamRenameViewModel : INotifyPropertyChanged
    {
        private ISwApplication _app;
        private string _textoFiltro = string.Empty;

        public CigamRenameViewModel()
        {
            ConectarApplication();

            ItensEstrutura = new ObservableCollection<CigamRenameItem>();
            ItensFiltrados = CollectionViewSource.GetDefaultView(ItensEstrutura);
            ItensFiltrados.Filter = FiltrarItens;

            LerEstruturaCommand = new RelayCommand(CarregarEstruturaMontagem);
            ExecutarRenomeacaoCommand = new RelayCommand(ExecutarRenomeacao);

            CarregarEstruturaMontagem();
        }

        private void ConectarApplication()
        {
            try
            {
                _app = SwApplicationFactory.FromProcess(Process.GetCurrentProcess());
            }
            catch { }
        }

        public ObservableCollection<CigamRenameItem> ItensEstrutura { get; private set; }
        public ICollectionView ItensFiltrados { get; private set; }

        public string TextoFiltro
        {
            get => _textoFiltro;
            set
            {
                _textoFiltro = value;
                OnPropertyChanged();
                ItensFiltrados.Refresh();
            }
        }

        public ICommand LerEstruturaCommand { get; private set; }
        public ICommand ExecutarRenomeacaoCommand { get; private set; }

        private bool FiltrarItens(object obj)
        {
            if (string.IsNullOrWhiteSpace(TextoFiltro))
                return true;

            if (obj is CigamRenameItem item)
            {
                string filtro = TextoFiltro.Trim();
                return (!string.IsNullOrEmpty(item.NomeAtual) && item.NomeAtual.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (!string.IsNullOrEmpty(item.NovoNome) && item.NovoNome.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       item.TipoDoc.ToString().IndexOf(filtro, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return false;
        }

        private void CarregarEstruturaMontagem()
        {
            ItensEstrutura.Clear();
            ConectarApplication();

            var docAtivo = _app?.Documents?.Active;

            // 1. Se for uma MONTAGEM (.sldasm)
            if (docAtivo is ISwAssembly swAssembly)
            {
                // Obtém a configuração e o componente raiz uma única vez
                Configuration configAtiva = (Configuration)swAssembly.Model.GetActiveConfiguration();
                Component2 rootComp = configAtiva?.GetRootComponent3(true);

                // 💡 SOLUÇÃO 1: Força o modo Peso Leve (Lightweight) na montagem ativa
                if (rootComp != null)
                {
                    try
                    {
                        ForcarPesoLeveRecursivo(rootComp);
                    }
                    catch { }
                }

                AdicionarItemSeNovo(
                    Path.GetFileNameWithoutExtension(swAssembly.Title),
                    swAssembly.Path,
                    TipoDocumento.Montagem,
                    0
                );

                if (rootComp != null)
                {
                    VarrerComponenteNativoRecursivo(rootComp, 1);
                }

                ItensFiltrados?.Refresh();
            }
            // 2. Se for uma PEÇA AVULSA (.sldprt)
            else if (docAtivo is ISwDocument swDoc)
            {
                AdicionarItemSeNovo(
                    Path.GetFileNameWithoutExtension(swDoc.Title),
                    swDoc.Path,
                    TipoDocumento.Peca,
                    0
                );

                ItensFiltrados?.Refresh();
            }
            else
            {
                MessageBox.Show("Abra um modelo (.sldprt ou .sldasm) no SolidWorks para carregar a estrutura.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método auxiliar para aplicar Lightweight recursivamente nos componentes
        private void ForcarPesoLeveRecursivo(Component2 compPai)
        {
            if (compPai == null) return;
            object[] children = (object[])compPai.GetChildren();
            if (children == null) return;

            foreach (object childObj in children)
            {
                Component2 comp = (Component2)childObj;
                if (comp != null && !comp.IsSuppressed())
                {
                    if (comp.GetSuppression() == (int)swComponentSuppressionState_e.swComponentFullyResolved)
                    {
                        comp.SetSuppression2((int)swComponentSuppressionState_e.swComponentLightweight);
                    }
                    ForcarPesoLeveRecursivo(comp);
                }
            }
        }

        private void VarrerComponenteNativoRecursivo(Component2 compPai, int nivelAtual)
        {
            if (compPai == null) return;

            object[] children = (object[])compPai.GetChildren();
            if (children == null) return;

            foreach (object childObj in children)
            {
                Component2 comp = (Component2)childObj;
                if (comp == null || comp.IsSuppressed()) continue;

                // CORREÇÃO AQUI: O cast correto é para ModelDoc2, e não Component2
                ModelDoc2 modelDoc = (ModelDoc2)comp.GetModelDoc2();
                string caminho = comp.GetPathName();
                string nomeDoc;

                if (modelDoc != null)
                {
                    nomeDoc = Path.GetFileNameWithoutExtension(modelDoc.GetTitle());
                    caminho = modelDoc.GetPathName();
                }
                else if (!string.IsNullOrEmpty(caminho))
                {
                    nomeDoc = Path.GetFileNameWithoutExtension(caminho);
                }
                else
                {
                    nomeDoc = comp.Name2;
                    if (nomeDoc.Contains("/"))
                    {
                        nomeDoc = nomeDoc.Split('/')[0];
                    }
                }

                bool isSubAssembly = (!string.IsNullOrEmpty(caminho) && caminho.EndsWith(".sldasm", StringComparison.OrdinalIgnoreCase)) ||
                                     (modelDoc != null && modelDoc.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY);

                TipoDocumento tipo = isSubAssembly ? TipoDocumento.Montagem : TipoDocumento.Peca;

                AdicionarItemSeNovo(nomeDoc, caminho, tipo, nivelAtual);

                if (isSubAssembly)
                {
                    VarrerComponenteNativoRecursivo(comp, nivelAtual + 1);
                }
            }
        }

        private void AdicionarItemSeNovo(string nome, string caminho, TipoDocumento tipo, int nivel)
        {
            if (string.IsNullOrEmpty(nome)) return;

            if (nome.Contains("-"))
            {
                int idx = nome.LastIndexOf('-');
                if (idx > 0 && idx < nome.Length - 1 && char.IsDigit(nome[idx + 1]))
                {
                    string resto = nome.Substring(idx + 1);
                    if (int.TryParse(resto, out _))
                    {
                        nome = nome.Substring(0, idx);
                    }
                }
            }

            if (!ItensEstrutura.Any(i => i.NomeAtual.Equals(nome, StringComparison.OrdinalIgnoreCase)))
            {
                ItensEstrutura.Add(new CigamRenameItem
                {
                    NomeAtual = nome,
                    CaminhoAtual = caminho,
                    TipoDoc = tipo,
                    NovoNome = nome,
                    Nivel = nivel
                });
            }
        }

        // ... (using existentes)

        private async void ExecutarRenomeacao()
        {
            var itensParaProcessar = ItensEstrutura.Where(i => i.Processar && !string.IsNullOrEmpty(i.NovoNome) && i.NovoNome != i.NomeAtual).ToList();

            if (!itensParaProcessar.Any())
            {
                MessageBox.Show("Nenhum item alterado para processar.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var swApp = (SldWorks)_app?.Sw;
            if (swApp == null) return;

            // 1. Validação de duplicatas (mantida como estava)
            var pastasProjeto = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var docAtivo = _app.Documents.Active;
            // ... (código de validação de duplicatas permanece igual) ...
            if (duplicados.Any())
            {
                // ... (exibição de erro permanece igual) ...
                return;
            }

            // 2. Desabilita a atualização gráfica para acelerar
            ModelDoc2 swModel = (ModelDoc2)docAtivo?.Model;
            ModelView swView = (ModelView)swModel?.ActiveView;
            if (swView != null) swView.EnableGraphicsUpdate = false;

            var repository = new TimeTrackerRepository();

            try
            {
                // 3. Executa a tarefa em um STA dedicado
                using (var staScheduler = new StaTaskScheduler(1))
                {
                    await Task.Factory.StartNew(() =>
                    {
                        foreach (var item in itensParaProcessar)
                        {
                            try
                            {
                                var doc = _app.Documents.FirstOrDefault(d => Path.GetFileNameWithoutExtension(d.Title).Equals(item.NomeAtual, StringComparison.OrdinalIgnoreCase));

                                if (doc == null && !string.IsNullOrEmpty(item.CaminhoAtual) && File.Exists(item.CaminhoAtual))
                                {
                                    doc = _app.Documents.Open(item.CaminhoAtual);
                                }

                                if (doc != null)
                                {
                                    if (doc is ISwDocument swDoc)
                                    {
                                        swDoc.Model.Extension.CustomPropertyManager[""].Add3("Código CIGAM", 3, item.NovoNome ?? "", 1);
                                    }

                                    if (!string.IsNullOrEmpty(doc.Path))
                                    {
                                        string caminhoAntigo = doc.Path;
                                        string pasta = Path.GetDirectoryName(caminhoAntigo);
                                        string ext = Path.GetExtension(caminhoAntigo);
                                        string novoCaminho = Path.Combine(pasta, item.NovoNome + ext);

                                        if (!caminhoAntigo.Equals(novoCaminho, StringComparison.OrdinalIgnoreCase))
                                        {
                                            doc.SaveAs(novoCaminho);

                                            if (File.Exists(caminhoAntigo) && File.Exists(novoCaminho))
                                            {
                                                try { File.Delete(caminhoAntigo); } catch { }
                                            }
                                        }

                                        repository.MigrarNomeSessao(item.NomeAtual, item.NovoNome);
                                        item.Status = "Concluído";
                                    }
                                }
                            }
                            catch
                            {
                                item.Status = "Erro";
                            }
                        }
                    }, CancellationToken.None, TaskCreationOptions.None, staScheduler);
                }
            }
            finally
            {
                // 4. Restaura a atualização gráfica e força o redesenho
                if (swView != null)
                {
                    swView.EnableGraphicsUpdate = true;
                    swView.GraphicsRedraw(null);
                }
            }

            MessageBox.Show("Processo concluído! Os arquivos foram atualizados e os tempos dos cronômetros foram mantidos.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}