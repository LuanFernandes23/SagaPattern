# Detalhamento do Sistema de Mensageria com RabbitMQ e Padrão Saga

O sistema de mensageria é fundamental para a comunicação desacoplada entre os diferentes serviços da arquitetura baseada no padrão Saga. Ele utiliza RabbitMQ, e os principais componentes são o Publisher, os Subscribers, Exchanges, Filas e Bindings.

## 1. Componentes do RabbitMQ e sua Configuração

No RabbitMQ, a comunicação flui do Publisher para um Exchange, que então distribui as mensagens para Filas, onde os Subscribers as consomem.

*   **Exchange**:
    *   **Função**: É como uma agência de correios para mensagens. Ele recebe mensagens do Publisher e as encaminha para uma ou mais filas. O tipo de exchange determina como as mensagens são roteadas.
    *   **No código**: Utiliza-se um exchange do tipo **Fanout**.
        *   Declarado com `type: ExchangeType.Fanout` tanto no Publisher (`SagaPedidos.Infra\Messaging\Publisher.cs`) quanto na classe base Subscriber (`SagaPedidos.Infra\Messaging\Subscribers\Subscriber.cs`).
        *   O nome do exchange é configurável via `appsettings.json` (chave `RabbitMQ:ExchangeName`), com `"saga-pedidos"` como padrão.
        *   **Comportamento do Fanout**: Envia uma cópia de cada mensagem recebida para *todas* as filas vinculadas a ele, ignorando a `routingKey`. Ideal para broadcast.

*   **Fila (Queue)**:
    *   **Função**: Armazena mensagens até que um Subscriber as processe.
    *   **No código**: Cada tipo de Subscriber possui sua fila dedicada:
        *   `PedidoSubscriber`: `"pedido_queue"`
        *   `PagamentoSubscriber`: `"pagamento_queue"`
        *   `EnvioSubscriber`: `"envio_queue"`
        *   As filas são declaradas como `durable: true` (persistem a reinicializações do broker) e `autoDelete: false` (não são deletadas quando o último consumidor se desconecta) no método `SetupInfrastructure` da classe `Subscriber`.

*   **Binding**:
    *   **Função**: Regra que conecta um exchange a uma fila.
    *   **No código**: No método `SetupInfrastructure` da classe `Subscriber`, cada fila é vinculada ao exchange Fanout:
        ```csharp
        _channel.QueueBind(
                queue: _queueName,      // Ex: "pedido_queue"
                exchange: _exchangeName, // Ex: "saga-pedidos"
                routingKey: "");        // Routing key é ignorada para Fanout
        ```
        Isso garante que mensagens no exchange `"saga-pedidos"` sejam roteadas para todas as filas vinculadas.

## 2. Publisher (`SagaPedidos.Infra\Messaging\Publisher.cs`)

*   **Função**: Enviar mensagens para o RabbitMQ.
*   **Inicialização**:
    *   Recebe `RabbitMQConnection` e o nome do exchange.
    *   Cria um canal de comunicação e chama `DeclareExchange()` para garantir a existência do exchange Fanout.
*   **Publicação de Mensagens (`Publish` método)**:
    1.  Recebe um `SagaMessage`.
    2.  Serializa para JSON e converte para bytes.
    3.  Cria propriedades básicas (`IBasicProperties`):
        *   `DeliveryMode = 2` (mensagem persistente).
        *   `MessageId`, `Timestamp`, `ContentType`.
        *   Adiciona `Headers` customizados.
    4.  Publica a mensagem no exchange (`_exchangeName`) com `routingKey: ""`.

## 3. Subscriber (Classe base `SagaPedidos.Infra\Messaging\Subscribers\Subscriber.cs` e suas implementações)

