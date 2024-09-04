using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Win32;
using Path = System.IO.Path;


namespace PetNotepadWpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private SettingsManager _settingsManager;
    
    private ObservableCollection<TabItemModel> _tabItems = new ObservableCollection<TabItemModel>();
    private int TabCounter = 0;
    public MainWindow()
    {
        InitializeComponent();
        Tabs.ItemsSource = _tabItems;
        AddTab($"RTF tab {TabCounter}", ".rtf");
        
        _settingsManager = new SettingsManager();
        _settingsManager.LoadSettings();  // Загружаем настройки при старте приложения
        ApplySettings();  // Применяем загруженные настройки
        _settingsManager.Settings.OpenFiles.Clear();
        
        NewComandsAdd();
        
        this.Closing += Window_Closing;
    }

    private string CreateFolderOnDesktop(string header)
    {
        // Определяем путь к рабочему столу
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        
        // Создаем уникальное имя для папки
        string folderPath = Path.Combine(desktopPath, header);
        int counter = 1;
        
        // Если папка с таким именем уже существует, добавляем суффикс с номером
        while (Directory.Exists(folderPath))
        {
            folderPath = Path.Combine(desktopPath, $"{header}_{counter}");
            counter++;
        }
        
        // Создаем папку
        Directory.CreateDirectory(folderPath);
        File.SetAttributes(folderPath, FileAttributes.Hidden);
        return folderPath;
    }
    private string CreateFileOnDesktop(string header, string fileExtension)
    {
        string folderPath = CreateFolderOnDesktop(header);
        
        string baseFileName = header + fileExtension;
        string filePath = Path.Combine(folderPath, baseFileName);
    
        int counter = 1;
        // Проверка на наличие файла с таким названием
        while (File.Exists(filePath))
        {
            // Добавляем числовой суффикс к имени файла для уникальности
            string newFileName = $"{header}_{counter}.{fileExtension}";
            filePath = Path.Combine(folderPath, newFileName);
            counter++;
        }
    
        using (FileStream fs = File.Create(filePath))
        {
            // Просто закрываем файл, чтобы он был создан
        }
    
        File.SetAttributes(filePath, FileAttributes.Hidden);
    
        return filePath;
    }
    private void AddTab(string header, string fileExtension)
    {
        string filePath = CreateFileOnDesktop(header, fileExtension);
        var newTab = new TabItemModel()
        {
            Header = header,
            Content = new FlowDocument(),
            IsPlaceholder = false,
            FilePath = filePath,
            FileExtension = fileExtension
        };
        
        Console.WriteLine($"Created FlowDocument for tab {header} {TabCounter}: {newTab.Content.GetHashCode()}");
        
        _tabItems.Add(newTab);
        Tabs.SelectedItem = newTab;
        
        TabCounter++;
    }
    private void AddRtfTab_Click(object sender, RoutedEventArgs e)
    {
        AddTab($"RTF tab {TabCounter}", ".rtf");
    }
    private void AddTxtTab_Click(object sender, RoutedEventArgs e)
    {
        AddTab($"TXT tab {TabCounter}", ".txt");
    }
    
    
    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button closeButton) 
        {
            if (closeButton.DataContext is TabItemModel tabItem)
            {
               ProcessTabItemClose(tabItem, false);
            }
        }
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Tabs.SelectedIndex = _tabItems.Count - 1;
        for (int i = _tabItems.Count - 1; i >= 0; i--)
        {
            var tabItem = _tabItems[i];
            ProcessTabItemClose(tabItem, true, e);
        }
    }

    private void ProcessTabItemClose(TabItemModel tabItem, bool shouldCancel, CancelEventArgs e = null)
    {
        string directoryPath = Path.GetDirectoryName(tabItem.FilePath);
        if (Directory.Exists(directoryPath))
        {
            try
            {
                var result = MessageBox.Show($"Save changes in {tabItem.Header}?", "Save file", MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                    
                // Спрашиваем у пользователя, хочет ли он сохранить изменения
                if (result == MessageBoxResult.Yes)
                {
                    // Если пользователь хочет сохранить изменения
                    if (!SaveFileAs()) // SaveFileAs возвращает false, если пользователь отменил сохранение
                    {
                        // Отмена закрытия окна
                        if (shouldCancel && e != null)
                        {
                            e.Cancel = true;
                        }
                        return;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    if (shouldCancel && e != null)
                    {
                        e.Cancel = true;
                    }
                    return;
                }

                // Снимаем атрибут скрытого файла
                File.SetAttributes(directoryPath, FileAttributes.Normal);

                // Удаляем папку и всё внутри
                Directory.Delete(directoryPath, true);

                _tabItems.Remove(tabItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении файла: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("error");
        }
    }
    
    
    private void RichTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is RichTextBox thisRichTextBox)
        {
            var currentTab = Tabs.SelectedItem as TabItemModel;
            if (currentTab != null)
            {
                currentTab.Content = thisRichTextBox.Document;
                Console.WriteLine("typed");
            }
        }
    }
    private void Tabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var currentTab = Tabs.SelectedItem as TabItemModel;
        
        if (sender is TabControl tabControl && currentTab != null)
        {
            richTextBox.Document = currentTab.Content;
            
            //AutoSaveTabContent(currentTab); //Почему это тут, а не до этого???
            
            if (_autoSaveTimer != null && _autoSaveTimer.IsEnabled)
            {
                _autoSaveTimer.Stop();
                _autoSaveTimer.Start();
            }
            
            Console.WriteLine("changed");
        }

        
    }
    
    
    
    private void SaveRichTextBoxContent(string filePath, string format)
    {
        TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            if (format == "RTF")
            {
                range.Save(fileStream, DataFormats.Rtf);
            }
            else if (format == "TXT")
            {
                range.Save(fileStream, DataFormats.Text);
            }
        }
    }
    private bool SaveFileAs()
    {
        Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
        saveFileDialog.Filter = "Rich Text Format (*.rtf)|*.rtf|Text file (*.txt)|*.txt";
        saveFileDialog.DefaultExt = "rtf";
        saveFileDialog.AddExtension = true;

        // Показываем диалоговое окно
        if (saveFileDialog.ShowDialog() == true)
        {
            // Определяем формат на основе выбранного расширения файла
            string format = saveFileDialog.FilterIndex == 1 ? "RTF" : "TXT";

            // Сохраняем текст в выбранном формате
            SaveRichTextBoxContent(saveFileDialog.FileName, format);
            MessageBox.Show("Текст сохранен в файл.");
            return true;
        }
        
        return false;
    }
    private void SaveFileAs_Click (object sender, RoutedEventArgs e)
    {
        SaveFileAs();
    }

    
    private void LoadRichTextBoxContent(string filePath, string format)
    {
        TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
        {
            if (format == ".rtf")
            {
                range.Load(fileStream, DataFormats.Rtf);
            }
            else if (format == ".txt")
            {
                range.Load(fileStream, DataFormats.Text);
            }
        }
    }
    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
        openFileDialog.Filter = "Rich Text Format (*.rtf)|*.rtf|Text file (*.txt)|*.txt";
        openFileDialog.DefaultExt = "rtf";
        openFileDialog.AddExtension = true;
        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;

            // Определяем формат файла (RTF или текст)
            string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();
            try
            {
                LoadRichTextBoxContent(filePath, fileExtension);
                
                _settingsManager.Settings.OpenFiles.Add(new OpenFileInfo
                {
                    FilePath = filePath,
                    FileExtension = fileExtension
                });
                _settingsManager.SaveSettings();
                
                MessageBox.Show("Text loaded successfully.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
    }
    
    
    //Auto-saving
    private DispatcherTimer _autoSaveTimer;
    private void StartAutoSave(int intervalInSeconds)
    {
        if (_autoSaveTimer == null)
        {
            _autoSaveTimer = new DispatcherTimer();
            _autoSaveTimer.Tick += AutoSaveTab;
        }
        _autoSaveTimer.Interval = TimeSpan.FromSeconds(intervalInSeconds);
        _autoSaveTimer.Start();
    }
    private void StopAutoSave()
    {
        _autoSaveTimer?.Stop();
    }
    private void AutoSaveTab(object sender, EventArgs e)
    {
        var currentTab = Tabs.SelectedItem as TabItemModel;
        if (!string.IsNullOrEmpty(currentTab.FilePath))
        {
            AutoSaveTabContent(currentTab);
        }
    }
    private void AutoSaveTabContent(TabItemModel tab)
    {
        if (tab == null || string.IsNullOrEmpty(tab.FilePath)) return;
        try
        {
            string directory = Path.GetDirectoryName(tab.FilePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(tab.FilePath);
            string extension = Path.GetExtension(tab.FilePath);
        
            // Используем регулярное выражение для удаления старой временной метки (формат _HHmmss)
            string pattern = @"_\d{6}$";
            fileNameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(fileNameWithoutExtension, pattern, "");

            // Добавляем новую временную метку
            string timestamp = DateTime.Now.ToString("HHmmss");
            string newFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
            string newSavePath = Path.Combine(directory, newFileName);
            
            
            File.SetAttributes(tab.FilePath, FileAttributes.Normal);
            
            TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            
            using (FileStream fileStream = new FileStream(newSavePath, FileMode.Create))
            {
                range.Save(fileStream, DataFormats.Rtf);
            }

            File.SetAttributes(tab.FilePath, FileAttributes.Hidden);
            
            tab.FilePath = newSavePath;
            
            Console.WriteLine($"Content auto-saved to file: {newSavePath}");
            
            CleanUpOldFiles(directory, fileNameWithoutExtension, extension, 5);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during auto-save: " + ex.Message);
        }
    }
    private void AutoSaveInterval_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && int.TryParse(menuItem.Tag.ToString(), out int interval))
        {
            // Сброс всех других пунктов меню
            foreach (MenuItem item in ((MenuItem)menuItem.Parent).Items)
            {
                item.IsChecked = false;
            }
            // Отмечаем выбранный пункт
            menuItem.IsChecked = true;
            
            
            if (interval == 0)
            {
                StopAutoSave();
                MessageBox.Show("AutoSave disabled.");
                
                _settingsManager.Settings.IsAutoSaveEnabled = false;
                _settingsManager.Settings.AutoSaveInterval = interval;
                _settingsManager.SaveSettings();
            }
            else
            {
                StartAutoSave(interval);
                MessageBox.Show($"AutoSave set to {interval / 60} minutes.");
                
                _settingsManager.Settings.IsAutoSaveEnabled = true;
                _settingsManager.Settings.AutoSaveInterval = interval;
                _settingsManager.SaveSettings();
            }
            
        }
    }
    private void CleanUpOldFiles(string directory, string fileNameWithoutExtension, string extension, int maxFiles)
    {
        try
        {
            // Получаем все файлы в папке с похожим именем и нужным расширением
            var files = Directory.GetFiles(directory, $"{fileNameWithoutExtension}_*{extension}")
                .OrderBy(File.GetCreationTime) // Сортируем файлы по времени создания
                .ToList();

            // Если количество файлов превышает максимальное допустимое, удаляем старые файлы
            while (files.Count > maxFiles)
            {
                string oldestFile = files.First();
                File.SetAttributes(oldestFile, FileAttributes.Normal); // Сбрасываем атрибуты
                File.Delete(oldestFile); // Удаляем старый файл
                files.RemoveAt(0); // Убираем его из списка
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during cleanup: " + ex.Message);
        }
    }
    //Auto-saving end
    
    
    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        richTextBox.SelectAll();
    }
    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        richTextBox.Cut();
    }
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        richTextBox.Copy();
    }
    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        richTextBox.Paste();
    }
    private void FormatText_Click(object sender, RoutedEventArgs e)
    {
        // Пример: изменение выделенного текста на жирный
        TextSelection selectedText = richTextBox.Selection;
        if (!selectedText.IsEmpty)
        {
            var currentWeight = selectedText.GetPropertyValue(TextElement.FontWeightProperty);
            selectedText.ApplyPropertyValue(TextElement.FontWeightProperty,
                currentWeight.Equals(FontWeights.Bold) ? FontWeights.Normal : FontWeights.Bold);
        }
    }


    private void NewComandsAdd()
    {
        CommandBindings.Add(new CommandBinding(ApplicationCommands.New, NewDocumentCommand));
        CommandBindings.Add(new CommandBinding(NewTabCommand, NewTabCommand_Executed));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveDocumentCommand));
        CommandBindings.Add(new CommandBinding(SaveAllCommand, SaveAllDocumentsCommand));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseApplicationCommand));

        // Связь команд с горячими клавишами
        InputBindings.Add(new KeyBinding(ApplicationCommands.New, Key.N, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(NewTabCommand, Key.T, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Save, Key.S, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(SaveAllCommand, Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Close, Key.F4, ModifierKeys.Alt));
    }
    
    // Команда создания документа в новом окне
    private void NewDocumentCommand(object sender, ExecutedRoutedEventArgs e)
    {
        MainWindow newWindow = new MainWindow();
        newWindow.Show();
    }

    // Команда создания документа в новой вкладке
    private static RoutedCommand NewTabCommand = new RoutedCommand();
    private void NewTabCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        AddTab("RTF tab", ".rtf"); // Метод AddTab нужно адаптировать для использования с горячими клавишами
    }

    // Команда сохранения текущего документа
    private void SaveDocumentCommand(object sender, ExecutedRoutedEventArgs e)
    {
        SaveFileAs(); // Метод сохранения файла
    }

    // Команда сохранения всех открытых документов
    private static RoutedCommand SaveAllCommand = new RoutedCommand();
    private void SaveAllDocumentsCommand(object sender, ExecutedRoutedEventArgs e)
    {
        for (int i = _tabItems.Count - 1; i >= 0; i--)
        {
            Tabs.SelectedIndex = i;
            SaveFileAs();
        }
    }

    // Команда закрытия приложения
    private void CloseApplicationCommand(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }
    
    
    
    private void ChangeTheme(string theme)
    {
            ResourceDictionary newTheme = new ResourceDictionary();

            switch (theme)
            {
                case "Light":
                    newTheme.Source = new Uri("pack://application:,,,/View/LightTheme.xaml");
                    break;
                case "Dark":
                    newTheme.Source = new Uri("pack://application:,,,/View/DarkTheme.xaml");
                    break;
                default:
                    throw new ArgumentException("Unknown theme", nameof(theme));
            }

            // Очищаем текущие ресурсы и добавляем новые
            Application.Current.Resources.Clear();
            Application.Current.Resources.MergedDictionaries.Add(newTheme);
            
            Application.Current.MainWindow?.UpdateLayout();
            
            
            Console.WriteLine($"Theme changed to: {theme}");
            
            // Сохраняем тему
            _settingsManager.Settings.Theme = theme;
            _settingsManager.SaveSettings();
    }
    private void LightTheme_Click(object sender, RoutedEventArgs e)
    {
        ChangeTheme("Light");
    }
    private void DarkTheme_Click(object sender, RoutedEventArgs e)
    {
        ChangeTheme("Dark");
    }
    
    
    
    private void Bold_Click(object sender, RoutedEventArgs e)
    {
        // Получаем выделенный текст
        TextSelection selectedText = richTextBox.Selection;

        // Если текст уже жирный, снимаем форматирование
        if (selectedText.GetPropertyValue(TextElement.FontWeightProperty).Equals(FontWeights.Bold))
        {
            selectedText.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
        }
        else
        {
            // Применяем жирное начертание
            selectedText.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
        }
    }
    private void Italic_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выделенный текст
            TextSelection selectedText = richTextBox.Selection;

            // Если текст уже курсивный, снимаем форматирование
            if (selectedText.GetPropertyValue(TextElement.FontStyleProperty).Equals(FontStyles.Italic))
            {
                selectedText.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
            }
            else
            {
                // Применяем курсив
                selectedText.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
            }
        }
    
    private void Underline_Click(object sender, RoutedEventArgs e)
    {
        TextSelection selectedText = richTextBox.Selection;
        SetDecorations(selectedText, TextDecorations.Underline);
    }
    private void Strikethrough_Click(object sender, RoutedEventArgs e)
    {
        TextSelection selectedText = richTextBox.Selection;
        SetDecorations(selectedText, TextDecorations.Strikethrough);
    }

    private static void SetDecorations(TextRange selectedText, TextDecorationCollection newDecoration)
    {
        var decorations = selectedText.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection
                          ?? new TextDecorationCollection();

        decorations = decorations.Contains(newDecoration.First())
            ? new TextDecorationCollection(decorations.Except(newDecoration))
            : new TextDecorationCollection(decorations.Union(newDecoration));

        selectedText.ApplyPropertyValue(Inline.TextDecorationsProperty, decorations);
    }

    
    
    private void ApplySettings()
    {
        this.Width = _settingsManager.Settings.WindowWidth;
        this.Height = _settingsManager.Settings.WindowHeight;
        ChangeTheme(_settingsManager.Settings.Theme);

        if (_settingsManager.Settings.IsAutoSaveEnabled)
        {
            // Логика включения автосохранения
            StartAutoSave(_settingsManager.Settings.AutoSaveInterval);
        }
        
        // Восстанавливаем открытые файлы
        foreach (var fileInfo in _settingsManager.Settings.OpenFiles)
        {
            if (File.Exists(fileInfo.FilePath))
            {
                // Восстанавливаем вкладку с соответствующим файлом
                AddTab_Existed($"Tab {TabCounter}", fileInfo.FileExtension, fileInfo.FilePath);
            }
        }
    }
    
    // Сохранение настроек при закрытии приложения
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _settingsManager.Settings.WindowWidth = this.Width;
        _settingsManager.Settings.WindowHeight = this.Height;
        
        _settingsManager.SaveSettings();  // Сохраняем настройки перед закрытием приложения
        base.OnClosing(e);
    }

    // Метод для добавления вкладки открытого в предыдущей сессии файла
    private void AddTab_Existed(string header, string fileExtension, string filePath = null)
    {
        AddTab($"Tab {TabCounter}", fileExtension);
        LoadRichTextBoxContent(filePath, fileExtension);

    }

    
}
