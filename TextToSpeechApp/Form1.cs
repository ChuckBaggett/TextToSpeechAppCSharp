using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using PiperSharp;
using PiperSharp.Models;

namespace TextToSpeechApp
{
    public partial class Form1 : Form
    {
        private PiperService? piperProvider;
        private VoiceModel? currentVoiceModel;
        private string selectedOutputPath = string.Empty;
        private string piperBaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TextToSpeechApp", "piper_tts");
        private string piperInstallationPath = string.Empty; // Will be AppData\Local\TextToSpeechApp\piper_tts\piper
        private string piperExecutablePath = string.Empty; // Will be AppData\Local\TextToSpeechApp\piper_tts\piper\piper.exe
        private string modelsCommonPath = string.Empty; // Will be AppData\Local\TextToSpeechApp\piper_tts\models

        public Form1()
        {
            InitializeComponent();
            this.Load += new System.EventHandler(this.Form1_Load);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            this.UseWaitCursor = true;
            lblStatus.Text = "Initializing TTS engine...";
            Application.DoEvents();

            try
            {
                Directory.CreateDirectory(piperBaseDirectory);
                piperInstallationPath = Path.Combine(piperBaseDirectory, "piper"); // Piper will be extracted into this subfolder
                piperExecutablePath = Path.Combine(piperInstallationPath, "piper.exe");
                modelsCommonPath = Path.Combine(piperBaseDirectory, "models");
                Directory.CreateDirectory(modelsCommonPath);


                lblStatus.Text = "Checking for Piper executable...";
                Application.DoEvents();
                if (!File.Exists(piperExecutablePath))
                {
                    Directory.CreateDirectory(piperInstallationPath);
                    lblStatus.Text = "Downloading Piper TTS (~25MB)...";
                    Application.DoEvents();
                    var piperZipPath = Path.Combine(piperBaseDirectory, "piper.zip");

                    // Corrected download and extraction
                    await PiperDownloader.DownloadPiper(piperZipPath, архитектура: PiperArchitecture.X64, progress: new Progress<string>(s => lblStatus.Text = $"Downloading Piper: {s}"));
                    System.IO.Compression.ZipFile.ExtractToDirectory(piperZipPath, piperInstallationPath, true);
                    File.Delete(piperZipPath);

                    if (!File.Exists(piperExecutablePath))
                    {
                        throw new FileNotFoundException("Piper executable not found after download and extraction.", piperExecutablePath);
                    }
                    lblStatus.Text = "Piper executable downloaded and extracted.";
                    Application.DoEvents();
                }
                else
                {
                    lblStatus.Text = "Piper executable found.";
                    Application.DoEvents();
                }

                string defaultModelKey = "en_US-lessac-medium"; // A common English voice
                lblStatus.Text = $"Looking for voice model: {defaultModelKey}...";
                Application.DoEvents();

                var modelConfigFile = Path.Combine(modelsCommonPath, $"{defaultModelKey}.onnx.json");
                var modelOnnxFile = Path.Combine(modelsCommonPath, $"{defaultModelKey}.onnx");

                if (!File.Exists(modelConfigFile) || !File.Exists(modelOnnxFile))
                {
                    lblStatus.Text = $"Downloading voice model: {defaultModelKey}...";
                    Application.DoEvents();
                    // DownloadModelByKey might create subdirectories, ensure modelsCommonPath is where it looks or saves.
                    currentVoiceModel = await PiperDownloader.DownloadModelByKey(defaultModelKey, modelsCommonPath, архитектура: PiperArchitecture.X64, progress: new Progress<string>(s => lblStatus.Text = $"Voice MDL: {s}"));
                    if (currentVoiceModel == null) throw new Exception($"Failed to download voice model: {defaultModelKey}");
                     // Ensure paths in model are updated if necessary
                    currentVoiceModel.ModelPath = Path.Combine(modelsCommonPath, Path.GetFileName(currentVoiceModel.ModelPath));
                    currentVoiceModel.ModelConfigPath = Path.Combine(modelsCommonPath, Path.GetFileName(currentVoiceModel.ModelConfigPath));
                    // await currentVoiceModel.SaveModel(currentVoiceModel.ModelPath); // This might not be needed if paths are correct
                }
                else
                {
                    lblStatus.Text = $"Loading voice model: {defaultModelKey}...";
                    Application.DoEvents();
                    currentVoiceModel = await VoiceModel.LoadModel(modelOnnxFile, modelConfigFile); // Use LoadModel with explicit paths
                }

                if (currentVoiceModel == null)
                {
                    throw new Exception("Voice model could not be loaded or downloaded.");
                }
                currentVoiceModel.Name = defaultModelKey; // Set name for display

                lblStatus.Text = "Initializing PiperService...";
                Application.DoEvents();
                PiperConfiguration config = new PiperConfiguration()
                {
                    ExecutablePath = piperExecutablePath,
                    DefaultVoice = currentVoiceModel
                };
                piperProvider = new PiperService(config);

                cmbVoiceSelection.Items.Clear();
                cmbVoiceSelection.Items.Add(currentVoiceModel);
                cmbVoiceSelection.DisplayMember = "Name";
                if (cmbVoiceSelection.Items.Count > 0)
                {
                    cmbVoiceSelection.SelectedIndex = 0;
                }

                lblStatus.Text = "TTS Engine Ready.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error initializing TTS: {ex.Message}";
                MessageBox.Show($"Detailed Error: {ex.ToString()}", "TTS Initialization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.UseWaitCursor = false;
            }
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
                        lblStatus.Text = $"File loaded: {Path.GetFileName(openFileDialog.FileName)}";
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "Error reading file.";
                        MessageBox.Show($"Error: Could not read file from disk. Original error: {ex.Message}", "File Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedOutputPath = folderBrowserDialog.SelectedPath;
                    lblSelectedFolder.Text = $"Output Folder: {selectedOutputPath}";
                    lblStatus.Text = "Output folder selected.";
                }
            }
        }

