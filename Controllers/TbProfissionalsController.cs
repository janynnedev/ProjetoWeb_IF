using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Authorization;
using Projeto1_IF.Data;
using System.Linq.Expressions;
using System.Data;
using Projeto1_IF.Models;
using Projeto1_IF.Services;

[Authorize(Roles = AppRoles.Medico + "," + AppRoles.Nutricionista + "," +
    AppRoles.GerenteMedico + "," + AppRoles.GerenteNutricionista + "," + AppRoles.GerenteGeral)]
public class TbProfissionalsController : Controller
{
    private readonly db_IFContext _context;
    private readonly ILogger<TbProfissionalsController> _logger;
    private readonly ApplicationDbContext _identityContext;
    private readonly ICurrentProfessionalService _currentProfessional;
    private readonly LocationService _locations;

    public TbProfissionalsController(db_IFContext context, ICurrentProfessionalService currentProfessional, ApplicationDbContext identityContext,
        ILogger<TbProfissionalsController> logger, LocationService locations)
    {
        _context = context;
        _locations = locations;
        _logger = logger;
        _identityContext = identityContext;
        _currentProfessional = currentProfessional;
    }

    private bool IsManager => User.IsInRole(AppRoles.GerenteGeral)
        || User.IsInRole(AppRoles.GerenteMedico)
        || User.IsInRole(AppRoles.GerenteNutricionista);

    // Consulta Identity de usuário/Role.
    private IQueryable<string> ManagedUserIds()
    {
        var managesDoctors = User.IsInRole(AppRoles.GerenteMedico);
        var managesNutritionists = User.IsInRole(AppRoles.GerenteNutricionista);
        return (from user in _identityContext.Users
                join membership in _identityContext.UserRoles on user.Id equals membership.UserId
                join role in _identityContext.Roles on membership.RoleId equals role.Id
                where (managesDoctors && role.Name == AppRoles.Medico)
                    || (managesNutritionists && role.Name == AppRoles.Nutricionista)
                select user.Id).Distinct();
    }

