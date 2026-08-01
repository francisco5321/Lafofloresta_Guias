using GuiasMadeira.Domain.Entities;
using GuiasMadeira.Infrastructure.Postgres;

namespace GuiasMadeira.IntegrationTests;

public class GuiaRepositoryTests : RepositoryTestBase
{
    private readonly GuiaRepository repository;
    private readonly CodigoBarraRepository codigosBarras;

    public GuiaRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
        repository = new GuiaRepository(fixture.ConnectionFactory);
        codigosBarras = new CodigoBarraRepository(fixture.ConnectionFactory);
    }

    [Fact]
    public async Task InsertAsync_CriaGuiaSemAtribuirVinheta()
    {
        var id = await repository.InsertAsync(new Guia { Fornecedor = "Fornecedor X" });

        var impressao = await repository.GetGuiaImpressaoAsync(id);
        Assert.NotNull(impressao);
        Assert.Equal("Fornecedor X", impressao!.Fornecedor);
    }

    [Fact]
    public async Task InsertComCertificadoAsync_AtribuiUmaVinhetaDisponivelDoCertificado()
    {
        await codigosBarras.InsertManyAsync([new CodigoBarra { Codigo = "111", NumeroCertificado = "CERT-1" }]);

        var id = await repository.InsertComCertificadoAsync(new Guia { Fornecedor = "F" }, "CERT-1");

        Assert.NotNull(id);
        var impressao = await repository.GetGuiaImpressaoAsync(id!.Value);
        Assert.Equal("111", impressao!.CodigoBarraCodigo);
    }

    [Fact]
    public async Task InsertComCertificadoAsync_DevolveNull_QuandoNaoHaVinhetaDisponivel()
    {
        var id = await repository.InsertComCertificadoAsync(new Guia { Fornecedor = "F" }, "CERT-INEXISTENTE");

        Assert.Null(id);
        Assert.Empty(await repository.ListAllIdsAsync());
    }

    [Fact]
    public async Task InsertComCertificadoAsync_NaoAtribuiDuasVezesAMesmaVinheta_MesmoComPedidosConcorrentes()
    {
        // Um único certificado com 3 vinhetas: disparar 6 pedidos concorrentes só pode resultar
        // em 3 sucessos (FOR UPDATE SKIP LOCKED em GuiaRepository garante a exclusão mútua).
        await codigosBarras.InsertManyAsync(
        [
            new CodigoBarra { Codigo = "V1", NumeroCertificado = "CERT-CONCORRENCIA" },
            new CodigoBarra { Codigo = "V2", NumeroCertificado = "CERT-CONCORRENCIA" },
            new CodigoBarra { Codigo = "V3", NumeroCertificado = "CERT-CONCORRENCIA" },
        ]);

        var tarefas = Enumerable.Range(0, 6)
            .Select(_ => repository.InsertComCertificadoAsync(new Guia { Fornecedor = "Concorrente" }, "CERT-CONCORRENCIA"))
            .ToArray();
        var resultados = await Task.WhenAll(tarefas);

        var sucesso = resultados.Where(id => id is not null).ToList();
        Assert.Equal(3, sucesso.Count);
        Assert.Equal(3, sucesso.Distinct().Count());

        var guiasCriadas = await repository.ListAllIdsAsync();
        Assert.Equal(3, guiasCriadas.Count);
    }

    [Fact]
    public async Task AtualizarComCertificadoAsync_ExcluiAProipriaGuiaDaVerificacaoDeDisponibilidade()
    {
        await codigosBarras.InsertManyAsync([new CodigoBarra { Codigo = "111", NumeroCertificado = "CERT-1" }]);
        var id = (await repository.InsertComCertificadoAsync(new Guia { Fornecedor = "F" }, "CERT-1"))!.Value;

        // Reatribuir a guia ao mesmo certificado deve continuar a funcionar (a vinheta já usada
        // por esta guia não deve ser tratada como "ocupada por outra guia").
        var atualizado = await repository.AtualizarComCertificadoAsync(
            new Guia { Id = id, Fornecedor = "F Atualizado" }, "CERT-1");

        Assert.True(atualizado);
        var impressao = await repository.GetGuiaImpressaoAsync(id);
        Assert.Equal("F Atualizado", impressao!.Fornecedor);
    }

    [Fact]
    public async Task AtualizarComCertificadoAsync_DevolveFalse_QuandoCertificadoNaoTemVinhetaLivre()
    {
        var id = await repository.InsertAsync(new Guia { Fornecedor = "F" });

        var atualizado = await repository.AtualizarComCertificadoAsync(
            new Guia { Id = id, Fornecedor = "F" }, "CERT-SEM-VINHETAS");

        Assert.False(atualizado);
    }

    [Fact]
    public async Task DeleteAsync_RemoveAGuia()
    {
        var id = await repository.InsertAsync(new Guia { Fornecedor = "F" });

        await repository.DeleteAsync(id);

        Assert.Empty(await repository.ListAllIdsAsync());
    }

    [Fact]
    public async Task ListResumoAsync_DevolveNomesResolvidosDasEntidadesRelacionadas()
    {
        var destinatarioId = await new DestinatarioRepository(Fixture.ConnectionFactory)
            .InsertAsync(new Destinatario { Nome = "Destinatário X" });
        var proprietarioId = await new ProprietarioRepository(Fixture.ConnectionFactory)
            .InsertAsync(new Proprietario { Nome = "Proprietário Y" });

        await repository.InsertAsync(new Guia
        {
            DestinatarioId = destinatarioId,
            ProprietarioId = proprietarioId,
            Fornecedor = "Fornecedor Z"
        });

        var resumo = Assert.Single(await repository.ListResumoAsync());
        Assert.Equal("Destinatário X", resumo.DestinatarioNome);
        Assert.Equal("Proprietário Y", resumo.ProprietarioNome);
        Assert.Equal("Fornecedor Z", resumo.Fornecedor);
    }

    [Fact]
    public async Task GetGuiaImpressaoAsync_DevolveNull_QuandoGuiaNaoExiste()
    {
        Assert.Null(await repository.GetGuiaImpressaoAsync(999_999));
    }
}
