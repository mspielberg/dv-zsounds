using System;
using System.Collections.Generic;
using System.Linq;
using DvMod.ZSounds.Patches;
using UnityEngine;

namespace DvMod.ZSounds.UI
{
    public class SoundManagerUI
    {
        private static SoundManagerUI? instance;
        private bool isVisible = false;
        private Vector2 scrollPosition = Vector2.zero;
        private SoundEditorWindow? editorWindow = null;

        // Cache for locomotives to avoid expensive FindObjectsOfType calls every frame
        private List<TrainCar>? cachedLocomotives = null;

        // Navigation state
        private enum UILevel
        {
            LocomotiveList,
            SoundEditor,
            ConfigEditor
        }
        private UILevel currentLevel = UILevel.LocomotiveList;

        public static SoundManagerUI Instance
        {
            get
            {
                if (instance == null)
                    instance = new SoundManagerUI();
                return instance;
            }
        }

        public void Toggle()
        {
            isVisible = !isVisible;
        }

        public void Show()
        {
            isVisible = true;
            // Invalidate cache to get fresh locomotive list when opening
            cachedLocomotives = null;
        }

        public void Hide()
        {
            isVisible = false;
            currentLevel = UILevel.LocomotiveList;
            if (editorWindow != null)
            {
                editorWindow.Hide();
            }
        }

        // Navigate back from Config Editor to Sound Editor
        public void NavigateBackToSoundEditor()
        {
            currentLevel = UILevel.SoundEditor;
            scrollPosition = Vector2.zero;
        }

        public void OnGUI()
        {
            // When used with ModToolbarAPI, this is only called when visible
            GUILayout.BeginVertical();

            // Render content based on current navigation level
            switch (currentLevel)
            {
                case UILevel.LocomotiveList:
                    DrawLocomotiveList();
                    break;
                case UILevel.SoundEditor:
                    DrawSoundEditorInline();
                    break;
                case UILevel.ConfigEditor:
                    DrawConfigEditorInline();
                    break;
            }

            GUILayout.EndVertical();
        }

        private void DrawLocomotiveList()
        {
            // Header
            GUILayout.Label("Select a locomotive to edit its sounds:");
            GUILayout.Space(10);

            // Get all loaded locomotives
            var locomotives = GetAllLocomotives();

            if (locomotives.Count == 0)
            {
                GUILayout.Label("No locomotives found. Spawn a locomotive to manage its sounds.");
            }
            else
            {
                // Scrollable list that expands to fill available window space
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

                foreach (var loco in locomotives)
                {
                    DrawLocomotiveEntry(loco);
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            // Buttons at the bottom
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh Locomotives", GUILayout.ExpandWidth(false)))
            {
                cachedLocomotives = null; // Force cache refresh
            }

            if (GUILayout.Button("Reload Sounds", GUILayout.ExpandWidth(false)))
            {
                ReloadSoundsFromDisk();
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        private void DrawSoundEditorInline()
        {
            // Back button
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back to Locomotive List", GUILayout.ExpandWidth(false)))
            {
                currentLevel = UILevel.LocomotiveList;
                scrollPosition = Vector2.zero;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Render editor content
            if (editorWindow != null && editorWindow.IsVisible)
            {
                editorWindow.DrawInline(_ =>
                {
                    currentLevel = UILevel.ConfigEditor;
                    scrollPosition = Vector2.zero;
                });
            }
        }

        private void DrawConfigEditorInline()
        {
            // Back button
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("← Back to Sound Editor", GUILayout.ExpandWidth(false)))
            {
                currentLevel = UILevel.SoundEditor;
                scrollPosition = Vector2.zero;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Render config editor content
            if (editorWindow != null)
            {
                editorWindow.DrawConfigEditorInline();
            }
        }

        private void DrawLocomotiveEntry(TrainCar locomotive)
        {
            // Check if locomotive is valid (not null and not destroyed)
            if (locomotive == null || !locomotive)
            {
                return;
            }

            GUILayout.BeginHorizontal("box");

            // Locomotive info
            var locoType = locomotive.name.Remove(locomotive.name.Length - 7); // Remove "(Clone)"
            var locoID = locomotive.ID;

            // Use new service
            var isCustomized = Main.registryService?.IsCustomized(locomotive) ?? false;

            GUILayout.Label($"{locoID}", GUILayout.MinWidth(150), GUILayout.ExpandWidth(true));
            GUILayout.Label($"({locoType})", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));

            if (isCustomized)
            {
                GUILayout.Label("[Custom]", GUILayout.ExpandWidth(false));
            }

            GUILayout.FlexibleSpace();

            // Edit button
            if (GUILayout.Button("Edit Sounds", GUILayout.ExpandWidth(false)))
            {
                OpenEditor(locomotive);
            }

            GUILayout.EndHorizontal();
        }

        private void OpenEditor(TrainCar locomotive)
        {
            if (editorWindow == null)
            {
                editorWindow = new SoundEditorWindow();
            }
            editorWindow.SetLocomotive(locomotive);
            editorWindow.Show();
            currentLevel = UILevel.SoundEditor;
            scrollPosition = Vector2.zero;
        }

        private List<TrainCar> GetAllLocomotives()
        {
            // Use cache if available, otherwise refresh
            if (cachedLocomotives == null)
            {
                RefreshLocomotiveCache();
            }

            // Filter out any destroyed objects from cache
            var validLocomotives = cachedLocomotives?.Where(l => l != null && l).ToList() ?? new List<TrainCar>();

            // If we lost locomotives, update the cache
            if (cachedLocomotives != null && validLocomotives.Count != cachedLocomotives.Count)
            {
                cachedLocomotives = validLocomotives;
            }

            return validLocomotives;
        }

        private void RefreshLocomotiveCache()
        {
            var locomotives = new List<TrainCar>();

            try
            {
                // Get all train cars in the world
                var allCars = UnityEngine.Object.FindObjectsOfType<TrainCar>();
                if (allCars != null)
                {
                    foreach (var car in allCars)
                    {
                        // Only include locomotives that are locos
                        if (car != null && Main.IsLoco(car.carLivery))
                        {
                            locomotives.Add(car);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to get locomotives: {ex.Message}");
            }

            // Sort by ID for consistent display
            cachedLocomotives = locomotives.OrderBy(l => l.ID).ToList();
        }

        private void ReloadSoundsFromDisk()
        {
            try
            {
                Main.mod?.Logger.Log("UI: Reloading sounds from disk...");
                Main.loaderService?.ReloadAllSounds();
                CommsRadioSoundSwitcherAPI.Reinitialize();
                Main.mod?.Logger.Log("UI: Sound reload complete");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to reload sounds: {ex.Message}");
            }
        }
    }
}
