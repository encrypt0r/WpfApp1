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

        private void ChangeEnability(bool value)
        {
            Extract.IsEnabled = value;
            BrowseSource.IsEnabled = value;
            BrowseDestination.IsEnabled = value;
            SourceTextBox.IsEnabled = value;
            DestinationTextBox.IsEnabled = value;

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
                SourceTextBox.Text = path;
            }
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

                var source = SourceTextBox.Text;
                var destination = DestinationTextBox.Text;

                if (string.IsNullOrWhiteSpace(source) || !Directory.Exists(source))
                {
                    MessageBox.Show("Please specify a valid source folder!");
                    return;
                }
                else if (string.IsNullOrWhiteSpace(destination))
                {
                    MessageBox.Show("Please specify destination folder!");
                    return;
                }
  
                var skipDuplicateFiles = SkipRadioButton.IsChecked == true;

                if (!Directory.Exists(source))
                {
                    MessageBox.Show("This folder doesn't exist!");
                    return;
                }

                var pdfs = Directory.EnumerateFiles(source, "*.pdf", SearchOption.AllDirectories).ToList();

                progressBar.Value = 0;

                int processed = 0;
                statusTextBlock.Text = "Starting...";

                await Task.Run(async () =>
                {
                    try
                    {
                        foreach (var pdf in pdfs)
                        {
                            _cts.Token.ThrowIfCancellationRequested();

                            var name = pdf.Split('\\', '/').Last();
                            var destinationFile = Path.Combine(destination, name + ".txt");

                            if (skipDuplicateFiles && File.Exists(destinationFile))
                            {
                                continue;
                            }

                            var builder = new StringBuilder();
                            PdfLoadedDocument loadedDocument = new PdfLoadedDocument(pdf);

                            foreach (PdfLoadedPage page in loadedDocument.Pages)
                            {
                                builder.AppendLine(page.ExtractText());
                            }

                            loadedDocument.Dispose();

                            var document = builder.ToString();

                            File.WriteAllText(Path.Combine(destination, name + ".txt"), document);

                            Interlocked.Increment(ref processed);

                            await Dispatcher.InvokeAsync(() =>
                            {
                                progressBar.Value = (double)processed / pdfs.Count;
                                statusTextBlock.Text = $"{processed} / {pdfs.Count} ({progressBar.Value * 100:N2}%)";
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {

                    }
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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
        }

        private void BrowseDestination_Click(object sender, RoutedEventArgs e)
        {
            var path = GetFolderPathFromUser();
            if (path != null)
            {
                DestinationTextBox.Text = path;
            }
        }
    }
}
