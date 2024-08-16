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
    private ObservableCollection<TabItemModel> _tabItems = new ObservableCollection<TabItemModel>();
    private int TabCounter = 0;
    public MainWindow()
    {
        InitializeComponent();
        Tabs.ItemsSource = _tabItems;
        AddTab("RTF tab", ".rtf");
        
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
    // Сделать чтобы файл создавался в папке
    private string CreateFileOnDesktop(string header, string fileExtension)
    {
        
        // Определяем путь к рабочему столу
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        // Создаём папку где будут версии документа
        string folderPath = CreateFolderOnDesktop(header);
        
        string baseFileName = header + fileExtension;
        string filePath = Path.Combine(desktopPath, baseFileName);
    
        int counter = 1;
        // Проверка на наличие файла с таким названием
        while (File.Exists(filePath))
        {
            // Добавляем числовой суффикс к имени файла для уникальности
            string newFileName = $"{header}_{counter}.{fileExtension}";
            filePath = Path.Combine(desktopPath, newFileName);
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
        if (sender is Button closeButton) //сендер пытаемся привести к кнопке - и обзываем это клоузбатон, если тру
        {
            if (closeButton.DataContext is TabItemModel tabItemModel) //смотрим которой табитем принадлежит кнопка - это табитем
            {
                if (File.Exists(tabItemModel.FilePath))
                {
                    try
                    {
                        // Сначала снимаем атрибут скрытого файла
                        File.SetAttributes(tabItemModel.FilePath, FileAttributes.Normal);
                    
                        // Удаляем файл без перемещения в корзину
                        File.Delete(tabItemModel.FilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении файла: {ex.Message}");
                    }
                }
                
                _tabItems.Remove(tabItemModel); //удаляем его из коллекции что автоматом видно в интерфейсе
            }
        }
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Tabs.SelectedIndex = _tabItems.Count - 1;
        for (int i = _tabItems.Count - 1; i >= 0; i--)
        {
            var tabItem = _tabItems[i];
            if (File.Exists(tabItem.FilePath))
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
                            e.Cancel = true;
                            return;
                        }
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }

                    // Снимаем атрибут скрытого файла
                    File.SetAttributes(tabItem.FilePath, FileAttributes.Normal);

                    // Удаляем файл без перемещения в корзину
                    File.Delete(tabItem.FilePath);

                    _tabItems.Remove(tabItem);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении файла: {ex.Message}");
                }
            }
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
            
            AutoSaveTabContent(currentTab);
            
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
            File.SetAttributes(tab.FilePath, FileAttributes.Normal);
            
            TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            using (FileStream fileStream = new FileStream(tab.FilePath, FileMode.Create))
            {
                range.Save(fileStream, DataFormats.Rtf);
            }
            
            File.SetAttributes(tab.FilePath, FileAttributes.Hidden);
            
            Console.WriteLine($"Content auto-saved to file: {tab.FilePath}");
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
            if (interval == 0)
            {
                StopAutoSave();
                MessageBox.Show("AutoSave disabled.");
            }
            else
            {
                StartAutoSave(interval);
                MessageBox.Show($"AutoSave set to {interval / 60} minutes.");
            }
        }
    }
    //Auto-saving end
}
