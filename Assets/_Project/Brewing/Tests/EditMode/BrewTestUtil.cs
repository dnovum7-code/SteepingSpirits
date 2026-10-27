using SteepingSpirits.Brewing.Core;

namespace SteepingSpirits.Brewing.Tests
{
    internal static class BrewTestUtil
    {
        public const float Room = 20f;

        public static readonly ExtractionParams Shape = new ExtractionParams();
        public static readonly QualityParams Quality = new QualityParams();

        public static TeaParams[] AllTeas() => new[] { TeaPresets.Black(), TeaPresets.White(), TeaPresets.Green() };

        public static TeaParams[] AllTeasWithOolong() =>
            new[] { TeaPresets.Black(), TeaPresets.White(), TeaPresets.Green(), TeaPresets.Oolong() };

        public static CalibrationSeries Series(TeaParams tea) =>
            BrewCalibration.CalibrateSeries(tea, Shape, Quality, Room);

        public static CalibrationResult Calibrate(TeaParams tea) =>
            BrewCalibration.Calibrate(tea, Shape, Quality, Room);

        public static QualityEvaluation Brew(TeaParams tea, float startTemperature, float liftSeconds, float aromaMax = 1f)
        {
            float qRef = Calibrate(tea).QReference;
            return BrewCalibration.Simulate(tea, Shape, Quality, Room, startTemperature, liftSeconds, aromaMax, qRef);
        }
    }
}
