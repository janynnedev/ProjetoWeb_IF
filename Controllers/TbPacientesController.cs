using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;

[Authorize]
public class TbPacientesController : Controller
{
    private readonly db_IFContext _context;

    public TbPacientesController(db_IFContext context)
    {
        _context = context;
    }

    // GET: TBPACIENTES
    public async Task<IActionResult> Index()    
    {
        var db_IFContext = _context.TbPaciente.Include(t => t.IdCidadeNavigation);
        return View(await _context.TbPaciente.ToListAsync());
    }

    // GET: TBPACIENTES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        TbPaciente? tbpaciente = await _context.TbPaciente
            .Include(t => t.IdCidadeNavigation)
            .ThenInclude(e => e.IdEstadoNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdPaciente == id);
        if (tbpaciente == null)
        {
            return NotFound();
        }

        return View(tbpaciente);
    }

    // GET: TBPACIENTES/Create
    public IActionResult Create()
    {
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome");
        return View();
    }

    // POST: TBPACIENTES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nome,Rg,Cpf,DataNascimento,NomeResponsavel,Sexo,Etnia,Endereco,Bairro,IdCidade,TelResidencial,TelComercial,TelCelular,Profissao,FlgAtleta,FlgGestante")] TbPaciente tbpaciente)
    {
        try
        {
            if (ModelState.IsValid)
            {
                _context.Add(tbpaciente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
           
        } catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Incapaz de salvar.");
        }
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome");
        return View(tbpaciente);
    }

    // GET: TBPACIENTES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbpaciente = await _context.TbPaciente.FindAsync(id);
        if (tbpaciente == null)
        {
            return NotFound();
        }
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbpaciente.IdCidade);
        return View(tbpaciente);
    }

    // POST: TBPACIENTES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int? id) 
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbpaciente = await _context.TbPaciente.FirstOrDefaultAsync(s => s.IdPaciente == id);
        if (tbpaciente == null)
        {
            return NotFound();
        }
        if (await TryUpdateModelAsync<TbPaciente>(
            tbpaciente, "",
            s => s.Nome, s => s.Rg, s => s.Cpf, s => s.DataNascimento, s => s.NomeResponsavel, s => s.Sexo,
            s => s.Etnia, s => s.Endereco, s => s.Bairro, s => s.IdCidade, s => s.TelResidencial, s => s.TelComercial,
            s => s.TelCelular, s => s.Profissao, s => s.FlgAtleta, s => s.FlgGestante))
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
        ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbpaciente.IdCidade);
        return View(tbpaciente);
    }

    // GET: TBPACIENTES/Delete/5
    public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
    {
        if (id == null)
        {
            return NotFound();
        }

        var tbpaciente = await _context.TbPaciente
            .Include(t => t.IdCidadeNavigation)
            .ThenInclude(e => e.IdEstadoNavigation)
            .AsNoTracking() 
            .FirstOrDefaultAsync(m => m.IdPaciente == id);
        if (tbpaciente == null)
        {
            return NotFound();
        }

        if (saveChangesError.GetValueOrDefault())
        {
            ViewData["ErrorMessage"] = "Falha ao excluir. Tente novamente.";
        }
        return View(tbpaciente);
    }

    // POST: TBPACIENTES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var tbpaciente = await _context.TbPaciente.FindAsync(id);
        if (tbpaciente == null)
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.TbPaciente.Remove(tbpaciente);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException) {
            return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
        }
    }

}
