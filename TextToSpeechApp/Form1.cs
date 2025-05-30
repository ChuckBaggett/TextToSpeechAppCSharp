using System;
using System.IO;
using System.Linq;
using System.Collections.Generic; // Added
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using PiperSharp; // For PiperProvider, PiperDownloader
using PiperSharp.Models; // For VoiceModel, PiperConfiguration, AudioOutputType

namespace TextToSpeechApp
{
    public partial class Form1 : Form
    {
        private PiperProvider? piperProvider; // Changed from PiperService
        private VoiceModel? currentVoiceModel;
        private string selectedOutputPath = string.Empty;
        private string piperBaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TextToSpeechApp", "piper_tts");
        private string piperInstallationPath = string.Empty;
        private string piperExecutablePath = string.Empty;
        private string modelsCommonPath = string.Empty;
        private Dictionary<string, uint> currentSpeakerMap = new Dictionary<string, uint>();
        private Dictionary<string, VoiceModel>? allVoicesList;

        public Form1()
        {
            InitializeComponent();
            this.Load += new System.EventHandler(this.Form1_Load);
            this.cmbVoiceSelection.SelectedIndexChanged += new System.EventHandler(this.cmbVoiceSelection_SelectedIndexChanged);
        }


        private async void Form1_Load(object sender, EventArgs e)
        {
            this.UseWaitCursor = true;
            lblStatus.Text = "Initializing TTS engine...";
            Application.DoEvents();

            try
            {
                Directory.CreateDirectory(piperBaseDirectory);
                piperInstallationPath = Path.Combine(piperBaseDirectory, "piper");
                piperExecutablePath = Path.Combine(piperInstallationPath, PiperDownloader.PiperExecutable);
                modelsCommonPath = Path.Combine(piperBaseDirectory, "models");
                Directory.CreateDirectory(modelsCommonPath);

                lblStatus.Text = "Checking for Piper executable...";
                Application.DoEvents();
                if (!File.Exists(piperExecutablePath))
                {
                    lblStatus.Text = "Downloading Piper TTS...";
                    Application.DoEvents();

                    Stream piperDownloadStream = await PiperDownloader.DownloadPiper();
                    await Task.Run(() => piperDownloadStream.ExtractPiper(piperBaseDirectory));

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

                lblStatus.Text = "Fetching available voices...";
                Application.DoEvents();
                Dictionary<string, VoiceModel>? allVoicesList = null;
                try
                {
                    allVoicesList = await PiperDownloader.GetHuggingFaceModelList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to fetch voice list: {ex.Message}", "Voice List Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    currentVoiceModel = null;
                }

                if (allVoicesList == null || allVoicesList.Count == 0)
                {
                    if (currentVoiceModel == null)
                    {
                        MessageBox.Show("No voices found or could not load voice list.", "Voice Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    currentVoiceModel = null;
                }
                else
                {
                    cmbVoiceSelection.Items.Clear();
                    foreach (VoiceModel voice in allVoicesList.Values.OrderBy(v => v.Name))
                    {
                        cmbVoiceSelection.Items.Add(voice.Key); // TEMPORARY: Using Key for now
                    }
                    cmbVoiceSelection.DisplayMember = "DisplayName";

                    string preferredDefaultModelKey = "en_US-lessac-medium";
                    VoiceModel? voiceToLoadAsDefault = allVoicesList.Values.FirstOrDefault(v => v.Key == preferredDefaultModelKey);

                    if (voiceToLoadAsDefault == null && allVoicesList.Values.Any())
                    {
                        voiceToLoadAsDefault = allVoicesList.Values.OrderBy(v => v.Name).First();
                    }

                    if (voiceToLoadAsDefault != null)
                    {
                        currentVoiceModel = voiceToLoadAsDefault;
                        if (currentVoiceModel != null)
                        {
                            foreach (object item in cmbVoiceSelection.Items)
                            {
                                if (item is VoiceViewModel viewModel && viewModel.Model.Key == currentVoiceModel.Key)
                                {
                                    cmbVoiceSelection.SelectedItem = viewModel;
                                    break;
                                }
                            }
                        }

                        lblStatus.Text = $"Loading default voice: {currentVoiceModel.Name}...";
                        Application.DoEvents();

                        var modelDirectory = Path.Combine(modelsCommonPath, currentVoiceModel.Key);
                        if (!Directory.Exists(modelDirectory) || !File.Exists(Path.Combine(modelDirectory, "model.json")))
                        {
                            lblStatus.Text = $"Downloading default voice: {currentVoiceModel.Name}...";
                            Application.DoEvents();
                            await currentVoiceModel.DownloadModel(modelsCommonPath);

                            var expectedModelSpecificDirectory = Path.Combine(modelsCommonPath, currentVoiceModel.Key);
                            if (!File.Exists(Path.Combine(expectedModelSpecificDirectory, "model.json")))
                            {
                                throw new Exception($"Failed to download voice model files for: {currentVoiceModel.Key}.");
                            }
                            lblStatus.Text = $"Voice {currentVoiceModel.Name} downloaded.";
                            Application.DoEvents();
                        }
                        else
                        {
                            lblStatus.Text = $"Loading voice {currentVoiceModel.Name} from disk...";
                            Application.DoEvents();
                            currentVoiceModel = await VoiceModel.LoadModel(modelDirectory);
                        }
                    }
                    else
                    {
                        currentVoiceModel = null;
                        lblStatus.Text = "No suitable default voice found.";
                        MessageBox.Show("No voices could be loaded as default.", "Voice Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                UpdateSpeakerSelectionUI(currentVoiceModel);
                await ReinitializePiperProvider();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error initializing TTS: {ex.Message}";
                MessageBox.Show($"Detailed Error: {ex.ToString()}", "TTS Initialization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnStartConversion.Enabled = false;
                cmbVoiceSelection.Enabled = false;
                if (cmbSpeakerSelection != null) cmbSpeakerSelection.Enabled = false;
            }
            finally
            {
                this.UseWaitCursor = false;
                cmbVoiceSelection.Enabled = cmbVoiceSelection.Items.Count > 0;
            }
        }
        private void UpdateSpeakerSelectionUI(VoiceModel? voice)
        {
            cmbSpeakerSelection.Items.Clear();
            currentSpeakerMap.Clear();
            cmbSpeakerSelection.Visible = false;
            lblSpeakerSelection.Visible = false;

            if (voice != null && voice.NumSpeakers > 0 && voice.SpeakerIdMap != null && voice.SpeakerIdMap.Any())
            {
                foreach (var speakerEntry in voice.SpeakerIdMap.OrderBy(kvp => kvp.Value))
                {
                    uint speakerId = Convert.ToUInt32(speakerEntry.Value);
                    currentSpeakerMap[speakerEntry.Key] = speakerId;
                    cmbSpeakerSelection.Items.Add(speakerEntry.Key);
                }

                if (cmbSpeakerSelection.Items.Count > 0)
                {
                    cmbSpeakerSelection.SelectedIndex = 0;
                    lblSpeakerSelection.Visible = true;
                    cmbSpeakerSelection.Visible = true;
                }
            }
        }


        private async Task ReinitializePiperProvider()
        {
            if (currentVoiceModel == null)
            {
                piperProvider = null;
                btnStartConversion.Enabled = false;
                lblStatus.Text = "TTS Engine not ready: No voice loaded.";
                return;
            }

            this.UseWaitCursor = true;
            btnStartConversion.Enabled = false;
            lblStatus.Text = $"Configuring TTS for voice '{currentVoiceModel.Name}'...";
            Application.DoEvents();

            try
            {
                uint selectedSpeakerId = 0;
                if (cmbSpeakerSelection.Visible && cmbSpeakerSelection.SelectedItem != null && cmbSpeakerSelection.Items.Count > 0)
                {
                    string? selectedSpeakerKey = cmbSpeakerSelection.SelectedItem.ToString();
                    if (selectedSpeakerKey != null && currentSpeakerMap.ContainsKey(selectedSpeakerKey))
                    {
                        selectedSpeakerId = currentSpeakerMap[selectedSpeakerKey];
                    }
                    else if (cmbSpeakerSelection.Items.Count > 0)
                    {
                        cmbSpeakerSelection.SelectedIndex = 0;
                        selectedSpeakerKey = cmbSpeakerSelection.SelectedItem.ToString();
                        if (selectedSpeakerKey != null && currentSpeakerMap.ContainsKey(selectedSpeakerKey))
                        {
                            selectedSpeakerId = currentSpeakerMap[selectedSpeakerKey];
                        }
                    }
                }

                PiperConfiguration newConfig = new PiperConfiguration()
                {
                    ExecutableLocation = piperExecutablePath,
                    WorkingDirectory = piperInstallationPath,
                    Model = currentVoiceModel,
                    SpeakerId = selectedSpeakerId
                };
                piperProvider = new PiperProvider(newConfig);
                lblStatus.Text = $"TTS Engine ready with voice '{currentVoiceModel.Name}'" + (cmbSpeakerSelection.Visible && cmbSpeakerSelection.SelectedItem != null ? $" (Speaker: {cmbSpeakerSelection.SelectedItem})" : "") + ".";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error initializing PiperProvider: {ex.Message}";
                MessageBox.Show($"Error setting up TTS: {ex.ToString()}", "TTS Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                piperProvider = null;
            }
            finally
            {
                this.UseWaitCursor = false;
                btnStartConversion.Enabled = (piperProvider != null);
            }
        }
        private async void population(object sender, EventArgs e)
        {
            this.UseWaitCursor = true;
            lblStatus.Text = "Initializing TTS engine...";
            Application.DoEvents();

            try
            {
                Directory.CreateDirectory(piperBaseDirectory);
                piperInstallationPath = Path.Combine(piperBaseDirectory, "piper");
                piperExecutablePath = Path.Combine(piperInstallationPath, PiperDownloader.PiperExecutable); // Use PiperDownloader.PiperExecutable
                modelsCommonPath = Path.Combine(piperBaseDirectory, "models");
                Directory.CreateDirectory(modelsCommonPath);

                lblStatus.Text = "Checking for Piper executable...";
                Application.DoEvents();
                if (!File.Exists(piperExecutablePath))
                {
                    // Directory.CreateDirectory(piperInstallationPath); // piperBaseDirectory is passed to ExtractPiper which should handle subfolder creation
                    lblStatus.Text = "Downloading Piper TTS...";
                    Application.DoEvents();

                    Stream piperDownloadStream = await PiperDownloader.DownloadPiper();
                    await Task.Run(() => piperDownloadStream.ExtractPiper(piperBaseDirectory));

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

                lblStatus.Text = "Fetching available voices...";
                Application.DoEvents();
                Dictionary<string, VoiceModel>? allVoicesList = null;
                try
                {
                    allVoicesList = await PiperDownloader.GetHuggingFaceModelList();
                }
                catch (Exception exVoiceList) // Renamed ex to exVoiceList for clarity
                {
                    MessageBox.Show($"Failed to fetch voice list: {exVoiceList.Message}", "Voice List Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    currentVoiceModel = null;
                }

                if (allVoicesList == null || allVoicesList.Count == 0)
                {
                    if (currentVoiceModel == null)
                    { // Only show message if the try-catch also failed
                        MessageBox.Show("No voices found or could not load voice list.", "Voice Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    currentVoiceModel = null;
                }
                else
                {
                    // --- START: New logic for Language ComboBox ---
                    var languages = allVoicesList.Values
                        .Select(v => v.Language?.NameEnglish)
                        .Where(langName => !string.IsNullOrEmpty(langName))
                        .Distinct()
                        .OrderBy(langName => langName)
                        .ToList();

                    cmbLanguageSelection.Items.Clear();
                    if (languages.Any())
                    {
                        foreach (string langName in languages)
                        {
                            cmbLanguageSelection.Items.Add(langName);
                        }

                        string preferredDefaultLanguage = "English";
                        if (languages.Contains(preferredDefaultLanguage))
                        {
                            cmbLanguageSelection.SelectedItem = preferredDefaultLanguage;
                        }
                        else
                        {
                            cmbLanguageSelection.SelectedIndex = 0;
                        }
                        cmbLanguageSelection.Visible = true;
                        lblLanguageSelection.Visible = true;
                    }
                    else
                    {
                        cmbLanguageSelection.Visible = false;
                        lblLanguageSelection.Visible = false;
                    }
                    // --- END: New logic for Language ComboBox ---
                    cmbVoiceSelection.Items.Clear();
                    // Ensure VoiceModel has a Name property suitable for display, or use Key.
                    // Adding the VoiceModel object directly to Items is good.
                    foreach (VoiceModel voice in allVoicesList.Values.OrderBy(v => v.Key))
                    {
                        cmbVoiceSelection.Items.Add(new VoiceViewModel(voice));
                    }
                    cmbVoiceSelection.DisplayMember = "Name";

                    string preferredDefaultModelKey = "en_US-lessac-medium";
                    VoiceModel? voiceToLoadAsDefault = allVoicesList.Values.FirstOrDefault(v => v.Key == preferredDefaultModelKey);

                    if (voiceToLoadAsDefault == null && allVoicesList.Values.Any())
                    {
                        voiceToLoadAsDefault = allVoicesList.Values.OrderBy(v => v.Name).First();
                    }

                    if (voiceToLoadAsDefault != null)
                    {
                        // Set currentVoiceModel to the one we intend to load as default.
                        // This assignment is crucial before it's used by the load/download logic.
                        currentVoiceModel = voiceToLoadAsDefault;
                        cmbVoiceSelection.SelectedItem = currentVoiceModel; // Set dropdown selection

                        lblStatus.Text = $"Loading default voice: {currentVoiceModel.Name}...";
                        Application.DoEvents();

                        var modelDirectory = Path.Combine(modelsCommonPath, currentVoiceModel.Key);
                        if (!Directory.Exists(modelDirectory) || !File.Exists(Path.Combine(modelDirectory, "model.json")))
                        {
                            lblStatus.Text = $"Downloading default voice: {currentVoiceModel.Name}...";
                            Application.DoEvents();
                            await currentVoiceModel.DownloadModel(modelsCommonPath);

                            var expectedModelSpecificDirectory = Path.Combine(modelsCommonPath, currentVoiceModel.Key);
                            if (!File.Exists(Path.Combine(expectedModelSpecificDirectory, "model.json")))
                            {
                                throw new Exception($"Failed to download voice model files for: {currentVoiceModel.Key}. Expected model.json at {Path.Combine(expectedModelSpecificDirectory, "model.json")}");
                            }
                            lblStatus.Text = $"Voice {currentVoiceModel.Name} downloaded.";
                            Application.DoEvents();
                        }
                        else
                        {
                            lblStatus.Text = $"Loading voice {currentVoiceModel.Name} from disk...";
                            Application.DoEvents();
                            // LoadModel returns a new, fully initialized VoiceModel instance.
                            currentVoiceModel = await VoiceModel.LoadModel(modelDirectory);
                        }
                    }
                    else
                    {
                        currentVoiceModel = null;
                        lblStatus.Text = "No suitable default voice found.";
                        MessageBox.Show("No voices could be loaded as default.", "Voice Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // Initialize PiperProvider only if a voice was successfully loaded/selected
                if (currentVoiceModel != null)
                {
                    lblStatus.Text = "Initializing PiperProvider...";
                    Application.DoEvents();
                    PiperConfiguration config = new PiperConfiguration()
                    {
                        ExecutableLocation = piperExecutablePath,
                        WorkingDirectory = piperInstallationPath,
                        Model = currentVoiceModel
                    };
                    piperProvider = new PiperProvider(config);
                    lblStatus.Text = "TTS Engine Ready.";
                }
                else
                {
                    lblStatus.Text = "TTS Engine not ready: No voice loaded.";
                    btnStartConversion.Enabled = false; // Disable conversion if no voice
                }
            }
            catch (Exception ex) // This is the main catch block for Form1_Load
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

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                lblStatus.Text = $"Converting line {i + 1} of {lines.Length}: \"{line.Substring(0, Math.Min(line.Length, 20)) + "..."}\"";
                Application.DoEvents();

                try
                {
                    string filename = SanitizeFilename(line, "speech", i + 1);
                    string fullPath = Path.Combine(selectedOutputPath, filename);

                    byte[]? audioData = await piperProvider.InferAsync(line, AudioOutputType.Mp3);

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
                sanitized = Regex.Replace(sanitized, @"\s+", "_");
                sanitized = Regex.Replace(sanitized, @"_+", "_");
                sanitized = sanitized.Length > 60 ? sanitized.Substring(0, 60) : sanitized;
                sanitized = sanitized.Trim('_');

                if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Replace("_", "").Length == 0)
                {
                    sanitized = $"{defaultNamePrefix}_{lineNum}_{Guid.NewGuid().ToString().Substring(0, 4)}";
                }
            }
            return $"{sanitized}.mp3";
        }

        private async void cmbVoiceSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbVoiceSelection.SelectedItem is VoiceModel selectedVoice)
            {
                if (selectedVoice == currentVoiceModel && piperProvider != null)
                {
                    // No change, or already loaded
                    lblStatus.Text = $"Voice '{selectedVoice.Name}' is already active.";
                    return;
                }

                this.UseWaitCursor = true;
                btnStartConversion.Enabled = false;
                lblStatus.Text = $"Loading voice '{selectedVoice.Name}'...";
                Application.DoEvents();

                try
                {
                    var modelDirectory = Path.Combine(modelsCommonPath, selectedVoice.Key);
                    if (!Directory.Exists(modelDirectory) || !File.Exists(Path.Combine(modelDirectory, "model.json")))
                    {
                        lblStatus.Text = $"Downloading voice: {selectedVoice.Name}...";
                        Application.DoEvents();
                        // The DownloadModel method on the VoiceModel instance should handle its own metadata
                        await selectedVoice.DownloadModel(modelsCommonPath);

                        var expectedModelSpecificDirectory = Path.Combine(modelsCommonPath, selectedVoice.Key);
                        if (!File.Exists(Path.Combine(expectedModelSpecificDirectory, "model.json")))
                        {
                            throw new Exception($"Failed to download voice model files for: {selectedVoice.Key}.");
                        }
                        lblStatus.Text = $"Voice {selectedVoice.Name} downloaded.";
                        Application.DoEvents();
                    }
                    else
                    {
                        lblStatus.Text = $"Loading voice {selectedVoice.Name} from disk...";
                        Application.DoEvents();
                        // Ensure we're using a fully loaded model instance, LoadModel gives a fresh one.
                        // selectedVoice might be from the list, not necessarily fully loaded for PiperConfig.
                    }

                    // Regardless of download, ensure it's loaded into a fresh variable for PiperConfig
                    // This ensures that properties like ModelLocation are correctly set from a full load.
                    VoiceModel fullyLoadedSelectedVoice = await VoiceModel.LoadModel(Path.Combine(modelsCommonPath, selectedVoice.Key));
                    if (fullyLoadedSelectedVoice == null)
                    {
                        throw new Exception($"Could not load {selectedVoice.Name} after ensuring it is local.");
                    }

                    currentVoiceModel = fullyLoadedSelectedVoice; // Update the global currentVoiceModel

                    PiperConfiguration newConfig = new PiperConfiguration()
                    {
                        ExecutableLocation = piperExecutablePath,
                        WorkingDirectory = piperInstallationPath,
                        Model = currentVoiceModel
                    };
                    piperProvider = new PiperProvider(newConfig); // Re-initialize provider

                    lblStatus.Text = $"Voice '{currentVoiceModel.Name}' is ready.";
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Error loading voice '{selectedVoice.Name}': {ex.Message}";
                    MessageBox.Show($"Failed to load selected voice: {ex.ToString()}", "Voice Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Optionally, try to revert to a previous valid voice or disable TTS
                    btnStartConversion.Enabled = false; // Keep it disabled if voice load failed
                    currentVoiceModel = null; // No valid model
                    piperProvider = null; // No provider
                }
                finally
                {
                    this.UseWaitCursor = false;
                    // Enable conversion only if a provider exists (voice loaded successfully)
                    btnStartConversion.Enabled = (piperProvider != null);
                }
            }
        }
    }
    public class VoiceViewModel
    {
        public VoiceModel Model { get; }
        public string Name => Model.Name; // Assuming VoiceModel has a Name property
        public VoiceViewModel(VoiceModel model)
        {
            Model = model;
        }
        public override string ToString()
        {
            return Name; // Display name in ComboBox
        }
    }
}//             btnSelectFolder.Size = new Size(120, 23);
