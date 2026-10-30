using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TRONIX.Modules
{
    public class CigamRenameItem : INotifyPropertyChanged
    {
        private string _novoNome;
        private string _novaDescricao;
        private bool _processar = true;

        public string NomeAtual { get; set; }
        public string CaminhoAtual { get; set; }
        public TipoDocumento TipoDoc { get; set; }

        // Nível na hierarquia da árvore (0 = Montagem Pai, 1 = Filho Direto, 2 = Subfilho, etc.)
        public int Nivel { get; set; } = 0;

        // Retorna o nome formatado com recuo/espaçamento de árvore
        public string NomeAtualExibicao
        {
            get
            {
                if (Nivel <= 0) return NomeAtual;
                string recuo = new string(' ', (Nivel - 1) * 4);
                return $"{recuo}└─ {NomeAtual}";
            }
        }

        public string NovoNome
        {
            get => _novoNome;
            set { _novoNome = value; OnPropertyChanged(); }
        }

        public string NovaDescricao
        {
            get => _novaDescricao;
            set { _novaDescricao = value; OnPropertyChanged(); }
        }

        public bool Processar
        {
            get => _processar;
            set { _processar = value; OnPropertyChanged(); }
        }

        public string Status { get; set; } = "Pendente";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}