# Análise Completa — DB_guias.accdb

> Documento de engenharia reversa da base de dados Access `C:\Lafofloresta\DB_guias.accdb`, extraído por automação COM/DAO (schema, relações, queries, formulários, relatório e VBA). Serve de especificação de referência para a reconstrução do software cliente-servidor.

---

## 1. Resumo executivo

O `DB_guias.accdb` é uma aplicação Access pequena e artesanal (6 formulários, 1 relatório, 9 queries, 6 tabelas, ~35 registos no total) para emitir **Guias de Entrada de Rolaria** — o documento legal que acompanha o transporte de madeira, com 4 vias (Original/Duplicado/Triplicado/Quadruplicado), texto de certificação florestal (FSC/PEFC via Cerna Portugal, APCER) e código de barras.

A lógica é deliberadamente mínima: o utilizador escolhe **Destinatário**, **Proprietário**, **Código de Barras** e **Rolaria** em dropdowns já pré-carregados, escreve o **Fornecedor** livremente, e carrega em "Imprimir". Toda a restante informação (NIF, morada, distrito, concelho, freguesia, certificado, etc.) já está na base de dados e é herdada automaticamente para o relatório — exatamente o padrão de "mínimo esforço" que o pedido do cliente exige replicar.

**Achado crítico:** o projeto .NET já iniciado em `src/` (WPF + SQLite + iTextSharp) **não corresponde à base de dados real**. Foi construído sobre um schema especulativo (campos como `numero`, `data_emissao`, `volume_m3`, `observacoes`, `telefone`, `email` não existem na BD real; faltam `NIF`, `Distrito`, `Concelho`, `Freguesia`, `CodigoProp`, `Parcela`, `Fornecedor`, e as tabelas inteiras `CodigosBarras` e `Vias`) e o gerador de PDF reinventa um layout diferente do relatório Access real (cores das vias erradas, "carimbo" que não existe, sem os textos legais/certificação, sem a lógica real de barcode). **Este scaffold deve ser tratado como protótipo descartável, não como base de trabalho.**

---

## 2. Modelo de dados real

### 2.1 Tabelas

| Tabela | PK | Campos | Registos atuais |
|---|---|---|---|
| **proprietarios** | IDProprietario (autonum) | Nome, Distrito, Concelho, Freguesia, CodigoProp, Parcela | 7 |
| **destinatarios** | IDDestinatario (autonum) | Nome, NIF (Long Integer), Morada, Concelho | 4 |
| **Rolaria** | IDRolaria (autonum) | Tipo (texto livre, ex. "Eucalipto Globulus") | 7 |
| **CodigosBarras** | IDCodigo (autonum) | Código de Barras, Número do Certificado, Número da UGF | 11 |
| **Vias** | idVia (autonum) | NomeVia — valores fixos: 1=ORIGINAL, 2=DUPLICADO, 3=TRIPLICADO, 4=QUADRIPLICADO | 4 |
| **guias** | IDGuia (autonum) | IDDestinatario (FK), IDProprietario (FK), IDCodigo (FK), IDRolaria (FK), Fornecedor (texto livre) | 6 |

Todos os campos texto são `AllowZeroLength=True` e nenhum é `Required` ao nível da BD — a validação existe apenas na camada VBA dos formulários (ver secção 5).

### 2.2 Relações (declaradas no Access)

- `proprietarios.IDProprietario` → `guias.IDProprietario`
- `destinatarios.IDDestinatario` → `guias.IDDestinatario`
- `Rolaria.IDRolaria` → `guias.IDRolaria`
- `CodigosBarras.IDCodigo` → `guias.IDCodigo`
- **`Vias` não tem relação declarada com `guias`** — é combinada por produto cartesiano na query `Relatorio` (ver 3), o que é o mecanismo real por trás das "4 vias" do documento.

### 2.3 Diagrama lógico

```
proprietarios ──┐
destinatarios ──┼──► guias ◄── CodigosBarras
Rolaria ─────────┘        (Vias é cruzada via produto cartesiano na query "Relatorio")
```

---

## 3. Queries

