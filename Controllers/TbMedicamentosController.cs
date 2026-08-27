
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;

// Janynne Stephanie de Oliveira Palheta
public class TbMedicamentosController : Controller
{
    private readonly db_IFContext _context;

    public TbMedicamentosController(db_IFContext context)
    {
        _context = context;
    }

    // GET: TBMEDICAMENTOS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.TbMedicamento.ToListAsync());
    }

    // GET: TBMEDICAMENTOS/Details/5
    public async Task<IActionResult> Details(int? idmedicamento)
    {
        if (idmedicamento == null)
        {
            return NotFound();
        }

        var tbmedicamento = await _context.TbMedicamento
            .FirstOrDefaultAsync(m => m.IdMedicamento == idmedicamento);
        if (tbmedicamento == null)
        {
            return NotFound();
        }

        return View(tbmedicamento);
    }

    // GET: TBMEDICAMENTOS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TBMEDICAMENTOS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdMedicamento,IdCategoriaMedicamento,Nome,Bula,BulaArquivo,IdCategoriaMedicamentoNavigation")] TbMedicamento tbmedicamento)
    {
        if (ModelState.IsValid)
        {
            _context.Add(tbmedicamento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(tbmedicamento);
    }

    // GET: TBMEDICAMENTOS/Edit/5
    public async Task<IActionResult> Edit(int? idmedicamento)
    {
        if (idmedicamento == null)
        {
            return NotFound();
        }

        var tbmedicamento = await _context.TbMedicamento.FindAsync(idmedicamento);
        if (tbmedicamento == null)
        {
            return NotFound();
        }
        return View(tbmedicamento);
    }

    // POST: TBMEDICAMENTOS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? idmedicamento, [Bind("IdMedicamento,IdCategoriaMedicamento,Nome,Bula,BulaArquivo,IdCategoriaMedicamentoNavigation")] TbMedicamento tbmedicamento)
    {
        if (idmedicamento != tbmedicamento.IdMedicamento)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tbmedicamento);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TbMedicamentoExists(tbmedicamento.IdMedicamento))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(tbmedicamento);
    }

    // GET: TBMEDICAMENTOS/Delete/5
    public async Task<IActionResult> Delete(int? idmedicamento)
    {
        if (idmedicamento == null)
        {
            return NotFound();
        }

        var tbmedicamento = await _context.TbMedicamento
            .FirstOrDefaultAsync(m => m.IdMedicamento == idmedicamento);
        if (tbmedicamento == null)
        {
            return NotFound();
        }

        return View(tbmedicamento);
    }

    // POST: TBMEDICAMENTOS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? idmedicamento)
    {
        var tbmedicamento = await _context.TbMedicamento.FindAsync(idmedicamento);
        if (tbmedicamento != null)
        {
            _context.TbMedicamento.Remove(tbmedicamento);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool TbMedicamentoExists(int? idmedicamento)
    {
        return _context.TbMedicamento.Any(e => e.IdMedicamento == idmedicamento);
    }
}
