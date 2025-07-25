using System.Collections.Generic;

namespace DvMod.ZSounds
{
    public static class Registry
    {
        public static readonly Dictionary<string, SoundSet> soundSets = new Dictionary<string, SoundSet>();
        public static readonly HashSet<string> customizedCars = new HashSet<string>();
        
        public static SoundSet Get(TrainCar car)
        {
            if (!soundSets.TryGetValue(car.logicCar.carGuid, out var soundSet))
            {
                if (Main.soundLoader != null)
                {
                    soundSet = Main.soundLoader.CreateSoundSetForTrain(car);
                }
                soundSet ??= new SoundSet();
                soundSets[car.logicCar.carGuid] = soundSet;
            }
            return soundSet;
        }
        
        public static void MarkAsCustomized(TrainCar car)
        {
            customizedCars.Add(car.logicCar.carGuid);
        }
        
        public static bool IsCustomized(TrainCar car)
        {
            return customizedCars.Contains(car.logicCar.carGuid);
        }
        
        public static void ClearCustomization(TrainCar car)
        {
            customizedCars.Remove(car.logicCar.carGuid);
        }
    }
}