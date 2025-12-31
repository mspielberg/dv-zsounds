using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DV;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds.Patches
{
    // Registers a CarChanger-style Comms Radio mode
    public static class CommsRadioSoundSwitcherAPI
    {
        public static void Initialize()
        {
            // No-op: Harmony in Main.PatchAll will discover our patch below and register the mode.
            Main.DebugLog(() => "CommsRadio: Sound Switcher mode ready (Harmony registration)");
        }

        public static void Cleanup()
        {
            // Nothing to cleanup explicitly; CommsRadio recreates modes per controller instance.
        }

        public static void Reinitialize()
        {
            // Nothing dynamic to rewire; the mode queries sounds live each time.
        }

        // Harmony patches that attach and register our mode component
        internal static class CommsRadioControllerPatches
        {
            [HarmonyPatch(typeof(CommsRadioController), "Awake")]
            internal static class Awake
            {
                private static void Postfix(CommsRadioController __instance)
                {
                    try
                    {
                        var existing = __instance.GetComponent<CommsRadioSoundSwitcherMode>();
                        if (existing == null)
                        {
                            existing = __instance.gameObject.AddComponent<CommsRadioSoundSwitcherMode>();
                            Main.DebugLog(() => "CommsRadio: Sound Switcher mode component added to controller");
                        }
                        CommsRadioModeRegistrar.TryRegisterOnce(__instance, existing);
                    }
                    catch (Exception ex)
                    {
                        Main.mod?.Logger.Warning($"Failed to attach Sound Switcher mode: {ex.Message}");
                    }
                }
            }

            [HarmonyPatch(typeof(CommsRadioController), "Start")]
            internal static class Start
            {
                private static void Postfix(CommsRadioController __instance)
                {
                    try
                    {
                        var existing = __instance.GetComponent<CommsRadioSoundSwitcherMode>();
                        if (existing != null)
                        {
                            CommsRadioModeRegistrar.TryRegisterOnce(__instance, existing);
                        }
                    }
                    catch (Exception ex)
                    {
                        Main.mod?.Logger.Warning($"Failed to register Sound Switcher mode in Start: {ex.Message}");
                    }
                }
            }
        }

        internal static class CommsRadioModeRegistrar
        {
            private static readonly HashSet<int> Registered = new HashSet<int>();

            internal static void TryRegisterOnce(CommsRadioController controller, ICommsRadioMode mode)
            {
                var id = controller.GetInstanceID();
                if (Registered.Contains(id))
                    return;
                if (TryRegisterMode(controller, mode, "SOUNDS"))
                {
                    Registered.Add(id);
                }
            }

            private static bool TryRegisterMode(CommsRadioController controller, ICommsRadioMode mode, string title)
            {
                try
                {
                    var fields = controller.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var f in fields)
                    {
                        if (!typeof(System.Collections.IList).IsAssignableFrom(f.FieldType))
                            continue;
                        var t = f.FieldType;
                        var gen = t.IsGenericType ? t.GetGenericArguments().FirstOrDefault() : null;
                        if (gen == null) continue;
                        if (!typeof(ICommsRadioMode).IsAssignableFrom(gen)) continue;

                        var list = (System.Collections.IList?)f.GetValue(controller);
                        if (list == null) continue;
                        if (!list.Contains(mode))
                        {
                            list.Add(mode);
                            Main.DebugLog(() => $"CommsRadio: Injected Sound Switcher into field '{f.Name}' ({t.Name})");
                            return true;
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Main.mod?.Logger.Warning($"CommsRadio: AddMode registration failed: {ex.Message}");
                    return false;
                }
            }
        }
    }

    internal class CommsRadioSoundSwitcherMode : MonoBehaviour, ICommsRadioMode
    {
        private enum State
        {
            PointAtCar,
            CarSelected_SelectType,
            CarSelected_SelectSound,
        }

        // UI/Controller
        public CommsRadioController Controller = null!;
        public CommsRadioDisplay Display = null!;
        public Transform SignalOrigin = null!;

        public ButtonBehaviourType ButtonBehaviour { get; private set; }

        // Highlight
        private static readonly Vector3 HighlightExtra = new Vector3(0.25f, 0.8f, 0f);
        private GameObject _highlighter = null!;
        private Renderer _highlighterRender = null!;

        // Raycast/selection
        private int _mask;
        private TrainCar _pointedCar = null!;
        private TrainCar _selectedCar = null!;
        private State _state;

        // Data for selection
        private SoundType[] _availableTypes = Array.Empty<SoundType>();
        private int _typeIndex;

        private readonly List<SoundDefinition> _availableSounds = new List<SoundDefinition>();
        private int _soundIndex;

        private void Awake()
        {
            Controller = GetComponent<CommsRadioController>();
            SignalOrigin = Controller.laserBeam.transform;
            Display = Controller.cargoLoaderControl.display;

            // Be generous with colliders in VR/PC
            _mask = LayerMask.GetMask("Train_Big_Collider", "Train_Car", "TrainCar", "Default");

            // Setup highlight
            _highlighter = Instantiate(Controller.cargoLoaderControl.trainHighlighter);
            _highlighter.SetActive(false);
            _highlighterRender = _highlighter.GetComponentInChildren<MeshRenderer>(true);

            SetStartingDisplay();
        }

        private void OnDestroy()
        {
            if (_highlighter != null)
            {
                Destroy(_highlighter.gameObject);
            }
        }

        public void SetStartingDisplay()
        {
            Display.SetDisplay("SOUNDS", "Aim at the vehicle you wish to change sounds on.");
            ButtonBehaviour = ButtonBehaviourType.Regular;
            _state = State.PointAtCar;
        }

        public void Enable()
        {
            SetStartingDisplay();
        }

        public void Disable()
        {
            ClearHighlightCar();
            ButtonBehaviour = ButtonBehaviourType.Regular;
            _state = State.PointAtCar;
            _pointedCar = null!;
            _selectedCar = null!;
            _availableTypes = Array.Empty<SoundType>();
            _availableSounds.Clear();
        }

        public void OnUpdate()
        {
            Controller.laserBeam.SetBeamColor(GetLaserBeamColor());

            // Update pointer
            if (Physics.Raycast(SignalOrigin.position, SignalOrigin.forward, out var hit, 200f, _mask))
            {
                var car = TrainCar.Resolve(hit.transform.root);
                PointToCar(car);
            }
            else
            {
                PointToCar(null!);
            }

            switch (_state)
            {
                case State.PointAtCar:
                    if (_pointedCar)
                    {
                        Display.SetContent($"{_pointedCar.carType} ({_pointedCar.ID})");
                    }
                    else
                    {
                        SetStartingDisplay();
                    }
                    break;

                case State.CarSelected_SelectType:
                    // Show current sound type
                    if (_availableTypes.Length == 0)
                    {
                        Display.SetContentAndAction($"{_selectedCar.carType} ({_selectedCar.ID})\nNo sound types available", "cancel");
                        ButtonBehaviour = ButtonBehaviourType.Regular;
                    }
                    else
                    {
                        var currentType = _availableTypes[_typeIndex];
                        Display.SetContentAndAction($"{_selectedCar.carType} ({_selectedCar.ID})\n{currentType}", "next");
                        ButtonBehaviour = ButtonBehaviourType.Override;
                    }
                    break;

                case State.CarSelected_SelectSound:
                    if (_availableSounds.Count == 0)
                    {
                        Display.SetContentAndAction($"{_selectedCar.carType} ({_selectedCar.ID})\nNo sounds", "cancel");
                        ButtonBehaviour = ButtonBehaviourType.Regular;
                    }
                    else
                    {
                        var sound = _availableSounds[_soundIndex];
                        var warn = (GetCurrentType() == SoundType.HornHit) ? "\n[WARNING: May not work if changed before]" : string.Empty;
                        Display.SetContentAndAction($"{_selectedCar.carType} ({_selectedCar.ID})\n{sound.name}{warn}", "confirm");
                        ButtonBehaviour = ButtonBehaviourType.Override;
                    }
                    break;
            }
        }

        public void OnUse()
        {
            switch (_state)
            {
                case State.PointAtCar:
                    if (!_pointedCar || !IsLocomotive(_pointedCar))
                    {
                        // cancel beep
                        return;
                    }
                    SelectCar(_pointedCar);
                    return;

                case State.CarSelected_SelectType:
                    if (_availableTypes.Length == 0)
                    {
                        // back to start
                        _state = State.PointAtCar;
                        return;
                    }
                    // If HornHit, show warning as in old flow, else go to sounds
                    if (GetCurrentType() == SoundType.HornHit)
                    {
                        // Directly proceed; warning is displayed in content
                    }
                    BuildAvailableSounds();
                    _state = State.CarSelected_SelectSound;
                    return;

                case State.CarSelected_SelectSound:
                    if (_availableSounds.Count == 0)
                    {
                        _state = State.CarSelected_SelectType;
                        return;
                    }
                    ApplySelectedSound();
                    // After apply, return to start for next target
                    _state = State.PointAtCar;
                    return;
            }
        }

        public bool ButtonACustomAction()
        {
            switch (_state)
            {
                case State.CarSelected_SelectType:
                    if (_availableTypes.Length > 0)
                    {
                        _typeIndex = Wrap(_typeIndex + 1, _availableTypes.Length);
                        return true;
                    }
                    return false;
                case State.CarSelected_SelectSound:
                    if (_availableSounds.Count > 0)
                    {
                        _soundIndex = Wrap(_soundIndex + 1, _availableSounds.Count);
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public bool ButtonBCustomAction()
        {
            switch (_state)
            {
                case State.CarSelected_SelectType:
                    if (_availableTypes.Length > 0)
                    {
                        _typeIndex = Wrap(_typeIndex - 1, _availableTypes.Length);
                        return true;
                    }
                    return false;
                case State.CarSelected_SelectSound:
                    if (_availableSounds.Count > 0)
                    {
                        _soundIndex = Wrap(_soundIndex - 1, _availableSounds.Count);
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public void OverrideSignalOrigin(Transform signalOrigin)
        {
            SignalOrigin = signalOrigin;
        }

        public Color GetLaserBeamColor()
        {
            // purple-ish and animated like sample, but stable hue for clarity
            return new Color(0.53f, 0f, 1f);
        }

        // --- Selection helpers ---

        private void SelectCar(TrainCar car)
        {
            _selectedCar = car;
            ButtonBehaviour = ButtonBehaviourType.Override;

            BuildAvailableTypes();
            _typeIndex = Wrap(_typeIndex, Math.Max(_availableTypes.Length, 1));
            _state = State.CarSelected_SelectType;
        }

        private void BuildAvailableTypes()
        {
            var list = new List<SoundType>();

            // Check if loader service is available (new or legacy)
            var hasLoaderService = Main.loaderService != null;
            if (!hasLoaderService)
            {
                _availableTypes = Array.Empty<SoundType>();
                return;
            }

            foreach (var st in Enum.GetValues(typeof(SoundType)).Cast<SoundType>())
            {
                if (st == SoundType.Unknown) continue;
                if (!HasAvailableSounds(_selectedCar, st)) continue;

                bool supported = (st == SoundType.EngineStartup || st == SoundType.EngineShutdown);

                // Try new discoveryService first, fallback to legacy AudioMapper
                if (!supported)
                {
                    if (Main.discoveryService != null)
                    {
                        // Use new service
                        bool hasLayered = SoundTypes.layeredAudioSoundTypes.Contains(st) &&
                                        Main.discoveryService.GetLayeredAudio(_selectedCar, st) != null;
                        bool hasClips = SoundTypes.audioClipsSoundTypes.Contains(st) &&
                                      Main.discoveryService.GetAudioClipPortReader(_selectedCar, st) != null;
                        supported = hasLayered || hasClips;
                    }
                }

                if (supported)
                    list.Add(st);
            }

            _availableTypes = list.ToArray();
        }

        private void BuildAvailableSounds()
        {
            _availableSounds.Clear();

            // Try new service first, fallback to legacy
            var carSounds = Main.loaderService?.GetAvailableSoundsForTrain(_selectedCar.carType);

            if (carSounds == null)
                return;

            if (carSounds.TryGetValue(GetCurrentType(), out var carSpecific))
                _availableSounds.AddRange(carSpecific);

            _soundIndex = Wrap(_soundIndex, Math.Max(_availableSounds.Count, 1));
        }

        private SoundType GetCurrentType() =>
            (_availableTypes.Length == 0) ? SoundType.Unknown : _availableTypes[_typeIndex];

        private static bool HasAvailableSounds(TrainCar car, SoundType soundType)
        {
            // Try new service first, fallback to legacy
            var carSounds = Main.loaderService?.GetAvailableSoundsForTrain(car.carType);

            if (carSounds == null)
                return false;

            return carSounds.TryGetValue(soundType, out var specific) && specific.Count > 0;
        }

        private void ApplySelectedSound()
        {
            if (_availableSounds.Count == 0) return;

            var selected = _availableSounds[_soundIndex];
            Main.DebugLog(() => $"CommsRadio: Applying {selected.name} ({selected.type}) to {_selectedCar.ID}");

            // Use new services if available, fallback to legacy
            if (Main.registryService != null && Main.applicatorService != null)
            {
                // New service architecture
                var soundSet = Main.registryService.GetSoundSet(_selectedCar);
                selected.Apply(soundSet);
                Main.registryService.MarkAsCustomized(_selectedCar);
                Main.applicatorService.ApplySoundSet(_selectedCar, soundSet);

                // Save state persistently
                Main.registryService.SaveSoundState(_selectedCar, soundSet);
                Main.DebugLog(() => $"CommsRadio: Applied sound using new services");
            }
        }

        // --- Ray/Highlight helpers ---

        private void PointToCar(TrainCar car)
        {
            if (_pointedCar == car)
                return;

            if (_pointedCar)
                _pointedCar.OnDestroyCar -= OnPointedCarDestroyed;

            if (car && IsLocomotive(car))
            {
                _pointedCar = car;
                _pointedCar.OnDestroyCar += OnPointedCarDestroyed;
                HighlightCar(_pointedCar, Controller.cargoLoaderControl.validMaterial);
                CommsRadioController.PlayAudioFromRadio(Controller.cargoLoaderControl.hoverOverCar, transform);
            }
            else
            {
                _pointedCar = null!;
                ClearHighlightCar();
            }
        }

        private static bool IsLocomotive(TrainCar car)
        {
            return CarTypes.IsLocomotive(car.carLivery);
        }

        private void HighlightCar(TrainCar car, Material mat)
        {
            _highlighterRender.material = mat;
            _highlighter.transform.localScale = car.Bounds.size + HighlightExtra;

            var up = car.transform.up * (_highlighter.transform.localScale.y * 0.5f);
            var fwd = car.transform.forward * car.Bounds.center.z;
            var pos = car.transform.position + up + fwd;

            _highlighter.transform.SetPositionAndRotation(pos, car.transform.rotation);
            _highlighter.SetActive(true);
            _highlighter.transform.SetParent(car.transform, true);
        }

        private void ClearHighlightCar()
        {
            if (_pointedCar)
            {
                _pointedCar.OnDestroyCar -= OnPointedCarDestroyed;
            }

            if (_highlighter != null)
            {
                _highlighter.SetActive(false);
                _highlighter.transform.SetParent(null);
            }
        }

        private void OnPointedCarDestroyed(TrainCar destroyedCar)
        {
            if (destroyedCar)
                destroyedCar.OnDestroyCar -= OnPointedCarDestroyed;
            ClearHighlightCar();
        }

        private static int Wrap(int value, int count)
        {
            if (count <= 0) return 0;
            var v = value % count;
            return v < 0 ? v + count : v;
        }
    }
}
