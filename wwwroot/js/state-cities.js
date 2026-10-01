// Filtro de UX. Register/Create/Edit validam novamente a associação no servidor.
(() => {
    for (const state of document.querySelectorAll('[data-city-select]')) {
        const city = document.getElementById(state.dataset.citySelect);
        let requestVersion = 0;
        state.addEventListener('change', async () => {
            const version = ++requestVersion;
            city.replaceChildren(new Option(state.value ? 'Carregando cidades...' : 'Selecione primeiro o estado', ''));
            city.disabled = true;
            if (!state.value) return;
            try {
                const url = new URL(state.dataset.citiesUrl, window.location.origin);
                url.searchParams.set('idEstado', state.value);
                const response = await fetch(url);
                if (!response.ok) throw new Error('Falha ao carregar cidades');
                const options = await response.json();
                if (version !== requestVersion) return;
                city.replaceChildren(new Option('Selecione uma cidade', ''));
                for (const option of options) city.add(new Option(option.nome, String(option.id)));
                city.disabled = false;
            } catch {
                if (version !== requestVersion) return;
                city.replaceChildren(new Option('Não foi possível carregar. Selecione o estado novamente.', ''));
            }
        });
    }
})();