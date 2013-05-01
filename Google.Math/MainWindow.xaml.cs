using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Google.Math
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using Microsoft.Win32;
    using System.Diagnostics;
    using System.Reflection;
    using Microsoft.VisualBasic.ApplicationServices;

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand FileCopyAsHatenaSyntaxCommand = new RoutedCommand();
        public static RoutedCommand FileCopyAsImageFileCommand = new RoutedCommand();
        public static RoutedCommand FileExportAsImageFileCommand = new RoutedCommand();
        public static RoutedCommand ToolsRefreshCommand = new RoutedCommand();
        public static RoutedCommand ToolsAutoRefreshCommand = new RoutedCommand();
        public static RoutedCommand HelpAboutCommand = new RoutedCommand();
        public static RoutedCommand HelpGoToHomepageCommand = new RoutedCommand();

        public bool AutoRefresh { get; set; }
        public bool IsModified { get { return InitialText != CurrentText; } }
        public bool IsSaved { get { return !string.IsNullOrEmpty(FileName); } }
        public string FileName { get; set; }
        public string InitialText { get; set; }
        public string CurrentText
        {
            get { return textBoxFormula.Text; }
            set { textBoxFormula.Text = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
            
            AutoRefresh = true;
            DataContext = this; 

            Loaded += (s, e) =>
            {
                InitialText = CurrentText = @"\phi=\frac{1}{2}erfc\(\frac{z}{\sqrt[]{2Dt}}\)";
                textBoxFormula.Focus();
            };

            Closing += (s, e) =>
            {
                ConfirmToSaveFileThen(() => { e.Cancel = false; }, () => { e.Cancel = true; });
            };
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoRefresh)
            {
                ToolsRefreshCommand.Execute(null, null);
            }

            Title = string.Format(
                "{0} - {1}{2}",
                "Formula Pad",
                string.IsNullOrEmpty(FileName) ? "新規ファイル" : Path.GetFileName(FileName),
                IsModified ? "*" : string.Empty);
        }

        private void ConfirmToSaveFileThen(Action default_action, Action cancel_action = null)
        {
            const string MESSAGE_CONFIRM_SAVING = "ファイルが保存されていません。保存しますか？";

            if (IsModified || !IsSaved)
            {
                switch (MessageBox.Show(this, MESSAGE_CONFIRM_SAVING, Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                    case MessageBoxResult.Yes: SaveCommandBinding_Executed(this, null); break;
                    case MessageBoxResult.No: break;
                    case MessageBoxResult.Cancel: if (cancel_action != null) cancel_action(); return;
                }
            }

            if (default_action != null) default_action();
        }

        private string PreProcessText(string text)
        {
            text = text.Replace(@"\land", @"\wedge");
            text = text.Replace(@"\lor", @"\vee");
            text = text.Replace(@"\lnot", @"\neg");

            return text;
        }

        private void NewCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ConfirmToSaveFileThen(() =>
            {
                FileName = string.Empty;
                InitialText = CurrentText = string.Empty;
            });
        }

        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ConfirmToSaveFileThen(() =>
            {
                var dialog = new OpenFileDialog()
                {
                    Title = "ファイルの選択",
                };

                if (dialog.ShowDialog(this).Value)
                {
                    try
                    {
                        FileName = dialog.FileName;
                        InitialText = CurrentText = File.ReadAllText(FileName);
                    }
                    catch (Exception exception)
                    {
                        textBlockError.Text = exception.Message;
                    }
                }
            });
        }

        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsSaved)
            {
                try
                {
                    File.WriteAllText(FileName, CurrentText);
                    InitialText = CurrentText = File.ReadAllText(FileName);
                }
                catch (Exception exception)
                {
                    textBlockError.Text = exception.Message;
                }
            }
            else
            {
                SaveAsCommandBinding_Executed(this, null);
            }
        }

        private void SaveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsModified;
        }

        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Title = "ファイルの保存",
                AddExtension = true,
                DefaultExt = ".formula.txt",
            };

            if (dialog.ShowDialog(this).Value)
            {
                try
                {
                    FileName = dialog.FileName;
                    File.WriteAllText(FileName, CurrentText);
                    InitialText = CurrentText = File.ReadAllText(FileName);
                }
                catch (Exception exception)
                {
                    textBlockError.Text = exception.Message;
                }
            }
        }

        private void CopyAsHatenaSyntaxCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

            try
            {
                Clipboard.SetText(string.Format("[tex:{0}]", PreProcessText(CurrentText)));
            }
            catch (Exception exception)
            {
                textBlockError.Text = exception.Message;
            }
        }

        private void FileCopyAsImageFileCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Clipboard.SetImage(imageFormula.Source as BitmapImage);
            }
            catch (Exception exception)
            {
                textBlockError.Text = exception.Message;
            }
        }

        private void ExportAsImageFileCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Title = "ファイルの保存",
                AddExtension = true,
                DefaultExt = ".png",
            };

            if (dialog.ShowDialog(this).Value)
            {
                using (var stream = new FileStream(dialog.FileName, FileMode.OpenOrCreate))
                {
                    try
                    {
                        var encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(imageFormula.Source as BitmapImage));
                        encoder.Save(stream);
                    }
                    catch (Exception exception)
                    {
                        textBlockError.Text = exception.Message;
                    }
                }
            }
        }

        private void CloseCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void UndoCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxFormula.Undo();
        }

        private void UndoCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = textBoxFormula.CanUndo;
        }

        private void RedoCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxFormula.Redo();
        }

        private void RedoCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = textBoxFormula.CanRedo;
        }

        private void CutCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxFormula.Cut();
        }

        private void CutCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = textBoxFormula.SelectedText.Length > 0;
        }

        private void CopyCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxFormula.Copy();
        }

        private void CopyCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = textBoxFormula.SelectedText.Length > 0;
        }

        private void PasteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxFormula.Paste();
        }

        private void PasteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsText();
        }

        private void SelectAllCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxFormula.SelectAll();
        }

        private void DeleteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBoxFormula.Text = textBoxFormula.Text.Remove(textBoxFormula.SelectionStart, textBoxFormula.SelectionLength);
            textBoxFormula.SelectionLength = 0;
        }

        private void DeleteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = textBoxFormula.SelectedText.Length > 0;
        }

        private void RefreshCommandBinding_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            const string API_URL = "http://chart.apis.google.com/chart?cht={0}&chl={1}";
            var cht = "tx";
            var chl = WebUtility.UrlEncode(PreProcessText(CurrentText));

            try
            {
                var uri = new Uri(string.Format(API_URL, cht, chl));
                var bitmap = new BitmapImage(uri);
                bitmap.DownloadFailed += (_s, _e) => { textBlockError.Text = _e.ErrorException.Message; };
                imageFormula.Source = bitmap;
            }
            catch (Exception exception)
            {
                textBlockError.Text = exception.Message;
            }

        }

        private void AutoRefreshCommandBinding_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            // AutoRefresh = !AutoRefresh;
        }

        private void AboutCommandBinding_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            const string MESSAGE_VERSOION_INFO = "{0}\r\n{1}\r\n\r\n{2}\r\n\r\n{3}";
            var information = new AssemblyInfo(Assembly.GetExecutingAssembly());

            MessageBox.Show(string.Format(MESSAGE_VERSOION_INFO, information.ProductName, information.Version, information.Description, information.Copyright), "バージョン情報");
        }

        private void GoToHomepageCommandBinding_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start(@"http://daruyanagi.net/");
        }
    }
}
