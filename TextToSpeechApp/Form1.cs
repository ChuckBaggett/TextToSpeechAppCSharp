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
        private ComboBox cmbSpeakerSelection; // Will be added via Designer later
        private Label lblSpeakerSelection;    // Will be added via Designer later
        private ComboBox cmbLanguageSelection; // Will be added via Designer later
        private Label lblLanguageSelection;    // Will be added via Designer later
        private Dictionary<string, VoiceModel>? allVoicesList; 
        private Dictionary<string, uint> currentSpeakerMap = new Dictionary<string, uint>();
        private string selectedOutputPath = string.Empty;
        private string piperBaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TextToSpeechApp", "piper_tts");
        private string piperInstallationPath = string.Empty; 
        private string piperExecutablePath = string.Empty; 
        private string modelsCommonPath = string.Empty; 

        public Form1()
        {
            InitializeComponent();
            this.Load += new System.EventHandler(this.Form1_Load);
            this.cmbVoiceSelection.SelectedIndexChanged += new System.EventHandler(this.cmbVoiceSelection_SelectedIndexChanged);

            // Placeholder for cmbSpeakerSelection initialization - user will add via Designer
            this.cmbSpeakerSelection = new System.Windows.Forms.ComboBox();
            this.lblSpeakerSelection = new System.Windows.Forms.Label(); 
            // Actual properties will be set in Designer. Add basic ones here for now.
            this.lblSpeakerSelection.Name = "lblSpeakerSelection";
            this.lblSpeakerSelection.Text = "Speaker:";
            this.lblSpeakerSelection.AutoSize = true; 
            this.cmbSpeakerSelection.Name = "cmbSpeakerSelection";
            this.cmbSpeakerSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSpeakerSelection.FormattingEnabled = true;
            this.cmbSpeakerSelection.Visible = false; // Initially hidden
            this.lblSpeakerSelection.Visible = false; // Initially hidden
            // Add to Controls - User will position with Designer. For now, just add.
            // this.Controls.Add(this.lblSpeakerSelection);
            // this.Controls.Add(this.cmbSpeakerSelection);
            // The above lines for adding to Controls are commented out as it's better done via designer.
            // The subtask will focus on logic, user will handle exact placement and adding to Controls collection.

            this.cmbSpeakerSelection.SelectedIndexChanged += new System.EventHandler(this.cmbSpeakerSelection_SelectedIndexChanged);

            // Placeholder for cmbLanguageSelection initialization
            this.cmbLanguageSelection = new System.Windows.Forms.ComboBox();
            this.lblLanguageSelection = new System.Windows.Forms.Label();
            this.lblLanguageSelection.Name = "lblLanguageSelection";
            this.lblLanguageSelection.Text = "Language:";
            this.lblLanguageSelection.AutoSize = true;
            this.cmbLanguageSelection.Name = "cmbLanguageSelection";
            this.cmbLanguageSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLanguageSelection.FormattingEnabled = true;
            this.cmbLanguageSelection.Visible = false; // Initially hidden
            this.lblLanguageSelection.Visible = false; // Initially hidden
            this.cmbLanguageSelection.SelectedIndexChanged += new System.EventHandler(this.cmbLanguageSelection_SelectedIndexChanged);
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
                allVoicesList = null;
                try
                {
                    allVoicesList = await PiperDownloader.GetHuggingFaceModelList();
                }
                catch (Exception exVoiceList) // Renamed ex to exVoiceList for clarity
                {
                    MessageBox.Show($"Failed to fetch voice list: {exVoiceList.Message}", "Voice List Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // currentVoiceModel = null; // currentVoiceModel is not set here
                }

                if (allVoicesList == null || allVoicesList.Count == 0)
                {
                    // if (currentVoiceModel == null) { // currentVoiceModel is not set here
                    MessageBox.Show("No voices found or could not load voice list.", "Voice Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // }
                    // currentVoiceModel = null; // currentVoiceModel is not set here
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

                    string preferredDefaultLanguage = "English"; // Or get from config, etc.
                    if (languages.Contains(preferredDefaultLanguage))
                    {
                        cmbLanguageSelection.SelectedItem = preferredDefaultLanguage;
                    }
                    else
                    {
                        cmbLanguageSelection.SelectedIndex = 0; // Select the first language if preferred not found
                    }
                    cmbLanguageSelection.Visible = true; // Make it visible
                    lblLanguageSelection.Visible = true; // Make label visible
                }
                else
                {
                    // No languages found or NameEnglish was null/empty for all
                    cmbLanguageSelection.Visible = false;
                    lblLanguageSelection.Visible = false;
                }
                // --- END: New logic for Language ComboBox ---
                // The old logic for populating cmbVoiceSelection and loading default voice is now removed.
                // The calls to UpdateSpeakerSelectionUI and ReinitializePiperProvider are also removed from here.
                // They will be handled within PopulateVoiceSelectionComboBox.
                }
                // else for (allVoicesList == null || allVoicesList.Count == 0)
                // {
                //     // This part remains, if allVoicesList is null/empty, PopulateVoiceSelectionComboBox will handle it.
                //     // UpdateSpeakerSelectionUI(null); // This was here but is effectively handled by Populate...
                // }

                // Call to new method. This call was already correctly placed in the previous diff.
                string? initialSelectedLanguage = cmbLanguageSelection.SelectedItem?.ToString();
                await PopulateVoiceSelectionComboBox(initialSelectedLanguage);
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
                lblStatus.Text = $"Converting line {i + 1} of {lines.Length}: "{line.Substring(0, Math.Min(line.Length, 20)) + "..."}"";
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
            if (cmbVoiceSelection.SelectedItem is VoiceViewModel selectedViewModel) // Changed to VoiceViewModel
            {
                VoiceModel selectedVoice = selectedViewModel.Model; // Get the actual VoiceModel

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
                    UpdateSpeakerSelectionUI(currentVoiceModel); // Update speaker UI based on new voice
                    await ReinitializePiperProvider(); // Re-initialize with new voice and possibly new speaker
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

        private void UpdateSpeakerSelectionUI(VoiceModel? voice)
        {
            cmbSpeakerSelection.Items.Clear();
            currentSpeakerMap.Clear();
            cmbSpeakerSelection.Visible = false;
            lblSpeakerSelection.Visible = false;

            if (voice != null && voice.NumSpeakers > 0 && voice.SpeakerIdMap != null && voice.SpeakerIdMap.Any())
            {
                // Use DisplayMember for direct binding if SpeakerIdMap values are simple strings,
                // otherwise, if they are complex, populate with custom objects or formatted strings.
                // For now, assume SpeakerIdMap keys are suitable for display.
                foreach (var speakerEntry in voice.SpeakerIdMap.OrderBy(kvp => kvp.Value)) // Order by ID
                {
                    // It's safer to cast speaker ID to uint as PiperConfiguration expects uint.
                    // Piper models typically use non-negative speaker IDs.
                    uint speakerId = Convert.ToUInt32(speakerEntry.Value); 
                    currentSpeakerMap[speakerEntry.Key] = speakerId;
                    cmbSpeakerSelection.Items.Add(speakerEntry.Key); // Display the string key
                }

                if (cmbSpeakerSelection.Items.Count > 0)
                {
                    cmbSpeakerSelection.SelectedIndex = 0; // Select the first speaker by default
                    lblSpeakerSelection.Visible = true;
                    cmbSpeakerSelection.Visible = true;
                }
            }
            // No specific call to ReinitializePiperProvider() here;
            // it will be called after this method by the calling context (Form1_Load or cmbVoiceSelection_SelectedIndexChanged)
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
            btnStartConversion.Enabled = false; // Disable while reconfiguring
            lblStatus.Text = $"Configuring TTS for voice '{currentVoiceModel.Name}'...";
            Application.DoEvents();

            try
            {
                // Ensure the model is fully loaded (it should be by this point from previous steps)
                // VoiceModel fullyLoadedVoice = await VoiceModel.LoadModel(Path.Combine(modelsCommonPath, currentVoiceModel.Key));
                // currentVoiceModel = fullyLoadedVoice; // Update currentVoiceModel with the fresh instance

                // The above LoadModel might be redundant if currentVoiceModel is already the fully loaded one.
                // Let's assume currentVoiceModel is sufficient for now from Form1_Load or cmbVoiceSelection_SelectedIndexChanged logic.

                uint selectedSpeakerId = 0; // Default to 0
                if (cmbSpeakerSelection.Visible && cmbSpeakerSelection.SelectedItem != null)
                {
                    string selectedSpeakerKey = cmbSpeakerSelection.SelectedItem.ToString();
                    if (currentSpeakerMap.ContainsKey(selectedSpeakerKey))
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

        private async void cmbSpeakerSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentVoiceModel != null && cmbSpeakerSelection.SelectedItem != null)
            {
                // Voice is already loaded, just need to reconfigure PiperProvider for the new speaker
                await ReinitializePiperProvider();
            }
        }

        private async Task PopulateVoiceSelectionComboBox(string? selectedLanguage)
        {
            cmbVoiceSelection.Items.Clear();
            currentVoiceModel = null; // Reset current voice model

            if (allVoicesList == null || !allVoicesList.Any())
            {
                lblStatus.Text = "No voices available to filter.";
                UpdateSpeakerSelectionUI(null); // Hide speaker UI
                await ReinitializePiperProvider(); // Will set provider to null
                return;
            }

            // Filter voices by selected language
            // If selectedLanguage is null, default to "English" or the first available language.
            string languageToFilter = selectedLanguage ?? "English"; 
            if (allVoicesList.Values.All(v => v.Language?.NameEnglish != languageToFilter) && allVoicesList.Values.Any(v => v.Language?.NameEnglish != null))
            {
                // If preferred language (e.g. "English") is not found, pick the first available language from the list
                languageToFilter = allVoicesList.Values
                                        .Where(v => !string.IsNullOrEmpty(v.Language?.NameEnglish))
                                        .OrderBy(v => v.Language!.NameEnglish)
                                        .FirstOrDefault()?.Language!.NameEnglish ?? languageToFilter;
            }
            
            var filteredVoices = allVoicesList.Values
                .Where(v => string.Equals(v.Language?.NameEnglish, languageToFilter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(v => v.Key) // Order by Key for consistency
                .ToList();

            if (!filteredVoices.Any())
            {
                // If no voices for the selected/defaulted language, try showing all English voices if not already tried
                if (languageToFilter != "English") {
                    filteredVoices = allVoicesList.Values
                        .Where(v => string.Equals(v.Language?.NameEnglish, "English", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(v => v.Key).ToList();
                }
                // If still no voices, then show all available, regardless of language as a last resort
                if (!filteredVoices.Any()){
                     filteredVoices = allVoicesList.Values.OrderBy(v => v.Key).ToList();
                }
            }

            if (filteredVoices.Any())
            {
                foreach (VoiceModel voice in filteredVoices)
                {
                    cmbVoiceSelection.Items.Add(new VoiceViewModel(voice));
                }
                cmbVoiceSelection.DisplayMember = "DisplayName";

                // Select a default voice from the filtered list
                string preferredDefaultModelKey = "en_US-lessac-medium";
                VoiceViewModel? viewModelToSelect = null;
                
                // Try preferred key within the filtered language
                var defaultVoiceByPreferredKey = filteredVoices.FirstOrDefault(v => v.Key == preferredDefaultModelKey);
                if (defaultVoiceByPreferredKey != null)
                {
                     // Find its ViewModel wrapper
                    viewModelToSelect = cmbVoiceSelection.Items.OfType<VoiceViewModel>().FirstOrDefault(vm => vm.Model == defaultVoiceByPreferredKey);
                }

                // If not found by key, or no preferred key, select the first in the filtered list
                if (viewModelToSelect == null && cmbVoiceSelection.Items.Count > 0)
                {
                    viewModelToSelect = (VoiceViewModel)cmbVoiceSelection.Items[0];
                }
                
                if (viewModelToSelect != null)
                {
                    cmbVoiceSelection.SelectedItem = viewModelToSelect;
                    currentVoiceModel = viewModelToSelect.Model; // Set currentVoiceModel

                    lblStatus.Text = $"Loading default voice for {languageToFilter}: {currentVoiceModel.Name}...";
                    Application.DoEvents();

                    var modelDirectory = Path.Combine(modelsCommonPath, currentVoiceModel.Key);
                    if (!Directory.Exists(modelDirectory) || !File.Exists(Path.Combine(modelDirectory, "model.json")))
                    {
                        lblStatus.Text = $"Downloading voice: {currentVoiceModel.Name}...";
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
                        currentVoiceModel = await VoiceModel.LoadModel(modelDirectory); // Ensure it's fully loaded
                    }
                } else {
                    lblStatus.Text = $"No voices available for language: {languageToFilter}.";
                }
            } else {
                 lblStatus.Text = "No voices found for selected criteria.";
            }
            
            cmbVoiceSelection.Enabled = cmbVoiceSelection.Items.Count > 0;
            UpdateSpeakerSelectionUI(currentVoiceModel);
            await ReinitializePiperProvider();
        }

        private async void cmbLanguageSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbLanguageSelection.SelectedItem is string selectedLanguage)
            {
                lblStatus.Text = $"Switching to language: {selectedLanguage}...";
                Application.DoEvents();
                // Disable voice and speaker selectors and convert button while changing language
                cmbVoiceSelection.Enabled = false;
                cmbSpeakerSelection.Visible = false; // Hide speaker UI as it will be repopulated
                lblSpeakerSelection.Visible = false;
                btnStartConversion.Enabled = false;
                this.UseWaitCursor = true;

                await PopulateVoiceSelectionComboBox(selectedLanguage);

                // Re-enable voice selector if it has items, and convert button if provider is ready
                cmbVoiceSelection.Enabled = cmbVoiceSelection.Items.Count > 0;
                btnStartConversion.Enabled = (piperProvider != null);
                this.UseWaitCursor = false;
                if (piperProvider != null)
                {
                    lblStatus.Text = $"Ready for language: {selectedLanguage}. Voice '{currentVoiceModel?.Name}' loaded.";
                }
                else if (cmbVoiceSelection.Items.Count == 0) {
                    lblStatus.Text = $"No voices found for language: {selectedLanguage}.";
                } else {
                    // This case should ideally be handled by PopulateVoiceSelectionComboBox setting an error status
                    lblStatus.Text = $"Language {selectedLanguage} selected, but no voice loaded.";
                }
            }
        }
    }
}

// Added VoiceViewModel class definition
public class VoiceViewModel
{
    public VoiceModel Model { get; }
    public string DisplayName { get; }

    public VoiceViewModel(VoiceModel model)
    {
        Model = model;
        string lang = model.Language?.NameEnglish ?? model.Language?.Code ?? "unk";
        DisplayName = $"{model.Key} ({lang})"; // Default to Key (lang)
        if (!string.IsNullOrWhiteSpace(model.Name) && model.Name.ToLowerInvariant() != model.Key.ToLowerInvariant().Split('-')[0]) // Heuristic: if Name is non-empty and not just the first part of the key
        {
             // Prepend Name if it adds value (e.g., "lessac - en_US-lessac-medium (English)")
             DisplayName = $"{model.Name} - {model.Key} ({lang})";
        }
    }
}