| Query | Propósito |
|---|---|
| `Consulta1` | Lookup de `destinatarios` (Id, Nome) — RowSource do combo no formulário `guias` |
| `Consulta2` | Lookup de `proprietarios` (Id, Nome) — não usada diretamente (substituída por `Guia_proprietario`) |
| `Cópia de Consulta1` | Duplicado de `Consulta1` com mais campos — resíduo de desenvolvimento, não referenciado |
| `destinatarios_para_guias` | Idêntica a `Cópia de Consulta1`, também não referenciada — resíduo |
| `Guia_Propriedades`, `Guia_Propriedades_novo`, `Guia_proprietario` | Três variantes quase idênticas do mesmo lookup de proprietário; **`Guia_Propriedades` e `Guia_Propriedades_novo` referenciam `proprietarios.NomePropriedade`, campo que não existe na tabela real — estas duas queries estão quebradas/mortas.** Só `Guia_proprietario` (sem esse campo) é usada, como RowSource do combo Proprietário no formulário `guias`. |
| `Guia_Rolaria` | Lookup de `Rolaria` — RowSource do combo Rolaria |
| `Guias_codigo_barras` | Lookup de `CodigosBarras` — RowSource do combo Código de Barras |
| **`Relatorio`** | **A query central.** Junta `guias` a `destinatarios`, `proprietarios`, `Rolaria`, `CodigosBarras` (todos LEFT JOIN) e depois faz produto cartesiano (`, Vias`) — cada guia gera 4 linhas, uma por via. É a `RecordSource` do relatório `Nova_Guia`. |

**Conclusão prática:** o modelo de dados "oficial" a replicar tem só 6 tabelas; as queries mortas (`Cópia de Consulta1`, `destinatarios_para_guias`, `Guia_Propriedades`, `Guia_Propriedades_novo`) não devem ser copiadas para o novo sistema — são lixo de desenvolvimento.

---

## 4. Formulários

| Formulário | Função | Campos editáveis | Validação (VBA) |
|---|---|---|---|
| `FrmMenu` | Menu principal | — | Botões: Nova Guia, Destinatários, Proprietários, Rolarias, Pesquisar Guias, Sair |
| `guias` | Criar guia | IDDestinatario (combo), IDProprietario (combo), IDCodigo (combo, "Código de barras"), IDRolaria (combo, "Tipo de rolaria"), Fornecedor (texto livre) | **Nenhuma validação de campos obrigatórios** — só verifica se `IDGuia` não é nulo antes de imprimir (é sempre preenchido automaticamente) |
| `Novo_Proprietário` | Criar/editar proprietário | Nome, Distrito, Concelho, Freguesia (Parcela e CodigoProp não estão neste formulário — só existem via edição direta da tabela) | Nome, Distrito, Concelho, Freguesia obrigatórios |
| `Novo_Destinatario` | Criar/editar destinatário | Nome, NIF, Morada, Concelho | Nome, NIF, Morada, Concelho obrigatórios |
| `Rolaria` | Criar/editar tipo de rolaria | Tipo | Tipo obrigatório |
| `Pesquisar_Guia` | Pesquisar/reimprimir guia existente | Combo com lista de todos os `IDGuia` | Obriga seleção antes de pesquisar |

### Fluxo de navegação

```
FrmMenu
 ├─ Nova Guia ──────────► guias (modo adicionar) ──[Imprimir]──► Nova_Guia (preview, filtrado por IDGuia)
 ├─ Destinatários ──────► Novo_Destinatario (navegação livre pelos registos existentes)
 ├─ Proprietários ──────► Novo_Proprietário (idem)
 ├─ Rolarias ───────────► Rolaria (idem)
 └─ Pesquisar Guias ────► Pesquisar_Guia ──[Pesquisar]──► Nova_Guia (preview, filtrado por IDGuia)
```

**Nota de qualidade:** `Pesquisar_Guia` tem código VBA morto (`Comanddo30_Click`, não ligado a nenhum controlo existente — resíduo de um botão renomeado). O botão ativo real é `Pesquisar_Click`.

---

## 5. Relatório `Nova_Guia` — especificação pixel-a-pixel

Este é o artefacto que **tem de sair idêntico** no software novo.

