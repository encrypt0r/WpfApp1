using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

                resultsListBox.Items.Clear();

                var files = Directory.EnumerateFiles(folder, "*.*")
                                     .Where(f =>
                                           f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                                           f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                                     .ToList();

                int processed = 0;

                Parallel.ForEach(files, async file =>
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var name = file.Split('\\', '/').Last();
                    var contains = await FileContains(file, search);

                    Interlocked.Increment(ref processed);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        progressBar.Value = (double)processed / files.Count;
                        statusTextBlock.Text = $"{processed} / {files.Count} ({progressBar.Value * 100:N2}%)";
                        if (contains)
                        {
                            resultsListBox.Items.Add(name);
                        }
                    });
                });
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

        private Task<bool> FileContains(string path, string search)
        {
            if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return ReadTextFile(path, search);
            else if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return ReadPdfFile(path, search);

            throw new ArgumentOutOfRangeException(nameof(path));
        }

        private Task<bool> ReadPdfFile(string pdf, string search)
        {
            return Task.Run(() =>
            {
                var builder = new StringBuilder();
                PdfLoadedDocument loadedDocument = new PdfLoadedDocument(pdf);

                foreach (PdfLoadedPage page in loadedDocument.Pages)
                {
                    var content = page.ExtractText();
                    if (content.IndexOf(search, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        private async Task<bool> ReadTextFile(string text, string search)
        {
            using (var f = File.OpenRead(text))
            using (var r = new StreamReader(f))
            {
                var content = await r.ReadToEndAsync();
                if (content.IndexOf(search, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void ChangeEnability(bool value)
        {
            Search.IsEnabled = value;
            Extract.IsEnabled = value;
            Browse.IsEnabled = value;
            folderTextBox.IsEnabled = value;
            searchTextBox.IsEnabled = value;

            Cancel.IsEnabled = !value;
        }

        private string GetFolderPathFromUser()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            var result = dialog.ShowDialog();
            if (result == true)
            {
                return dialog.SelectedPath;
            }

            return null;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var path = GetFolderPathFromUser();
            if (path != null)
            {
                folderTextBox.Text = path;
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

        private async void Extract_Click(object sender, RoutedEventArgs e)
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

                var source = folderTextBox.Text;
                var search = searchTextBox.Text;

                if (!Directory.Exists(source))
                {
                    MessageBox.Show("This folder doesn't exist!");
                    return;
                }

                var destination = GetFolderPathFromUser();
                if (destination == null)
                    return;

                var pdfs = Directory.EnumerateFiles(source, "*.pdf").ToList();

                progressBar.Value = 0;

                int processed = 0;

                Parallel.ForEach(pdfs, async pdf =>
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var name = pdf.Split('\\', '/').Last();
                    var document = await Task.Run<string>(() =>
                    {
                        var builder = new StringBuilder();
                        PdfLoadedDocument loadedDocument = new PdfLoadedDocument(pdf);

                        foreach (PdfLoadedPage page in loadedDocument.Pages)
                        {
                            builder.AppendLine(page.ExtractText());
                        }

                        return builder.ToString();
                    });

                    File.WriteAllText(Path.Combine(destination, name + ".txt"), document);

                    Interlocked.Increment(ref processed);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        progressBar.Value = (double)processed / pdfs.Count;
                        statusTextBlock.Text = $"{processed} / {pdfs.Count} ({progressBar.Value * 100:N2}%)";
                    });
                });
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
    }
}
