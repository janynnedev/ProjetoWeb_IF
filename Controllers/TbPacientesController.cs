using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Authorization;
using Projeto1_IF.Models;
using Projeto1_IF.Services;
using System.Data;

[Authorize(Roles = AppRoles.Medico + "," + AppRoles.Nutricionista)]
public class TbPacientesController : Controller
{
    private readonly db_IFContext _context;
    private readonly LocationService _locations;
    private readonly ICurrentProfessionalService _currentProfessional;
    private readonly ILogger<TbPacientesController> _logger;

    public TbPacientesController(db_IFContext context, ICurrentProfessionalService currentProfessional,
        ILogger<TbPacientesController> logger, LocationService locations)
    {
        _context = context;
        _currentProfessional = currentProfessional;
        _logger = logger;
        _locations = locations;
    }

    private Task<TbProfissional?> GetProfessionalAsync()
    {
        // Gerentes não recebem CRUD de pacientes.
        if (User.IsInRole(AppRoles.GerenteMedico) || User.IsInRole(AppRoles.GerenteNutricionista)
            || User.IsInRole(AppRoles.GerenteGeral)
            || (!User.IsInRole(AppRoles.Medico) && !User.IsInRole(AppRoles.Nutricionista)))
            return Task.FromResult<TbProfissional?>(null);
        return _currentProfessional.GetCurrentProfessionalAsync(User);
    }

    private IQueryable<TbPaciente> OwnedPatients(int professionalId)
    {
        return _context.TbPaciente.Where(patient => patient.TbMedicoPaciente
            .Any(link => link.IdProfissional == professionalId));
    }

    private async Task<TbPaciente?> GetOwnedPatientAsync(int? id, bool tracking = false)
    {
        var professional = await GetProfessionalAsync();
        if (professional == null || id == null) return null;
        var patients = OwnedPatients(professional.IdProfissional);
        if (!tracking) patients = patients.AsNoTracking();
        return await patients.Include(patient => patient.IdCidadeNavigation)
            .ThenInclude(city => city.IdEstadoNavigation)
            .SingleOrDefaultAsync(patient => patient.IdPaciente == id);
    }

