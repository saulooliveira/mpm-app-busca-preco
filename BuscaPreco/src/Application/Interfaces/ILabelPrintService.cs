using System.Collections.Generic;
using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Application.Interfaces;

public interface ILabelPrintService
{
    void Imprimir(string nomePrinter, EtiquetaLayout layout,
        Dictionary<string, string> variaveis, int copias = 1);
}
