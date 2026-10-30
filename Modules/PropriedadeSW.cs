using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TRONIX.Modules
{
    public enum TipoControleSW
    {
        Texto,
        Lista,
        Check,
        Data,
        BotaoRevisao // Renderiza o botão no meio da lista
    }

    public class PropriedadeSW : INotifyPropertyChanged
    {
        private string _nome;
        private string _valor = string.Empty;
        private TipoControleSW _tipoControle = TipoControleSW.Texto;
        private bool _isEditable = true;

        public string Nome
        {
            get => _nome;
            set { _nome = value; OnPropertyChanged(); }
        }

        public string Valor
        {
            get => _valor;
            set
            {
                _valor = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChecked));
                OnPropertyChanged(nameof(ValorData));
            }
        }

        // Conversão bidirecional segura para o DatePicker (salva como string no SolidWorks)
        public DateTime? ValorData
        {
            get
            {
                if (DateTime.TryParseExact(_valor, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dt) ||
                    DateTime.TryParse(_valor, out dt))
                    return dt;
                return null;
            }
            set
            {
                _valor = value?.ToString("dd/MM/yyyy") ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Valor));
            }
        }

        public bool IsChecked
        {
            get => string.Equals(_valor, "X", StringComparison.OrdinalIgnoreCase);
            set { Valor = value ? "X" : string.Empty; OnPropertyChanged(); }
        }

        public TipoControleSW TipoControle
        {
            get => _tipoControle;
            set
            {
                _tipoControle = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLista));
                OnPropertyChanged(nameof(IsCheck));
                OnPropertyChanged(nameof(IsTexto));
                OnPropertyChanged(nameof(IsData));
                OnPropertyChanged(nameof(IsBotao));
            }
        }

        public bool IsLista => TipoControle == TipoControleSW.Lista;
        public bool IsCheck => TipoControle == TipoControleSW.Check;
        public bool IsTexto => TipoControle == TipoControleSW.Texto;
        public bool IsData => TipoControle == TipoControleSW.Data;
        public bool IsBotao => TipoControle == TipoControleSW.BotaoRevisao;

        public bool IsEditable { get => _isEditable; set { _isEditable = value; OnPropertyChanged(); } }
        public ObservableCollection<string> Opcoes { get; set; } = new ObservableCollection<string>();
        public ICommand BotaoCommand { get; set; } // Comando para o botão inline

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}