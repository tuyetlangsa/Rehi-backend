namespace Rehi.Domain.Flashcards;

public static class SpaceRepititionOptions
{
    public static List<TimeSpan> LearningSteps { get; set; } = new()
    {
        TimeSpan.FromMinutes(10),
        TimeSpan.FromDays(1)
    };

    public static int GraduatingIntervalDays { get; set; } = 3;
    public static double EaseFactorInitial { get; set; } = 2.5;
    public static double EaseFactorMin { get; set; } = 1.3;
    public static double HardFactor { get; set; } = 1.2;
    public static double EasyBonus { get; set; } = 1.3;
}