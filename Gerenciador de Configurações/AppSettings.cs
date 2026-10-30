using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace TRONIX.Modules
{
    public class AppConfig
    {
        // Parâmetros do Sistema
        public int LimiteInatividadeSegundos { get; set; } = 10;
        public string SenhaAdmin { get; set; } = "admin123"; // Senha padrão inicial

        // Listas
        public List<string> Desenhistas { get; set; } = new List<string> { "RODRIGO", "ANTONIO", "CARLOS" };
        public List<string> Materiais { get; set; } = new List<string> { "SAE 1020", "INOX 304", "ALUMINIO" };
        public List<string> Pinturas { get; set; } = new List<string> { "EPOXI PRETO", "RAL 7035" };
        public List<string> Banhos { get; set; } = new List<string> { "ZINCADO BRANCO", "ZINCADO AMARELO" };
        public List<string> JustificativasOciosidade { get; set; } = new List<string> { "Almoço", "Banheiro", "Reunião", "Treinamento" };
    }

    public static class ConfigManager
    {
        private static readonly string PastaConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TRONIX");
        private static readonly string CaminhoJson = Path.Combine(PastaConfig, "config_app.json");

        public static AppConfig Carregar()
        {
            try
            {
                if (File.Exists(CaminhoJson))
                {
                    string json = File.ReadAllText(CaminhoJson);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch { }
            var padrao = new AppConfig();
            Salvar(padrao);
            return padrao;
        }

        public static void Salvar(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(PastaConfig)) Directory.CreateDirectory(PastaConfig);
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CaminhoJson, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar: {ex.Message}");
            }
        }
    }
}