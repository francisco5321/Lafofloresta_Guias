# Guias Madeira

Aplicação Windows em .NET/WPF que substitui o Access de guias de madeira (`DB_guias.accdb`), ligada a um servidor PostgreSQL central. Ver `ANALISE_ACCESS.md` para a especificação completa extraída do sistema Access original.

## Estrutura

- `GuiasMadeira.Domain`: entidades e enums do domínio (Proprietario, Destinatario, Rolaria, CodigoBarra, Guia, ViaImpressao).
- `GuiasMadeira.Infrastructure`: acesso a dados PostgreSQL (Npgsql + Dapper), schema SQL e repositórios.
- `GuiasMadeira.Pdf`: geração do relatório da Guia em PDF A5, 4 páginas (Original/Duplicado/Triplicado/Quadriplicado), fiel ao relatório `Nova_Guia` do Access.
- `GuiasMadeira.Desktop`: aplicação WPF (menu + 5 ecrãs) que liga tudo.

## Configurar a base de dados

1. Criar uma base de dados PostgreSQL (ex. `guias_madeira`) no servidor da empresa.
2. Correr o script `src/GuiasMadeira.Infrastructure/Postgres/schema.sql` contra essa base de dados (`psql -f schema.sql` ou uma ferramenta como o pgAdmin/DBeaver).
3. Editar `src/GuiasMadeira.Desktop/appsettings.json` em cada posto de trabalho com a connection string real (o PC liga-se ao servidor via VPN):

   ```json
   {
     "ConnectionStrings": {
       "GuiasDb": "Host=<ip-ou-nome-do-servidor>;Port=5432;Database=guias_madeira;Username=<utilizador>;Password=<password>"
     }
   }
   ```

4. Migrar os dados existentes do Access (tabelas `proprietarios`, `destinatarios`, `Rolaria` → `rolarias`, `CodigosBarras` → `codigos_barras`, `guias`) — os dados de amostra reais estão documentados em `docs/access-export/data_samples.txt` para conferência.

## Imagens do relatório

O relatório usa duas imagens que estavam embutidas no Access (`src/GuiasMadeira.Pdf/Resources/logo.png` e `fundo.png`) — atualmente são placeholders gerados automaticamente. Para as substituir pelas originais:

1. Abrir o `DB_guias.accdb` no Access, em vista de design do relatório `Nova_Guia`.
2. Clicar com o botão direito em cada imagem → "Guardar como imagem" (ou copiar e colar num editor de imagem e exportar como PNG).
3. Substituir os ficheiros em `src/GuiasMadeira.Pdf/Resources/` mantendo os mesmos nomes.

## Correr a aplicação

```
dotnet build GuiasMadeira.sln
dotnet run --project src/GuiasMadeira.Desktop
```

## Documentação de referência

- `ANALISE_ACCESS.md` — análise completa do Access original (schema, queries, formulários, relatório, VBA, inconsistências encontradas e decisões de arquitetura).
- `docs/access-export/` — exports em bruto extraídos do Access (schema, relações, queries, dados de amostra, definições de formulários/relatório) usados como referência durante a implementação.
