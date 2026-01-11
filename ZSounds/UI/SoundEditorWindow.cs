using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DvMod.ZSounds.SoundHandler;

namespace DvMod.ZSounds.UI
{
    public class SoundEditorWindow
    {
        private bool isVisible = false;
        private TrainCar? currentLocomotive = null;
        private Vector2 scrollPosition = Vector2.zero;
        private Rect windowRect = new Rect(150, 150, 700, 500);
        private Dictionary<SoundType, bool> dropdownStates = new Dictionary<SoundType, bool>();
        private Dictionary<SoundType, Vector2> dropdownScrollPositions = new Dictionary<SoundType, Vector2>();

        // Generic sound dropdown tracking (using int keys to avoid enum conflicts)
        private Dictionary<int, bool> genericDropdownStates = new Dictionary<int, bool>();
        private Dictionary<int, Vector2> genericDropdownScrollPositions = new Dictionary<int, Vector2>();

        // Config editor window
        private SoundConfigEditorWindow? configEditorWindow = null;

        public bool IsVisible => isVisible;

        public void Show()
        {
            isVisible = true;
        }

        public void Hide()
        {
            isVisible = false;
            dropdownStates.Clear();
            dropdownScrollPositions.Clear();
            genericDropdownStates.Clear();
            genericDropdownScrollPositions.Clear();
        }

        public void SetLocomotive(TrainCar locomotive)
        {
            currentLocomotive = locomotive;
            dropdownStates.Clear();
            dropdownScrollPositions.Clear();
            Show();
        }

        public void OnGUI()
        {
            if (!isVisible || currentLocomotive == null)
                return;

            windowRect = GUILayout.Window(
                12346,
                windowRect,
                DrawWindow,
                $"Sound Editor - {currentLocomotive.ID}",
                GUILayout.MinWidth(700),
                GUILayout.MinHeight(500)
            );

            // Draw config editor window if open
            if (configEditorWindow != null && configEditorWindow.IsVisible)
            {
                configEditorWindow.OnGUI();
            }
        }

        // Render content inline without window wrapper (for ModToolbarAPI hierarchical navigation)
        public void DrawInline(Action<TrainCar> onOpenConfigEditor)
        {
            if (!isVisible || currentLocomotive == null)
                return;

            DrawWindowContent(onOpenConfigEditor);
        }

        // Render config editor inline
        public void DrawConfigEditorInline()
        {
            if (configEditorWindow != null && configEditorWindow.IsVisible)
            {
                configEditorWindow.DrawInline();
            }
        }

        private void DrawWindow(int windowID)
        {
            DrawWindowContent(null);

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
        }