*   **Função**: Receber e processar mensagens de uma fila específica.
*   **Estrutura**:
    *   **`Subscriber` (Classe Abstrata)**:
        *   **Inicialização**:
            *   Recebe `RabbitMQConnection`, `IServiceProvider`, nome do exchange e da fila.
            *   Cria um canal e chama `SetupInfrastructure()`:
                *   Declara o exchange Fanout.
                *   Declara a fila específica.
                *   Vincula a fila ao exchange.
                *   Configura `BasicQos` (prefetchCount: 1) para processar uma mensagem por vez.
        *   **`Subscribe()` Método**:
            *   Cria um `EventingBasicConsumer`.
            *   No evento `consumer.Received`:
                1.  Cria um novo escopo de injeção de dependência para cada mensagem.
                2.  Desserializa a mensagem para `SagaMessage`.
                3.  Chama `ProcessMessageAsync(message, scope.ServiceProvider)`.
                4.  Se sucesso, confirma a mensagem (`_channel.BasicAck`).
                5.  Se erro, rejeita a mensagem (`_channel.BasicNack` com `requeue: false`).
            *   Inicia o consumo da fila com `autoAck: false`.
    *   **Subscribers Concretos (`PedidoSubscriber`, `PagamentoSubscriber`, `EnvioSubscriber`)**:
        *   Herdeiros de `Subscriber`.
        *   Configurados com seus nomes de fila em `Program.cs`.
        *   Implementam `ProcessMessageAsync(SagaMessage message, IServiceProvider serviceProvider)`:
            *   Verificam `message.Type`.
            *   Desserializam `message.Payload`.
            *   Obtêm serviços via `serviceProvider`.
            *   Executam lógica de negócios e interagem com `PedidoSagaOrchestrator`.

## 4. Comunicação e Fluxo Geral

1.  **Publicação**: Uma ação no sistema leva à chamada do `Publish` do `Publisher`. A `SagaMessage` vai para o exchange Fanout.
2.  **Distribuição**: O exchange Fanout envia a mensagem para todas as filas vinculadas.
3.  **Consumo**: Cada Subscriber escuta sua fila específica e recebe a mensagem.
4.  **Processamento**: No `ProcessMessageAsync`, o subscriber decide como processar a mensagem com base no seu `Type`.

## 5. Interação com o `PedidoSagaOrchestrator`

O `PedidoSagaOrchestrator` coordena os passos da saga.

*   **Início da Saga**:
    *   Uma ação inicial (ex: criação de pedido) leva à publicação de um evento (ex: `PedidoCriadoEvent`).
    *   O `PedidoSagaOrchestrator.IniciarSaga` publica a primeira mensagem da saga (ex: `Type = "ProcessarPagamento"`).

*   **Passos da Saga e Respostas**:
    *   **Exemplo: Pagamento**:
        1.  `PagamentoSubscriber` recebe `"ProcessarPagamento"`.
        2.  Chama `IPagamentoService`.
        3.  **Sucesso**: Cria `PagamentoAprovadoEvent`, chama `_sagaOrchestrator.ContinuarSagaAposPagamento()`. Orquestrador publica próximo passo (ex: `"ProcessarEnvio"`).
        4.  **Falha**: Cria `PagamentoRecusadoEvent`, chama `_sagaOrchestrator.TratarFalhaPagamento()`. Orquestrador inicia compensação (ex: `"CancelarPedido"`).
    *   **Exemplo: Envio**:
        1.  `EnvioSubscriber` recebe `"ProcessarEnvio"`.
        2.  Chama `IEnvioService`.
        3.  **Sucesso**: Cria `EnvioProcessadoEvent`, chama `_sagaOrchestrator.FinalizarSaga()`.
        4.  **Falha**: Cria `EnvioFalhadoEvent`, chama `_sagaOrchestrator.TratarFalhaEnvio()`. Orquestrador inicia compensação.

*   **Compensação**:
    *   Se um passo falha, o orquestrador publica mensagens de compensação (ex: `"EstornarPagamento"`, depois `"CancelarPedido"`), que são processadas pelos subscribers correspondentes.

Esta arquitetura promove baixo acoplamento e resiliência, onde o Publisher envia eventos/comandos, o Exchange os distribui, e os Subscribers os processam, notificando o SagaOrchestrator para conduzir o fluxo da transação distribuída.
