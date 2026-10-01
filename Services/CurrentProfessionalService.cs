using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;

namespace Projeto1_IF.Services;

public class CurrentProfessionalService : ICurrentProfessionalService
{
    private readonly db_IFContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public CurrentProfessionalService(db_IFContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<TbProfissional?> GetCurrentProfessionalAsync(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identityUserId = _userManager.GetUserId(user);
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return null;
        }

        var professionals = await _context.TbProfissional
            .AsNoTracking()
            .Where(professional => professional.IdUser == identityUserId)
            .Take(2)
            .ToListAsync();

        // Sem constraint única, rejeitamos a ambiguidade em vez de escolher um vínculo arbitrário.
        if (professionals.Count > 1)
        {
            throw new InvalidOperationException(
                "Inconsistência de dados: mais de um TbProfissional possui o mesmo IdUser do usuário autenticado.");
        }

        return professionals.SingleOrDefault();
    }
}
