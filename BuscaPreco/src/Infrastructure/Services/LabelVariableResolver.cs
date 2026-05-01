using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BuscaPreco.Infrastructure.Services;

public class LabelVariableResolver
{
    private static readonly Regex VarPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Replaces all {VariableName} tokens in <paramref name="template"/> with values
    /// from <paramref name="variaveis"/>. Unmatched tokens are left unchanged.
    /// Lookup is case-insensitive (dictionary must use OrdinalIgnoreCase).
    /// </summary>
    public string Resolver(string template, Dictionary<string, string> variaveis)
    {
        if (string.IsNullOrEmpty(template)) return template;
        return VarPattern.Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            return variaveis.TryGetValue(key, out var val) ? val : m.Value;
        });
    }

    /// <summary>
    /// Builds a variable dictionary from product data. All keys are case-insensitive.
    /// </summary>
    public static Dictionary<string, string> CriarVariaveis(
        string descricao,
        string preco,
        string codigo,
        string unidade = "",
        string nomeEmpresa = "")
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DescricaoProduto"] = descricao,
            ["PrecoVenda"]       = preco,
            ["CodigoProduto"]    = codigo,
            ["CodigoBarras"]     = codigo,
            ["Unidade"]          = unidade,
            ["NomeEmpresa"]      = nomeEmpresa,
            ["DataValidade"]     = string.Empty,
            ["DataAtual"]        = DateTime.Today.ToString("dd/MM/yyyy"),
        };
    }
}
