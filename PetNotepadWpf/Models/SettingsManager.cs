namespace PetNotepadWpf;

using Newtonsoft.Json;
using System.IO;

public class SettingsManager
{
    private const string settingsFilePath = "appsettings.json";  // путь к файлу настроек
    public AppSettings Settings { get; private set; }

    // Загружаем настройки из файла
    public void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            string json = File.ReadAllText(settingsFilePath);  // читаем содержимое JSON-файла
            Settings = JsonConvert.DeserializeObject<AppSettings>(json);  // десериализуем настройки
        }
        else
        {
            // Если файл не найден, создаем объект настроек по умолчанию
            Settings = new AppSettings
            {
                Theme = "Light",               // Тема по умолчанию
                WindowWidth = 800,             // Ширина окна по умолчанию
                WindowHeight = 600,            // Высота окна по умолчанию
                IsAutoSaveEnabled = false,     // Автосохранение отключено по умолчанию
                AutoSaveInterval = 0          // Интервал автосохранения по умолчанию (в минутах)
            };
        }
    }
    
    // Сохраняем текущие настройки в файл
    public void SaveSettings()
    {
        string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);  // сериализуем объект настроек в JSON
        File.WriteAllText(settingsFilePath, json);  // записываем JSON в файл
    }
}
