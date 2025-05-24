# SagaPedidos

SagaPedidos � um sistema de gerenciamento de pedidos que implementa o padr�o Saga para manter a consist�ncia em transa��es distribu�das. Este projeto demonstra uma implementa��o pr�tica de um sistema event-driven para processamento de pedidos, pagamentos e envios.

## ?? Sum�rio

- [Vis�o Geral](#-vis�o-geral)
- [Arquitetura](#-arquitetura)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Fluxo de Processamento](#-fluxo-de-processamento)
- [Padr�o Saga](#-padr�o-saga)
- [Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [Como Executar](#-como-executar)
- [APIs](#-apis)
- [Mensageria](#-mensageria)
- [Tratamento de Falhas](#-tratamento-de-falhas)

## ?? Vis�o Geral

SagaPedidos � um sistema de e-commerce que gerencia todo o ciclo de vida de um pedido, desde sua cria��o at� a entrega ao cliente. O sistema � constru�do usando uma arquitetura orientada a eventos com o padr�o de orquestra��o Saga para coordenar transa��es distribu�das entre os diferentes servi�os:

- Servi�o de Pedidos
- Servi�o de Pagamentos
- Servi�o de Envios

A comunica��o entre estes componentes � realizada atrav�s de mensageria com RabbitMQ, garantindo opera��es ass�ncronas e desacoplamento entre os servi�os.

## ?? Arquitetura

O projeto segue uma arquitetura limpa (Clean Architecture) com separa��o clara de responsabilidades:

- **Dom�nio**: Cont�m as entidades de neg�cio e regras de dom�nio
- **Aplica��o**: Implementa casos de uso e orquestra��o de servi�os
- **Infraestrutura**: Fornece implementa��es concretas para persist�ncia e mensageria
- **Apresenta��o**: Exp�e APIs RESTful e configura a aplica��o

A comunica��o entre servi�os � realizada usando eventos atrav�s do RabbitMQ, com um exchange do tipo Fanout para distribuir as mensagens para as filas apropriadas.

## ?? Estrutura do Projeto

O sistema � dividido em quatro projetos principais:

### SagaPedidos.Domain

Cont�m as entidades, eventos e interfaces do core do neg�cio:

- **Entities**: Pedido, Pagamento, Envio, Cliente, ItemPedido, etc.
- **Events**: Eventos de dom�nio como PedidoCriado, PagamentoAprovado, etc.
- **Interfaces**: Contratos para reposit�rios e servi�os de infraestrutura

### SagaPedidos.Application

Implementa a l�gica de aplica��o e coordena os fluxos de neg�cio:

- **Services**: Implementa��o dos servi�os de aplica��o
- **DTOs**: Objetos de transfer�ncia de dados
- **Sagas**: Orquestrador de sagas que coordena o fluxo de transa��es
- **EventHandlers**: Manipuladores de eventos de dom�nio

### SagaPedidos.Infra

Fornece implementa��es concretas para persist�ncia e comunica��o:

- **Repositories**: Implementa��o dos reposit�rios para acesso a dados
- **Messaging**: Implementa��o do Publisher e Subscribers usando RabbitMQ
- **DbContext**: Configura��o do Entity Framework Core

### SagaPedidos.Presentation

Configura a aplica��o e exp�e interfaces externas:

- **Controllers**: Controladores REST para intera��o com o sistema
- **Program.cs**: Configura��o e inicializa��o da aplica��o

## ?? Fluxo de Processamento

O fluxo t�pico de processamento de um pedido segue estas etapas:

1. **Cria��o do Pedido**:
   - Um pedido � criado atrav�s da API
   - O evento `PedidoCriado` � publicado

2. **Processamento de Pagamento**:
   - O orquestrador inicia o processamento de pagamento
   - O evento `ProcessarPagamento` � enviado para o servi�o de pagamento
   - O pagamento � processado e um evento `PagamentoAprovado` ou `PagamentoRecusado` � publicado

3. **Processamento de Envio** (se o pagamento for aprovado):
   - O orquestrador solicita o processamento do envio
   - O evento `ProcessarEnvio` � enviado para o servi�o de envio
   - O envio � processado e um evento `EnvioProcessado` ou `EnvioFalhado` � publicado

4. **Conclus�o ou Compensa��o**:
   - Em caso de sucesso, o pedido � finalizado
   - Em caso de falha em qualquer etapa, a��es de compensa��o s�o executadas para garantir consist�ncia

## ?? Padr�o Saga

O sistema implementa o padr�o Saga com orquestra��o para garantir a consist�ncia em transa��es distribu�das. O `PedidoSagaOrchestrator` coordena todo o fluxo da transa��o e implementa mecanismos de compensa��o para garantir que o sistema permane�a em um estado consistente mesmo em caso de falhas.

### Compensa��es Implementadas:

- **Falha no Pagamento**: Cancela o pedido
- **Falha no Envio**: Estorna o pagamento e cancela o pedido

## ?? Tecnologias Utilizadas

- **.NET 8**: Framework de desenvolvimento
- **Entity Framework Core**: ORM para acesso a dados
- **RabbitMQ**: Sistema de mensageria para comunica��o ass�ncrona
- **SQL Server**: Banco de dados relacional
- **Arquitetura Limpa**: Padr�o arquitetural

## ?? Como Executar

### Pr�-requisitos:

- .NET 8 SDK
- SQL Server
- RabbitMQ

### Passos para Execu��o:

1. **Clone o Reposit�rio**:
   ```bash
   git clone [url-do-repositorio]
   cd SagaPedidos
   ```

2. **Configure o Banco de Dados**:
   - Atualize a string de conex�o no arquivo `appsettings.json`
   - Execute as migra��es:
     ```bash
     dotnet ef database update --project SagaPedidos.Infra --startup-project SagaPedidos.Presentation
     ```

3. **Configure o RabbitMQ**:
   - Atualize as configura��es do RabbitMQ no arquivo `appsettings.json`

4. **Execute a Aplica��o**:
   ```bash
   dotnet run --project SagaPedidos.Presentation
   ```

## ?? APIs

O sistema exp�e as seguintes APIs RESTful:

### API de Pedido

- **POST /api/pedidos**: Cria um novo pedido
- **GET /api/pedidos/{id}**: Consulta um pedido espec�fico
- **PUT /api/pedidos/{id}/cancelar**: Cancela um pedido

### API de Pagamento

- **POST /api/pagamentos**: Processa um pagamento
- **POST /api/pagamentos/{id}/estornar**: Estorna um pagamento

### API de Envio

- **POST /api/envios**: Processa um envio
- **GET /api/envios/{id}**: Consulta o status de um envio

## ?? Mensageria

O sistema utiliza RabbitMQ com um exchange do tipo Fanout para distribuir mensagens para as filas apropriadas:

- **Exchange**: `pedido_exchange` (tipo Fanout)
- **Filas**:
  - `pedido_queue`: Processa eventos relacionados a pedidos
  - `pagamento_queue`: Processa eventos relacionados a pagamentos
  - `envio_queue`: Processa eventos relacionados a envios

## ?? Tratamento de Falhas

O sistema implementa estrat�gias robustas para lidar com falhas em diferentes etapas:

1. **Falha na Cria��o do Pedido**: O pedido n�o � criado e uma mensagem de erro � retornada
2. **Falha no Pagamento**: O pedido � cancelado automaticamente
3. **Falha no Envio**: O pagamento � estornado e o pedido � cancelado

Todas as opera��es s�o registradas no console para facilitar o diagn�stico de problemas.

---

Este projeto demonstra uma implementa��o pr�tica do padr�o Saga para manter a consist�ncia em sistemas distribu�dos, utilizando mensageria e arquitetura orientada a eventos.