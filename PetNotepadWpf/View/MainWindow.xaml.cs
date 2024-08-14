﻿using System.Text;
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
    public MainWindow()
    {
        InitializeComponent();
        Tabs.ItemsSource = _tabItems;
        AddTab("New tab", ".rtf");
        
        this.Closing += Window_Closing;
    }

    private string CreateFileOnDesktop(string header, string fileExtension)
    {
        string baseFileName = header + fileExtension;
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, baseFileName);
    
        // Проверка на наличие файла с таким названием
        int counter = 1;
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
        
        Console.WriteLine($"Created FlowDocument for tab '{header}': {newTab.Content.GetHashCode()}");
        
        _tabItems.Add(newTab);
    }
    private void AddRtfTab_Click(object sender, RoutedEventArgs e)
    {
        AddTab("New tab", ".rtf");
    }
    private void AddTxtTab_Click(object sender, RoutedEventArgs e)
    {
        AddTab("New tab", ".txt");
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
        foreach (var tabItem in _tabItems)
        {
            if (File.Exists(tabItem.FilePath))
            {
                try
                {
                    // Снимаем атрибут скрытого файла
                    File.SetAttributes(tabItem.FilePath, FileAttributes.Normal);

                    // Удаляем файл без перемещения в корзину
                    File.Delete(tabItem.FilePath);
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
    private void SaveFileAs_Click (object sender, RoutedEventArgs e)
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
