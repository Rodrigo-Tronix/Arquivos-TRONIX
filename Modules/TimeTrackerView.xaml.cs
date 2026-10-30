using System.Windows.Controls;

namespace TRONIX.Modules
{
    public partial class TimeTrackerView : UserControl
    {
        // Instância estática única para evitar múltiplos ViewModels e timers concorrentes
        private static TimeTrackerViewModel _viewModelInstance;

        public TimeTrackerView()
        {
            InitializeComponent();

            if (_viewModelInstance == null)
            {
                _viewModelInstance = new TimeTrackerViewModel();
            }

            this.DataContext = _viewModelInstance;
        }
    }
}