    public async Task<IActionResult> Index()
    {
        if (!IsManager) return Forbid();
        var professionals = _context.TbProfissional.AsNoTracking();
        if (!User.IsInRole(AppRoles.GerenteGeral))
        {
            var userIds = await ManagedUserIds().ToListAsync();
            professionals = professionals.Where(p => userIds.Contains(p.IdUser));
        }
        return View(await professionals
            .Include(p => p.IdCidadeNavigation)
            .Include(p => p.IdContratoNavigation)
            .Include(p => p.IdTipoAcessoNavigation)
            .OrderBy(p => p.Nome).ToListAsync());
    }
    public IActionResult Create() => Forbid();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TbProfissional professional) => Forbid();

    [Authorize(Roles = AppRoles.GerenteMedico + "," + AppRoles.GerenteNutricionista + "," + AppRoles.GerenteGeral)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!IsManager) return Forbid();
        if (id == null) return NotFound();
        var professional = await _context.TbProfissional.AsNoTracking()
            .SingleOrDefaultAsync(p => p.IdProfissional == id);
        if (professional == null) return NotFound();
        if (!await CanAccessAsync(id)) return Forbid();
        return await DeleteViewAsync(professional);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.GerenteMedico + "," + AppRoles.GerenteNutricionista + "," + AppRoles.GerenteGeral)]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (!IsManager) return Forbid();
        if (id == null) return NotFound();
        TbProfissional? professional = null;
        try
        {
            // Revalidar e manter os vínculos estáveis até a remoção.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            professional = await _context.TbProfissional.SingleOrDefaultAsync(p => p.IdProfissional == id);
            if (professional == null) return NotFound();
            if (!await CanAccessAsync(id)) return Forbid();
            if (await HasPatientsAsync(professional.IdProfissional))
                return BlockedDeleteView(professional,
                    "Este profissional possui pacientes cadastrados e não pode ser excluído.");
            if (await HasOtherDependenciesAsync(professional.IdProfissional))
                return BlockedDeleteView(professional,
                    "Este profissional possui consultas, perguntas ou receitas associadas e não pode ser excluído.");

            // Remover apenas TbProfissional. Conta Identity, Roles e dados relacionados permanecem.
            _context.TbProfissional.Remove(professional);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Falha ao excluir profissional {ProfessionalId}.", id);
            if (professional == null) return NotFound();
            return await DeleteViewAsync(professional,
                "Não foi possível excluir o profissional. Pode haver registros dependentes; nenhum dado relacionado foi removido.");
        }
    }

    private Task<bool> HasPatientsAsync(int professionalId) => _context.TbMedicoPaciente
        .AnyAsync(link => link.IdProfissional == professionalId);

    private Task<bool> HasOtherDependenciesAsync(int professionalId) => _context.TbProfissional
        .Where(p => p.IdProfissional == professionalId)
        .AnyAsync(p => p.TbHoraPacienteProfissional.Any() || p.TbPergunta.Any()
            || p.TbReceitaMedicaPadrao.Any() || p.TbReceitaAlimentarPadrao.Any());

    private IActionResult BlockedDeleteView(TbProfissional professional, string message)
    {
        ViewData["CanDelete"] = false;
        ViewData["DeletionMessage"] = message;
        return View("Delete", professional);
    }

    private async Task<IActionResult> DeleteViewAsync(TbProfissional professional, string? errorMessage = null)
    {
        if (await HasPatientsAsync(professional.IdProfissional))
            return BlockedDeleteView(professional,
                "Este profissional possui pacientes cadastrados e não pode ser excluído.");
        if (await HasOtherDependenciesAsync(professional.IdProfissional))
            return BlockedDeleteView(professional,
                "Este profissional possui consultas, perguntas ou receitas associadas e não pode ser excluído.");
        ViewData["CanDelete"] = true;
        ViewData["DeletionMessage"] = errorMessage;
        return View("Delete", professional);
    }

    private async Task<bool> CanAccessAsync(int? id)
    {
        if (id == null) return false;
        if (User.IsInRole(AppRoles.GerenteGeral)) return true;
        if (IsManager)
        {
            var userId = await _context.TbProfissional.AsNoTracking()
                .Where(p => p.IdProfissional == id).Select(p => p.IdUser).SingleOrDefaultAsync();
            return userId != null && await ManagedUserIds().AnyAsync(value => value == userId);
        }
        if (!User.IsInRole(AppRoles.Medico) && !User.IsInRole(AppRoles.Nutricionista)) return false;
        var professional = await _currentProfessional.GetCurrentProfessionalAsync(User);
        return professional != null && professional.IdProfissional == id;
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!await CanAccessAsync(id)) return Forbid();
        var professional = await _context.TbProfissional.AsNoTracking()
            .Include(p => p.IdCidadeNavigation)
            .Include(p => p.IdContratoNavigation)
            .Include(p => p.IdTipoAcessoNavigation)
            .SingleOrDefaultAsync(p => p.IdProfissional == id);
        if (professional == null) return Forbid();
        return View(professional);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Medico + "," + AppRoles.Nutricionista)]
    public async Task<IActionResult> MyProfile()
    {
        if (IsManager) return Forbid();
        var professional = await _currentProfessional.GetCurrentProfessionalAsync(User);
        if (professional == null) return Forbid();
        return RedirectToAction(nameof(Edit), new { id = professional.IdProfissional });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (!await CanAccessAsync(id)) return Forbid();
        var professional = await _context.TbProfissional.AsNoTracking()
            .SingleOrDefaultAsync(p => p.IdProfissional == id);
        if (professional == null) return Forbid();
        var stateId = await _context.TbCidade.Where(c => c.IdCidade == professional.IdCidade)
            .Select(c => c.IdEstado).SingleOrDefaultAsync();
        await LoadCitiesAsync(stateId, professional.IdCidade);
        return View(professional);
    }

    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int? id, int? idEstado)
    {
        if (!await CanAccessAsync(id)) return Forbid();
        var professional = await _context.TbProfissional.SingleOrDefaultAsync(p => p.IdProfissional == id);
        if (professional == null) return Forbid();

        // Somente gerentes podem incluir CPF.
        var allowedFields = new List<Expression<Func<TbProfissional, object?>>>
        {
            p => p.IdCidade, p => p.Nome, p => p.CrmCrn, p => p.Especialidade,
            p => p.Logradouro, p => p.Numero, p => p.Bairro, p => p.Cep,
            p => p.Cidade, p => p.Estado, p => p.Ddd1, p => p.Ddd2,
            p => p.Telefone1, p => p.Telefone2
        };
        if (IsManager) allowedFields.Add(p => p.Cpf);
        if (await TryUpdateModelAsync(professional, "", allowedFields.ToArray()))
        {
            foreach (var error in await _locations.ValidateAsync(idEstado, professional.IdCidade, required: true))
                ModelState.AddModelError(error.Field, error.Message);
            if (ModelState.IsValid)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id = professional.IdProfissional });
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Não foi possível salvar as alterações.");
                }
            }
        }
        // Não deixar um valor de CPF fabricado reaparecer na tela após erro de validação.
        if (!IsManager) ModelState.Remove(nameof(TbProfissional.Cpf));
        await LoadCitiesAsync(idEstado, professional.IdCidade);
        return View("Edit", professional);
    }

    private async Task LoadCitiesAsync(int? stateId, int cityId)
    {
        ViewData["IdEstado"] = new SelectList(await _locations.GetStatesAsync(), "Id", "Nome", stateId);
        ViewData["IdCidade"] = new SelectList(await _locations.GetCitiesByStateAsync(stateId), "Id", "Nome", cityId);
        ViewData["SelectedStateId"] = stateId;
    }
}
