using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds.UI
{
    /// <summary>
    /// Window for editing sound configuration parameters (pitch, volume, curves, etc.)
    /// </summary>
    public class SoundConfigEditorWindow
    {
        private bool isVisible = false;
        private string? currentSoundName = null;
        private string? currentConfigPath = null;
        private SoundType currentSoundType = SoundType.Unknown;
        private SoundConfiguration? currentConfig = null;
        private SoundConfiguration? workingConfig = null; // Copy for editing
        private Action? onSaveCallback = null; // Callback for navigation

        private Vector2 scrollPosition = Vector2.zero;
        private Rect windowRect = new Rect(200, 200, 600, 700);

        // UI state for curve editing
        private bool pitchCurveExpanded = false;
        private bool volumeCurveExpanded = false;
        private Vector2 pitchCurveScroll = Vector2.zero;
        private Vector2 volumeCurveScroll = Vector2.zero;

        // Temporary input fields for adding new keyframes
        private string newPitchKeyTime = "0.5";
        private string newPitchKeyValue = "1.0";
        private string newVolumeKeyTime = "0.5";
        private string newVolumeKeyValue = "1.0";

        // String fields for editing float values (to allow proper text input)
        private Dictionary<string, string> floatFields = new Dictionary<string, string>();

        public bool IsVisible => isVisible;

        private bool IsAudioClipSound()
        {
            var result = SoundTypes.audioClipsSoundTypes.Contains(currentSoundType);
            Main.mod?.Logger.Log($"IsAudioClipSound: currentSoundType={currentSoundType}, result={result}");
            return result;
        }

        public void Show(string soundName, string configPath, SoundType soundType, Action? saveCallback = null)
        {
            Main.mod?.Logger.Log($"SoundConfigEditorWindow.Show called for: {soundName}");
            Main.mod?.Logger.Log($"Config path: {configPath}");
            Main.mod?.Logger.Log($"Sound type: {soundType}");

            currentSoundName = soundName;
            currentConfigPath = configPath;
            currentSoundType = soundType;
            onSaveCallback = saveCallback;

            // Load the configuration
            currentConfig = Main.loaderService?.LoadSoundConfiguration(configPath);

            if (currentConfig == null)
            {
                Main.mod?.Logger.Log("No existing config found, creating default");
                // Create a new default config if none exists
                currentConfig = CreateDefaultConfig();
            }
            else
            {
                Main.mod?.Logger.Log("Loaded existing config");
            }

            // Create working copy
            workingConfig = CloneConfig(currentConfig);

            // Initialize float field strings
            InitializeFloatFields();

            isVisible = true;
            Main.mod?.Logger.Log($"Config editor window is now visible: {isVisible}");
        }

        public void Hide()
        {
            isVisible = false;
            currentSoundName = null;
            currentConfigPath = null;
            currentSoundType = SoundType.Unknown;
            currentConfig = null;
            workingConfig = null;
            onSaveCallback = null;
            floatFields.Clear();
        }

        public void OnGUI()
        {
            if (!isVisible || workingConfig == null)
                return;

            windowRect = GUILayout.Window(
                12347,
                windowRect,
                DrawWindow,
                $"Sound Configuration - {currentSoundName}",
                GUILayout.MinWidth(600),
                GUILayout.MinHeight(700)
            );
        }

        // Render content inline without window wrapper (for ModToolbarAPI hierarchical navigation)
        public void DrawInline()
        {
            if (!isVisible || workingConfig == null)
                return;

            GUILayout.Label($"=== Sound Configuration: {currentSoundName} ===", EditorStyles.BoldLabel);
            GUILayout.Space(5);
            DrawWindowContent();
        }

        private void DrawWindow(int windowID)
        {
            DrawWindowContent();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
        }

        private void DrawWindowContent()
        {
            GUILayout.BeginVertical();

            // Header info
            GUILayout.Label($"Editing: {currentSoundName}");
            GUILayout.Label($"Config: {System.IO.Path.GetFileName(currentConfigPath)}");
            GUILayout.Space(10);

            // Main content in scroll view that expands to fill window
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            // Enabled checkbox
            GUILayout.BeginHorizontal();
            GUILayout.Label("Enabled:", GUILayout.ExpandWidth(false));
            var enabled = workingConfig!.enabled ?? true;
            var newEnabled = GUILayout.Toggle(enabled, enabled ? "Yes (Config is active)" : "No (Using defaults)");
            if (newEnabled != enabled)
            {
                workingConfig.enabled = newEnabled;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Show info banner for AudioClip sounds
            if (IsAudioClipSound())
            {
                GUILayout.BeginVertical("box");
                var infoStyle = new GUIStyle(GUI.skin.label);
                infoStyle.wordWrap = true;
                GUILayout.Label("ℹ️ AudioClip sounds only support absolute pitch and volume values. Curves, min/max ranges, and randomize start time are not available.", infoStyle);
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // Only show configuration fields if enabled
            if (workingConfig.enabled ?? true)
            {
                DrawBasicSettings();
                GUILayout.Space(15);

                // Only show advanced settings for non-AudioClip sounds
                if (!IsAudioClipSound())
                {
                    DrawPitchSettings();
                    GUILayout.Space(15);
                }

                DrawVolumeSettings();
                GUILayout.Space(15);

                // Only show other settings and curves for non-AudioClip sounds
                if (!IsAudioClipSound())
                {
                    DrawOtherSettings();
                    GUILayout.Space(15);
                    DrawCurveEditor("Pitch Curve", ref pitchCurveExpanded, ref pitchCurveScroll,
                        ref workingConfig.pitchCurveData, ref newPitchKeyTime, ref newPitchKeyValue);
                    GUILayout.Space(15);
                    DrawCurveEditor("Volume Curve", ref volumeCurveExpanded, ref volumeCurveScroll,
                        ref workingConfig.volumeCurveData, ref newVolumeKeyTime, ref newVolumeKeyValue);
                }
            }
            else
            {
                GUILayout.Label("Configuration is disabled. The sound will use default settings.");
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Bottom buttons
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
            {
                SaveAndReload();
            }

            if (GUILayout.Button("Reset to Default", GUILayout.ExpandWidth(false)))
            {
                ResetToDefault();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
            {
                Hide();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawBasicSettings()
        {
            GUILayout.Label("Basic Settings", EditorStyles.BoldLabel);

            // Pitch
            DrawNullableFloatField("Pitch:", "pitch", ref workingConfig!.pitch, 0.1f, 3.0f);
        }

        private void DrawPitchSettings()
        {
            GUILayout.Label("Pitch Range (Random)", EditorStyles.BoldLabel);

            DrawNullableFloatField("Min Pitch:", "minPitch", ref workingConfig!.minPitch, 0.1f, 3.0f);
            DrawNullableFloatField("Max Pitch:", "maxPitch", ref workingConfig!.maxPitch, 0.1f, 3.0f);
        }

        private void DrawVolumeSettings()
        {
            if (IsAudioClipSound())
            {
                // For AudioClip sounds, only show a single volume field (using maxVolume)
                GUILayout.Label("Volume", EditorStyles.BoldLabel);
                DrawNullableFloatField("Volume:", "maxVolume", ref workingConfig!.maxVolume, 0.0f, 1.0f);
            }
            else
            {
                // For LayeredAudio sounds, show volume range
                GUILayout.Label("Volume Range", EditorStyles.BoldLabel);
                DrawNullableFloatField("Min Volume:", "minVolume", ref workingConfig!.minVolume, 0.0f, 1.0f);
                DrawNullableFloatField("Max Volume:", "maxVolume", ref workingConfig!.maxVolume, 0.0f, 1.0f);
            }
        }

        private void DrawOtherSettings()
        {
            GUILayout.Label("Other Settings", EditorStyles.BoldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Randomize Start Time:", GUILayout.Width(150));
            var randomize = workingConfig!.randomizeStartTime ?? false;
            var newRandomize = GUILayout.Toggle(randomize, "");
            if (newRandomize != randomize)
            {
                workingConfig.randomizeStartTime = newRandomize;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawNullableFloatField(string label, string fieldKey, ref float? value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.ExpandWidth(false));

            // Checkbox for null/not null
            var hasValue = value.HasValue;
            var newHasValue = GUILayout.Toggle(hasValue, "", GUILayout.ExpandWidth(false));

            if (newHasValue != hasValue)
            {
                if (newHasValue)
                {
                    value = (min + max) / 2f; // Default to middle
                    floatFields[fieldKey] = value.Value.ToString("F2");
                }
                else
                {
                    value = null;
                    floatFields.Remove(fieldKey);
                }
            }

            if (value.HasValue)
            {
                // Text field for input
                if (!floatFields.ContainsKey(fieldKey))
                {
                    floatFields[fieldKey] = value.Value.ToString("F2");
                }

                var newText = GUILayout.TextField(floatFields[fieldKey], GUILayout.Width(80));
                if (newText != floatFields[fieldKey])
                {
                    floatFields[fieldKey] = newText;
                    if (float.TryParse(newText, out var parsed))
                    {
                        value = Mathf.Clamp(parsed, min, max);
                    }
                }

                // Slider that expands to fill available space
                var sliderValue = GUILayout.HorizontalSlider(value.Value, min, max, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
                if (!Mathf.Approximately(sliderValue, value.Value))
                {
                    value = sliderValue;
                    floatFields[fieldKey] = value.Value.ToString("F2");
                }
            }
            else
            {
                GUILayout.Label("(null - using defaults)", GUILayout.ExpandWidth(true));
            }

            GUILayout.EndHorizontal();
        }

        private void DrawCurveEditor(string title, ref bool expanded, ref Vector2 scroll,
            ref KeyframeData[]? curveData, ref string newKeyTime, ref string newKeyValue)
        {
            GUILayout.BeginVertical("box");

            // Header with expand/collapse
            GUILayout.BeginHorizontal();
            var expandText = expanded ? "▼" : "▶";
            if (GUILayout.Button($"{expandText} {title}", GUILayout.ExpandWidth(false)))
            {
                expanded = !expanded;
            }

            var keyframeCount = curveData?.Length ?? 0;
            GUILayout.Label($"({keyframeCount} keyframes)");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (expanded)
            {
                GUILayout.Space(5);

                // Keyframe list
                if (curveData == null || curveData.Length == 0)
                {
                    GUILayout.Label("No keyframes defined");
                    curveData = new KeyframeData[0];
                }
                else
                {
                    scroll = GUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(200));

                    var keyframesToRemove = new List<int>();

                    for (int i = 0; i < curveData.Length; i++)
                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label($"#{i}", GUILayout.Width(30));
                        GUILayout.Label($"Time: {curveData[i].time:F2}", GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));
                        GUILayout.Label($"Value: {curveData[i].value:F2}", GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
                        {
                            keyframesToRemove.Add(i);
                        }

                        GUILayout.EndHorizontal();
                    }

                    // Remove marked keyframes
                    foreach (var idx in keyframesToRemove.OrderByDescending(x => x))
                    {
                        var list = curveData.ToList();
                        list.RemoveAt(idx);
                        curveData = list.ToArray();
                    }

                    GUILayout.EndScrollView();
                }

                GUILayout.Space(5);

                // Add new keyframe
                GUILayout.Label("Add Keyframe:");
                GUILayout.BeginHorizontal();

                GUILayout.Label("Time:", GUILayout.ExpandWidth(false));
                newKeyTime = GUILayout.TextField(newKeyTime, GUILayout.Width(60));

                GUILayout.Label("Value:", GUILayout.ExpandWidth(false));
                newKeyValue = GUILayout.TextField(newKeyValue, GUILayout.Width(60));

                if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
                {
                    if (float.TryParse(newKeyTime, out var time) && float.TryParse(newKeyValue, out var value))
                    {
                        var list = curveData?.ToList() ?? new List<KeyframeData>();
                        list.Add(new KeyframeData(time, value));
                        // Sort by time
                        list.Sort((a, b) => a.time.CompareTo(b.time));
                        curveData = list.ToArray();
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private void SaveAndReload()
        {
            if (workingConfig == null || currentConfigPath == null)
                return;

            try
            {
                // Save the configuration
                Main.loaderService?.SaveSoundConfiguration(currentConfigPath, workingConfig);
                Main.mod?.Logger.Log($"Saved configuration for {currentSoundName}");

                // Reload all sounds to apply changes
                Main.loaderService?.ReloadAllSounds();
                Main.mod?.Logger.Log("Reloaded all sounds with new configuration");

                // If config was disabled, we need to reset all locomotives using this sound
                if (workingConfig.enabled == false)
                {
                    Main.mod?.Logger.Log($"Config was disabled - resetting all locomotives using {currentSoundName}");
                    ResetAllLocomotivesUsingSound();
                }

                // Invoke callback to navigate back (if using inline mode)
                if (onSaveCallback != null)
                {
                    onSaveCallback();
                }
                else
                {
                    // Fallback for window mode
                    Hide();
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to save configuration: {ex.Message}");
            }
        }

        private void ResetAllLocomotivesUsingSound()
        {
            // Find all locomotives and reset any that are using this sound
            try
            {
                var allCars = UnityEngine.Object.FindObjectsOfType<TrainCar>();
                foreach (var car in allCars)
                {
                    if (Main.registryService == null || Main.restoratorService == null || Main.applicatorService == null)
                        continue;

                    var soundSet = Main.registryService.GetSoundSet(car);

                    // Check if this car has any custom sound that matches our disabled config
                    foreach (var kvp in soundSet.sounds.ToList())
                    {
                        if (kvp.Value.name == currentSoundName)
                        {
                            Main.mod?.Logger.Log($"Resetting {kvp.Key} on {car.ID} due to disabled config");

                            // Remove from sound set
                            soundSet.sounds.Remove(kvp.Key);

                            // Restore to vanilla
                            Main.restoratorService.RestoreSound(car, kvp.Key);

                            // Save updated sound set
                            Main.registryService.SaveSoundState(car, soundSet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to reset locomotives: {ex.Message}");
            }
        }

        private void ResetToDefault()
        {
            if (currentConfigPath == null)
                return;

            try
            {
                // Disable the configuration file
                Main.loaderService?.DisableSoundConfiguration(currentConfigPath);
                Main.mod?.Logger.Log($"Disabled configuration file: {currentConfigPath}");

                // Reload sounds to apply the changes
                Main.loaderService?.ReloadAllSounds();
                Main.mod?.Logger.Log("Reloaded sounds after disabling configuration");

                // Close the editor
                if (onSaveCallback != null)
                {
                    onSaveCallback();
                }
                else
                {
                    Hide();
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to reset to default: {ex.Message}");
            }
        }

        private void InitializeFloatFields()
        {
            floatFields.Clear();

            if (workingConfig == null) return;

            if (workingConfig.pitch.HasValue)
                floatFields["pitch"] = workingConfig.pitch.Value.ToString("F2");
            if (workingConfig.minPitch.HasValue)
                floatFields["minPitch"] = workingConfig.minPitch.Value.ToString("F2");
            if (workingConfig.maxPitch.HasValue)
                floatFields["maxPitch"] = workingConfig.maxPitch.Value.ToString("F2");
            if (workingConfig.minVolume.HasValue)
                floatFields["minVolume"] = workingConfig.minVolume.Value.ToString("F2");
            if (workingConfig.maxVolume.HasValue)
                floatFields["maxVolume"] = workingConfig.maxVolume.Value.ToString("F2");
        }

        private SoundConfiguration CreateDefaultConfig()
        {
            return new SoundConfiguration
            {
                enabled = true,
                pitch = 1.0f,
                minPitch = null,
                maxPitch = null,
                minVolume = 0.0f,
                maxVolume = 1.0f,
                randomizeStartTime = false,
                pitchCurveData = new[]
                {
                    new KeyframeData(0.0f, 1.0f),
                    new KeyframeData(1.0f, 1.0f)
                },
                volumeCurveData = new[]
                {
                    new KeyframeData(0.0f, 1.0f),
                    new KeyframeData(1.0f, 1.0f)
                }
            };
        }

        private SoundConfiguration CloneConfig(SoundConfiguration source)
        {
            return new SoundConfiguration
            {
                enabled = source.enabled,
                pitch = source.pitch,
                minPitch = source.minPitch,
                maxPitch = source.maxPitch,
                minVolume = source.minVolume,
                maxVolume = source.maxVolume,
                randomizeStartTime = source.randomizeStartTime,
                pitchCurveData = source.pitchCurveData?.ToArray(),
                volumeCurveData = source.volumeCurveData?.ToArray()
            };
        }
    }

    /// <summary>
    /// Helper class to provide bold label style for IMGUI
    /// </summary>
    internal static class EditorStyles
    {
        private static GUIStyle? _boldLabel;

        public static GUIStyle BoldLabel
        {
            get
            {
                if (_boldLabel == null)
                {
                    _boldLabel = new GUIStyle(GUI.skin.label);
                }
                return _boldLabel;
            }
        }
    }
}

