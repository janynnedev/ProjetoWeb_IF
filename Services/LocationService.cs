using Microsoft.EntityFrameworkCore;
using Projeto1_IF.Models;

namespace Projeto1_IF.Services;

public sealed class LocationService(db_IFContext context)
{
    public record Option(int Id, string Nome);
    public record ValidationError(string Field, string Message);

    public Task<List<Option>> GetStatesAsync() => context.TbEstado.AsNoTracking()
        .OrderBy(state => state.Nome).Select(state => new Option(state.IdEstado, state.Nome)).ToListAsync();

    public Task<bool> StateExistsAsync(int? stateId) => context.TbEstado
        .AnyAsync(state => state.IdEstado == stateId);

    public Task<List<Option>> GetCitiesByStateAsync(int? stateId) => context.TbCidade.AsNoTracking()
        .Where(city => stateId != null && city.IdEstado == stateId)
        .OrderBy(city => city.Nome).Select(city => new Option(city.IdCidade, city.Nome)).ToListAsync();

    public async Task<List<ValidationError>> ValidateAsync(int? stateId, int? cityId, bool required)
    {
        var errors = new List<ValidationError>();
        // Cidade é opcional no paciente; quando informada, exige Estado compatível.
        if (!required && stateId == null && cityId == null) return errors;
        var validState = await StateExistsAsync(stateId);
        if (!validState)
            errors.Add(new("IdEstado", "Selecione um estado existente."));
        if (cityId == null)
        {
            if (required) errors.Add(new("IdCidade", "Selecione uma cidade existente."));
            return errors;
        }
        var city = await context.TbCidade.AsNoTracking()
            .Where(city => city.IdCidade == cityId).Select(city => new { city.IdEstado }).SingleOrDefaultAsync();
        if (city == null)
            errors.Add(new("IdCidade", "Selecione uma cidade existente."));
        else if (validState && city.IdEstado != stateId)
            errors.Add(new("IdCidade", "A cidade selecionada não pertence ao estado informado."));
        return errors;
    }
}