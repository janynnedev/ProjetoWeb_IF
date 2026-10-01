using System.Security.Claims;
using Projeto1_IF.Models;

namespace Projeto1_IF.Services;

public interface ICurrentProfessionalService
{
    /// <summary>
    /// Retorna null para usuário não autenticado, sem ID ou sem profissional vinculado.
    /// </summary>
    /// <exception cref="InvalidOperationException">Mais de um profissional possui o mesmo IdUser.</exception>
    Task<TbProfissional?> GetCurrentProfessionalAsync(ClaimsPrincipal user);
}