    public async Task<IActionResult> Index()
    {
        var professional = await GetProfessionalAsync();
        if (professional == null) return Forbid();
        return View(await OwnedPatients(professional.IdProfissional).AsNoTracking()
            .Include(patient => patient.IdCidadeNavigation).OrderBy(patient => patient.Nome).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        if (await GetProfessionalAsync() == null) return Forbid();
        await LoadCitiesAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nome,Rg,Cpf,DataNascimento,NomeResponsavel,Sexo,Etnia,Endereco,Bairro,IdCidade,TelResidencial,TelComercial,TelCelular,Profissao,FlgAtleta,FlgGestante")] TbPaciente patient, int? idEstado)
    {
        var professional = await GetProfessionalAsync();
        if (professional == null) return Forbid();
        await ValidateRegistrationAsync(idEstado, patient);
        if (ModelState.IsValid)
        {
            // O formulário usa dois estados; normalizar somente no cadastro efetivamente salvo.
            patient.FlgAtleta ??= false;
            patient.FlgGestante ??= false;

            var link = new TbMedicoPaciente
            {
                IdProfissional = professional.IdProfissional,
                IdPacienteNavigation = patient
            };
            _context.TbMedicoPaciente.Add(link);
            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Falha ao criar paciente e associação.");
                ModelState.AddModelError(string.Empty, "Não foi possível salvar o paciente e seu vínculo.");
            }
        }
        await LoadCitiesAsync(idEstado, patient.IdCidade);
        return View(patient);
    }

    public async Task<IActionResult> Details(int? id)
    {
        var patient = await GetOwnedPatientAsync(id);
        if (patient == null) return Forbid();
        return View(patient);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        var patient = await GetOwnedPatientAsync(id);
        if (patient == null) return Forbid();
        await LoadCitiesAsync(patient.IdCidadeNavigation?.IdEstado, patient.IdCidade);
        return View(patient);
    }

    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int? id, int? idEstado)
    {
        var patient = await GetOwnedPatientAsync(id, tracking: true);
        if (patient == null) return Forbid();
        if (await TryUpdateModelAsync(patient, "",
            p => p.Nome, p => p.Rg, p => p.Cpf, p => p.DataNascimento, p => p.NomeResponsavel,
            p => p.Sexo, p => p.Etnia, p => p.Endereco, p => p.Bairro, p => p.IdCidade,
            p => p.TelResidencial, p => p.TelComercial, p => p.TelCelular, p => p.Profissao,
            p => p.FlgAtleta, p => p.FlgGestante))
        {
            await ValidateRegistrationAsync(idEstado, patient);
            if (ModelState.IsValid)
            {
                // NULL legado vira false apenas quando este formulário é salvo.
                patient.FlgAtleta ??= false;
                patient.FlgGestante ??= false;
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException exception)
                {
                    _logger.LogError(exception, "Falha ao editar paciente.");
                    ModelState.AddModelError(string.Empty, "Não foi possível salvar as alterações.");
                }
            }
        }
        await LoadCitiesAsync(idEstado, patient.IdCidade);
        return View("Edit", patient);
    }

    public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
    {
        var patient = await GetOwnedPatientAsync(id);
        if (patient == null) return Forbid();
        if (saveChangesError.GetValueOrDefault())
            ViewData["ErrorMessage"] = "Não foi possível excluir. O paciente e seu vínculo foram preservados. Pode haver registros dependentes.";
        return View(patient);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var professional = await GetProfessionalAsync();
        if (professional == null || id == null) return Forbid();
        try
        {
            // Serializable protege a verificação de outros vínculos contra alterações concorrentes.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var patient = await OwnedPatients(professional.IdProfissional)
                .SingleOrDefaultAsync(patient => patient.IdPaciente == id);
            if (patient == null) return Forbid();

            var ownLinks = await _context.TbMedicoPaciente
                .Where(link => link.IdPaciente == id && link.IdProfissional == professional.IdProfissional)
                .ToListAsync();
            var hasOtherProfessional = await _context.TbMedicoPaciente
                .AnyAsync(link => link.IdPaciente == id && link.IdProfissional != professional.IdProfissional);
            _context.TbMedicoPaciente.RemoveRange(ownLinks);
            if (!hasOtherProfessional)
                _context.TbPaciente.Remove(patient);

            // Sem cascade presumido. Se outra FK impedir a exclusão, o rollback preserva tudo.
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Falha ao excluir paciente ou remover associação.");
            return RedirectToAction(nameof(Delete), new { id, saveChangesError = true });
        }
    }

    private async Task ValidateRegistrationAsync(int? stateId, TbPaciente patient)
    {
        foreach (var error in await _locations.ValidateAsync(stateId, patient.IdCidade, required: false))
            ModelState.AddModelError(error.Field, error.Message);
        if (!PatientRegistrationOptions.IsValidEthnicity(patient.Etnia))
            ModelState.AddModelError(nameof(TbPaciente.Etnia), "Selecione uma etnia válida.");
        if (!PatientRegistrationOptions.IsValidSex(patient.Sexo))
            ModelState.AddModelError(nameof(TbPaciente.Sexo), "Selecione F ou M.");
    }

    private async Task LoadCitiesAsync(int? stateId = null, int? cityId = null)
    {
        ViewData["IdEstado"] = new SelectList(await _locations.GetStatesAsync(), "Id", "Nome", stateId);
        ViewData["IdCidade"] = new SelectList(await _locations.GetCitiesByStateAsync(stateId), "Id", "Nome", cityId);
        ViewData["SelectedStateId"] = stateId;
        ViewData["SexoOptions"] = PatientRegistrationOptions.SexOptions;
    }
}