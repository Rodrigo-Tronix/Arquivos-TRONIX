using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xarial.XCad.Documents;
using Xarial.XCad.SolidWorks;

namespace TRONIX.Services
{
    public class TaskPaneViewModel : INotifyPropertyChanged
    {
        private readonly ISwApplication _app;
        private string _nome;
        private string _descricao;
        private string _mensagemStatus;

        public TaskPaneViewModel()
        {
            try
            {
                // Conecta com a instância do SolidWorks em execução
                _app = SwApplicationFactory.FromProcess(Process.GetCurrentProcess());

                // Inscreve no evento correto da v0.8.3: DocumentActivated
                _app.Documents.DocumentActivated += OnDocumentActivated;

                CarregarPropriedadesDoSolidWorks();
            }
            catch (Exception ex)
            {
                MensagemStatus = $"⚠️ Erro ao inicializar conexão: {ex.Message}";
            }
        }

        public string Nome
        {
            get => _nome;
            set { _nome = value; OnPropertyChanged(); }
        }

        public string Descricao
        {
            get => _descricao;
            set { _descricao = value; OnPropertyChanged(); }
        }

        public string MensagemStatus
        {
            get => _mensagemStatus;
            set { _mensagemStatus = value; OnPropertyChanged(); }
        }

        public void CarregarPropriedadesDoSolidWorks()
        {
            var doc = _app?.Documents.Active;
            if (doc == null)
            {
                MensagemStatus = "⚠️ Nenhum documento ativo no SolidWorks.";
                return;
            }

            // Lê as propriedades personalizadas do documento
            Nome = doc.Properties["Nome"]?.Value?.ToString() ?? "";
            Descricao = doc.Properties["Descricao"]?.Value?.ToString() ?? "";
            MensagemStatus = $"🟢 Conectado ao arquivo: {doc.Title}";
        }

        public void SalvarPropriedadesNoSolidWorks()
        {
            var doc = _app?.Documents.Active;
            if (doc == null)
            {
                MensagemStatus = "⚠️ Nenhum documento ativo para salvar.";
                return;
            }

            // Grava/Atualiza as propriedades personalizadas
            doc.Properties["Nome"].Value = Nome;
            doc.Properties["Descricao"].Value = Descricao;

            MensagemStatus = "✅ Propriedades salvas com sucesso!";
        }

        // Utiliza o parâmetro 'doc' para atualizar a interface ao alternar arquivos
        private void OnDocumentActivated(IXDocument doc)
        {
            if (doc != null)
            {
                Nome = doc.Properties["Nome"]?.Value?.ToString() ?? "";
                Descricao = doc.Properties["Descricao"]?.Value?.ToString() ?? "";
                MensagemStatus = $"🟢 Documento ativo alterado: {doc.Title}";
            }
            else
            {
                CarregarPropriedadesDoSolidWorks();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}