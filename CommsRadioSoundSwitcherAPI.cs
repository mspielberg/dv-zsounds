using System;
using System.Collections.Generic;
using System.Linq;
using CommsRadioAPI;
using DV;
using DV.ThingTypes;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class CommsRadioSoundSwitcherAPI
    {
        private static CommsRadioMode? soundSwitcherMode;

        public static void Initialize()
        {
            try
            {
                Main.mod?.Logger.Log("Initializing CommsRadio Sound Switcher API integration...");
                
                // Subscribe to the CommsRadio ready event
                ControllerAPI.Ready += StartCommsRadioMode;
                Main.mod?.Logger.Log("Successfully subscribed to CommsRadio ready event");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to subscribe to CommsRadio ready event: {ex.Message}");
                Main.mod?.Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        public static void Cleanup()
        {
            try
            {
                Main.mod?.Logger.Log("Cleaning up CommsRadio Sound Switcher API integration...");
                
                // Unsubscribe from the CommsRadio ready event
                ControllerAPI.Ready -= StartCommsRadioMode;
                
                // Clear the sound switcher mode reference
                soundSwitcherMode = null;
                
                Main.mod?.Logger.Log("CommsRadio Sound Switcher API cleanup completed");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to cleanup CommsRadio integration: {ex.Message}");
                Main.mod?.Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        public static void Reinitialize()
        {
            Main.mod?.Logger.Log("Reinitializing CommsRadio Sound Switcher API integration...");
            Cleanup();
            Initialize();
        }

        private static void StartCommsRadioMode()
        {
            try
            {
                Main.mod?.Logger.Log("CommsRadio ready event triggered, attempting to register Sound Switcher mode...");
                
                if (soundSwitcherMode != null)
                {
                    Main.mod?.Logger.Log("CommsRadio Sound Switcher mode already registered");
                    return;
                }

                var initialState = new SelectCarBehaviour();
                soundSwitcherMode = CommsRadioMode.Create(
                    initialState,
                    laserColor: new Color(0.53f, 0f, 1f), // Purple laser color
                    insertBefore: mode => mode == ControllerAPI.GetVanillaMode(VanillaMode.LED)
                );
                
                Main.mod?.Logger.Log("CommsRadio Sound Switcher mode registered successfully using CommsRadioAPI");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to register CommsRadio Sound Switcher mode: {ex.Message}");
                Main.mod?.Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    // State: Select a car to modify sounds
    public class SelectCarBehaviour : AStateBehaviour
    {
        private const float SIGNAL_RANGE = 200f; // Increased range for VR
        private readonly LayerMask trainCarMask;

        public SelectCarBehaviour() : base(new CommsRadioState(
            titleText: "SOUNDS",
            contentText: "Aim at the vehicle you wish to change sounds on.",
            buttonBehaviour: ButtonBehaviourType.Regular))
        {
            // Use multiple layer masks to catch more train car colliders in VR
            trainCarMask = LayerMask.GetMask("Train_Big_Collider", "Train_Car", "TrainCar", "Default");
        }

        public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
        {
            // Try raycast with debug logging for VR troubleshooting
            Vector3 rayOrigin = utility.SignalOrigin.position;
            Vector3 rayDirection = utility.SignalOrigin.forward;
            
            Main.DebugLog(() => $"CommsRadio raycast: origin={rayOrigin}, direction={rayDirection}, range={SIGNAL_RANGE}, mask={trainCarMask.value}");
            
            if (!Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, SIGNAL_RANGE, trainCarMask))
            {
                // Try a broader raycast with all layers for debugging
                if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit debugHit, SIGNAL_RANGE))
                {
                    Main.DebugLog(() => $"CommsRadio raycast hit something else: {debugHit.transform.name} on layer {debugHit.transform.gameObject.layer}");
                }
                return this;
            }

            Main.DebugLog(() => $"CommsRadio raycast hit: {hit.transform.name} (root: {hit.transform.root.name})");

            TrainCar? targetCar = TrainCar.Resolve(hit.transform.root);
            if (targetCar == null)
            {
                Main.DebugLog(() => $"CommsRadio: Could not resolve TrainCar from hit object {hit.transform.root.name}");
                return this;
            }
            
            if (!CarTypes.IsLocomotive(targetCar.carLivery))
            {
                Main.DebugLog(() => $"CommsRadio: Car {targetCar.ID} is not a locomotive (type: {targetCar.carType})");
                return this;
            }

            Main.DebugLog(() => $"CommsRadio: Valid locomotive found: {targetCar.ID} ({targetCar.carType})");
            utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
            return new PointAtCarBehaviour(targetCar);
        }

        public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
        {
            return this;
        }
    }

    // State: Pointing at a valid car
    public class PointAtCarBehaviour : AStateBehaviour
    {
        private const float SIGNAL_RANGE = 200f; // Increased range for VR
        private readonly LayerMask trainCarMask;
        private readonly TrainCar selectedCar;

        public PointAtCarBehaviour(TrainCar car) : base(new CommsRadioState(
            titleText: "SOUNDS",
            contentText: $"Car: {car.carType}\nPress to select this locomotive",
            actionText: "select",
            buttonBehaviour: ButtonBehaviourType.Regular))
        {
            selectedCar = car;
            trainCarMask = LayerMask.GetMask("Train_Big_Collider", "Train_Car", "TrainCar", "Default");
            
            Main.mod?.Logger.Log($"CommsRadioAPI: Targeting car {car.ID} ({car.carType}) for sound modification");
        }

        public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
        {
            if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out RaycastHit hit, SIGNAL_RANGE, trainCarMask))
            {
                return new SelectCarBehaviour();
            }

            TrainCar? targetCar = TrainCar.Resolve(hit.transform.root);
            if (targetCar != selectedCar)
            {
                return new SelectCarBehaviour();
            }

            return this;
        }

        public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
        {
            if (action == InputAction.Activate)
            {
                utility.PlaySound(VanillaSoundCommsRadio.Confirm);
                return new SelectSoundTypeBehaviour(selectedCar);
            }
            return this;
        }
    }

    // State: Select which sound type to modify
    public class SelectSoundTypeBehaviour : AStateBehaviour
    {
        private const float SIGNAL_RANGE = 200f; // Increased range for VR
        private readonly LayerMask trainCarMask;
        private readonly TrainCar selectedCar;
        private readonly SoundType[] availableSoundTypes;
        private readonly int soundTypeIndex;

        public SelectSoundTypeBehaviour(TrainCar car, int index = 0) : base(CreateState(car, GetAvailableSoundTypes(car), index))
        {
            selectedCar = car;
            availableSoundTypes = GetAvailableSoundTypes(car);
            soundTypeIndex = index;
            trainCarMask = LayerMask.GetMask("Train_Big_Collider", "Train_Car", "TrainCar", "Default");
        }

        private static SoundType[] GetAvailableSoundTypes(TrainCar car)
        {
            var carType = car.carType;
            var supportedTypes = new List<SoundType>();
            
            Main.DebugLog(() => $"CommsRadioAPI: Getting available sound types for car {car.ID} (type: {carType})");
            
            if (Main.soundLoader == null)
            {
                Main.DebugLog(() => $"CommsRadioAPI: SoundLoader is null");
                return supportedTypes.ToArray();
            }

            // Get all sound types that have actual sound files available
            foreach (var soundType in Enum.GetValues(typeof(SoundType)).Cast<SoundType>())
            {
                if (soundType == SoundType.Unknown) continue;
                
                // Check if we have sound files for this sound type
                if (HasAvailableSounds(car, soundType))
                {
                    // Also verify that the car actually supports this sound type through AudioMapper
                    // (except for special cases like EngineStartup/EngineShutdown)
                    bool carSupportsType = false;
                    
                    if (soundType == SoundType.EngineStartup || soundType == SoundType.EngineShutdown)
                    {
                        // Special sound types that may not have AudioMapper entries but can still be applied
                        carSupportsType = true;
                    }
                    else if (AudioMapper.Mappers.TryGetValue(carType, out var mapper))
                    {
                        var hasLayered = SoundTypes.layeredAudioSoundTypes.Contains(soundType) && 
                                       mapper.GetLayeredAudio(soundType, AudioUtils.GetTrainAudio(car)) != null;
                        var hasClips = SoundTypes.audioClipsSoundTypes.Contains(soundType) && 
                                     mapper.GetAudioClipPortReader(soundType, AudioUtils.GetTrainAudio(car)) != null;
                        carSupportsType = hasLayered || hasClips;
                    }
                    
                    if (carSupportsType)
                    {
                        supportedTypes.Add(soundType);
                        Main.DebugLog(() => $"CommsRadioAPI: Added sound type {soundType} for car {carType} (sounds available and car supports it)");
                    }
                    else
                    {
                        Main.DebugLog(() => $"CommsRadioAPI: Sound type {soundType} has files but car {carType} doesn't support it");
                    }
                }
            }
            
            Main.DebugLog(() => $"CommsRadioAPI: Found {supportedTypes.Count} available sound types for car {carType}: {string.Join(", ", supportedTypes)}");
            return supportedTypes.ToArray();
        }

        private static bool HasAvailableSounds(TrainCar car, SoundType soundType)
        {
            if (Main.soundLoader == null) 
            {
                Main.DebugLog(() => $"CommsRadioAPI: SoundLoader is null for {soundType} check");
                return false;
            }
            
            // Only check car-specific sounds - don't fall back to generic sounds
            var carSounds = Main.soundLoader.GetAvailableSoundsForTrain(car.carType);
            if (carSounds.TryGetValue(soundType, out var carSpecificSounds) && carSpecificSounds.Count > 0)
            {
                Main.DebugLog(() => $"CommsRadioAPI: Found {carSpecificSounds.Count} car-specific {soundType} sounds for {car.carType}");
                return true;
            }
            
            Main.DebugLog(() => $"CommsRadioAPI: No car-specific sounds available for {soundType} (car: {car.carType})");
            return false;
        }

        private static CommsRadioState CreateState(TrainCar car, SoundType[] soundTypes, int index)
        {
            if (soundTypes.Length == 0)
            {
                return new CommsRadioState(
                    titleText: "SOUNDS",
                    contentText: $"No sound types available for {car.carType}",
                    actionText: "cancel",
                    buttonBehaviour: ButtonBehaviourType.Regular);
            }

            var currentType = soundTypes[index];
            var content = $"Car: {car.carType}\nSound Type:\n{currentType}";
            
            return new CommsRadioState(
                titleText: "SOUNDS",
                contentText: content,
                actionText: "next",
                buttonBehaviour: ButtonBehaviourType.Override);
        }

        public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
        {
            if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out RaycastHit hit, SIGNAL_RANGE, trainCarMask))
            {
                return new SelectCarBehaviour();
            }

            TrainCar? targetCar = TrainCar.Resolve(hit.transform.root);
            if (targetCar != selectedCar)
            {
                return new SelectCarBehaviour();
            }

            return this;
        }

        public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
        {
            if (availableSoundTypes.Length == 0)
            {
                return new SelectCarBehaviour();
            }

            switch (action)
            {
                case InputAction.Activate:
                    var currentType = availableSoundTypes[soundTypeIndex];
                    utility.PlaySound(VanillaSoundCommsRadio.Confirm);
                    
                    // Check if it's HornHit and show warning
                    if (currentType == SoundType.HornHit)
                    {
                        return new ConfirmHornHitBehaviour(selectedCar, currentType);
                    }
                    else
                    {
                        return new SelectSoundBehaviour(selectedCar, currentType);
                    }

                case InputAction.Up:
                    var newIndex = (soundTypeIndex + 1) % availableSoundTypes.Length;
                    return new SelectSoundTypeBehaviour(selectedCar, newIndex);

                case InputAction.Down:
                    var prevIndex = (soundTypeIndex - 1 + availableSoundTypes.Length) % availableSoundTypes.Length;
                    return new SelectSoundTypeBehaviour(selectedCar, prevIndex);

                default:
                    return this;
            }
        }
    }

    // State: Confirm HornHit warning
    public class ConfirmHornHitBehaviour : AStateBehaviour
    {
        private const float SIGNAL_RANGE = 200f; // Increased range for VR
        private readonly LayerMask trainCarMask;
        private readonly TrainCar selectedCar;
        private readonly SoundType soundType;

        public ConfirmHornHitBehaviour(TrainCar car, SoundType type) : base(new CommsRadioState(
            titleText: "SOUNDS",
            contentText: "WARNING: HornHit sounds may only work once and can cause issues.\n\nContinue anyway?",
            actionText: "confirm",
            buttonBehaviour: ButtonBehaviourType.Regular))
        {
            selectedCar = car;
            soundType = type;
            trainCarMask = LayerMask.GetMask("Train_Big_Collider", "Train_Car", "TrainCar", "Default");
        }

        public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
        {
            if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out RaycastHit hit, SIGNAL_RANGE, trainCarMask))
            {
                return new SelectCarBehaviour();
            }

            TrainCar? targetCar = TrainCar.Resolve(hit.transform.root);
            if (targetCar != selectedCar)
            {
                return new SelectCarBehaviour();
            }

            return this;
        }

        public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
        {
            if (action == InputAction.Activate)
            {
                utility.PlaySound(VanillaSoundCommsRadio.Confirm);
                return new SelectSoundBehaviour(selectedCar, soundType);
            }
            return this;
        }
    }

    // State: Select specific sound
    public class SelectSoundBehaviour : AStateBehaviour
    {
        private const float SIGNAL_RANGE = 200f; // Increased range for VR
        private readonly LayerMask trainCarMask;
        private readonly TrainCar selectedCar;
        private readonly SoundType soundType;
        private readonly List<SoundDefinition> availableSounds;
        private readonly int soundIndex;

        public SelectSoundBehaviour(TrainCar car, SoundType type, int index = 0) : base(CreateState(car, type, GetAvailableSounds(car, type), index))
        {
            selectedCar = car;
            soundType = type;
            availableSounds = GetAvailableSounds(car, type);
            soundIndex = index;
            trainCarMask = LayerMask.GetMask("Train_Big_Collider", "Train_Car", "TrainCar", "Default");
        }

        private static List<SoundDefinition> GetAvailableSounds(TrainCar car, SoundType soundType)
        {
            var sounds = new List<SoundDefinition>();
            
            if (Main.soundLoader != null)
            {
                // Only get car-specific sounds (no generic sounds exist anymore)
                var carSounds = Main.soundLoader.GetAvailableSoundsForTrain(car.carType);
                if (carSounds.TryGetValue(soundType, out var carSpecificSounds))
                {
                    sounds.AddRange(carSpecificSounds);
                }
            }
            
            return sounds;
        }

        private static CommsRadioState CreateState(TrainCar car, SoundType soundType, List<SoundDefinition> sounds, int index)
        {
            if (sounds.Count == 0)
            {
                return new CommsRadioState(
                    titleText: "SOUNDS",
                    contentText: $"No {soundType} sounds available",
                    actionText: "cancel",
                    buttonBehaviour: ButtonBehaviourType.Regular);
            }

            var currentSound = sounds[index];
            var content = $"Sound:\n{currentSound.name}";
            
            if (soundType == SoundType.HornHit)
            {
                content += "\n[WARNING: May not work if changed before]";
            }
            
            return new CommsRadioState(
                titleText: "SOUNDS",
                contentText: content,
                actionText: "confirm",
                buttonBehaviour: ButtonBehaviourType.Override);
        }

        public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
        {
            if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out RaycastHit hit, SIGNAL_RANGE, trainCarMask))
            {
                return new SelectCarBehaviour();
            }

            TrainCar? targetCar = TrainCar.Resolve(hit.transform.root);
            if (targetCar != selectedCar)
            {
                return new SelectCarBehaviour();
            }

            return this;
        }

        public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
        {
            if (availableSounds.Count == 0)
            {
                return new SelectCarBehaviour();
            }

            switch (action)
            {
                case InputAction.Activate:
                    ApplySelectedSound();
                    utility.PlaySound(VanillaSoundCommsRadio.Confirm);
                    return new SelectCarBehaviour();

                case InputAction.Up:
                    var newIndex = (soundIndex + 1) % availableSounds.Count;
                    return new SelectSoundBehaviour(selectedCar, soundType, newIndex);

                case InputAction.Down:
                    var prevIndex = (soundIndex - 1 + availableSounds.Count) % availableSounds.Count;
                    return new SelectSoundBehaviour(selectedCar, soundType, prevIndex);

                default:
                    return this;
            }
        }

        private void ApplySelectedSound()
        {
            if (availableSounds.Count == 0) return;

            var selectedSound = availableSounds[soundIndex];
            
            Main.DebugLog(() => $"CommsRadioAPI: Applying sound {selectedSound.name} (type: {selectedSound.type}) to car {selectedCar.ID}");

            var soundSet = Registry.Get(selectedCar);
            selectedSound.Apply(soundSet);
            Registry.MarkAsCustomized(selectedCar);

            // Use ResetAndApply to ensure proper application
            AudioUtils.ResetAndApply(selectedCar, selectedSound.type, soundSet);
            Main.DebugLog(() => $"CommsRadioAPI: Applied sound {selectedSound.name} to car {selectedCar.ID}");
            
            Main.DebugLog(() => $"CommsRadioAPI: Sound application completed for {selectedSound.type}");
        }
    }
}
