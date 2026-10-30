using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TRONIX.Modules
{
    public enum StatusSessao
    {
        EmAndamento,
        EmPausa,
        Finalizada,
        Interrompida
    }

    public enum TipoDocumento
    {
        Peca,
        Montagem,
        Desenho
    }

    public class ItemPausa
    {
        public DateTime HoraInicio { get; set; } = DateTime.Now;
        public DateTime? HoraFim { get; set; }

        public double DuracaoSegundos => HoraFim.HasValue
            ? (HoraFim.Value - HoraInicio).TotalSeconds
            : (DateTime.Now - HoraInicio).TotalSeconds;
    }

    public class SessaoTrabalho
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Código do Projeto Pai / CIGAM (Ex: PIMODXXXX ou PIMOD1515)
        public string CodigoProjetoPai { get; set; }

        public string NomeDocumento { get; set; }
        public string CaminhoDocumento { get; set; }
        public string Projetista { get; set; } = Environment.UserName;
        public TipoDocumento TipoDoc { get; set; }
        public string Etapa { get; set; }
        public StatusSessao Status { get; set; } = StatusSessao.EmAndamento;

        public DateTime HoraInicio { get; set; } = DateTime.Now;
        public DateTime? HoraFim { get; set; }
        public DateTime UltimoPingAtividade { get; set; } = DateTime.Now;

        // Tempo trabalhado no item específico (líquido em segundos)
        public double TempoAcumuladoSegundos { get; set; }

        // Histórico de pausas
        public List<ItemPausa> Pausas { get; set; } = new List<ItemPausa>();
        public double TempoTotalPausaSegundos => Pausas.Sum(p => p.DuracaoSegundos);

        public string JustificativaFinal { get; set; }

        // Propriedade formatada para exibição no Histórico
        public string TempoFormatadoItem => TimeSpan.FromSeconds(TempoAcumuladoSegundos).ToString(@"hh\:mm\:ss");
    }

    // CLASSE QUE FALTAVA: Gerencia a sessão em memória durante a execução
    public class ProjectSession
    {
        public SessaoTrabalho Sessao { get; set; } = new SessaoTrabalho();
        public Stopwatch Stopwatch { get; set; } = new Stopwatch();
        public double TempoBaseSegundos { get; set; }
        public bool EmPausa { get; set; }
        public string EtapaSelecionada { get; set; }
        public string Justificativa { get; set; }
    }
}