### 5.1 Estrutura

- `RecordSource = "Relatorio"`, filtrado em runtime por `Q.IDGuia = <id>` (chamado a partir de `guias.btnImprimir` ou `Pesquisar_Guia.Pesquisar`).
- Sem cabeçalho/rodapé de relatório nem de página (alturas = 0).
- **Um único "Group Header" agrupado por `idVia`, com `ForceNewPage = True`** — cada uma das 4 linhas (via) do recordset gera uma página nova. Isto é o mecanismo real das "4 vias"; a secção Detalhe está vazia.
- Página em formato A5 (largura de relatório 7620 twips ≈ 13,4 cm — confirmar orientação exata em Access, mas a proporção é A5).

### 5.2 Conteúdo de cada página (campos ligados aos dados)

| Campo no relatório | Origem (query `Relatorio`) | Rótulo mostrado |
|---|---|---|
| NumeroGuia | `IDGuia` | (nº da guia, canto superior) |
| NIF | `NIF` (destinatarios) | "Contribuinte n.º:" |
| Morada (x2 controlos) | `Morada` (destinatarios) | "Morada:" |
| Nome_destinatarios1 | `Nome` de destinatarios | "Destinatário:" |
| Distrito | `Distrito` (proprietarios) | "Distrito:" |
| Concelho_proprietarios | `Concelho` (proprietarios) | "Concelho:" |
| Freguesia (x2 controlos) | `Freguesia` (proprietarios) | "Freguesia:" / "Carregamento:" |
| Nome | `Nome` (proprietarios) | "Nome:" |
| Fornecedor | `Fornecedor` (guias) | "Entregue pelo Fornecedor:" |
| Tipo | `Tipo` (Rolaria) | "Rolaria:" |
| Número da UGF | `CodigosBarras.[Número da UGF]` | "ORIGEM DA MADEIRA:" |
| **Número do Certificado** | `CodigosBarras.[Número do Certificado]` | **"Nome da Propriedade:" ⚠ rótulo incoerente com o dado (ver 7.1)** |
| Código de Barras (texto) | `="*" & [CodigoBarras] & "*"` | Fonte **"Libre Barcode 39"**, tamanho 30 — o "código de barras" é texto normal envolvido em `*...*` renderizado com uma fonte Code-39, não uma imagem gerada |
| CodigoBarras | `CodigosBarras.[Código de Barras]` | Texto simples por baixo do barcode visual |
| NomeVia / IDVia | `Vias.NomeVia` / `idVia` | Título "GUIA PARA ENTRADA DE ROLARIA -" + nome da via |

### 5.3 Textos fixos (não ligados a dados — hardcoded no design)

- "ESTA GUIA NÃO PODE SER UTILIZADA COMO DOCUMENTO DE TRANSPORTE"
- "______ de __________________ de 20_______" (linha de data manual)
- " MADEIRA CERTIFICADA FSC 100%"
- " CERTIFICADO: APCER-COC-150294-AP"
- "Cerna Portugal /"
- "Contrato Nº: 59573.1"
- "N.º Guia Remessa/Cod AT: _______________________________________________________"
- "Matrícula do Carro: _______ - _______ - _______     Reboque: ___________________"
- "Quantidade: __________________________" + caixas "Toneladas" / "M3" / "C/ Casca" / "S/ Casca"
- Bloco legal completo (letras A–F) sobre não-proveniência de madeira ilegal, conforme critérios FSC/OIT
- "N.º Compra: ________" (só visível nas cópias, ver 5.4)

### 5.4 Lógica condicional por via (VBA, evento `CabeçalhoDoGrupo0_Format`)

| Via (idVia) | Cor de fundo do cabeçalho | Cor do texto "NomeVia" | Código de barras visível | "N.º Compra: ____" visível |
|---|---|---|---|---|
| 1 — ORIGINAL | RGB(123,123,123) cinza | Branco | **Sim** | Não |
| 2 — DUPLICADO | RGB(255,221,255) rosa claro | Preto | Não | **Sim** |
| 3 — TRIPLICADO | RGB(209,255,255) azul claro | Preto | Não | **Sim** |
| 4 — QUADRIPLICADO | RGB(230,223,235) lilás claro | Preto | Não | **Sim** |

