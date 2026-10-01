# Gerentes de demonstração

Execute na raiz do repositório. O comando usa DefaultConnection com JSON, User Secrets em Development e variáveis de ambiente, como a aplicação. Não inicia o servidor nem executa migrations.

Contas: `gerentemedico@local.test`, `gerentenutricionista@local.test`, `gerentegeral@local.test`. Cada uma termina com somente sua Role de gerente; nenhum profissional é criado.

```powershell
dotnet run --project tools/ProvisionManagers -- --inspect
$managerPassword = Read-Host 'Senha temporária' -AsSecureString
$env:PROJETO1_IF_MANAGER_PASSWORD = [System.Net.NetworkCredential]::new('', $managerPassword).Password
try {
    dotnet run --project tools/ProvisionManagers -- --apply
    if ($LASTEXITCODE -ne 0) { throw 'Provisionamento falhou' }
    dotnet run --project tools/ProvisionManagers -- --verify
} finally {
    Remove-Item Env:PROJETO1_IF_MANAGER_PASSWORD -ErrorAction SilentlyContinue
    $managerPassword.Dispose()
}
```

A senha permanece somente em memória e no ambiente do processo durante a execução. Não a coloque em arquivos, argumentos ou no Git. Para senhas distintas, utilize `PROJETO1_IF_MANAGER_PASSWORD_GERENTEMEDICO`, `PROJETO1_IF_MANAGER_PASSWORD_GERENTENUTRICIONISTA` e `PROJETO1_IF_MANAGER_PASSWORD_GERENTEGERAL`; elas prevalecem sobre a senha comum.

`--apply` cria contas ausentes e corrige somente as Roles dessas três contas. Reexecução não redefine senhas; a senha informada deve corresponder à conta existente. Contas candidatas vinculadas a TbProfissional ou com identificadores conflitantes são recusadas. Criação, correção de Roles e validação inicial de login ocorrem em uma transação Identity. A validação não emite cookies. Não altera outras contas.

Para execução pelo agente, uma variável de ambiente de usuário Windows pode ser transferida ao ambiente do comando sem imprimir seu valor. Remova essa variável de usuário após o provisionamento.
