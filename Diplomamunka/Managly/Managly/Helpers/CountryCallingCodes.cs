using System.Collections.Generic;

namespace Managly.Helpers
{
    public static class CountryCallingCodes
    {
        public static List<(string Code, string Name)> GetCountryCodes()
        {
            return new List<(string, string)>
            {
                ("+1", "United States"),
                ("+44", "United Kingdom"),
                ("+49", "Germany"),
                ("+33", "France"),
                ("+39", "Italy"),
                ("+34", "Spain"),
                ("+36", "Hungary"),
                ("+91", "India"),
                ("+81", "Japan"),
                ("+86", "China"),
                ("+61", "Australia"),
                ("+55", "Brazil"),
                ("+7", "Russia"),
                ("+27", "South Africa"),
                ("+52", "Mexico"),
                ("+82", "South Korea"),
                ("+46", "Sweden"),
                ("+47", "Norway"),
                ("+31", "Netherlands"),
                ("+32", "Belgium")
            };
        }
    }
}
