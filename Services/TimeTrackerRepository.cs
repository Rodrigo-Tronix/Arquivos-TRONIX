using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TRONIX.Modules;

namespace TRONIX.Services
{
    public class TimeTrackerRepository
    {
        private readonly string _diretorioDados;

        public TimeTrackerRepository()
        {
            _diretorioDados = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TRONIX",
                "Sessions"
            );

            if (!Directory.Exists(_diretorioDados))
            {
                Directory.CreateDirectory(_diretorioDados);
            }
        }

        public void SalvarSessaoAtomico(SessaoTrabalho sessao)
        {
            if (sessao == null || string.IsNullOrEmpty(sessao.Id)) return;

            string caminhoArquivo = Path.Combine(_diretorioDados, $"{sessao.Id}.json");
            string json = JsonSerializer.Serialize(sessao, new JsonSerializerOptions { WriteIndented = true });

            string caminhoTemp = caminhoArquivo + ".tmp";
            File.WriteAllText(caminhoTemp, json);

            if (File.Exists(caminhoArquivo))
            {
                File.Delete(caminhoArquivo);
            }

            File.Move(caminhoTemp, caminhoArquivo);
        }

        public List<SessaoTrabalho> CarregarTodasSessoes(string nomeProjeto = null)
        {
            var sessoes = new List<SessaoTrabalho>();

            if (!Directory.Exists(_diretorioDados)) return sessoes;

            var arquivos = Directory.GetFiles(_diretorioDados, "*.json");
            foreach (var arq in arquivos)
            {
                try
                {
                    string json = File.ReadAllText(arq);
                    var sessao = JsonSerializer.Deserialize<SessaoTrabalho>(json);
                    if (sessao != null)
                    {
                        if (string.IsNullOrEmpty(nomeProjeto) || string.Equals(sessao.NomeDocumento, nomeProjeto, StringComparison.OrdinalIgnoreCase))
                        {
                            sessoes.Add(sessao);
                        }
                    }
                }
                catch { }
            }

            return sessoes;
        }

        // NOVO: Busca todas as sessões vinculadas ao mesmo projeto pai / CIGAM
        public List<SessaoTrabalho> CarregarSessoesPorProjetoPai(string codigoProjetoPai)
        {
            if (string.IsNullOrEmpty(codigoProjetoPai)) return new List<SessaoTrabalho>();

            var todas = CarregarTodasSessoes();
            return todas.Where(s => string.Equals(s.CodigoProjetoPai, codigoProjetoPai, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // NOVO: Retorna os N apontamentos mais recentes para a lista do histórico
        public List<SessaoTrabalho> CarregarHistoricoRecente(int quantidade = 10)
        {
            var todas = CarregarTodasSessoes();
            return todas.OrderByDescending(s => s.UltimoPingAtividade)
                         .ThenByDescending(s => s.HoraInicio)
                         .Take(quantidade)
                         .ToList();
        }
        /// <summary>
        /// Migra todos os registros de tempo salvos com o nome de arquivo antigo para o novo nome CIGAM
        /// </summary>
        public void MigrarNomeSessao(string nomeAntigo, string nomeNovo)
        {
            if (string.IsNullOrEmpty(nomeAntigo) || string.IsNullOrEmpty(nomeNovo)) return;

            var sessoes = CarregarTodasSessoes(nomeAntigo);
            foreach (var sessao in sessoes)
            {
                sessao.NomeDocumento = nomeNovo;
                SalvarSessaoAtomico(sessao);
            }
        }
    }
}