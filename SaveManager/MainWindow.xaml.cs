using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace SaveManager {
    public partial class MainWindow : Window {
        private string selectedKey = "";
        private string selectedFilePath = "";
        private bool isSelecting = false;
        private bool isRunning = false;
        private readonly string saveGamePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EscapeTheBackrooms", "Saved", "SaveGames");
        private KeyEventHandler keyDownHandler;
        
        public MainWindow() {
            InitializeComponent();
        }
        
        private void SelectHotkey_Click(object sender, RoutedEventArgs e) {
            if (isSelecting == false) {
                isSelecting = true;
                keyDownHandler = (s, args) => {
                    if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl ||
                        args.Key == Key.LeftAlt || args.Key == Key.RightAlt ||
                        args.Key == Key.LeftShift || args.Key == Key.RightShift) {
                        return;
                    }
                    string key = args.Key.ToString();
                    if (key == "System") {
                        key = args.SystemKey.ToString();
                    }
                    selectedKey = key;
                    KeyTextBlock.Text = selectedKey;
                    args.Handled = true;
                    StopListening();
                };
                KeyDown += keyDownHandler;
                SelectHotkeyButton.Content = "Press a key...";
            } else {
                StopListening();
            }
        }
        
        private void StopListening() {
            if (isSelecting) {
                KeyDown -= keyDownHandler;
                isSelecting = false;
                SelectHotkeyButton.Content = "Select Hotkey";
            }
        }
        
        private void SelectFile_Click(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog {
                Title = "Select Save File",
                Filter = "Save Files|*.sav|All Files|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true) {
                selectedFilePath = openFileDialog.FileName;
                string fileName = Path.GetFileNameWithoutExtension(selectedFilePath);
                string formattedName = FormatSaveName(fileName);
                SelectedFileText.Text = formattedName;
            }
        }

        private string FormatSaveName(string fileName) {
            try {
                if (fileName.StartsWith("SINGLEPLAYER_", StringComparison.OrdinalIgnoreCase)) {
                    return HandleSinglePlayerFormat(fileName);
                } else if (fileName.StartsWith("MULTIPLAYER_", StringComparison.OrdinalIgnoreCase)) {
                    return HandleMultiPlayerFormat(fileName);
                } else {
                    return fileName;
                }
            } catch {
                return fileName;
            }
        }

        private string HandleSinglePlayerFormat(string fileName) {
            string remainingString = fileName.Substring("SINGLEPLAYER_".Length);
            int lastUnderscoreIndex = remainingString.LastIndexOf('_');
            if (lastUnderscoreIndex > 0) {
                string namePart = remainingString.Substring(0, lastUnderscoreIndex);
                string cleanName = RemoveNumericPrefix(namePart);
                return $"SP: {cleanName}";
            }
            return $"SP: {remainingString}";
        }

        private string HandleMultiPlayerFormat(string fileName) {
            string remainingString = fileName.Substring("MULTIPLAYER_".Length);
            int lastUnderscoreIndex = remainingString.LastIndexOf('_');
            if (lastUnderscoreIndex > 0) {
                string namePart = remainingString.Substring(0, lastUnderscoreIndex);
                string cleanName = RemoveNumericPrefix(namePart);
                return $"MP: {cleanName}";
            }
            return $"MP: {remainingString}";
        }

        private string RemoveNumericPrefix(string name) {
            int dotIndex = name.IndexOf('.');
            if (dotIndex > 0) {
                string prefix = name.Substring(0, dotIndex);
                if (double.TryParse(prefix, out _)) {
                    return name.Substring(dotIndex + 1).Trim();
                }
            }
            return name.Trim();
        }
        
        private void StartStop_Click(object sender, RoutedEventArgs e) {
            if (isRunning) {
                StopReplacement();
            } else {
                StartReplacement();
            }
        }

        private void StartReplacement() {
            if (string.IsNullOrEmpty(selectedFilePath)) {
                MessageBox.Show(
                    "No file selected. Please select a file first.",
                    "File Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
                return;
            }
            if (string.IsNullOrEmpty(selectedKey)) {
                MessageBox.Show(
                    "Please select a key first.",
                    "Key Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
                return;
            }
            isRunning = true;
            StartStopButton.Content = "Stop";
            KeyManager.RegisterKey(this, selectedKey, ReplaceFile);
        }

        private void StopReplacement() {
            isRunning = false;
            StartStopButton.Content = "Start";
            KeyManager.UnregisterKey(this);
        }

        private void ReplaceFile() {
            try {
                string[] saveFiles = Directory.GetFiles(saveGamePath, "*.sav");
                string sourceFileName = Path.GetFileName(selectedFilePath);
                if (saveFiles.Length == 0) {
                    string destinationFile = Path.Combine(saveGamePath, sourceFileName);
                    File.Copy(selectedFilePath, destinationFile, true);
                } else {
                    bool matchFound = false;
                    foreach (string saveFile in saveFiles) {
                        if (Path.GetFileName(saveFile).Equals(sourceFileName, StringComparison.OrdinalIgnoreCase)) {
                            File.Copy(selectedFilePath, saveFile, true);
                            matchFound = true;
                        }
                    }
                    if (!matchFound) {
                        string destinationFile = Path.Combine(saveGamePath, sourceFileName);
                        File.Copy(selectedFilePath, destinationFile, true);
                    }
                }
            } catch (Exception ex) {
                Dispatcher.Invoke(() => {
                    MessageBox.Show(
                        $"Error replacing save file: {ex.Message}", 
                        "Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error
                    );
                });
            }
        }

        protected override void OnClosed(EventArgs e) {
            if (isRunning) {
                KeyManager.UnregisterKey(this);
            }
            base.OnClosed(e);
        }
    }
}