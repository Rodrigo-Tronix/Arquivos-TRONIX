// ============================================
// PROPERTYEDITORADDIN.CS - VERSÃO CORRIGIDA 0.8.3
// ============================================

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using TRONIX.Modules;
using Xarial.XCad.Base.Attributes;
using Xarial.XCad.SolidWorks;
using Xarial.XCad.UI;
using Xarial.XCad.UI.TaskPane;

namespace TRONIX.Services
{
    [ComVisible(true)]
    [Guid("A8E89B12-9A4E-4B1B-83C3-890123456789")]
    [Title("Editor de Propriedades")]
    [Description("Automação de Propriedades do Modelo via Desenho")]
    public class PropertyEditorAddIn : SwAddInEx
    {
        private SolidWorksService _solidWorksService;
        private IXTaskPane<TaskPaneView> _taskPane; // ← Tipo correto do xCAD
        private TaskPaneViewModel _viewModel;

        public override void OnConnect()
        {
            try
            {
                base.OnConnect();

                if (Application == null)
                {
                    MessageBox.Show("Erro: Aplicação SolidWorks não inicializada!", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _solidWorksService = new SolidWorksService(Application);
                _viewModel = new TaskPaneViewModel();

                // ← Método correto: CreateTaskPane é chamado a partir do 'this' (SwAddInEx)
                // O xCAD instancia o 'TaskPaneView' e injeta na aba automaticamente.
                _taskPane = this.CreateTaskPane<TaskPaneView>();

                //MessageBox.Show("✅ Add-in carregado com sucesso!\n\n" +
                //                "📝 O TaskPane 'Editor de Propriedades' foi criado.",
                //                "Editor de Propriedades",
                //                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro ao carregar add-in:\n\n{ex.Message}",
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public override void OnDisconnect()
        {
            try
            {
                _taskPane = null;
                _viewModel = null;
                _solidWorksService?.Dispose();
                _solidWorksService = null;
                base.OnDisconnect();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro no OnDisconnect: {ex.Message}");
            }
        }
    }
}