        private string[] GetTextLinesForConversion()
        {
            if (string.IsNullOrWhiteSpace(txtEditor.Text))
            {
                return Array.Empty<string>();
            }
            return txtEditor.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                 .Where(line => !string.IsNullOrWhiteSpace(line))
                                 .ToArray();
        }

        private async void btnStartConversion_Click(object sender, EventArgs e)
        {
            if (piperProvider == null || cmbVoiceSelection.SelectedItem == null)
            {
                MessageBox.Show("TTS engine is not ready or no voice is selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error: TTS not ready or no voice selected.";
                return;
            }

            if (string.IsNullOrEmpty(selectedOutputPath))
            {
                MessageBox.Show("Please select an output folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error: Output folder not selected.";
                return;
            }

            string[] lines = GetTextLinesForConversion();
            if (lines.Length == 0)
            {
                lblStatus.Text = "Nothing to convert.";
                MessageBox.Show("The text box is empty or contains only whitespace.", "No Text", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            this.UseWaitCursor = true;
            btnStartConversion.Enabled = false;
            lblStatus.Text = "Starting conversion...";
            Application.DoEvents();

            int successCount = 0;
            int errorCount = 0;
            System.Text.StringBuilder errorDetails = new System.Text.StringBuilder();

            VoiceModel? selectedVoice = cmbVoiceSelection.SelectedItem as VoiceModel;
            if (selectedVoice == null) {
                 MessageBox.Show("Selected voice is not valid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 this.UseWaitCursor = false;
                 btnStartConversion.Enabled = true;
                 return;
            }


            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                lblStatus.Text = $"Converting line {i + 1} of {lines.Length}: "{line.Substring(0, Math.Min(line.Length, 20))}..."";
                Application.DoEvents();

                try
                {
                    string filename = SanitizeFilename(line, "speech", i + 1);
                    string fullPath = Path.Combine(selectedOutputPath, filename);

                    var synthesisConfig = new PiperSynthesisConfig()
                    {
                        Model = selectedVoice, // Use the selected voice model
                        OutputType = AudioOutputType.Mp3 // Specify MP3 output
                    };

                    // Assuming InferAsync returns byte[] for MP3. Adjust if it returns WAV and needs conversion.
                    byte[]? audioData = await piperProvider.InferAsync(line, synthesisConfig);

                    if (audioData != null && audioData.Length > 0)
                    {
                        await File.WriteAllBytesAsync(fullPath, audioData);
                        successCount++;
                    }
                    else
                    {
                        throw new Exception("TTS engine returned no audio data.");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errorDetails.AppendLine($"Error on line {i + 1} ('{line}'): {ex.Message}");
                    // Optionally, log more details: ex.ToString()
                }
            }

            this.UseWaitCursor = false;
            btnStartConversion.Enabled = true;

            string summaryMessage = $"{successCount} line(s) converted successfully.";
            if (errorCount > 0)
            {
                summaryMessage += $"\n{errorCount} line(s) failed.";
                lblStatus.Text = "Conversion complete with errors.";
                MessageBox.Show(summaryMessage + "\n\nError Details:\n" + errorDetails.ToString(), "Conversion Finished with Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                lblStatus.Text = "Conversion complete.";
                MessageBox.Show(summaryMessage, "Conversion Finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string SanitizeFilename(string inputText, string defaultNamePrefix = "speech", int lineNum = 0)
        {
            string sanitized;
            if (string.IsNullOrWhiteSpace(inputText))
            {
                sanitized = $"{defaultNamePrefix}_{lineNum}_{Guid.NewGuid().ToString().Substring(0, 4)}";
            }
            else
            {
                string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                sanitized = new string(inputText.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
                sanitized = Regex.Replace(sanitized, @"\s+", "_"); // Replace multiple whitespace with single underscore
                sanitized = Regex.Replace(sanitized, @"_+", "_"); // Replace multiple underscores with single one
                sanitized = sanitized.Length > 60 ? sanitized.Substring(0, 60) : sanitized; // Truncate
                sanitized = sanitized.Trim('_'); // Remove leading/trailing underscores

                if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Replace("_", "").Length == 0)
                {
                    sanitized = $"{defaultNamePrefix}_{lineNum}_{Guid.NewGuid().ToString().Substring(0, 4)}";
                }
            }
            return $"{sanitized}.mp3";
        }
    }
}
