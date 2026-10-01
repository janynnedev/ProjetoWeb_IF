using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Projeto1_IF.Services;

public static class PatientRegistrationOptions
{
    public static bool IsValidEthnicity(int value) => Enum.IsDefined(typeof(PatientEthnicity), value);
    public static string EthnicityDescription(int value)
    {
        if (!IsValidEthnicity(value)) return "Código não reconhecido";
        var ethnicity = (PatientEthnicity)value;
        return typeof(PatientEthnicity).GetField(ethnicity.ToString())!
            .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? ethnicity.ToString();
    }
    public static IEnumerable<SelectListItem> EthnicityOptions => Enum.GetValues<PatientEthnicity>()
        .Select(value => new SelectListItem(EthnicityDescription((int)value), ((int)value).ToString(CultureInfo.InvariantCulture)));

    private static readonly string[] Sexes = ["F", "M"];
    public static bool IsValidSex(string? value) => Sexes.Contains(value, StringComparer.Ordinal);
    public static IEnumerable<SelectListItem> SexOptions => Sexes.Select(value => new SelectListItem(value, value));
}