        private void DrawWindowContent(Action<TrainCar>? onOpenConfigEditor = null)
        {
            GUILayout.BeginVertical();

            // Header
            GUILayout.Label($"Editing sounds for: {currentLocomotive!.ID} ({currentLocomotive.carType})");
            GUILayout.Space(10);

            // Get available sound types for this locomotive
            var soundTypes = GetAvailableSoundTypes();

            if (soundTypes.Count == 0)
            {
                GUILayout.Label("No sound types available for this locomotive.");
            }
            else
            {
                // Scrollable list that expands to fill available window space
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

                foreach (var soundType in soundTypes)
                {
                    DrawSoundTypeEntry(soundType, onOpenConfigEditor);
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            // Buttons at the bottom
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset All Sounds", GUILayout.ExpandWidth(false)))
            {
                ResetAllSounds();
            }

            GUILayout.FlexibleSpace();

            if (onOpenConfigEditor == null && GUILayout.Button("Close", GUILayout.ExpandWidth(false)))
            {
                Hide();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawSoundTypeEntry(SoundType soundType, Action<TrainCar>? onOpenConfigEditor)
        {
            GUILayout.BeginVertical("box");

            // For Unknown type, show generic sounds list instead
            if (soundType == SoundType.Unknown)
            {
                DrawGenericSoundsSection(onOpenConfigEditor);
            }
            else
            {
                GUILayout.BeginHorizontal();

                // Sound type name
                GUILayout.Label(soundType.ToString(), GUILayout.MinWidth(120), GUILayout.ExpandWidth(true));

                // Current sound
                var currentSound = GetCurrentSound(soundType);
                var displayName = currentSound ?? "Default";
                GUILayout.Label($"Current: {displayName}", GUILayout.MinWidth(150), GUILayout.ExpandWidth(true));

                GUILayout.FlexibleSpace();

                // Toggle dropdown button
                var isDropdownOpen = dropdownStates.ContainsKey(soundType) && dropdownStates[soundType];
                var buttonText = isDropdownOpen ? "Close ▲" : "Select Sound ▼";

                if (GUILayout.Button(buttonText, GUILayout.ExpandWidth(false)))
                {
                    dropdownStates[soundType] = !isDropdownOpen;
                }

                GUILayout.EndHorizontal();

                // Draw dropdown if open
                if (isDropdownOpen)
                {
                    DrawSoundDropdown(soundType, onOpenConfigEditor);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawGenericSoundsSection(Action<TrainCar>? onOpenConfigEditor)
        {
            if (currentLocomotive == null) return;

            var genericSounds = Main.discoveryService?.GetGenericSoundNames(currentLocomotive.carType) ?? new HashSet<string>();

            if (genericSounds.Count == 0) return;

            // Section header
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Other Sounds", GUILayout.ExpandWidth(true));
            GUILayout.Label($"({genericSounds.Count} generic sound{(genericSounds.Count > 1 ? "s" : "")})", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Draw each generic sound as a selectable entry
            foreach (var soundName in genericSounds.OrderBy(s => s))
            {
                DrawGenericSoundEntry(soundName, onOpenConfigEditor);
            }
        }

        private void DrawGenericSoundEntry(string soundName, Action<TrainCar>? onOpenConfigEditor)
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();

            // Sound name
            GUILayout.Label(soundName, GUILayout.MinWidth(120), GUILayout.ExpandWidth(true));

            // Current sound status
            var currentSound = GetCurrentGenericSound(soundName);
            var displayName = currentSound ?? "Default";
            GUILayout.Label($"Current: {displayName}", GUILayout.MinWidth(150), GUILayout.ExpandWidth(true));

            GUILayout.FlexibleSpace();

            // Toggle dropdown button (use negative hash to avoid collision with SoundType keys)
            var dropdownKey = -(soundName.GetHashCode());
            var isDropdownOpen = genericDropdownStates.ContainsKey(dropdownKey) && genericDropdownStates[dropdownKey];
            var buttonText = isDropdownOpen ? "Close ▲" : "Select Sound ▼";

            if (GUILayout.Button(buttonText, GUILayout.ExpandWidth(false)))
            {
                genericDropdownStates[dropdownKey] = !isDropdownOpen;
            }

            GUILayout.EndHorizontal();

            // Draw dropdown if open
            if (isDropdownOpen)
            {
                DrawGenericSoundDropdown(soundName, dropdownKey, onOpenConfigEditor);
            }

            GUILayout.EndVertical();
        }

        private void DrawGenericSoundDropdown(string soundName, int dropdownKey, Action<TrainCar>? onOpenConfigEditor)
        {
            GUILayout.BeginVertical("box");

            var availableSounds = GetAvailableGenericSounds();

            if (availableSounds.Count == 0)
            {
                GUILayout.Label("No sounds available in Other folder.");
            }
            else
            {
                // Initialize scroll position if needed
                if (!genericDropdownScrollPositions.ContainsKey(dropdownKey))
                {
                    genericDropdownScrollPositions[dropdownKey] = Vector2.zero;
                }

                // Scrollable list of sounds (max height 200)
                genericDropdownScrollPositions[dropdownKey] = GUILayout.BeginScrollView(
                    genericDropdownScrollPositions[dropdownKey],
                    GUILayout.MaxHeight(200),
                    GUILayout.ExpandWidth(true)
                );

                foreach (var sound in availableSounds)
                {
                    GUILayout.BeginHorizontal();

                    var soundDisplayName = GetSoundDisplayName(sound);

                    if (GUILayout.Button(soundDisplayName, GUILayout.ExpandWidth(true)))
                    {
                        ApplyGenericSound(soundName, sound.name);
                        genericDropdownStates[dropdownKey] = false; // Close dropdown after selection
                    }

                    // Configure button
                    if (GUILayout.Button("Config", GUILayout.ExpandWidth(false)))
                    {
                        Main.mod?.Logger.Log($"Gear button clicked for generic sound: {sound.name}");
                        OpenConfigEditor(sound, onOpenConfigEditor);
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }

            // Reset to default button
            GUILayout.Space(5);
            if (GUILayout.Button("Reset to Default", GUILayout.ExpandWidth(true)))
            {
                ResetGenericSound(soundName);
                genericDropdownStates[dropdownKey] = false;
            }

            GUILayout.EndVertical();
        }

        private void DrawSoundDropdown(SoundType soundType, Action<TrainCar>? onOpenConfigEditor)
        {
            GUILayout.BeginVertical("box");

            var availableSounds = GetAvailableSounds(soundType);

            if (availableSounds.Count == 0)
            {
                GUILayout.Label("No sounds available for this type.");
            }
            else
            {
                // Initialize scroll position if needed
                if (!dropdownScrollPositions.ContainsKey(soundType))
                {
                    dropdownScrollPositions[soundType] = Vector2.zero;
                }

                // Scrollable list of sounds
                dropdownScrollPositions[soundType] = GUILayout.BeginScrollView(
                    dropdownScrollPositions[soundType],
                    GUILayout.MaxHeight(300),
                    GUILayout.ExpandWidth(true)
                );

                foreach (var sound in availableSounds)
                {
                    GUILayout.BeginHorizontal();

                    var soundName = GetSoundDisplayName(sound);

                    if (GUILayout.Button(soundName, GUILayout.ExpandWidth(true)))
                    {
                        ApplySound(soundType, sound.name);
                        dropdownStates[soundType] = false; // Close dropdown after selection
                    }

                    // Configure button
                    if (GUILayout.Button("Config", GUILayout.ExpandWidth(false)))
                    {
                        Main.mod?.Logger.Log($"Config button clicked for sound: {sound.name}");
                        OpenConfigEditor(sound, onOpenConfigEditor);
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }

            // Reset to default button
            GUILayout.Space(5);
            if (GUILayout.Button("Reset to Default", GUILayout.ExpandWidth(false)))
            {
                ResetSound(soundType);
                dropdownStates[soundType] = false;
            }

            GUILayout.EndVertical();
        }

        private List<SoundType> GetAvailableSoundTypes()
        {
            if (currentLocomotive == null)
                return new List<SoundType>();

            // Use discoveryService
            var supportedTypes = Main.discoveryService?.GetSupportedSoundTypes(currentLocomotive.carType)
                              ?? new HashSet<SoundType>();

            var typesList = supportedTypes.OrderBy(st => st.ToString()).ToList();

            // Add "Unknown" type if there are generic sounds
            var genericSounds = Main.discoveryService?.GetGenericSoundNames(currentLocomotive.carType) ?? new HashSet<string>();
            if (genericSounds.Count > 0)
            {
                typesList.Add(SoundType.Unknown); // Will be displayed as "Other"
            }

            return typesList;
        }

        private List<SoundDefinition> GetAvailableSounds(SoundType soundType)
        {
            if (currentLocomotive == null)
                return new List<SoundDefinition>();

            // Use loaderService - pass the TrainCar instance to support custom locomotives
            var availableSounds = Main.loaderService?.GetAvailableSoundsForTrain(currentLocomotive);

            if (availableSounds != null && availableSounds.TryGetValue(soundType, out var sounds))
            {
                var identifier = SoundDiscovery.GetTrainIdentifier(currentLocomotive);
                return sounds.OrderBy(s => GetSoundDisplayName(s)).ToList();
            }

            var id = SoundDiscovery.GetTrainIdentifier(currentLocomotive);
            Main.DebugLog(() => $"UI: No sounds found for {id}/{soundType}");
            return new List<SoundDefinition>();
        }

        private string? GetCurrentSound(SoundType soundType)
        {
            if (currentLocomotive == null)
                return null;

            // Use registryService
            var soundSet = Main.registryService?.GetSoundSet(currentLocomotive);

            if (soundSet.sounds.TryGetValue(soundType, out var soundDef))
            {
                return GetSoundDisplayName(soundDef);
            }

            return null;
        }

        private string GetSoundDisplayName(SoundDefinition sound)
        {
            // Extract just the filename without path and extension
            if (!string.IsNullOrEmpty(sound.filename))
            {
                return System.IO.Path.GetFileNameWithoutExtension(sound.filename);
            }

            return sound.name;
        }

        private void ApplySound(SoundType soundType, string soundName)
        {
            if (currentLocomotive == null)
                return;

            try
            {
                // Use new services if available, fallback to legacy
                if (Main.loaderService != null && Main.registryService != null)
                {
                    Main.loaderService.ApplySoundToTrain(currentLocomotive, soundType, soundName);
                    Main.registryService.MarkAsCustomized(currentLocomotive);
                    Main.mod?.Logger.Log($"Applied {soundType} sound '{soundName}' to {currentLocomotive.ID}");
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to apply sound: {ex.Message}");
            }
        }

        private void ResetSound(SoundType soundType)
        {
            if (currentLocomotive == null)
                return;

            try
            {
                // Use new services if available, fallback to legacy
                if (Main.registryService != null && Main.restoratorService != null && Main.applicatorService != null)
                {
                    var soundSet = Main.registryService.GetSoundSet(currentLocomotive);
                    soundSet.sounds.Remove(soundType);

                    // Restore the specific sound to default
                    Main.restoratorService.RestoreSound(currentLocomotive, soundType);

                    // Apply the updated sound set
                    Main.applicatorService.ApplySoundSet(currentLocomotive, soundSet);

                    // Save the updated sound state
                    Main.registryService.SaveSoundState(currentLocomotive, soundSet);

                    Main.mod?.Logger.Log($"Reset {soundType} sound for {currentLocomotive.ID}");
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to reset sound: {ex.Message}");
            }
        }

        private void ResetAllSounds()
        {
            if (currentLocomotive == null)
                return;

            try
            {
                // Use new services if available, fallback to legacy
                if (Main.registryService != null && Main.restoratorService != null)
                {
                    var soundSet = Main.registryService.GetSoundSet(currentLocomotive);
                    soundSet.sounds.Clear();

                    // Restore all sounds to default
                    Main.restoratorService.RestoreAllSounds(currentLocomotive);

                    Main.registryService.ClearCustomization(currentLocomotive);

                    // Remove from persistent state
                    Main.registryService.RemoveSoundState(currentLocomotive.ID);

                    Main.mod?.Logger.Log($"Reset all sounds for {currentLocomotive.ID}");
                }

                // Close all dropdowns
                dropdownStates.Clear();
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to reset all sounds: {ex.Message}");
            }
        }

        // Generic sound helper methods

        private string? GetCurrentGenericSound(string soundName)
        {
            if (currentLocomotive == null)
                return null;

            var soundSet = Main.registryService?.GetSoundSet(currentLocomotive);
            if (soundSet == null)
                return null;

            // Get the generic sound definition
            var soundDef = soundSet.GetGenericSound(soundName);
            if (soundDef != null)
            {
                return soundDef.filename ?? soundDef.name;
            }

            return null;
        }

        private List<SoundDefinition> GetAvailableGenericSounds()
        {
            if (currentLocomotive == null)
                return new List<SoundDefinition>();

            // Get sounds from the "Other" folder
            var availableSounds = Main.loaderService?.GetAvailableSoundsForTrain(currentLocomotive.carType);

            if (availableSounds != null && availableSounds.TryGetValue(SoundType.Unknown, out var sounds))
            {
                return sounds.OrderBy(s => GetSoundDisplayName(s)).ToList();
            }

            return new List<SoundDefinition>();
        }

        private void ApplyGenericSound(string soundName, string soundFileName)
        {
            if (currentLocomotive == null)
                return;

            try
            {
                Main.mod?.Logger.Log($"Applying generic sound '{soundFileName}' to '{soundName}' on {currentLocomotive.ID}");

                // Get the sound definition
                var sound = Main.loaderService?.GetSound(soundFileName);
                if (sound == null)
                {
                    Main.mod?.Logger.Error($"Sound '{soundFileName}' not found");
                    return;
                }

                // Get or create sound set for this car
                var soundSet = Main.registryService?.GetSoundSet(currentLocomotive);
                if (soundSet == null)
                {
                    Main.mod?.Logger.Error($"Could not get sound set for {currentLocomotive.ID}");
                    return;
                }

                // Store the generic sound definition by name
                soundSet.SetGenericSound(soundName, sound);

                // Mark this car as customized
                Main.registryService?.MarkAsCustomized(currentLocomotive);

                // Save the state
                Main.registryService?.SaveSoundState(currentLocomotive, soundSet);

                Main.mod?.Logger.Log($"Successfully applied generic sound '{soundFileName}' to '{soundName}'");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to apply generic sound: {ex.Message}");
            }
        }

        private void ResetGenericSound(string soundName)
        {
            if (currentLocomotive == null)
                return;

            try
            {
                Main.mod?.Logger.Log($"Resetting generic sound '{soundName}' to default on {currentLocomotive.ID}");

                var soundSet = Main.registryService?.GetSoundSet(currentLocomotive);
                if (soundSet == null)
                    return;

                // Remove the generic sound customization
                soundSet.RemoveGenericSound(soundName);

                // Save the state
                Main.registryService?.SaveSoundState(currentLocomotive, soundSet);

                Main.mod?.Logger.Log($"Successfully reset generic sound '{soundName}' to default");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to reset generic sound: {ex.Message}");
            }
        }

        private void OpenConfigEditor(SoundDefinition sound, Action<TrainCar>? onOpenConfigEditor)
        {
            Main.mod?.Logger.Log($"Opening config editor for sound: {sound.name}");

            if (sound.configPath == null)
            {
                Main.mod?.Logger.Warning($"No config path available for sound: {sound.name}");
                return;
            }

            Main.mod?.Logger.Log($"Config path: {sound.configPath}");

            if (configEditorWindow == null)
            {
                configEditorWindow = new SoundConfigEditorWindow();
                Main.mod?.Logger.Log("Created new SoundConfigEditorWindow instance");
            }

            Action? saveCallback = null;
            if (onOpenConfigEditor != null)
            {
                saveCallback = () =>
                {
                    UI.SoundManagerUI.Instance.NavigateBackToSoundEditor();
                };
            }

            configEditorWindow.Show(sound.name, sound.configPath, sound.type, saveCallback);
            Main.mod?.Logger.Log($"Config editor window shown for {sound.name}");

            if (onOpenConfigEditor != null && currentLocomotive != null)
            {
                onOpenConfigEditor(currentLocomotive);
            }
        }
    }
}

