using BuscaPreco.Domain.Entities;

namespace BuscaPreco.Application.Interfaces;

public interface IEtiquetaLayoutRepository
{
    EtiquetaLayout? Carregar(string id = "default");
    void Salvar(EtiquetaLayout layout);
    EtiquetaLayout ObterOuCriarPadrao();
}
