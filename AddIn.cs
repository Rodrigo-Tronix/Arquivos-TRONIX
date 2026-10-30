using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using TRONIX.Modules;
using Xarial.XCad.Base.Attributes;
using Xarial.XCad.SolidWorks;
using Xarial.XCad.SolidWorks.UI;
using Xarial.XCad.UI;

namespace TRONIX
{
    [Guid("A5F4B921-1234-4C2E-89AB-9876543210AB")]
    [ComVisible(true)]
    [Title("TRONIX Add-In")]
    [Description("TRONIX SolidWorks Add-In")]
    public class AddIn : SwAddInEx
    {
        private IXTaskPane<TaskPaneView> m_TaskPane;

        public override void OnConnect()
        {
            // Instancia apenas um TaskPane nativo contendo as abas do TRONIX
            m_TaskPane = CreateTaskPane<TaskPaneView>();
        }

        public override void OnDisconnect()
        {
            m_TaskPane = null; // Libera a referência para o SolidWorks fechar corretamente
        }
    }
}