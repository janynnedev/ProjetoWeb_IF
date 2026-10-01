// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Projeto1_IF.Data;
using Projeto1_IF.Models;
using Projeto1_IF.Authorization;
using Projeto1_IF.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;

namespace Projeto1_IF.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserStore<IdentityUser> _userStore;
    private readonly IUserEmailStore<IdentityUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;
    private readonly ApplicationDbContext _identityContext;
    private readonly db_IFContext _context;
    private readonly LocationService _locations;

    public enum ProfessionalKind { Medico = 1, Nutricionista = 2 }
    public SelectList States { get; private set; } = default!;
    public SelectList Cities { get; private set; } = default!;
    public SelectList Plans { get; private set; } = default!;
    public Dictionary<int, List<PlanOption>> PlansByKind { get; private set; } = new();
    public record PlanOption(int IdPlano, string Nome);

    // Seleção do tipo de profissional e do plano é feita no front-end, mas a validação é feita no back-end para evitar manipulação de dados.
    private static bool IsPlanAllowed(ProfessionalKind? kind, string? name) => kind switch
    {
        ProfessionalKind.Medico => name is "Médico Total" or "Médico Parcial",
        ProfessionalKind.Nutricionista => name is "Nutrição Total" or "Nutrição Parcial",
        _ => false
    };

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        IUserStore<IdentityUser> userStore,
        SignInManager<IdentityUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender,
        ApplicationDbContext identityContext,
        db_IFContext context,
        LocationService locations)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
        _identityContext = identityContext;
        _context = context;
        _locations = locations;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required]
        [EnumDataType(typeof(ProfessionalKind))]
        [Display(Name = "Profissão")]
        public ProfessionalKind? Kind { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Cidade")]
        public int IdCidade { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Estado")]
        public int IdEstado { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Plano")]
        public int IdPlano { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = default!;

        [Required]
        [StringLength(15)]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = default!;

        [StringLength(20)]
        [Display(Name = "CRM / CRN")]
        public string? CrmCrn { get; set; }

        [StringLength(100)]
        [Display(Name = "Especialidade")]
        public string? Especialidade { get; set; }

        [StringLength(100)]
        [Display(Name = "Logradouro")]
        public string? Logradouro { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Número")]
        public string Numero { get; set; } = default!;

        [Required]
        [StringLength(100)]
        [Display(Name = "Bairro")]
        public string Bairro { get; set; } = default!;

        [Required]
        [StringLength(10)]
        [Display(Name = "CEP")]
        public string Cep { get; set; } = default!;

        [StringLength(100)]
        [Display(Name = "Cidade")]
        public string? Cidade { get; set; }

        [StringLength(2)]
        [Display(Name = "Estado")]
        public string? Estado { get; set; }

        [StringLength(2)]
        [Display(Name = "DDD 1")]
        public string? Ddd1 { get; set; }

        [StringLength(2)]
        [Display(Name = "DDD 2")]
        public string? Ddd2 { get; set; }

        [StringLength(25)]
        [Display(Name = "Telefone 1")]
        public string? Telefone1 { get; set; }

        [StringLength(25)]
        [Display(Name = "Telefone 2")]
        public string? Telefone2 { get; set; }


    }


    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await LoadOptionsAsync();
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await LoadOptionsAsync();
        if (Input.Kind is not ProfessionalKind.Medico and not ProfessionalKind.Nutricionista)
            ModelState.AddModelError("Input.Kind", "Selecione Médico ou Nutricionista.");
        foreach (var error in await _locations.ValidateAsync(Input.IdEstado, Input.IdCidade, required: true))
            ModelState.AddModelError("Input." + error.Field, error.Message);
        var selectedPlan = await _context.TbPlano.AsNoTracking()
            .SingleOrDefaultAsync(p => p.IdPlano == Input.IdPlano);
        if (selectedPlan == null)
            ModelState.AddModelError("Input.IdPlano", "Selecione um plano existente.");
        else if (!IsPlanAllowed(Input.Kind, selectedPlan.Nome))
            ModelState.AddModelError("Input.IdPlano", "O plano selecionado não é válido para o tipo de profissional escolhido.");
        returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        if (ModelState.IsValid)
        {
            var user = CreateUser();

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            if (await CreateProfessionalAccountAsync(user))
            {
                _logger.LogInformation("User created a new account with password.");

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme)!;

                await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                }
                else
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

        }

        return Page();
    }

    private async Task LoadOptionsAsync()
    {
        States = new SelectList(await _locations.GetStatesAsync(), "Id", "Nome", Input?.IdEstado);
        Cities = new SelectList(await _locations.GetCitiesByStateAsync(Input?.IdEstado), "Id", "Nome", Input?.IdCidade);
        var plans = await _context.TbPlano.AsNoTracking().OrderBy(p => p.Nome)
            .Select(p => new PlanOption(p.IdPlano, p.Nome)).ToListAsync();
        PlansByKind = Enum.GetValues<ProfessionalKind>().ToDictionary(
            kind => (int)kind,
            kind => plans.Where(p => IsPlanAllowed(kind, p.Nome)).ToList());
        var selectedPlans = Input?.Kind is ProfessionalKind kind && PlansByKind.TryGetValue((int)kind, out var options)
            ? options : new List<PlanOption>();
        Plans = new SelectList(selectedPlans, nameof(PlanOption.IdPlano), nameof(PlanOption.Nome), Input?.IdPlano);
    }

    private bool CheckIdentityResult(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            _logger.LogWarning("Falha Identity no cadastro: {Code} - {Description}", error.Code, error.Description);
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return result.Succeeded;
    }

    private async Task<bool> CreateProfessionalAccountAsync(IdentityUser user)
    {
        // Mesma conexão e transação SQL: Identity, Role, contrato e profissional são atômicos.
        _context.Database.SetDbConnection(_identityContext.Database.GetDbConnection());
        await using var transaction = await _identityContext.Database.BeginTransactionAsync();
        await using var domainTransaction = await _context.Database.UseTransactionAsync(transaction.GetDbTransaction());
        var committed = false;
        try
        {
            if (!CheckIdentityResult(await _userManager.CreateAsync(user, Input.Password)))
                return false;

            var professionalRole = Input.Kind switch
            {
                ProfessionalKind.Medico => AppRoles.Medico,
                ProfessionalKind.Nutricionista => AppRoles.Nutricionista,
                _ => throw new InvalidOperationException("Profissão inválida.")
            };
            if (!CheckIdentityResult(await _userManager.AddToRoleAsync(user, professionalRole)))
                return false;

            var startDate = DateTime.UtcNow;
            var professional = new TbProfissional
            {
                IdUser = user.Id,
                IdCidade = Input.IdCidade,
                IdContratoNavigation = new TbContrato
                {
                    IdPlano = Input.IdPlano,
                    DataInicio = startDate,
                    DataFim = startDate.AddMonths(1)
                },
                Nome = Input.Nome,
                Cpf = Input.Cpf,
                CrmCrn = Input.CrmCrn!,
                Especialidade = Input.Especialidade!,
                Logradouro = Input.Logradouro!,
                Numero = Input.Numero,
                Bairro = Input.Bairro,
                Cep = Input.Cep,
                Cidade = Input.Cidade!,
                Estado = Input.Estado!,
                Ddd1 = Input.Ddd1!,
                Ddd2 = Input.Ddd2!,
                Telefone1 = Input.Telefone1!,
                Telefone2 = Input.Telefone2!,
            };
            _context.TbProfissional.Add(professional);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            committed = true;
            return true;
        }
        catch (Exception exception) when (exception is DbUpdateException or SqlException or InvalidOperationException)
        {
            _logger.LogError(exception, "Falha no cadastro integrado do profissional.");
            ModelState.AddModelError(string.Empty, "Não foi possível concluir o cadastro. Nenhum cadastro parcial foi salvo.");
            return false;
        }
        finally
        {
            if (!committed)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                _identityContext.ChangeTracker.Clear();
            }
        }
    }

    private IdentityUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<IdentityUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
        }
    }

    private IUserEmailStore<IdentityUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }
        return (IUserEmailStore<IdentityUser>)_userStore;
    }
}
