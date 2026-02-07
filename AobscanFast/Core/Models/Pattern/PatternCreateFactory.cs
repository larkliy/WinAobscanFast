namespace AobscanFast.Core.Models.Pattern;

internal static class PatternCreateFactory
{
    public static PatternBase Create(string input)
    {
        if (!input.Contains('?'))
            return new SolidPattern(input);

        return new MaskPattern(input);
    }
}
