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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Xarial.XCad.Documents;
using Xarial.XCad.SolidWorks;

namespace TRONIX.Modules
{
    public class PresetDetailing
    {
        public string NomePreset { get; set; }
        public string TemplateNome { get; set; }
        public string[] Vistas { get; set; }
        public bool CriarTabela { get; set; }
    }

    public class DetailingViewModel : INotifyPropertyChanged
    {
        private readonly ISwApplication _app;
        private PresetDetailing _presetSelecionado;
        private bool _cotagemAutomatica = true;
        private bool _inserirTabela = true;
        private bool _autoOrganizarCotas = true;
        private string _pastaDestinoExportacao;
        private string _pastaTemplatesServidor = @"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\data\templates";

        public DetailingViewModel()
        {
            try
            {
                _app = SwApplicationFactory.FromProcess(Process.GetCurrentProcess());
                if (_app?.Documents != null)
                {
                    _app.Documents.DocumentActivated += OnDocumentActivated;
                }
            }
            catch { }

            SincronizarPropriedadesCommand = new RelayCommand(SalvarPropriedadesNoModelo);
            SelecionarPastaCommand = new RelayCommand(SelecionarPasta);
            GerarDesenho2DCommand = new RelayCommand(GerarDesenho2D);
            ExportarArquivosCommand = new RelayCommand(ExportarArquivos);
            NovaRevisaoCommand = new RelayCommand(DeslocarHistoricoRevisao);
            

            CarregarDefinicaoPropriedades();

            PresetsDisponiveis = new ObservableCollection<PresetDetailing>
            {
                new PresetDetailing { NomePreset = "1.1 Protótipo - Peça", TemplateNome = "Prototipo_Peca.drwdot", Vistas = new[] { "Front", "Top", "Right", "Isometric" }, CriarTabela = true },
                new PresetDetailing { NomePreset = "1.2 Protótipo - Montagem", TemplateNome = "Prototipo_Montagem.drwdot", Vistas = new[] { "Front", "Top", "Isometric" }, CriarTabela = true },
                new PresetDetailing { NomePreset = "2.1 Definitivo - Peça", TemplateNome = "PEÇA-CHAPA-ALT09.DRWDOT", Vistas = new[] { "Front", "Top", "Right", "Isometric" }, CriarTabela = true },
                new PresetDetailing { NomePreset = "2.2 Definitivo - Montagem", TemplateNome = "Definitivo_Montagem.drwdot", Vistas = new[] { "Front", "Top", "Isometric" }, CriarTabela = true },
                new PresetDetailing { NomePreset = "3.1 Retrabalho - Peça", TemplateNome = "Retrabalho_Peca.drwdot", Vistas = new[] { "Front", "Top", "Right" }, CriarTabela = false },
                new PresetDetailing { NomePreset = "3.2 Retrabalho - Montagem", TemplateNome = "Retrabalho_Montagem.drwdot", Vistas = new[] { "Front", "Isometric" }, CriarTabela = true }
            };

            PresetSelecionado = PresetsDisponiveis.FirstOrDefault();

            CarregarPropriedadesDoModelo();
        }

        private void OnDocumentActivated(IXDocument doc) => CarregarPropriedadesDoModelo();

        public ObservableCollection<PropriedadeSW> ListaPropriedades { get; } = new ObservableCollection<PropriedadeSW>();
        public ObservableCollection<PresetDetailing> PresetsDisponiveis { get; }

        public PresetDetailing PresetSelecionado { get => _presetSelecionado; set { _presetSelecionado = value; OnPropertyChanged(); } }
        public bool CotagemAutomatica { get => _cotagemAutomatica; set { _cotagemAutomatica = value; OnPropertyChanged(); } }
        public bool InserirTabela { get => _inserirTabela; set { _inserirTabela = value; OnPropertyChanged(); } }
        public bool AutoOrganizarCotas { get => _autoOrganizarCotas; set { _autoOrganizarCotas = value; OnPropertyChanged(); } }
        public string PastaDestinoExportacao { get => _pastaDestinoExportacao; set { _pastaDestinoExportacao = value; OnPropertyChanged(); } }
        public string PastaTemplatesServidor { get => _pastaTemplatesServidor; set { _pastaTemplatesServidor = value; OnPropertyChanged(); } }

