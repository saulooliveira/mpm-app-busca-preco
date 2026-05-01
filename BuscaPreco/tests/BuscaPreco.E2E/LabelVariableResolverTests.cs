using System;
using System.Collections.Generic;
using Xunit;
using BuscaPreco.Infrastructure.Services;

namespace BuscaPreco.E2E;

public class LabelVariableResolverTests
{
    private readonly LabelVariableResolver _sut = new();

    [Fact]
    public void Deve_SubstituirVariavelSimples_Quando_ChaveExiste()
    {
        var vars   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Nome"] = "Produto X" };
        var result = _sut.Resolver("Olá {Nome}", vars);
        Assert.Equal("Olá Produto X", result);
    }

    [Fact]
    public void Deve_SubstituirMultiplasVariaveis_Quando_TodasExistem()
    {
        var vars = LabelVariableResolver.CriarVariaveis("Leite", "R$ 5,00", "789", "UN", "Empresa");
        var result = _sut.Resolver("{DescricaoProduto} — {PrecoVenda}", vars);
        Assert.Equal("Leite — R$ 5,00", result);
    }

    [Fact]
    public void Deve_ManterToken_Quando_ChaveNaoExiste()
    {
        var vars   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var result = _sut.Resolver("{Desconhecido}", vars);
        Assert.Equal("{Desconhecido}", result);
    }

    [Fact]
    public void Deve_ResolverCaseInsensitivo_Quando_CasosDiferentes()
    {
        var vars   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["PrecoVenda"] = "9,90" };
        var result = _sut.Resolver("{precovenda}", vars);
        Assert.Equal("9,90", result);
    }

    [Fact]
    public void Deve_RetornarVazio_Quando_TemplateVazio()
    {
        var vars   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var result = _sut.Resolver(string.Empty, vars);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Deve_ResolverFormula_Quando_TemplateContemUrl()
    {
        var vars   = LabelVariableResolver.CriarVariaveis("Biscoito", "2,99", "001");
        var result = _sut.Resolver("www.google.com.br/{DescricaoProduto}", vars);
        Assert.Equal("www.google.com.br/Biscoito", result);
    }

    [Fact]
    public void CriarVariaveis_Deve_PopularDataAtual()
    {
        var vars = LabelVariableResolver.CriarVariaveis("X", "1,00", "123");
        Assert.Equal(DateTime.Today.ToString("dd/MM/yyyy"), vars["DataAtual"]);
    }

    [Fact]
    public void CriarVariaveis_Deve_MappearCodigoBarrasECodigoProduto()
    {
        var vars = LabelVariableResolver.CriarVariaveis("X", "1,00", "7891000315507");
        Assert.Equal("7891000315507", vars["CodigoBarras"]);
        Assert.Equal("7891000315507", vars["CodigoProduto"]);
    }
}
