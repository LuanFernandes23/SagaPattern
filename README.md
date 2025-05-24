# SagaPedidos

SagaPedidos é um sistema de gerenciamento de pedidos que implementa o padrão Saga para manter a consistência em transações distribuídas. Este projeto demonstra uma implementação prática de um sistema event-driven para processamento de pedidos, pagamentos e envios.

## ?? Sumário

- [Visão Geral](#-visão-geral)
- [Arquitetura](#-arquitetura)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Fluxo de Processamento](#-fluxo-de-processamento)
- [Padrão Saga](#-padrão-saga)
- [Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [Como Executar](#-como-executar)
- [APIs](#-apis)
- [Mensageria](#-mensageria)
- [Tratamento de Falhas](#-tratamento-de-falhas)

## ?? Visão Geral

SagaPedidos é um sistema de e-commerce que gerencia todo o ciclo de vida de um pedido, desde sua criação até a entrega ao cliente. O sistema é construído usando uma arquitetura orientada a eventos com o padrão de orquestração Saga para coordenar transações distribuídas entre os diferentes serviços:

- Serviço de Pedidos
- Serviço de Pagamentos
- Serviço de Envios

A comunicação entre estes componentes é realizada através de mensageria com RabbitMQ, garantindo operações assíncronas e desacoplamento entre os serviços.

## ?? Arquitetura

O projeto segue uma arquitetura limpa (Clean Architecture) com separação clara de responsabilidades:

- **Domínio**: Contém as entidades de negócio e regras de domínio
- **Aplicação**: Implementa casos de uso e orquestração de serviços
- **Infraestrutura**: Fornece implementações concretas para persistência e mensageria
- **Apresentação**: Expõe APIs RESTful e configura a aplicação

A comunicação entre serviços é realizada usando eventos através do RabbitMQ, com um exchange do tipo Fanout para distribuir as mensagens para as filas apropriadas.

## ?? Estrutura do Projeto

O sistema é dividido em quatro projetos principais:

### SagaPedidos.Domain

Contém as entidades, eventos e interfaces do core do negócio:

- **Entities**: Pedido, Pagamento, Envio, Cliente, ItemPedido, etc.
- **Events**: Eventos de domínio como PedidoCriado, PagamentoAprovado, etc.
- **Interfaces**: Contratos para repositórios e serviços de infraestrutura

### SagaPedidos.Application

Implementa a lógica de aplicação e coordena os fluxos de negócio:

- **Services**: Implementação dos serviços de aplicação
- **DTOs**: Objetos de transferência de dados
- **Sagas**: Orquestrador de sagas que coordena o fluxo de transações
- **EventHandlers**: Manipuladores de eventos de domínio

### SagaPedidos.Infra

Fornece implementações concretas para persistência e comunicação:

- **Repositories**: Implementação dos repositórios para acesso a dados
- **Messaging**: Implementação do Publisher e Subscribers usando RabbitMQ
- **DbContext**: Configuração do Entity Framework Core

### SagaPedidos.Presentation

Configura a aplicação e expõe interfaces externas:

- **Controllers**: Controladores REST para interação com o sistema
- **Program.cs**: Configuração e inicialização da aplicação

## ?? Fluxo de Processamento

O fluxo típico de processamento de um pedido segue estas etapas:

1. **Criação do Pedido**:
   - Um pedido é criado através da API
   - O evento `PedidoCriado` é publicado

2. **Processamento de Pagamento**:
   - O orquestrador inicia o processamento de pagamento
   - O evento `ProcessarPagamento` é enviado para o serviço de pagamento
   - O pagamento é processado e um evento `PagamentoAprovado` ou `PagamentoRecusado` é publicado

3. **Processamento de Envio** (se o pagamento for aprovado):
   - O orquestrador solicita o processamento do envio
   - O evento `ProcessarEnvio` é enviado para o serviço de envio
   - O envio é processado e um evento `EnvioProcessado` ou `EnvioFalhado` é publicado

4. **Conclusão ou Compensação**:
   - Em caso de sucesso, o pedido é finalizado
   - Em caso de falha em qualquer etapa, ações de compensação são executadas para garantir consistência

## ?? Padrão Saga

O sistema implementa o padrão Saga com orquestração para garantir a consistência em transações distribuídas. O `PedidoSagaOrchestrator` coordena todo o fluxo da transação e implementa mecanismos de compensação para garantir que o sistema permaneça em um estado consistente mesmo em caso de falhas.

### Compensações Implementadas:

- **Falha no Pagamento**: Cancela o pedido
- **Falha no Envio**: Estorna o pagamento e cancela o pedido

## ?? Tecnologias Utilizadas

- **.NET 8**: Framework de desenvolvimento
- **Entity Framework Core**: ORM para acesso a dados
- **RabbitMQ**: Sistema de mensageria para comunicação assíncrona
- **SQL Server**: Banco de dados relacional
- **Arquitetura Limpa**: Padrão arquitetural

## ?? Como Executar

### Pré-requisitos:

- .NET 8 SDK
- SQL Server
- RabbitMQ

### Passos para Execução:

1. **Clone o Repositório**:
   ```bash
   git clone [url-do-repositorio]
   cd SagaPedidos
   ```

2. **Configure o Banco de Dados**:
   - Atualize a string de conexão no arquivo `appsettings.json`
   - Execute as migrações:
     ```bash
     dotnet ef database update --project SagaPedidos.Infra --startup-project SagaPedidos.Presentation
     ```

3. **Configure o RabbitMQ**:
   - Atualize as configurações do RabbitMQ no arquivo `appsettings.json`

4. **Execute a Aplicação**:
   ```bash
   dotnet run --project SagaPedidos.Presentation
   ```

## ?? APIs

O sistema expõe as seguintes APIs RESTful:

### API de Pedido

- **POST /api/pedidos**: Cria um novo pedido
- **GET /api/pedidos/{id}**: Consulta um pedido específico
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

O sistema implementa estratégias robustas para lidar com falhas em diferentes etapas:

1. **Falha na Criação do Pedido**: O pedido não é criado e uma mensagem de erro é retornada
2. **Falha no Pagamento**: O pedido é cancelado automaticamente
3. **Falha no Envio**: O pagamento é estornado e o pedido é cancelado

Todas as operações são registradas no console para facilitar o diagnóstico de problemas.

---

Este projeto demonstra uma implementação prática do padrão Saga para manter a consistência em sistemas distribuídos, utilizando mensageria e arquitetura orientada a eventos.