        public ICommand SincronizarPropriedadesCommand { get; }
        public ICommand SelecionarPastaCommand { get; }
        public ICommand GerarDesenho2DCommand { get; }
        public ICommand ExportarArquivosCommand { get; }
        public ICommand NovaRevisaoCommand { get; }
        

        private void CarregarDefinicaoPropriedades()
        {
            ListaPropriedades.Clear();

            var listas = GerenciadorListas.Carregar();

            // BLOCO 1
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Description", TipoControle = TipoControleSW.Texto });

            var propDesenhista = new PropriedadeSW { Nome = "Desenhista", TipoControle = TipoControleSW.Lista };
            AtribuirOpcoes(propDesenhista, listas.Desenhistas);
            ListaPropriedades.Add(propDesenhista);

            var propMaterial = new PropriedadeSW { Nome = "Material", TipoControle = TipoControleSW.Lista };
            AtribuirOpcoes(propMaterial, listas.Materiais);
            ListaPropriedades.Add(propMaterial);

            ListaPropriedades.Add(new PropriedadeSW { Nome = "Matriz de dobra", TipoControle = TipoControleSW.Texto });

            var propRevisor = new PropriedadeSW { Nome = "Mecânica revisada por", TipoControle = TipoControleSW.Lista };
            AtribuirOpcoes(propRevisor, listas.Desenhistas);
            ListaPropriedades.Add(propRevisor);

            ListaPropriedades.Add(new PropriedadeSW { Nome = "Data fabricação", TipoControle = TipoControleSW.Data });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Codigo anterior", TipoControle = TipoControleSW.Texto });

            // BLOCO 2
            var propTratamento = new PropriedadeSW { Nome = "Tratamento", TipoControle = TipoControleSW.Lista };
            AtribuirOpcoes(propTratamento, listas.Pinturas);
            ListaPropriedades.Add(propTratamento);

            var propBanho = new PropriedadeSW { Nome = "BANHO", TipoControle = TipoControleSW.Lista };
            AtribuirOpcoes(propBanho, listas.Banhos);
            ListaPropriedades.Add(propBanho);

            ListaPropriedades.Add(new PropriedadeSW { Nome = "SILK01", TipoControle = TipoControleSW.Check });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "SOLDA01", TipoControle = TipoControleSW.Check });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "IMPRESSORA3D01", TipoControle = TipoControleSW.Check });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "NATURAL01", TipoControle = TipoControleSW.Texto });

            // BLOCO 3 (Revisões)
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Rev_Data_01", TipoControle = TipoControleSW.Data });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Alteracao_01", TipoControle = TipoControleSW.Texto });

            var propAltPor1 = new PropriedadeSW { Nome = "Alt_Por_01", TipoControle = TipoControleSW.Lista };
            AtribuirOpcoes(propAltPor1, listas.Desenhistas);
            ListaPropriedades.Add(propAltPor1);

            ListaPropriedades.Add(new PropriedadeSW { Nome = "Rev_Data_02", TipoControle = TipoControleSW.Data });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Alteracao_02", TipoControle = TipoControleSW.Texto });

            var propAltPor2 = new PropriedadeSW { Nome = "Alt_Por_02", TipoControle = TipoControleSW.Lista };
            AtribuirOpcoes(propAltPor2, listas.Desenhistas);
            ListaPropriedades.Add(propAltPor2);

            ListaPropriedades.Add(new PropriedadeSW { Nome = "Rev_Data_03", TipoControle = TipoControleSW.Data });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Alteracao_03", TipoControle = TipoControleSW.Texto });

            var propAltPor3 = new PropriedadeSW { Nome = "Alt_por_03", TipoControle = TipoControleSW.Lista };
            AtribuirOpcoes(propAltPor3, listas.Desenhistas);
            ListaPropriedades.Add(propAltPor3);

            ListaPropriedades.Add(new PropriedadeSW { Nome = "Ultima Alteração", TipoControle = TipoControleSW.Texto });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Alterado por", TipoControle = TipoControleSW.Texto });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Data alteração", TipoControle = TipoControleSW.Data });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Alteração", TipoControle = TipoControleSW.Texto });

            // BOTÃO DE REVISÃO
            ListaPropriedades.Add(new PropriedadeSW
            {
                Nome = "📜 Adicionar novas revisões",
                TipoControle = TipoControleSW.BotaoRevisao,
                BotaoCommand = NovaRevisaoCommand
            });

            // BLOCO 4 (IT)
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Description IT", TipoControle = TipoControleSW.Texto });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Projetista IT", TipoControle = TipoControleSW.Texto });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Data de criação IT", TipoControle = TipoControleSW.Data });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Alteração IT", TipoControle = TipoControleSW.Texto });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "IT ultima alteração", TipoControle = TipoControleSW.Texto });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "IT Alterado por", TipoControle = TipoControleSW.Texto });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Data alteração IT", TipoControle = TipoControleSW.Data });

            // BLOCO 5
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Revisão da embalagem", TipoControle = TipoControleSW.Texto });
            ListaPropriedades.Add(new PropriedadeSW { Nome = "Codigo Embalagem", TipoControle = TipoControleSW.Texto });
        }

        private void AtribuirOpcoes(PropriedadeSW prop, List<string> itens)
        {
            prop.Opcoes.Clear();
            prop.Opcoes.Add("");
            if (itens != null)
            {
                foreach (var item in itens)
                {
                    if (!string.IsNullOrWhiteSpace(item) && !prop.Opcoes.Contains(item.Trim()))
                    {
                        prop.Opcoes.Add(item.Trim());
                    }
                }
            }
        }

        private void CarregarPropriedadesDoModelo()
        {
            var swApp = (SldWorks)_app?.Sw;
            if (swApp == null || !(swApp.ActiveDoc is ModelDoc2 swModelAtivo)) return;

            ModelDoc2 swModelAlvo = swModelAtivo;
            if (swModelAtivo.GetType() == (int)swDocumentTypes_e.swDocDRAWING)
            {
                DrawingDoc swDraw = (DrawingDoc)swModelAtivo;
                View swViewPrimeira = (View)((View)swDraw.GetFirstView())?.GetNextView();
                if (swViewPrimeira?.ReferencedDocument != null)
                    swModelAlvo = (ModelDoc2)swViewPrimeira.ReferencedDocument;
            }

            CarregarDefinicaoPropriedades();

            CustomPropertyManager cusPropGlobal = swModelAlvo.Extension.CustomPropertyManager[""];
            string configAtiva = string.Empty;
            try
            {
                configAtiva = swModelAlvo.ConfigurationManager?.ActiveConfiguration?.Name ?? string.Empty;
            }
            catch { }

            CustomPropertyManager cusPropConfig = !string.IsNullOrEmpty(configAtiva)
                ? swModelAlvo.Extension.CustomPropertyManager[configAtiva]
                : null;

            string LerValorPropriedade(string nomeProp)
            {
                string val = string.Empty;
                if (cusPropGlobal != null)
                {
                    cusPropGlobal.Get4(nomeProp, false, out string valOut, out string resOut);
                    val = !string.IsNullOrEmpty(resOut) ? resOut : valOut;
                }

                if (string.IsNullOrEmpty(val) && cusPropConfig != null)
                {
                    cusPropConfig.Get4(nomeProp, false, out string valOut, out string resOut);
                    val = !string.IsNullOrEmpty(resOut) ? resOut : valOut;
                }

                return val ?? string.Empty;
            }

            foreach (var prop in ListaPropriedades)
            {
                if (prop.TipoControle == TipoControleSW.BotaoRevisao) continue;

                prop.Valor = LerValorPropriedade(prop.Nome);

                if (prop.Nome.Equals("Description", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(prop.Valor))
                {
                    string desc = FormatarDescricaoDoNomeArquivo(swModelAlvo.GetPathName() ?? swModelAlvo.GetTitle());
                    if (!string.IsNullOrEmpty(desc))
                    {
                        prop.Valor = desc;
                    }
                }
            }

            HashSet<string> nomesExistentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (cusPropGlobal != null)
            {
                object namesObj = cusPropGlobal.GetNames();
                if (namesObj is string[] namesArray)
                {
                    foreach (var n in namesArray) if (!string.IsNullOrWhiteSpace(n)) nomesExistentes.Add(n);
                }
            }

            if (cusPropConfig != null)
            {
                object namesObj = cusPropConfig.GetNames();
                if (namesObj is string[] namesArray)
                {
                    foreach (var n in namesArray) if (!string.IsNullOrWhiteSpace(n)) nomesExistentes.Add(n);
                }
            }

            foreach (var nomeExtra in nomesExistentes)
            {
                if (nomeExtra.Equals("📜 Adicionar novas revisões", StringComparison.OrdinalIgnoreCase)) continue;

                bool jaExiste = ListaPropriedades.Any(p => p.Nome.Equals(nomeExtra, StringComparison.OrdinalIgnoreCase));
                if (!jaExiste)
                {
                    string valExtra = LerValorPropriedade(nomeExtra);
                    ListaPropriedades.Add(new PropriedadeSW
                    {
                        Nome = nomeExtra,
                        Valor = valExtra,
                        TipoControle = TipoControleSW.Texto
                    });
                }
            }

            if (string.IsNullOrEmpty(PastaDestinoExportacao) && !string.IsNullOrEmpty(swModelAlvo.GetPathName()))
                PastaDestinoExportacao = Path.GetDirectoryName(swModelAlvo.GetPathName());
        }

        private void SalvarPropriedadesNoModelo()
        {
            var swApp = (SldWorks)_app?.Sw;
            if (swApp == null || !(swApp.ActiveDoc is ModelDoc2 swModelAtivo)) return;

            ModelDoc2 swModelAlvo = swModelAtivo;
            if (swModelAtivo.GetType() == (int)swDocumentTypes_e.swDocDRAWING)
            {
                DrawingDoc swDraw = (DrawingDoc)swModelAtivo;
                View swViewPrimeira = (View)((View)swDraw.GetFirstView())?.GetNextView();
                if (swViewPrimeira?.ReferencedDocument != null)
                    swModelAlvo = (ModelDoc2)swViewPrimeira.ReferencedDocument;
            }

            var dadosListas = GerenciadorListas.Carregar();
            bool listaModificada = false;

            void ChecarENovoItem(string nomeProp, List<string> destino)
            {
                var p = ListaPropriedades.FirstOrDefault(x => x.Nome.Equals(nomeProp, StringComparison.OrdinalIgnoreCase));
                if (p != null && !string.IsNullOrWhiteSpace(p.Valor) && !destino.Contains(p.Valor.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    destino.Add(p.Valor.Trim());
                    listaModificada = true;
                }
            }

            ChecarENovoItem("Desenhista", dadosListas.Desenhistas);
            ChecarENovoItem("Material", dadosListas.Materiais);
            ChecarENovoItem("Tratamento", dadosListas.Pinturas);
            ChecarENovoItem("BANHO", dadosListas.Banhos);

            if (listaModificada)
            {
                GerenciadorListas.Salvar(dadosListas);
            }

            CustomPropertyManager cusProp = swModelAlvo.Extension.CustomPropertyManager[""];
            if (cusProp != null)
            {
                cusProp.Delete("📜 Adicionar novas revisões");

                var propRevisor = ListaPropriedades.FirstOrDefault(p => p.Nome.Equals("Mecânica revisada por", StringComparison.OrdinalIgnoreCase));
                if (propRevisor != null && !string.IsNullOrWhiteSpace(propRevisor.Valor) && !propRevisor.Valor.Contains("->"))
                    propRevisor.Valor = $"{propRevisor.Valor.Trim()} -> {DateTime.Now:dd/MM/yyyy}";

                var propBanho = ListaPropriedades.FirstOrDefault(p => p.Nome.Equals("BANHO", StringComparison.OrdinalIgnoreCase));
                var propPintura = ListaPropriedades.FirstOrDefault(p => p.Nome.Equals("PINTURA", StringComparison.OrdinalIgnoreCase));
                var propNatural = ListaPropriedades.FirstOrDefault(p => p.Nome.Equals("NATURAL01", StringComparison.OrdinalIgnoreCase));

                if (propNatural != null)
                {
                    bool semBanho = string.IsNullOrWhiteSpace(propBanho?.Valor);
                    bool semPintura = string.IsNullOrWhiteSpace(propPintura?.Valor);
                    propNatural.Valor = (semBanho && semPintura) ? "NATURAL" : string.Empty;
                }

                foreach (var prop in ListaPropriedades)
                {
                    if (prop.TipoControle == TipoControleSW.BotaoRevisao) continue;

                    // GRAVA APENAS PROPRIEDADES COM CONTEÚDO
                    if (!string.IsNullOrWhiteSpace(prop.Valor))
                    {
                        cusProp.Add3(prop.Nome, (int)swCustomInfoType_e.swCustomInfoText, prop.Valor.Trim(), (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
                    }
                    else
                    {
                        // DELETA DO SOLIDWORKS CASO O CAMPO ESTEJA EM BRANCO (EVITA LINHAS VAZIAS NA TABELA 3D)
                        cusProp.Delete(prop.Nome);
                    }
                }

                swModelAtivo.EditRebuild3();
                if (swModelAlvo != swModelAtivo)
                {
                    swModelAlvo.EditRebuild3();
                }

                swModelAlvo.SetSaveFlag();
                if (swModelAlvo != swModelAtivo)
                {
                    swModelAtivo.SetSaveFlag();
                }

                MessageBox.Show("Propriedades salvas com sucesso no modelo!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeslocarHistoricoRevisao()
        {
            var pRev1 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Rev_Data_01");
            var pAlt1 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Alteracao_01");
            var pPor1 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Alt_Por_01");

            var pRev2 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Rev_Data_02");
            var pAlt2 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Alteracao_02");
            var pPor2 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Alt_Por_02");

            var pRev3 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Rev_Data_03");
            var pAlt3 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Alteracao_03");
            var pPor3 = ListaPropriedades.FirstOrDefault(x => x.Nome == "Alt_por_03");

            if (string.IsNullOrWhiteSpace(pRev1?.Valor) || string.IsNullOrWhiteSpace(pAlt1?.Valor) || string.IsNullOrWhiteSpace(pPor1?.Valor) ||
                string.IsNullOrWhiteSpace(pRev2?.Valor) || string.IsNullOrWhiteSpace(pAlt2?.Valor) || string.IsNullOrWhiteSpace(pPor2?.Valor) ||
                string.IsNullOrWhiteSpace(pRev3?.Valor) || string.IsNullOrWhiteSpace(pAlt3?.Valor) || string.IsNullOrWhiteSpace(pPor3?.Valor))
            {
                MessageBox.Show(
                    "Não foi possível adicionar uma nova revisão.\n\nTodos os campos das revisões (01, 02 e 03) devem estar preenchidos para realizar o deslocamento de histórico.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var swApp = (SldWorks)_app?.Sw;
            if (swApp != null && swApp.ActiveDoc is ModelDoc2 swModelAtivo)
            {
                ModelDoc2 swModelAlvo = swModelAtivo;

                if (swModelAtivo.GetType() == (int)swDocumentTypes_e.swDocDRAWING)
                {
                    DrawingDoc swDraw = (DrawingDoc)swModelAtivo;
                    View swViewSheet = (View)swDraw.GetFirstView();
                    View swViewPrimeira = (View)swViewSheet?.GetNextView();
                    if (swViewPrimeira != null && swViewPrimeira.ReferencedDocument != null)
                    {
                        swModelAlvo = (ModelDoc2)swViewPrimeira.ReferencedDocument;
                    }
                }

                CustomPropertyManager cusProp = swModelAlvo.Extension.CustomPropertyManager[""];
                if (cusProp != null)
                {
                    int indiceHistorico = 1;
                    while (true)
                    {
                        string suf = indiceHistorico.ToString("D2");
                        cusProp.Get4("Rev_Data_01-" + suf, false, out string valExiste, out _);
                        if (string.IsNullOrEmpty(valExiste))
                        {
                            break;
                        }
                        indiceHistorico++;
                    }

                    string sufixo = indiceHistorico.ToString("D2");

                    cusProp.Add3("Rev_Data_01-" + sufixo, (int)swCustomInfoType_e.swCustomInfoText, pRev1.Valor, (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
                    cusProp.Add3("Alteracao_01-" + sufixo, (int)swCustomInfoType_e.swCustomInfoText, pAlt1.Valor, (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
                    cusProp.Add3("Alt_Por_01-" + sufixo, (int)swCustomInfoType_e.swCustomInfoText, pPor1.Valor, (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
                }
            }

            pRev1.Valor = pRev2.Valor;
            pAlt1.Valor = pAlt2.Valor;
            pPor1.Valor = pPor2.Valor;

            pRev2.Valor = pRev3.Valor;
            pAlt2.Valor = pAlt3.Valor;
            pPor2.Valor = pPor3.Valor;

            pRev3.Valor = DateTime.Now.ToString("dd/MM/yyyy");
            pAlt3.Valor = string.Empty;
            pPor3.Valor = string.Empty;

            SalvarPropriedadesNoModelo();

            MessageBox.Show(
                "Histórico de revisões deslocado com sucesso!\n\n" +
                "• A Revisão 01 anterior foi arquivada nas propriedades do modelo.\n" +
                "• Os dados foram movidos (01 ← 02 ← 03).\n" +
                "• O bloco da Revisão 03 está pronto para novas informações.",
                "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string FormatarDescricaoDoNomeArquivo(string nomeArquivo)
        {
            if (string.IsNullOrEmpty(nomeArquivo)) return string.Empty;

            string nomeSemExt = Path.GetFileNameWithoutExtension(nomeArquivo);
            string[] partes = nomeSemExt.Split('-');
            foreach (var parte in partes)
            {
                var match = Regex.Match(parte.Trim(), @"^([A-Za-z]{2})([A-Za-z]{3})(\d+)$");
                if (match.Success)
                {
                    return $"{match.Groups[1].Value.ToUpper()}.{match.Groups[2].Value.ToUpper()}.{match.Groups[3].Value}";
                }
            }
            return string.Empty;
        }

        private void SelecionarPasta()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) PastaDestinoExportacao = dialog.SelectedPath;
            }
        }

        private void MarcarTodasDimensoesParaDesenho(ModelDoc2 swModel)
        {
            Feature swFeat = null;
            DisplayDimension swDispDim = null;
            try
            {
                swFeat = swModel.FirstFeature() as Feature;
                while (swFeat != null)
                {
                    swDispDim = swFeat.GetFirstDisplayDimension() as DisplayDimension;
                    while (swDispDim != null)
                    {
                        swDispDim.MarkedForDrawing = true;
                        var nextDim = swFeat.GetNextDisplayDimension(swDispDim) as DisplayDimension;
                        Marshal.ReleaseComObject(swDispDim);
                        swDispDim = nextDim;
                    }
                    var nextFeat = swFeat.GetNextFeature() as Feature;
                    Marshal.ReleaseComObject(swFeat);
                    swFeat = nextFeat;
                }
            }
            finally
            {
                if (swDispDim != null) Marshal.ReleaseComObject(swDispDim);
                if (swFeat != null) Marshal.ReleaseComObject(swFeat);
            }
        }

        private void GerarDesenho2D()
        {
            SalvarPropriedadesNoModelo();

            var swApp = (SldWorks)_app?.Sw;
            if (swApp == null || !(swApp.ActiveDoc is ModelDoc2 swModel)) return;
            string cMod = swModel.GetPathName();
            if (string.IsNullOrEmpty(cMod)) return;

            string cTemp = Path.Combine(PastaTemplatesServidor, PresetSelecionado?.TemplateNome ?? "");
            if (!File.Exists(cTemp)) return;

            if (CotagemAutomatica) MarcarTodasDimensoesParaDesenho(swModel);

            DrawingDoc swDraw = swApp.NewDocument(cTemp, (int)swDwgPaperSizes_e.swDwgPaperA3size, 0, 0) as DrawingDoc;
            if (swDraw == null) return;

            ModelDoc2 swDrawModel = (ModelDoc2)swDraw;
            string[] folhas = (string[])swDraw.GetSheetNames();
            if (folhas.Length >= 1)
            {
                swDraw.ActivateSheet(folhas[0]);
                swDraw.Create1stAngleViews2(cMod);
                View vIso = swDraw.CreateDrawViewFromModelView3(cMod, "*Isométrica", 0.33, 0.08, 0);
                if (vIso != null) vIso.UseSheetScale = 1;
                swDrawModel.EditRebuild3();
            }

            int err = 0, warn = 0;
            swDrawModel.Extension.SaveAs3(Path.ChangeExtension(cMod, ".slddrw"), (int)swSaveAsVersion_e.swSaveAsCurrentVersion, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref err, ref warn);
        }

        private void ExportarArquivos()
        {
            if (string.IsNullOrEmpty(PastaDestinoExportacao)) return;
            var swApp = (SldWorks)_app?.Sw;
            if (swApp == null || !(swApp.ActiveDoc is DrawingDoc swDraw)) return;

            ModelDoc2 swDrawModel = (ModelDoc2)swDraw;
            ExportPdfData pdf = swApp.GetExportFileData((int)swExportDataFileType_e.swExportPdfData) as ExportPdfData;
            string pathPdf = Path.Combine(PastaDestinoExportacao, $"{Path.GetFileNameWithoutExtension(swDrawModel.GetPathName())}.pdf");

            int err = 0, warn = 0;
            swDrawModel.Extension.SaveAs(pathPdf, (int)swSaveAsVersion_e.swSaveAsCurrentVersion, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, pdf, ref err, ref warn);
        }

        

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}