O código de barras (imagem/fonte + texto) só aparece na via Original; nas cópias esse espaço é substituído por uma linha em branco "N.º Compra: ________" para preenchimento manual. **Isto é diferente do que o scaffold .NET atual implementou** (que mostra um "carimbo CÓPIA NÃO DOCUMENTAL" inexistente no original, e cores diferentes das reais).

### 5.5 Imagens

- `Imagem134` ("Design sem nome (1)") — 7533×7775 twips, cobre praticamente a página inteira; provável arte de fundo/marca de água.
- `Imagem128` ("2_logo_cores-small-01") — 3056×1596 twips; logótipo da empresa.

Estas imagens estão embutidas na BD Access como "shared images" e **não são extraíveis por SQL/DAO simples** — terão de ser exportadas manualmente a partir do Access (vista de design → clicar na imagem → guardar/copiar) para ficheiros PNG a incluir nos recursos do novo software.

---

## 6. Lógica de negócio (VBA completo)

Todo o VBA da aplicação (não há módulos standalone nem macros complexas — só 2 macros de navegação embutidas):

- **`guias.btnImprimir_Click`** — valida `IDGuia` não nulo, grava o registo (`Me.Dirty = False`), abre `Nova_Guia` em pré-visualização filtrado por `Q.IDGuia = Me.IDGuia`, maximiza a janela. Trata erro 2501 (cancelar impressão) silenciosamente.
- **`Novo_Proprietário.btn_save_propri_Click`** — valida Nome/Distrito/Concelho/Freguesia obrigatórios, grava, mostra "sucesso", abre novo registo em branco.
- **`Novo_Destinatario.Comando22_Click`** — valida Nome/NIF/Morada/Concelho obrigatórios, grava, mostra "sucesso", abre novo registo em branco.
- **`Rolaria.btn_save_rolaria_Click`** — valida Tipo obrigatório, mostra "sucesso" (nota: **não chama `RunCommand acCmdSaveRecord`** como os outros dois — possível bug de gravação inconsistente, a confirmar).
- **`Pesquisar_Guia.Pesquisar_Click`** (+ `Comanddo30_Click`, morto) — valida seleção, abre `Nova_Guia` filtrado por `IDGuia`.
- **`FrmMenu`** — 4 botões abrem formulários (`Novo_Proprietário` via macro embutida; `Novo_Destinatario`, `guias` em modo adicionar, `Pesquisar_Guia` via VBA/Event Procedure).
- **`Nova_Guia.CabeçalhoDoGrupo0_Format`** — lógica de cor/visibilidade por via descrita em 5.4.

Não há triggers, macros de dados, nem VBA nos módulos standalone — **toda a lógica de negócio real está nestas ~8 sub-rotinas.** Isto é uma vantagem: a superfície a replicar é pequena e bem definida.

---

## 7. Inconsistências e dívida técnica na BD atual

Registadas para decisão consciente — replicar fielmente ou corrigir no novo sistema:

1. **Rótulo "Nome da Propriedade:" ligado ao campo `Número do Certificado`** no relatório (secção 5.2) — parece um bug de etiquetagem esquecido no design; não existe hoje um campo "Nome da Propriedade" nem em `proprietarios` nem em lado nenhum, apesar de duas queries mortas (`Guia_Propriedades*`) sugerirem que um dia existiu.
2. **Sem validação de campos obrigatórios ao criar uma Guia** — é possível gravar uma guia sem Destinatário, Proprietário, Código de Barras ou Rolaria (visível nos próprios dados de exemplo: `IDGuia=1` tem `IDDestinatario` e `IDCodigo` em branco). Formulários de Proprietário/Destinatário/Rolaria já validam; o de Guias não.
3. **`Rolaria.btn_save_rolaria_Click` não confirma a gravação** (falta `DoCmd.RunCommand acCmdSaveRecord`) — possível perda de dados se o utilizador não sair do campo antes de fechar.
4. **Dados de teste/sujidade** presentes: proprietário "fra" com campos "a"/"a"/"a", Rolaria "Tipo=23", `IDGuia=2` com `Fornecedor` vazio, NIFs armazenados como `Long Integer` (perdem zeros à esquerda se algum NIF começar por 0).
5. **`Fornecedor` é texto livre** em vez de lookup — já se vê inconsistência nos dados ("Sombras da Natureza. Lda" vs "Francisco. Lda", pontuação irregular). Candidato natural a tabela de lookup no novo sistema, mantendo a possibilidade de texto livre para não bloquear o utilizador.
6. **4 queries mortas** (`Cópia de Consulta1`, `destinatarios_para_guias`, `Guia_Propriedades`, `Guia_Propriedades_novo`) e **1 handler VBA morto** (`Comanddo30_Click`) — não devem ser portados.

