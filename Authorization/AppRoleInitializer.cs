using Microsoft.AspNetCore.Identity;

namespace Projeto1_IF.Authorization;

public static class AppRoleInitializer
{
    public static async Task InitializeAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[]
        {
            AppRoles.Medico, AppRoles.Nutricionista, AppRoles.GerenteMedico,
            AppRoles.GerenteNutricionista, AppRoles.GerenteGeral
        })
        {
            if (await roleManager.RoleExistsAsync(role)) continue;
            var result = await roleManager.CreateAsync(new IdentityRole(role));
            // Outra instância pode ter criado a Role entre a verificação e a inserção.
            if (!result.Succeeded && !await roleManager.RoleExistsAsync(role))
                throw new InvalidOperationException($"Falha ao criar Role '{role}': "
                    + string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
