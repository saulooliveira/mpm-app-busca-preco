using System.Collections.Generic;
using BuscaPreco.Domain.Entities;
using BuscaPreco.Domain.Interfaces;
using BuscaPreco.Infrastructure.Database;

namespace BuscaPreco.Infrastructure.Repositories
{
    public class ProdutoRepository : IProdutoRepository
    {
        private readonly DbfDatabase _dbfDatabase;

        public ProdutoRepository(DbfDatabase dbfDatabase)
        {
            _dbfDatabase = dbfDatabase;
        }

        public (string des, decimal vlrVenda1) BuscarPorCodigo(string codigo)
        {
            return _dbfDatabase.BuscarPorCodigo(codigo);
        }

        public List<Produto> ListarTudo()
        {
            return _dbfDatabase.ListarTudo();
        }
    }
}
