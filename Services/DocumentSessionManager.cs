using System;
using System.Collections.Generic;
using System.Linq;
using TRONIX.Modules;

namespace TRONIX.Services
{
    public class DocumentSessionManager
    {
        private readonly Dictionary<string, SessaoTrabalho> _sessoesAtivas = new Dictionary<string, SessaoTrabalho>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeTrackerRepository _repository;
        private const int TIMEOUT_INTERRUPCAO_MINUTOS = 5;

        public DocumentSessionManager(TimeTrackerRepository repository)
        {
            _repository = repository;
            VerificarSessoesInterrompidasGeral();
        }

        // Examina arquivos JSON ao iniciar para encontrar sessões que ficaram abertas por queda de energia/crash
        private void VerificarSessoesInterrompidasGeral()
        {
            var sessoes = _repository.CarregarTodasSessoes();
            foreach (var sessao in sessoes)
            {
                if (sessao.Status == StatusSessao.EmAndamento || sessao.Status == StatusSessao.EmPausa)
                {
                    TimeSpan inativo = DateTime.Now - sessao.UltimoPingAtividade;
                    if (inativo.TotalMinutes > TIMEOUT_INTERRUPCAO_MINUTOS)
                    {
                        sessao.Status = StatusSessao.Interrompida;
                        sessao.HoraFim = sessao.UltimoPingAtividade; // Fim registrado no último instante detectado

                        // Fecha eventual pausa aberta
                        var pausaAberta = sessao.Pausas.FirstOrDefault(p => !p.HoraFim.HasValue);
                        if (pausaAberta != null) pausaAberta.HoraFim = sessao.UltimoPingAtividade;

                        _repository.SalvarSessaoAtomico(sessao);
                    }
                }
            }
        }

        // Atualização periódica enviada pelo ViewModel enquanto a janela está aberta
        public void PingAtividade(SessaoTrabalho sessao)
        {
            if (sessao != null && sessao.Status == StatusSessao.EmAndamento)
            {
                sessao.UltimoPingAtividade = DateTime.Now;
                _repository.SalvarSessaoAtomico(sessao);
            }
        }
    }
}