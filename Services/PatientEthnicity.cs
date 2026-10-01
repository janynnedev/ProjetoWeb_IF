using System.ComponentModel.DataAnnotations;

namespace Projeto1_IF.Services;

// Convenção definida para esta aplicação; não é um mapeamento histórico do banco.
public enum PatientEthnicity
{
    Branca = 1,
    Parda = 2,
    Negra = 3,
    Amarela = 4,
    [Display(Name = "Indígena")]
    Indigena = 5
}