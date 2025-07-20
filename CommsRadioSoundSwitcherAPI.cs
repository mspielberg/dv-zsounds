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
        private const float SIGNAL_RANGE = 100f;
        private readonly LayerMask trainCarMask;

        public SelectCarBehaviour() : base(new CommsRadioState(
            titleText: "SOUNDS",
            contentText: "Aim at the vehicle you wish to change sounds on.",
            buttonBehaviour: ButtonBehaviourType.Regular))
        {
            trainCarMask = LayerMask.GetMask("Train_Big_Collider");
        }

        public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
        {
            if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out RaycastHit hit, SIGNAL_RANGE, trainCarMask))
            {
                return this;
            }

            TrainCar? targetCar = TrainCar.Resolve(hit.transform.root);
            if (targetCar == null || !CarTypes.IsLocomotive(targetCar.carLivery))
            {
                return this;
            }

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
        private const float SIGNAL_RANGE = 100f;
        private readonly LayerMask trainCarMask;
        private readonly TrainCar selectedCar;

        public PointAtCarBehaviour(TrainCar car) : base(new CommsRadioState(
            titleText: "SOUNDS",
            contentText: $"Car: {car.carType}\nPress to select this locomotive",
            actionText: "select",
            buttonBehaviour: ButtonBehaviourType.Regular))
        {
            selectedCar = car;
            trainCarMask = LayerMask.GetMask("Train_Big_Collider");
            
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
        private const float SIGNAL_RANGE = 100f;
        private readonly LayerMask trainCarMask;
        private readonly TrainCar selectedCar;
        private readonly SoundType[] availableSoundTypes;
        private readonly int soundTypeIndex;

        public SelectSoundTypeBehaviour(TrainCar car, int index = 0) : base(CreateState(car, GetAvailableSoundTypes(car), index))
        {
            selectedCar = car;
            availableSoundTypes = GetAvailableSoundTypes(car);
            soundTypeIndex = index;
            trainCarMask = LayerMask.GetMask("Train_Big_Collider");
        }

        private static SoundType[] GetAvailableSoundTypes(TrainCar car)
        {
            var carType = car.carType;
            var supportedTypes = new List<SoundType>();
            
            Main.DebugLog(() => $"CommsRadioAPI: Getting available sound types for car {car.ID} (type: {carType})");
            
            if (AudioMapper.mappings.TryGetValue(carType, out var mapper))
            {
                foreach (var soundType in Enum.GetValues(typeof(SoundType)).Cast<SoundType>())
                {
                    if (soundType == SoundType.Unknown) continue;
                    if (soundType >= SoundType.Collision) continue; // Skip generic sounds
                    
                    // Special handling for EngineStartup and EngineShutdown - they may not have AudioMapper entries
                    // but can still be applied if sound files are available
                    if (soundType == SoundType.EngineStartup || soundType == SoundType.EngineShutdown)
                    {
                        if (HasAvailableSounds(car, soundType))
                        {
                            supportedTypes.Add(soundType);
                            Main.DebugLog(() => $"CommsRadioAPI: Added special sound type {soundType} for car {carType} (sounds available)");
                        }
                        else
                        {
                            Main.DebugLog(() => $"CommsRadioAPI: No sounds available for special sound type {soundType} (car: {carType})");
                        }
                        continue;
                    }
                    
                    // Check if this car type supports this sound type through AudioMapper
                    var hasLayered = SoundTypes.layeredAudioSoundTypes.Contains(soundType) && 
                                   mapper.GetLayeredAudio(soundType, AudioUtils.GetTrainAudio(car)) != null;
                    var hasClips = SoundTypes.audioClipsSoundTypes.Contains(soundType) && 
                                 mapper.GetAudioClipPortReader(soundType, AudioUtils.GetTrainAudio(car)) != null;
                    
                    // Only include this sound type if the car supports it AND sounds are available
                    if ((hasLayered || hasClips) && HasAvailableSounds(car, soundType))
                    {
                        supportedTypes.Add(soundType);
                        Main.DebugLog(() => $"CommsRadioAPI: Added sound type {soundType} for car {carType}");
                    }
                    else if (hasLayered || hasClips)
                    {
                        Main.DebugLog(() => $"CommsRadioAPI: Car {carType} supports {soundType} but no sounds available");
                    }
                }
            }
            else
            {
                Main.DebugLog(() => $"CommsRadioAPI: No audio mapper found for car type {carType}");
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
            
            // Check car-specific sounds first
            var carSounds = Main.soundLoader.GetAvailableSoundsForTrain(car.carType);
            if (carSounds.TryGetValue(soundType, out var carSpecificSounds) && carSpecificSounds.Count > 0)
            {
                Main.DebugLog(() => $"CommsRadioAPI: Found {carSpecificSounds.Count} car-specific {soundType} sounds for {car.carType}");
                return true;
            }
            
            // If no car-specific sounds, check generic sounds of this type
            var genericSounds = Main.soundLoader.GetSoundsOfType(soundType);
            if (genericSounds.Count > 0)
            {
                Main.DebugLog(() => $"CommsRadioAPI: Found {genericSounds.Count} generic {soundType} sounds");
                return true;
            }
            
            Main.DebugLog(() => $"CommsRadioAPI: No sounds available for {soundType} (car: {car.carType})");
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
        private const float SIGNAL_RANGE = 100f;
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
            trainCarMask = LayerMask.GetMask("Train_Big_Collider");
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
        private const float SIGNAL_RANGE = 100f;
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
            trainCarMask = LayerMask.GetMask("Train_Big_Collider");
        }

        private static List<SoundDefinition> GetAvailableSounds(TrainCar car, SoundType soundType)
        {
            var sounds = new List<SoundDefinition>();
            
            if (Main.soundLoader != null)
            {
                // Get car-specific sounds first
                var carSounds = Main.soundLoader.GetAvailableSoundsForTrain(car.carType);
                if (carSounds.TryGetValue(soundType, out var carSpecificSounds))
                {
                    sounds.AddRange(carSpecificSounds);
                }
                
                // If no car-specific sounds, get generic sounds of this type
                if (sounds.Count == 0)
                {
                    sounds.AddRange(Main.soundLoader.GetSoundsOfType(soundType));
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
