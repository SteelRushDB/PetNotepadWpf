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
using Microsoft.Win32;


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
        AddTab("New tab");
    }

    private void AddTab(string header)
    {
        var newTab = new TabItemModel()
        {
            Header = header,
            Content = new FlowDocument(),
            IsPlaceholder = false
        };
        
        Console.WriteLine($"Created FlowDocument for tab '{header}': {newTab.Content.GetHashCode()}");
        
        _tabItems.Add(newTab);
    }
    private void AddTab_Click(object sender, RoutedEventArgs e)
    {
        AddTab("New tab");
    }
    
    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button closeButton) //сендер пытаемся привести к кнопке - и обзываем это клоузбатон, если тру
        {
            if (closeButton.DataContext is TabItemModel tabItemModel) //смотрим которой табитем принадлежит кнопка - это табитем
            {
                _tabItems.Remove(tabItemModel); //удаляем его из коллекции что автоматом видно в интерфейсе
            }
        }
    }

    private void RichTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is RichTextBox thisRichTextBox)
        {
            if (Tabs.SelectedItem is TabItemModel currentTab && currentTab != null)
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
}
