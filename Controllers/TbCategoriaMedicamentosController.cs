
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;

// Janynne Stephanie de Oliveira Palheta
public class TbCategoriaMedicamentosController : Controller
{
    private readonly db_IFContext _context;

    public TbCategoriaMedicamentosController(db_IFContext context)
    {
        _context = context;
    }

    // GET: TBCATEGORIAMEDICAMENTOS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.TbCategoriaMedicamento.ToListAsync());
    }

    // GET: TBCATEGORIAMEDICAMENTOS/Details/5
    public async Task<IActionResult> Details(int? idcategoriamedicamento)
    {
        if (idcategoriamedicamento == null)
        {
            return NotFound();
        }

        var tbcategoriamedicamento = await _context.TbCategoriaMedicamento
            .FirstOrDefaultAsync(m => m.IdCategoriaMedicamento == idcategoriamedicamento);
        if (tbcategoriamedicamento == null)
        {
            return NotFound();
        }

        return View(tbcategoriamedicamento);
    }

    // GET: TBCATEGORIAMEDICAMENTOS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TBCATEGORIAMEDICAMENTOS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdCategoriaMedicamento,Nome,InformacaoComplementar,TbMedicamento")] TbCategoriaMedicamento tbcategoriamedicamento)
    {
        if (ModelState.IsValid)
        {
            _context.Add(tbcategoriamedicamento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(tbcategoriamedicamento);
    }

    // GET: TBCATEGORIAMEDICAMENTOS/Edit/5
    public async Task<IActionResult> Edit(int? idcategoriamedicamento)
    {
        if (idcategoriamedicamento == null)
        {
            return NotFound();
        }

        var tbcategoriamedicamento = await _context.TbCategoriaMedicamento.FindAsync(idcategoriamedicamento);
        if (tbcategoriamedicamento == null)
        {
            return NotFound();
        }
        return View(tbcategoriamedicamento);
    }

    // POST: TBCATEGORIAMEDICAMENTOS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? idcategoriamedicamento, [Bind("IdCategoriaMedicamento,Nome,InformacaoComplementar,TbMedicamento")] TbCategoriaMedicamento tbcategoriamedicamento)
    {
        if (idcategoriamedicamento != tbcategoriamedicamento.IdCategoriaMedicamento)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tbcategoriamedicamento);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TbCategoriaMedicamentoExists(tbcategoriamedicamento.IdCategoriaMedicamento))
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
        return View(tbcategoriamedicamento);
    }

    // GET: TBCATEGORIAMEDICAMENTOS/Delete/5
    public async Task<IActionResult> Delete(int? idcategoriamedicamento)
    {
        if (idcategoriamedicamento == null)
        {
            return NotFound();
        }

        var tbcategoriamedicamento = await _context.TbCategoriaMedicamento
            .FirstOrDefaultAsync(m => m.IdCategoriaMedicamento == idcategoriamedicamento);
        if (tbcategoriamedicamento == null)
        {
            return NotFound();
        }

        return View(tbcategoriamedicamento);
    }

    // POST: TBCATEGORIAMEDICAMENTOS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? idcategoriamedicamento)
    {
        var tbcategoriamedicamento = await _context.TbCategoriaMedicamento.FindAsync(idcategoriamedicamento);
        if (tbcategoriamedicamento != null)
        {
            _context.TbCategoriaMedicamento.Remove(tbcategoriamedicamento);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool TbCategoriaMedicamentoExists(int? idcategoriamedicamento)
    {
        return _context.TbCategoriaMedicamento.Any(e => e.IdCategoriaMedicamento == idcategoriamedicamento);
    }
}
