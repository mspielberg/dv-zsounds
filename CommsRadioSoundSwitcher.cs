using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DV;
using DvMod.ZSounds.Config;
using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds
{
    public class CommsRadioSoundSwitcher : MonoBehaviour, ICommsRadioMode
    {
        public static CommsRadioController? Controller;

        public ButtonBehaviourType ButtonBehaviour { get; private set; }

        public CommsRadioDisplay? display;
        public Transform? signalOrigin;
        public Material? selectionMaterial;
        public Material? skinningMaterial;
        public GameObject? trainHighlighter;

        // Sounds
        public AudioClip? HoverCarSound;
        public AudioClip? SelectedCarSound;
        public AudioClip? ConfirmSound;
        public AudioClip? CancelSound;

        private State CurrentState;
        private LayerMask TrainCarMask;
        private RaycastHit Hit;
        private TrainCar? SelectedCar = null;
        private TrainCar? PointedCar = null;
        private MeshRenderer? HighlighterRender;

        private SoundType selectedSoundType = SoundType.HornHit;
        private static readonly SoundType minSoundType;
        private static readonly SoundType maxSoundType;

        private IList<SoundDefinition>? availableSounds = null;
        private int soundIndex = 0;

        private void RefreshAvailableSounds()
        {
            bool found = Config.Config.Active!.soundTypes.TryGetValue(selectedSoundType, out var soundList);
            availableSounds = found ? soundList : null;
            soundIndex = 0;
        }

        private SoundDefinition? selectedSound => availableSounds?[soundIndex];

        private const float SIGNAL_RANGE = 100f;
        private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);
        private static readonly Color LASER_COLOR = new Color(0.53f, 0f, 1f);
        public Color GetLaserBeamColor()
        {
            return LASER_COLOR;
        }
        public void OverrideSignalOrigin( Transform signalOrigin ) => this.signalOrigin = signalOrigin;

        #region Initialization

        static CommsRadioSoundSwitcher()
        {
            var soundTypes = Enum.GetValues(typeof(SoundType))
                .Cast<SoundType>()
                .Where(t => t != SoundType.Unknown)
                .OrderBy(t => t);

            minSoundType = soundTypes.First();
            maxSoundType = soundTypes.Last();
        }

        public void Awake()
        {
            // steal components from other radio modes
            if( Controller?.deleteControl is CommsRadioCarDeleter deleter )
            {
                signalOrigin = deleter.signalOrigin;
                display = deleter.display;
                selectionMaterial = new Material(deleter.selectionMaterial);
                skinningMaterial = new Material(deleter.deleteMaterial);
                trainHighlighter = deleter.trainHighlighter;

                // sounds
                HoverCarSound = deleter.hoverOverCar;
                SelectedCarSound = deleter.warningSound;
                ConfirmSound = deleter.confirmSound;
                CancelSound = deleter.cancelSound;
            }
            else
            {
                Debug.LogError("CommsRadioSoundSwitcher: couldn't get properties from siblings");
            }
        }

        public void Start()
        {
            if( !signalOrigin )
            {
                Debug.LogError("CommsRadioNumberSwitcher: signalOrigin on isn't set, using this.transform!", this);
                signalOrigin = transform;
            }

            if( display == null )
            {
                Debug.LogError("CommsRadioNumberSwitcher: display not set, can't function properly!", this);
            }

            if( (selectionMaterial == null) || (skinningMaterial == null) )
            {
                Debug.LogError("CommsRadioNumberSwitcher: Selection material(s) not set. Visuals won't be correct.", this);
            }

            if( trainHighlighter == null )
            {
                Debug.LogError("CommsRadioNumberSwitcher: trainHighlighter not set, can't function properly!!", this);
            }

            if( (HoverCarSound == null) || (SelectedCarSound == null) || (ConfirmSound == null) || (CancelSound == null) )
            {
                Debug.LogError("Not all audio clips set, some sounds won't be played!", this);
            }

            TrainCarMask = LayerMask.GetMask(new string[]
            {
                "Train_Big_Collider"
            });

            HighlighterRender = trainHighlighter?.GetComponentInChildren<MeshRenderer>(true);
            trainHighlighter?.SetActive(false);
            trainHighlighter?.transform?.SetParent(null);
        }

        public void Enable() { }

        public void Disable()
        {
            ResetState();
        }

        public void SetStartingDisplay()
        {
            string content = "Aim at the vehicle you wish to change sounds on.";
            display?.SetDisplay("SOUNDS", content, "");
        }

        #endregion

        #region Car Highlighting

        private void HighlightCar( TrainCar car, Material highlightMaterial )
        {
            if( car == null )
            {
                Debug.LogError("Highlight car is null. Ignoring request.");
                return;
            }

            if( (HighlighterRender != null) && (trainHighlighter != null) )
            {
                HighlighterRender.material = highlightMaterial;

                trainHighlighter.transform.localScale = car.Bounds.size + HIGHLIGHT_BOUNDS_EXTENSION;
                Vector3 b = car.transform.up * (trainHighlighter.transform.localScale.y / 2f);
                Vector3 b2 = car.transform.forward * car.Bounds.center.z;
                Vector3 position = car.transform.position + b + b2;

                trainHighlighter.transform.SetPositionAndRotation(position, car.transform.rotation);
                trainHighlighter.SetActive(true);
                trainHighlighter.transform.SetParent(car.transform, true);
            }
        }

        private void ClearHighlightedCar()
        {
            if( trainHighlighter ?? false )
            {
                trainHighlighter.SetActive(false);
                trainHighlighter.transform.SetParent(null);
            }
        }

        private void PointToCar( TrainCar? car )
        {
            if( PointedCar != car )
            {
                if( (car != null) && CarTypes.IsLocomotive(car.carType) )
                {
                    PointedCar = car;
                    HighlightCar(PointedCar, selectionMaterial!);
                    CommsRadioController.PlayAudioFromRadio(HoverCarSound, transform);
                }
                else
                {
                    PointedCar = null;
                    ClearHighlightedCar();
                }
            }
        }

        #endregion

        #region State Machine Actions

        private void UpdateSelectionText()
        {
            if( CurrentState == State.SelectSoundType )
            {
                display!.SetContent($"Sound Type:\n{selectedSoundType}");
            }
            else if( CurrentState == State.SelectSound )
            {
                display!.SetContent($"Sound:\n{selectedSound?.name ?? string.Empty}");
            }
        }

        private void SetState( State newState )
        {
            if( newState == CurrentState ) return;

            CurrentState = newState;
            switch( CurrentState )
            {
                case State.SelectCar:
                    SetStartingDisplay();
                    ButtonBehaviour = ButtonBehaviourType.Regular;
                    break;

                case State.SelectSoundType:
                    selectedSoundType = minSoundType;
                    UpdateSelectionText();
                    ButtonBehaviour = ButtonBehaviourType.Override;
                    break;

                case State.SelectSound:
                    RefreshAvailableSounds();
                    UpdateSelectionText();
                    ButtonBehaviour = ButtonBehaviourType.Override;
                    break;
            }
        }

        private void ResetState()
        {
            PointedCar = null;
            SelectedCar = null;
            ClearHighlightedCar();

            SetState(State.SelectCar);
        }

        public void OnUpdate()
        {
            TrainCar trainCar;

            switch( CurrentState )
            {
                case State.SelectCar:
                    if( !(SelectedCar == null) )
                    {
                        Debug.LogError("Invalid setup for current state, reseting flags!", this);
                        ResetState();
                        return;
                    }

                    // Check if not pointing at anything
                    if( !Physics.Raycast(signalOrigin!.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) )
                    {
                        PointToCar(null);
                    }
                    else
                    {
                        // Try to get the traincar we're pointing at
                        trainCar = TrainCar.Resolve(Hit.transform.root);
                        PointToCar(trainCar);
                    }

                    break;

                case State.SelectSoundType:
                    if( !Physics.Raycast(signalOrigin!.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) )
                    {
                        PointToCar(null);
                        display!.SetAction("cancel");
                    }
                    else
                    {
                        trainCar = TrainCar.Resolve(Hit.transform.root);
                        PointToCar(trainCar);
                        display!.SetAction("next");
                    }

                    break;

                case State.SelectSound:
                    if( !Physics.Raycast(signalOrigin!.position, signalOrigin.forward, out Hit, SIGNAL_RANGE, TrainCarMask) )
                    {
                        PointToCar(null);
                        display!.SetAction("cancel");
                    }
                    else
                    {
                        trainCar = TrainCar.Resolve(Hit.transform.root);
                        PointToCar(trainCar);
                        display!.SetAction("confirm");
                    }

                    break;

                default:
                    ResetState();
                    break;
            }
        }

        public void OnUse()
        {
            switch( CurrentState )
            {
                case State.SelectCar:
                    if( PointedCar != null )
                    {
                        SelectedCar = PointedCar;
                        PointedCar = null;

                        HighlightCar(SelectedCar, skinningMaterial!);
                        CommsRadioController.PlayAudioFromRadio(SelectedCarSound, transform);
                        SetState(State.SelectSoundType);
                    }
                    break;

                case State.SelectSoundType:
                    if( (PointedCar != null) && (PointedCar == SelectedCar) )
                    {
                        // clicked on the selected car again, this means move to selecting sound
                        CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);

                        State nextState = CurrentState + 1;
                        SetState(nextState);
                    }
                    else
                    {
                        // clicked off the selected car, this means cancel
                        CommsRadioController.PlayAudioFromRadio(CancelSound, transform);
                        ResetState();
                    }

                    break;

                case State.SelectSound:
                    if( (PointedCar != null) && (PointedCar == SelectedCar) )
                    {
                        // clicked on the selected car again, this means confirm
                        ApplySelectedSound();
                        CommsRadioController.PlayAudioFromRadio(ConfirmSound, transform);
                    }
                    else
                    {
                        // clicked off the selected car, this means cancel
                        CommsRadioController.PlayAudioFromRadio(CancelSound, transform);
                    }

                    ResetState();
                    break;
            }
        }

        // scroll up
        public bool ButtonACustomAction()
        {
            switch( CurrentState )
            {
                case State.SelectSoundType:
                    selectedSoundType += 1;
                    if( selectedSoundType > maxSoundType )
                    {
                        selectedSoundType = minSoundType;
                    }
                    break;

                case State.SelectSound:
                    if( availableSounds != null )
                    {
                        soundIndex += 1;
                        if( soundIndex >= availableSounds.Count )
                        {
                            soundIndex = 0;
                        }
                    }
                    else
                    {
                        soundIndex = 0;
                    }
                    break;

                default:
                    Debug.LogError(string.Format("Unexpected state {0}!", CurrentState), this);
                    return false;
            }

            UpdateSelectionText();
            return true;
        }

        // scroll down
        public bool ButtonBCustomAction()
        {
            switch( CurrentState )
            {
                case State.SelectSoundType:
                    selectedSoundType -= 1;
                    if( selectedSoundType < minSoundType )
                    {
                        selectedSoundType = maxSoundType;
                    }
                    break;

                case State.SelectSound:
                    soundIndex -= 1;
                    if( soundIndex < 0 )
                    {
                        soundIndex = (availableSounds != null) ? availableSounds.Count - 1 : 0;
                    }
                    break;

                default:
                    Debug.LogError(string.Format("Unexpected state {0}!", CurrentState), this);
                    return false;
            }

            UpdateSelectionText();
            return true;
        }

        #endregion

        #region Sound Application

        private void ApplySelectedSound()
        {
            if( (SelectedCar == null) || !CarTypes.IsLocomotive(SelectedCar.carType) )
            {
                Debug.LogWarning("Tried to apply sound to null car");
                ResetState();
                return;
            }

            if( selectedSound == null )
            {
                Debug.LogWarning("Tried to apply null sound definition");
                ResetState();
                return;
            }

            var soundSet = Registry.Get(SelectedCar);
            selectedSound.Apply(soundSet);
            SpawnPatches.ApplyAudio(SelectedCar);

            Debug.Log($"Applied sound {selectedSound.name} to car {SelectedCar.ID}");
            ResetState();
        }

        #endregion

        private enum State
        {
            SelectCar = 0,

            SelectSoundType = 1,
            SelectSound = 2,
        }
    }

    [HarmonyPatch(typeof(CommsRadioController), "Awake")]
    static class CommsRadio_Awake_Patch
    {
        public static CommsRadioSoundSwitcher? numSwitcher = null;

        static void Postfix( CommsRadioController __instance, List<ICommsRadioMode> ___allModes )
        {
            CommsRadioSoundSwitcher.Controller = __instance;
            numSwitcher = __instance.gameObject.AddComponent<CommsRadioSoundSwitcher>();
            ___allModes.Add(numSwitcher);
        }
    }
}
