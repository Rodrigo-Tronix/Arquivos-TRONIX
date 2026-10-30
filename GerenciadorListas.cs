using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace TRONIX.Modules
{
    public static class GerenciadorListas
    {
        private static readonly string PastaConfig = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TRONIX"
        );
        private static readonly string CaminhoJson = Path.Combine(PastaConfig, "config_listas.json");

        public class DadosListas
        {
            public List<string> Desenhistas { get; set; } = new List<string> { "RODRIGO", "ANTONIO", "CARLOS" };
            public List<string> Materiais { get; set; } = new List<string> { "SAE 1020", "INOX 304", "ALUMINIO" };
            public List<string> Pinturas { get; set; } = new List<string> { "EPOXI PRETO", "RAL 7035" };
            public List<string> Banhos { get; set; } = new List<string> { "ZINCADO BRANCO", "ZINCADO AMARELO" };
        }

        public static DadosListas Carregar()
        {
            try
            {
                if (File.Exists(CaminhoJson))
                {
                    string json = File.ReadAllText(CaminhoJson);
                    return JsonSerializer.Deserialize<DadosListas>(json) ?? new DadosListas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar listas salvas: {ex.Message}", "Aviso TRONIX", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            var padrao = new DadosListas();
            Salvar(padrao);
            return padrao;
        }

        public static void Salvar(DadosListas dados)
        {
            try
            {
                if (!Directory.Exists(PastaConfig))
                    Directory.CreateDirectory(PastaConfig);

                string json = JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true });

                // Gravação direta sem leitura prévia
                File.WriteAllText(CaminhoJson, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar alterações no disco: {ex.Message}", "Erro TRONIX", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}