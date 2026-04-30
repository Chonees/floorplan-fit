namespace FloorplanFit.Application.FloorPlans.Import;

public static class FloorPlanCodeNormalizer
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A floor plan name is required to build a code.", nameof(value));
        }

        var normalizedCharacters = new List<char>();
        var lastWasSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                normalizedCharacters.Add(character);
                lastWasSeparator = false;
                continue;
            }

            if (lastWasSeparator)
            {
                continue;
            }

            normalizedCharacters.Add('-');
            lastWasSeparator = true;
        }

        return new string(normalizedCharacters.ToArray()).Trim('-');
    }
}
