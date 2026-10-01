using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projeto1_IF.Authorization;
using Projeto1_IF.Data;
using Projeto1_IF.Models;

var mode = args.FirstOrDefault();
if (args.Length != 1 || mode is not ("--inspect" or "--apply" or "--verify"))
{
    Console.Error.WriteLine("Uso: dotnet run --project tools/ProvisionManagers -- --inspect|--apply|--verify (na raiz do repositório).");
    return 1;
}
var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "Projeto1_IF.csproj")))
{
    Console.Error.WriteLine("Execute a partir da raiz de Projeto1_IF.");
    return 1;
}
var accounts = new[]
{
    (Email: "gerentemedico@local.test", Role: AppRoles.GerenteMedico),
    (Email: "gerentenutricionista@local.test", Role: AppRoles.GerenteNutricionista),
    (Email: "gerentegeral@local.test", Role: AppRoles.GerenteGeral)
};
try
{
    // Mesmas fontes da aplicação: JSON, User Secrets em Development e ambiente.
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        ContentRootPath = root,
        EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
        Args = Array.Empty<string>()
    });
    builder.Logging.ClearProviders(); // Não imprimir SQL, hashes ou configuração sensível.
    if (builder.Environment.EnvironmentName == "Development")
        builder.Configuration.AddUserSecrets<ApplicationDbContext>(optional: true);
    builder.Configuration.AddEnvironmentVariables();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection ausente.");
    var connection = new SqlConnectionStringBuilder(connectionString);
    Console.WriteLine($"Provider=SQL Server; servidor={connection.DataSource}; banco={connection.InitialCatalog}; ambiente={builder.Environment.EnvironmentName}");
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
    builder.Services.AddDbContext<db_IFContext>(options => options.UseSqlServer(connectionString));
    builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
        .AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    // Construir apenas DI. Não executar Program/startup nem servidor da aplicação.
    using var services = builder.Services.BuildServiceProvider();
    using var scope = services.CreateScope();
    var identity = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var domain = scope.ServiceProvider.GetRequiredService<db_IFContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>();
    if (!await identity.Database.CanConnectAsync())
    {
        Console.Error.WriteLine("Banco inacessível. Nenhum provisionamento executado.");
        return 2;
    }
    // Consultar as três tabelas sem retornar hashes.
    Console.WriteLine($"Identity acessível: Users={await identity.Users.CountAsync()}, Roles={await identity.Roles.CountAsync()}, UserRoles={await identity.UserRoles.CountAsync()}");
    foreach (var role in new[] { AppRoles.Medico, AppRoles.Nutricionista, AppRoles.GerenteMedico, AppRoles.GerenteNutricionista, AppRoles.GerenteGeral })
        Console.WriteLine($"Role {role}: {(await roles.RoleExistsAsync(role) ? "existe" : "ausente")}");
    foreach (var account in accounts)
    {
        var user = await users.FindByEmailAsync(account.Email);
        var byName = await users.FindByNameAsync(account.Email);
        if (byName != null && (user == null || byName.Id != user.Id))
            throw new InvalidOperationException($"Identificador conflitante: {account.Email}.");
        if (user != null && !string.Equals(user.UserName, account.Email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Conta existente com username inesperado: {account.Email}.");
        if (user != null && await domain.TbProfissional.AnyAsync(p => p.IdUser == user.Id))
            throw new InvalidOperationException($"Conta candidata possui TbProfissional: {account.Email}. Nada será convertido automaticamente.");
        var currentRoles = user == null ? Array.Empty<string>() : (await users.GetRolesAsync(user)).ToArray();
        Console.WriteLine($"Candidato {account.Email}: {(user == null ? "ausente" : "existe")}; Roles=[{string.Join(",", currentRoles)}]");
    }
    if (mode == "--inspect") return 0;

    string? ReadPassword(string role)
    {
        var key = "PROJETO1_IF_MANAGER_PASSWORD_" + role.ToUpperInvariant();
        return Environment.GetEnvironmentVariable(key) ?? Environment.GetEnvironmentVariable("PROJETO1_IF_MANAGER_PASSWORD");
    }
    void RequireSuccess(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(operation + ": " + string.Join("; ", result.Errors.Select(error => error.Description)));
    }
    if (mode == "--apply")
    {
        // Validar todas as senhas e contas antes de qualquer escrita.
        foreach (var account in accounts)
        {
            var password = ReadPassword(account.Role);
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("Forneça PROJETO1_IF_MANAGER_PASSWORD ou variáveis individuais. Senhas não são impressas.");
            var user = await users.FindByEmailAsync(account.Email);
            if (user != null)
            {
                if (!await users.CheckPasswordAsync(user, password))
                    throw new InvalidOperationException($"Senha fornecida não corresponde à conta existente {account.Email}. O utilitário não redefine senhas.");
            }
            else
            {
                var candidate = new IdentityUser { UserName = account.Email, Email = account.Email };
                foreach (var validator in users.PasswordValidators)
                    RequireSuccess(await validator.ValidateAsync(users, candidate, password), "Senha inválida");
            }
        }
        await using var transaction = await identity.Database.BeginTransactionAsync();
        await AppRoleInitializer.InitializeAsync(roles);
        foreach (var account in accounts)
        {
            var user = await users.FindByEmailAsync(account.Email);
            var created = user == null;
            if (created)
            {
                user = new IdentityUser { UserName = account.Email, Email = account.Email, EmailConfirmed = true };
                RequireSuccess(await users.CreateAsync(user, ReadPassword(account.Role)!), "Criar " + account.Email);
            }
            var previous = await users.GetRolesAsync(user!);
            var remove = previous.Where(role => role != account.Role).ToArray();
            if (remove.Length > 0)
                RequireSuccess(await users.RemoveFromRolesAsync(user!, remove), "Corrigir Roles de " + account.Email);
            if (!await users.IsInRoleAsync(user!, account.Role))
                RequireSuccess(await users.AddToRoleAsync(user!, account.Role), "Atribuir Role a " + account.Email);
            if (!created && (remove.Length > 0 || !previous.Contains(account.Role)))
                RequireSuccess(await users.UpdateSecurityStampAsync(user!), "Atualizar stamp de " + account.Email);
            var finalRoles = await users.GetRolesAsync(user!);
            if (finalRoles.Count != 1 || finalRoles[0] != account.Role)
                throw new InvalidOperationException("Roles finais inconsistentes: " + account.Email);
            var login = await signIn.CheckPasswordSignInAsync(user!, ReadPassword(account.Role)!, lockoutOnFailure: false);
            if (!login.Succeeded) throw new InvalidOperationException("Autenticação recusada: " + account.Email);
        }
        await transaction.CommitAsync();
        Console.WriteLine("Provisionamento concluído atomicamente. Nenhum TbProfissional/paciente criado.");
    }
    // Validar diretamente a persistência, também ao reexecutar --verify.
    foreach (var account in accounts)
    {
        var user = await users.FindByEmailAsync(account.Email)
            ?? throw new InvalidOperationException("Conta ausente: " + account.Email);
        var persisted = await (from membership in identity.UserRoles
                               join role in identity.Roles on membership.RoleId equals role.Id
                               where membership.UserId == user.Id select role.Name).ToListAsync();
        if (persisted.Count != 1 || persisted[0] != account.Role)
            throw new InvalidOperationException("Associações persistidas incorretas: " + account.Email);
        if (await domain.TbProfissional.AnyAsync(p => p.IdUser == user.Id))
            throw new InvalidOperationException("Gerente vinculado a profissional: " + account.Email);
        var password = ReadPassword(account.Role);
        var loginStatus = "não testado (senha ausente)";
        if (!string.IsNullOrEmpty(password))
        {
            if (!(await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false)).Succeeded)
                throw new InvalidOperationException("Login recusado: " + account.Email);
            loginStatus = "PASS";
        }
        var principal = await signIn.CreateUserPrincipalAsync(user);
        if (!principal.IsInRole(account.Role) || principal.IsInRole(AppRoles.Medico) || principal.IsInRole(AppRoles.Nutricionista))
            throw new InvalidOperationException("Claims inconsistentes: " + account.Email);
        Console.WriteLine($"PASS {account.Email}: existe; Role={account.Role}; quantidade=1; TbProfissional=0; claims=PASS; login={loginStatus}");
    }
    return 0;
}
catch (SqlException exception)
{
    Console.Error.WriteLine($"Falha SQL (código {exception.Number}). Verifique acesso, DefaultConnection e tabelas existentes. Nenhuma migration foi executada.");
    return 2;
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
