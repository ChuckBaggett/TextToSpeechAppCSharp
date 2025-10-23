using System;
using System.Collections.Generic;
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
        private PiperProvider? piperProvider;
        private VoiceModel? currentVoiceModel;
        private string selectedOutputPath = string.Empty;
        private readonly string piperBaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TextToSpeechApp", "piper_tts");
        private string piperInstallationPath = string.Empty;
        private string piperExecutablePath = string.Empty;
        private string modelsCommonPath = string.Empty;

        private Dictionary<string, VoiceModel>? allVoicesList;
        private readonly Dictionary<string, uint> currentSpeakerMap = new Dictionary<string, uint>();
        private System.Media.SoundPlayer? soundPlayer;

        public Form1()
        {
            InitializeComponent();

            this.Load += new System.EventHandler(this.Form1_Load);
            if (this.cmbVoiceSelection != null) this.cmbVoiceSelection.SelectedIndexChanged += new System.EventHandler(this.cmbVoiceSelection_SelectedIndexChanged);
            if (this.cmbSpeakerSelection != null) this.cmbSpeakerSelection.SelectedIndexChanged += new System.EventHandler(this.cmbSpeakerSelection_SelectedIndexChanged);
            if (this.cmbLanguageSelection != null) this.cmbLanguageSelection.SelectedIndexChanged += new System.EventHandler(this.cmbLanguageSelection_SelectedIndexChanged);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            this.UseWaitCursor = true;
            SafeSetStatus("Initializing TTS engine...");
            Application.DoEvents();

            try
            {
                Directory.CreateDirectory(piperBaseDirectory);
                piperInstallationPath = Path.Combine(piperBaseDirectory, "piper");
                piperExecutablePath = Path.Combine(piperInstallationPath, PiperDownloader.PiperExecutable);
                modelsCommonPath = Path.Combine(piperBaseDirectory, "models");
                Directory.CreateDirectory(modelsCommonPath);

                SafeSetStatus("Checking for Piper executable...");
                Application.DoEvents();
                if (!File.Exists(piperExecutablePath))
                {
                    SafeSetStatus("Downloading Piper TTS...");
                    Application.DoEvents();
                    Stream piperDownloadStream = await PiperDownloader.DownloadPiper();
                    await Task.Run(() => piperDownloadStream.ExtractPiper(piperBaseDirectory));
                    if (!File.Exists(piperExecutablePath))
                    {
                        throw new FileNotFoundException("Piper executable not found after download and extraction.", piperExecutablePath);
                    }
                    SafeSetStatus("Piper executable downloaded and extracted.");
                    Application.DoEvents();
                }
                else
                {
                    SafeSetStatus("Piper executable found.");
                    Application.DoEvents();
                }

                SafeSetStatus("Fetching available voices...");
                Application.DoEvents();
                this.allVoicesList = null;
                try
                {
                    this.allVoicesList = await PiperDownloader.GetHuggingFaceModelList();
                }
                catch (Exception exVoiceList)
                {
                    MessageBox.Show($"Failed to fetch voice list: {exVoiceList.Message}. Please check your internet connection.", "Voice List Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (this.allVoicesList == null || !this.allVoicesList.Any())
                {
                    MessageBox.Show("No voices found or could not load voice list. Ensure internet connection for first run.", "Voice Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    currentVoiceModel = null;
                    SafeControlSetVisibility(cmbLanguageSelection, false);
                    SafeControlSetVisibility(lblLanguageSelection, false);
                    SafeControlSetEnabled(btnStartConversion, false);
                    SafeControlSetEnabled(cmbVoiceSelection, false);
                }
                else
                {
                    var languages = this.allVoicesList.Values
                        .Where(v => v.Language != null && !string.IsNullOrEmpty(v.Language.Name))
                        .Select(v => v.Language!.Name)
                        .Distinct()
                        .OrderBy(langName => langName)
                        .ToList();

                    if (cmbLanguageSelection != null)
                    {
                        if (languages.Any())
                        {
                            SafeComboBoxClearAndAddRange(cmbLanguageSelection, languages.Cast<object>().ToArray());

                            string preferredDefaultLanguage = "English";
                            if (languages.Contains(preferredDefaultLanguage))
                            {
                                cmbLanguageSelection.SelectedItem = preferredDefaultLanguage;
                            }
                            else if (cmbLanguageSelection.Items.Count > 0)
                            {
                                cmbLanguageSelection.SelectedIndex = 0;
                            }
                            SafeControlSetVisibility(cmbLanguageSelection, true);
                            SafeControlSetVisibility(lblLanguageSelection, true);
                        }
                        else
                        {
                            SafeControlSetVisibility(cmbLanguageSelection, false);
                            SafeControlSetVisibility(lblLanguageSelection, false);
                        }
                    }

                    string? initialSelectedLanguage = cmbLanguageSelection?.SelectedItem?.ToString();
                    await PopulateVoiceSelectionComboBox(initialSelectedLanguage);
                }
            }
            catch (Exception ex)
            {
                SafeSetStatus($"Error initializing TTS: {ex.Message}");
                MessageBox.Show($"Detailed Error during initialization: {ex.ToString()}", "TTS Initialization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SafeControlSetEnabled(btnStartConversion, false);
                SafeControlSetEnabled(cmbVoiceSelection, false);
                SafeControlSetEnabled(cmbSpeakerSelection, false);
                SafeControlSetEnabled(cmbLanguageSelection, false);
            }
            finally
            {
                this.UseWaitCursor = false;
                if (cmbVoiceSelection != null) SafeControlSetEnabled(cmbVoiceSelection, cmbVoiceSelection.Items.Count > 0 && currentVoiceModel != null);
            }
        }

        private async Task PopulateVoiceSelectionComboBox(string? selectedLanguage)
        {
            if (cmbVoiceSelection == null) return;

            SafeComboBoxClear(cmbVoiceSelection);
            currentVoiceModel = null;

            if (this.allVoicesList == null || !this.allVoicesList.Any())
            {
                SafeSetStatus("No voices available to filter.");
                UpdateSpeakerSelectionUI(null);
                await ReinitializePiperProvider();
                return;
            }

            string languageToFilter = selectedLanguage ?? "English";

            var allAvailableRealLanguages = this.allVoicesList.Values
                                     .Where(v => v.Language != null && !string.IsNullOrEmpty(v.Language.Name))
                                     .Select(v => v.Language!.Name)
                                     .Distinct().ToList();

            if (!allAvailableRealLanguages.Contains(languageToFilter) && allAvailableRealLanguages.Any())
            {
                languageToFilter = allAvailableRealLanguages.OrderBy(l => l).First();
                SafeComboBoxSetSelected(cmbLanguageSelection, languageToFilter);
            }

            var filteredVoices = this.allVoicesList.Values
                .Where(v => v.Language != null && string.Equals(v.Language.Name, languageToFilter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(v => v.Key)
                .ToList();

            if (!filteredVoices.Any() && allAvailableRealLanguages.Contains("English") && !string.Equals(languageToFilter, "English", StringComparison.OrdinalIgnoreCase))
            {
                languageToFilter = "English";
                filteredVoices = this.allVoicesList.Values
                    .Where(v => v.Language != null && string.Equals(v.Language.Name, languageToFilter, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(v => v.Key)
                    .ToList();
                SafeComboBoxSetSelected(cmbLanguageSelection, languageToFilter);
            }
            if (!filteredVoices.Any() && this.allVoicesList.Any())
            {
                filteredVoices = this.allVoicesList.Values.OrderBy(v => v.Key).ToList();
            }

            if (filteredVoices.Any())
            {
                SafeComboBoxClearAndAddRange(cmbVoiceSelection, filteredVoices.Select(v => new VoiceViewModel(v)).ToArray<object>());
                cmbVoiceSelection.DisplayMember = "DisplayName";

                string preferredDefaultModelKey = "en_US-lessac-medium";
                VoiceViewModel? viewModelToSelect = null;

                var defaultVoiceInList = filteredVoices.FirstOrDefault(v => v.Key == preferredDefaultModelKey);
                if (defaultVoiceInList != null)
                {
                    if (cmbVoiceSelection != null) viewModelToSelect = cmbVoiceSelection.Items.OfType<VoiceViewModel>().FirstOrDefault(vm => vm.Model.Key == defaultVoiceInList.Key);
                }

                if (viewModelToSelect == null && cmbVoiceSelection != null && cmbVoiceSelection.Items.Count > 0)
                {
                    viewModelToSelect = (VoiceViewModel)cmbVoiceSelection.Items[0];
                }

                if (viewModelToSelect != null)
                {
                    if (cmbVoiceSelection != null) cmbVoiceSelection.SelectedItem = viewModelToSelect;
                    VoiceModel tempModelForLoading = viewModelToSelect.Model;

                    SafeSetStatus($"Loading voice for {languageToFilter}: {tempModelForLoading.Name ?? tempModelForLoading.Key}...");
                    Application.DoEvents();

                    var modelDirectory = Path.Combine(modelsCommonPath, tempModelForLoading.Key);
                    if (!Directory.Exists(modelDirectory) || !File.Exists(Path.Combine(modelDirectory, "model.json")))
                    {
                        SafeSetStatus($"Downloading voice: {tempModelForLoading.Name ?? tempModelForLoading.Key}...");
                        Application.DoEvents();
                        await tempModelForLoading.DownloadModel(modelsCommonPath);
                        var expectedModelSpecificDirectory = Path.Combine(modelsCommonPath, tempModelForLoading.Key);
                        if (!File.Exists(Path.Combine(expectedModelSpecificDirectory, "model.json")))
                        {
                            throw new Exception($"Failed to download/verify voice model: {tempModelForLoading.Key}. File not found: {Path.Combine(expectedModelSpecificDirectory, "model.json")}");
                        }
                        SafeSetStatus($"Voice {tempModelForLoading.Name ?? tempModelForLoading.Key} downloaded.");
                        Application.DoEvents();
                    }

                    currentVoiceModel = await VoiceModel.LoadModel(Path.Combine(modelsCommonPath, tempModelForLoading.Key));
                    if (currentVoiceModel == null)
                    {
                        throw new Exception($"Failed to load model {tempModelForLoading.Key} from disk.");
                    }
                    SafeSetStatus($"Voice {currentVoiceModel.Name ?? currentVoiceModel.Key} loaded from disk.");
                    Application.DoEvents();
                }
                else
                {
                    currentVoiceModel = null;
                    SafeSetStatus($"No voices available for language: {languageToFilter}.");
                }
            }
            else
            {
                currentVoiceModel = null;
                SafeSetStatus("No voices found for selected criteria.");
            }

            if (cmbVoiceSelection != null) SafeControlSetEnabled(cmbVoiceSelection, cmbVoiceSelection.Items.Count > 0);
            UpdateSpeakerSelectionUI(currentVoiceModel);
            if (btnPlaySample != null && cmbVoiceSelection.SelectedItem is VoiceViewModel selectedVoice)
            {
                bool isEnglishVoice = selectedVoice.Model.Language.Family.Equals("en", StringComparison.InvariantCultureIgnoreCase);
                SafeControlSetEnabled(btnPlaySample, isEnglishVoice);
            }
            await ReinitializePiperProvider();
        }

        private void UpdateSpeakerSelectionUI(VoiceModel? voice)
        {
            if (cmbSpeakerSelection == null || lblSpeakerSelection == null || currentSpeakerMap == null) return;

            SafeComboBoxClear(cmbSpeakerSelection);
            currentSpeakerMap.Clear();
            SafeControlSetVisibility(cmbSpeakerSelection, false);
            SafeControlSetVisibility(lblSpeakerSelection, false);

            if (voice != null && voice.NumSpeakers > 0 && voice.SpeakerIdMap != null && voice.SpeakerIdMap.Any())
            {
                var speakerItems = new List<object>();
                foreach (var speakerEntry in voice.SpeakerIdMap.OrderBy(kvp => kvp.Value))
                {
                    uint speakerId = Convert.ToUInt32(speakerEntry.Value);
                    currentSpeakerMap[speakerEntry.Key] = speakerId;
                    speakerItems.Add(speakerEntry.Key);
                }
                SafeComboBoxClearAndAddRange(cmbSpeakerSelection, speakerItems.ToArray());

                if (cmbSpeakerSelection.Items.Count > 0)
                {
                    cmbSpeakerSelection.SelectedIndex = 0;
                    SafeControlSetVisibility(lblSpeakerSelection, true);
                    SafeControlSetVisibility(cmbSpeakerSelection, true);
                }
            }
        }

        private async Task ReinitializePiperProvider()
        {
            if (currentVoiceModel == null)
            {
                piperProvider = null;
                SafeControlSetEnabled(btnStartConversion, false);
                SafeSetStatus("TTS Engine not ready: No voice loaded.");
                return;
            }

            this.UseWaitCursor = true;
            SafeControlSetEnabled(btnStartConversion, false);
            SafeSetStatus($"Configuring TTS for voice '{currentVoiceModel.Name ?? currentVoiceModel.Key}'...");
            Application.DoEvents();

            try
            {
                uint selectedSpeakerId = 0;
                if (cmbSpeakerSelection != null && cmbSpeakerSelection.Visible && cmbSpeakerSelection.SelectedItem != null && cmbSpeakerSelection.Items.Count > 0)
                {
                    string? selectedSpeakerKey = cmbSpeakerSelection.SelectedItem.ToString();
                    if (selectedSpeakerKey != null && currentSpeakerMap.ContainsKey(selectedSpeakerKey))
                    {
                        selectedSpeakerId = currentSpeakerMap[selectedSpeakerKey];
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
                string speakerInfo = (cmbSpeakerSelection != null && cmbSpeakerSelection.Visible && cmbSpeakerSelection.SelectedItem != null ? $" (Speaker: {cmbSpeakerSelection.SelectedItem})" : "");
                SafeSetStatus($"TTS Engine ready with voice '{currentVoiceModel.Name ?? currentVoiceModel.Key}'{speakerInfo}.");
            }
            catch (Exception ex)
            {
                SafeSetStatus($"Error initializing PiperProvider: {ex.Message}");
                MessageBox.Show($"Error setting up TTS: {ex.ToString()}", "TTS Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                piperProvider = null;
            }
            finally
            {
                this.UseWaitCursor = false;
                SafeControlSetEnabled(btnStartConversion, piperProvider != null);
            }
        }

        private async void cmbVoiceSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbVoiceSelection == null || lblStatus == null || btnStartConversion == null) return;

            if (cmbVoiceSelection.SelectedItem is VoiceViewModel selectedViewModel)
            {
                VoiceModel selectedVoice = selectedViewModel.Model;
                if (currentVoiceModel != null && currentVoiceModel.Key == selectedVoice.Key && piperProvider != null)
                {
                    SafeSetStatus($"Voice '{selectedVoice.Name ?? selectedVoice.Key}' is already active.");
                    UpdateSpeakerSelectionUI(currentVoiceModel);
                    await ReinitializePiperProvider();
                    return;
                }

                this.UseWaitCursor = true;
                SafeControlSetEnabled(btnStartConversion, false);
                SafeControlSetEnabled(cmbLanguageSelection, false);
                SafeControlSetEnabled(cmbSpeakerSelection, false);

                SafeSetStatus($"Loading voice '{selectedVoice.Name ?? selectedVoice.Key}'...");
                Application.DoEvents();

                try
                {
                    var modelDirectory = Path.Combine(modelsCommonPath, selectedVoice.Key);
                    if (!Directory.Exists(modelDirectory) || !File.Exists(Path.Combine(modelDirectory, "model.json")))
                    {
                        SafeSetStatus($"Downloading voice: {selectedVoice.Name ?? selectedVoice.Key}...");
                        Application.DoEvents();
                        await selectedVoice.DownloadModel(modelsCommonPath);
                        var expectedModelSpecificDirectory = Path.Combine(modelsCommonPath, selectedVoice.Key);
                        if (!File.Exists(Path.Combine(expectedModelSpecificDirectory, "model.json")))
                        {
                            throw new Exception($"Failed to download/verify voice model: {selectedVoice.Key}. File not found: {Path.Combine(expectedModelSpecificDirectory, "model.json")}");
                        }
                        SafeSetStatus($"Voice {selectedVoice.Name ?? selectedVoice.Key} downloaded.");
                        Application.DoEvents();
                    }

                    currentVoiceModel = await VoiceModel.LoadModel(Path.Combine(modelsCommonPath, selectedVoice.Key));
                    if (currentVoiceModel == null)
                    {
                        throw new Exception($"Could not load {selectedVoice.Name ?? selectedVoice.Key} after ensuring it is local.");
                    }
                    SafeSetStatus($"Voice {currentVoiceModel.Name ?? currentVoiceModel.Key} loaded.");
                    Application.DoEvents();

                    UpdateSpeakerSelectionUI(currentVoiceModel);
                    await ReinitializePiperProvider();
                }
                catch (Exception ex)
                {
                    SafeSetStatus($"Error loading voice '{selectedVoice.Name ?? selectedVoice.Key}': {ex.Message}");
                    MessageBox.Show($"Failed to load selected voice: {ex.ToString()}", "Voice Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    currentVoiceModel = null;
                    piperProvider = null;
                    UpdateSpeakerSelectionUI(null);
                }
                finally
                {
                    this.UseWaitCursor = false;
                    SafeControlSetEnabled(btnStartConversion, piperProvider != null);
                    SafeControlSetEnabled(cmbLanguageSelection, true);
                    if (cmbSpeakerSelection != null) SafeControlSetEnabled(cmbSpeakerSelection, cmbSpeakerSelection.Items.Count > 0 && piperProvider != null);
                
                    if (btnPlaySample != null)
                    {
                        bool isEnglishVoice = selectedViewModel.Model.Language.Family.Equals("en", StringComparison.InvariantCultureIgnoreCase);
                        SafeControlSetEnabled(btnPlaySample, isEnglishVoice);
                    }
                }
            }
        }

        private async void cmbLanguageSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbLanguageSelection == null || lblStatus == null || cmbVoiceSelection == null || btnStartConversion == null) return;

            if (cmbLanguageSelection.SelectedItem is string selectedLanguage)
            {
                SafeSetStatus($"Switching to language: {selectedLanguage}...");
                Application.DoEvents();

                SafeControlSetEnabled(cmbVoiceSelection, false);
                SafeControlSetVisibility(cmbSpeakerSelection, false);
                SafeControlSetVisibility(lblSpeakerSelection, false);
                SafeControlSetEnabled(btnStartConversion, false);
                this.UseWaitCursor = true;

                await PopulateVoiceSelectionComboBox(selectedLanguage);

                SafeControlSetEnabled(cmbVoiceSelection, cmbVoiceSelection.Items.Count > 0);
                SafeControlSetEnabled(btnStartConversion, piperProvider != null);
                this.UseWaitCursor = false;

                if (piperProvider != null && currentVoiceModel != null)
                {
                    SafeSetStatus($"Ready for language: {selectedLanguage}. Voice '{currentVoiceModel.Name ?? currentVoiceModel.Key}' loaded.");
                }
                else if (cmbVoiceSelection.Items.Count == 0)
                {
                    SafeSetStatus($"No voices found for language: {selectedLanguage}.");
                }
                else
                {
                    SafeSetStatus($"Language {selectedLanguage} selected, but no voice loaded for TTS.");
                }
            }
        }

        private async void cmbSpeakerSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentVoiceModel != null && cmbSpeakerSelection != null && cmbSpeakerSelection.SelectedItem != null)
            {
                await ReinitializePiperProvider();
            }
        }

        private void btnPlaySample_Click(object sender, EventArgs e)
        {
            if (cmbVoiceSelection.SelectedItem is VoiceViewModel selectedVoice)
            {
                try
                {
                    if (soundPlayer != null)
                    {
                        soundPlayer.Stop();
                        soundPlayer.Dispose();
                        soundPlayer = null;
                    }

                    string voiceKey = selectedVoice.Model.Key;
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var resourceName = $"TextToSpeechApp.Samples.{voiceKey}.wav";

                    using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            soundPlayer = new System.Media.SoundPlayer(stream);
                            soundPlayer.Play();
                        }
                        else
                        {
                            MessageBox.Show($"Sample not found for voice: {voiceKey}", "Sample Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing sample: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                        if (txtEditor != null) txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
                        SafeSetStatus($"File loaded: {Path.GetFileName(openFileDialog.FileName)}");
                    }
                    catch (Exception ex)
                    {
                        SafeSetStatus("Error reading file.");
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
                    if (lblSelectedFolder != null) lblSelectedFolder.Text = $"Output Folder: {selectedOutputPath}";
                    SafeSetStatus("Output folder selected.");
                }
            }
        }

        private string[] GetTextLinesForConversion()
        {
            if (txtEditor == null || string.IsNullOrWhiteSpace(txtEditor.Text))
            {
                return Array.Empty<string>();
            }
            return txtEditor.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                 .Where(line => !string.IsNullOrWhiteSpace(line))
                                 .ToArray();
        }

        private async void btnStartConversion_Click(object sender, EventArgs e)
        {
            if (piperProvider == null || cmbVoiceSelection == null || cmbVoiceSelection.SelectedItem == null)
            {
                MessageBox.Show("TTS engine is not ready or no voice is selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SafeSetStatus("Error: TTS not ready or no voice selected.");
                return;
            }

            if (string.IsNullOrEmpty(selectedOutputPath))
            {
                MessageBox.Show("Please select an output folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SafeSetStatus("Error: Output folder not selected.");
                return;
            }

            string[] lines = GetTextLinesForConversion();
            if (lines.Length == 0)
            {
                SafeSetStatus("Nothing to convert.");
                MessageBox.Show("The text box is empty or contains only whitespace.", "No Text", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            this.UseWaitCursor = true;
            SafeControlSetEnabled(btnStartConversion, false);
            SafeSetStatus("Starting conversion...");
            Application.DoEvents();

            int successCount = 0;
            int errorCount = 0;
            System.Text.StringBuilder errorDetails = new System.Text.StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                SafeSetStatus($"Converting line {i + 1} of {lines.Length}: \"{line.Substring(0, Math.Min(line.Length, 20)) + "..."}\"");
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
            SafeControlSetEnabled(btnStartConversion, true);

            string summaryMessage = $"{successCount} line(s) converted successfully.";
            if (errorCount > 0)
            {
                summaryMessage += $"\n{errorCount} line(s) failed.";
                SafeSetStatus("Conversion complete with errors.");
                MessageBox.Show(summaryMessage + "\n\nError Details:\n" + errorDetails.ToString(), "Conversion Finished with Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                SafeSetStatus("Conversion complete.");
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

        // Helper methods for safe UI updates 
        private void SafeSetStatus(string text)
        {
            if (lblStatus == null) return;
            if (lblStatus.InvokeRequired) lblStatus.Invoke(new Action(() => lblStatus.Text = text));
            else lblStatus.Text = text;
        }

        private void SafeControlSetEnabled(Control? ctl, bool enabled)
        {
            if (ctl == null) return;
            if (ctl.InvokeRequired) ctl.Invoke(new Action(() => ctl.Enabled = enabled));
            else ctl.Enabled = enabled;
        }

        private void SafeControlSetVisibility(Control? ctl, bool visible)
        {
            if (ctl == null) return;
            if (ctl.InvokeRequired) ctl.Invoke(new Action(() => ctl.Visible = visible));
            else ctl.Visible = visible;
        }

        private void SafeComboBoxClear(ComboBox? cmb)
        {
            if (cmb == null) return;
            if (cmb.InvokeRequired) cmb.Invoke(new Action(() => cmb.Items.Clear()));
            else cmb.Items.Clear();
        }

        private void SafeComboBoxClearAndAddRange(ComboBox? cmb, object[] items)
        {
            if (cmb == null) return;
            if (cmb.InvokeRequired) cmb.Invoke(new Action(() => { cmb.Items.Clear(); cmb.Items.AddRange(items); }));
            else { cmb.Items.Clear(); cmb.Items.AddRange(items); }
        }

        private void SafeComboBoxSetSelected(ComboBox? cmb, object? item)
        {
            if (cmb == null) return;
            if (cmb.InvokeRequired) cmb.Invoke(new Action(() => cmb.SelectedItem = item));
            else cmb.SelectedItem = item;
        }
    }
}