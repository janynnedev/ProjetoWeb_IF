using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;
using System.Data;
using System.Runtime.ConstrainedExecution;
using static System.Runtime.InteropServices.JavaScript.JSType;

[Authorize]
public class TbProfissionalsController : Controller
{
    private readonly db_IFContext _context;

    public TbProfissionalsController(db_IFContext context)
    {
        _context = context;
    }

    // GET: TBPROFISSIONALS
    public async Task<IActionResult> Index()    
    {
        var db_IFContext = _context.TbProfissional.Include(t => t.IdCidadeNavigation).Include(t => t.IdContratoNavigation).Include(t => t.IdTipoAcessoNavigation);
        return View(await db_IFContext.ToListAsync());
    }

    // GET: TBPROFISSIONALS/Details/5
    public async Task<IActionResult> Details(int? idprofissional)
    {
        if (idprofissional == null)
        {
            return NotFound();
        }

        TbProfissional? tbprofissional = await _context.TbProfissional
            .Include(t => t.IdCidadeNavigation)
            .Include(t => t.IdContratoNavigation)
            .Include(t => t.IdTipoAcessoNavigation)
            .FirstOrDefaultAsync(m => m.IdProfissional == idprofissional);
        if (tbprofissional == null)
        {
            return NotFound();
        }

        return View(tbprofissional);
    }

    // GET: TBPROFISSIONALS/Create
    public IActionResult Create()
    {
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome");
        ViewData["IdPlano"] = new SelectList(_context.TbPlano, "IdPlano", "Nome");
        ViewData["IdTipoAcesso"] = new SelectList(_context.TbTipoAcesso, "IdTipoAcesso", "Nome");

        return View();
    }

    // POST: TBPROFISSIONALS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdTipoProfissional,IdTipoAcesso,IdCidade,IdUser,Nome,Cpf,CrmCrn,Especialidade,Logradouro,Numero,Bairro,Cep,Cidade,Estado,Ddd1,Ddd2,Telefone1,Telefone2,Salario")] TbProfissional tbprofissional, [Bind("IdPlano")] TbContrato IdContratoNavigation)
    {
        ModelState.Remove("IdUser");
        try
        {
            if (ModelState.IsValid)
            {
                IdContratoNavigation.DataInicio = DateTime.UtcNow;
                IdContratoNavigation.DataFim = IdContratoNavigation.DataInicio.Value.AddMonths(1);
                _context.Add(IdContratoNavigation);
                await _context.SaveChangesAsync();


                var userManager = HttpContext.RequestServices.GetService<UserManager<IdentityUser>>();
                if (userManager != null)
                {
                    var email = User.Identity?.Name;
                    if (email != null)
                    {
                        var user = await userManager.FindByEmailAsync(email);
                        if (user != null)
                        {
                            tbprofissional.IdUser = user.Id;
                        }
                        else return NotFound(new { message = "Usuário não encontrado." });
                    }
                    else return NotFound(new { message = "Email do usuário não encontrado." });
                }
                else return NotFound(new { message = "UserManager não encontrado." });

                tbprofissional.IdContrato = IdContratoNavigation.IdContrato;
                _context.Add(tbprofissional);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
        } catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Incapaz de salvar.");
        }
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbprofissional.IdCidade);
        ViewData["IdPlano"] = new SelectList(_context.TbPlano, "IdPlano", "Nome", tbprofissional.IdContratoNavigation.IdPlano);
        ViewData["IdTipoAcesso"] = new SelectList(_context.TbTipoAcesso, "IdTipoAcesso", "Nome", tbprofissional.IdTipoAcesso);
        return View(tbprofissional);
    }

    // GET: TBPROFISSIONALS/Edit/5
    public async Task<IActionResult> Edit(int? idprofissional)
    {
        if (idprofissional == null)
        {
            return NotFound();
        }

        var tbprofissional = await _context.TbProfissional.Include(t => t.IdContratoNavigation).FirstOrDefaultAsync(t => t.IdProfissional == idprofissional);
        if (tbprofissional == null)
        {
            return NotFound();
        }
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbprofissional.IdCidade);
        ViewData["IdContrato"] = new SelectList(_context.TbPlano, "IdPlano", "Nome", tbprofissional.IdContratoNavigation.IdPlano);
        ViewData["IdTipoAcesso"] = new SelectList(_context.TbTipoAcesso, "IdTipoAcesso", "Nome", tbprofissional.IdTipoAcesso);
        return View(tbprofissional);
    }

    // POST: TBPROFISSIONALS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int? idprofissional)
    {
        if (idprofissional == null)
        {
            return NotFound();
        }

        var tbprofissional = await _context.TbProfissional.FirstOrDefaultAsync(s => s.IdProfissional == idprofissional);
        if (await TryUpdateModelAsync<TbProfissional>(tbprofissional, "",
            s => s.IdTipoProfissional, s => s.IdTipoAcesso, s => s.IdCidade, s => s.IdUser, s => s.Nome, s => s.Cpf, s => s.CrmCrn,
            s => s.Especialidade, s => s.Logradouro, s => s.Numero, s => s.Bairro, s => s.Cep, s => s.Cidade, s => s.Estado, s => s.Ddd1,
            s => s.Ddd2, s => s.Telefone1, s => s.Telefone2, s => s.Salario))
            {

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Incapaz de salvar as mudanças");
                }
            }
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbprofissional.IdCidade);
        ViewData["IdContrato"] = new SelectList(_context.TbPlano, "IdPlano", "Nome", tbprofissional.IdContratoNavigation.IdPlano);
        ViewData["IdTipoAcesso"] = new SelectList(_context.TbTipoAcesso, "IdTipoAcesso", "Nome", tbprofissional.IdTipoAcesso);
        return View(tbprofissional);
    }

    // GET: TBPROFISSIONALS/Delete/5
    public async Task<IActionResult> Delete(int? idprofissional)
    {
        if (idprofissional == null)
        {
            return NotFound();
        }

        var tbProfissional = await _context.TbProfissional
            .Include(t => t.IdCidadeNavigation)
            .Include(t => t.IdContratoNavigation)
            .Include(t => t.IdTipoAcessoNavigation)
            .FirstOrDefaultAsync(m => m.IdProfissional == idprofissional);
        if (tbProfissional == null)
        {
            return NotFound();
        }

        return View(tbProfissional);
    }

    // POST: TBPROFISSIONALS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? idprofissional)
    {
        var tbProfissional = await _context.TbProfissional.FindAsync(idprofissional);
        if (tbProfissional != null)
        {
            _context.TbProfissional.Remove(tbProfissional);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool TbProfissionalExists(int? idprofissional)
    {
        return _context.TbProfissional.Any(e => e.IdProfissional == idprofissional);
    }
}
