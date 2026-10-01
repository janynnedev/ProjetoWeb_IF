using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeto1_IF.Services;

namespace Projeto1_IF.Controllers;

[AllowAnonymous]
public class LocationsController(LocationService locations) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Cities(int? idEstado)
    {
        if (!ModelState.IsValid || !await locations.StateExistsAsync(idEstado))
            return BadRequest("Selecione um estado existente.");
        return Json(await locations.GetCitiesByStateAsync(idEstado));
    }
}