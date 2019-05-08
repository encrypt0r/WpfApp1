using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private CancellationTokenSource _cts;

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                return;
            }

            try
            {
                _cts = new CancellationTokenSource();
                ChangeEnability(false);

                var folder = folderTextBox.Text;
                var search = searchTextBox.Text;

                if (string.IsNullOrWhiteSpace(search))
                {
                    MessageBox.Show("Please write something to search for!");
                    return;
                }
                else if (!Directory.Exists(folder))
                {
                    MessageBox.Show("This folder doesn't exist!");
                    return;
                }

                var pdfs = Directory.EnumerateFiles(folder, "*.pdf").ToList();

                int processed = 0;

                foreach (var pdf in pdfs)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var name = pdf.Split('\\', '/').Last();
                    statusTextBlock.Text = $"{processed}/{pdfs.Count} => {name}";
                    progressBar.Value = (double)processed / pdfs.Count;

                    var contains = await Task.Run<bool>(() =>
                    {

                        PdfLoadedDocument loadedDocument = new PdfLoadedDocument(pdf);

                        foreach (PdfLoadedPage page in loadedDocument.Pages)
                        {
                            var content = page.ExtractText();
                            if (content == null) continue;
                            if (content.IndexOf(search, StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                return true;
                            }
                        }

                        return false;
                    });

                    processed++;
                    if (contains)
                    {
                        resultsListBox.Items.Add(name);
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                ChangeEnability(true);
                statusTextBlock.Text = "";
                progressBar.Value = 0;
                _cts = null;
            }
        }

        private void ChangeEnability(bool value)
        {
            Search.Content = value ? "Search" : "Cancel";
            Browse.IsEnabled = value;
            folderTextBox.IsEnabled = value;
            searchTextBox.IsEnabled = value;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            var result = dialog.ShowDialog();
            if (result == true)
            {
                folderTextBox.Text = dialog.SelectedPath;
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var builder = new StringBuilder();
            foreach (var item in resultsListBox.Items)
            {
                if (item is string text)
                {
                    builder.AppendLine(text);
                }
            }

            Clipboard.SetText(builder.ToString());
        }
    }
}
