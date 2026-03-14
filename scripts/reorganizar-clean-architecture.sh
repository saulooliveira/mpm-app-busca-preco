#!/usr/bin/env bash
set -euo pipefail

# Executar na raiz da solução

mkdir -p BuscaPreco/src/{Domain/{Entities,Interfaces},Application/{Services,Interfaces},Infrastructure/{Data,Repositories,HttpClients,Scrapers},CrossCutting,Presentation/WindowsForms}

mv BuscaPreco/Produto.cs BuscaPreco/src/Domain/Entities/Produto.cs
mv BuscaPreco/Configuracoes.cs BuscaPreco/src/Domain/Entities/Configuracoes.cs
mv BuscaPreco/DBConfig.cs BuscaPreco/src/Infrastructure/Data/DBConfig.cs
mv BuscaPreco/DbfConnection.cs BuscaPreco/src/Infrastructure/Data/DbfConnection.cs
mv BuscaPreco/Servidor.cs BuscaPreco/src/Infrastructure/Scrapers/Servidor.cs
mv BuscaPreco/Terminal.cs BuscaPreco/src/Infrastructure/Scrapers/Terminal.cs
mv BuscaPreco/Log.cs BuscaPreco/src/CrossCutting/Logger.cs
mv BuscaPreco/Exportador.cs BuscaPreco/src/Infrastructure/Repositories/Exportador.cs
mv BuscaPreco/Form1.cs BuscaPreco/src/Presentation/WindowsForms/Form1.cs
mv BuscaPreco/Form1.Designer.cs BuscaPreco/src/Presentation/WindowsForms/Form1.Designer.cs
mv BuscaPreco/Form1.resx BuscaPreco/src/Presentation/WindowsForms/Form1.resx

# Atualizar namespaces e Includes no .csproj conforme a nova estrutura.
# Atualizar Program.cs para composição das dependências.