---

## 8. O scaffold .NET existente — não está alinhado

`src/GuiasMadeira.*` (Domain, Infrastructure, Pdf, Desktop) foi criado antes desta análise, presumivelmente por suposição, e diverge da BD real em três pontos fundamentais:

1. **Schema inventado**: `GuiasMadeira.Domain.Entities.Guia/Proprietario/Destinatario/Rolaria` têm campos que não existem (`Numero`, `DataEmissao`, `VolumeM3`, `Observacoes`, `Telefone`, `Email`, `LocalEntrega`, `Contacto`, `CodigoProp`... trocado, `TipoMadeira`, `Unidade`) e falta tudo o que existe de facto (`NIF`, `Distrito`, `Concelho`, `Freguesia`, `CodigoProp`, `Parcela`, `Fornecedor`, tabela `CodigosBarras`, tabela `Vias`).
2. **PDF reinventado**: `GuiaPdfGenerator` gera um layout genérico com cores erradas (verde no Original em vez de cinza; sem os textos de certificação/legais; "carimbo CÓPIA NÃO DOCUMENTAL" que não existe no original; sem lógica de barcode-por-fonte).
3. **SQLite local** — funciona para protótipo mas colide com o requisito de "aceder de qualquer local" via servidor (ver secção 9).

**Recomendação:** manter a estrutura de projetos (`Domain` / `Infrastructure` / `Pdf` / `Desktop`) como esqueleto arquitetural válido, mas **substituir por completo** o modelo de domínio, o schema e o gerador de relatório pelos especificados neste documento.

---

## 9. Decisões de arquitetura (validadas com o cliente em 2026-07-27)

| Decisão | Escolha | Nota |
|---|---|---|
| **Motor de base de dados servidor** | **PostgreSQL** | Substitui o SQLite do scaffold atual; suporta acesso concorrente multi-posto |
| **Modo de acesso ao servidor** | **Ligação direta à BD via VPN** | A empresa já usa VPN para outros serviços — não é necessária uma API REST intermédia |
| **Corrigir bugs identificados na secção 7** | **Corrigir silenciosamente** | O relatório mantém-se visualmente idêntico ao Access; a lógica por trás (validações, rótulo "Nome da Propriedade", gravação da Rolaria) fica corrigida sem alterar o que o utilizador vê |
| **Extração de imagens** | Exportação manual a partir do Access | As duas imagens do relatório (logótipo + arte de fundo) têm de ser guardadas manualmente como PNG — não há forma direta via SQL/DAO |
| **Fonte de código de barras** | A confirmar durante implementação | Reutilizar fonte "Libre Barcode 39" (mais simples) vs. gerar imagem Code-39 real (mais robusto para impressão) |

---

## 10. Ficheiros de referência gerados nesta análise

Todos em `%TEMP%\...\scratchpad\access_export\`:
- `tables_schema.txt`, `relationships.txt`, `queries.txt`, `data_samples.txt` — schema e dados completos
- `reports\Nova_Guia_clean.txt` — definição completa do relatório (layout + VBA)
- `forms\*_clean.txt` — definição completa de cada formulário (layout + VBA)

Estes ficheiros são temporários; recomenda-se copiar os relevantes para o repositório (ex. `docs/access-export/`) antes de terminarem a sessão, caso se queiram consultar detalhes de posicionamento (Left/Top/Width/Height em twips) durante a implementação do relatório.
