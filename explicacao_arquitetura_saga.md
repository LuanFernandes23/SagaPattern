# Detalhamento do Sistema de Mensageria com RabbitMQ e Padr�o Saga

O sistema de mensageria � fundamental para a comunica��o desacoplada entre os diferentes servi�os da arquitetura baseada no padr�o Saga. Ele utiliza RabbitMQ, e os principais componentes s�o o Publisher, os Subscribers, Exchanges, Filas e Bindings.

## 1. Componentes do RabbitMQ e sua Configura��o

No RabbitMQ, a comunica��o flui do Publisher para um Exchange, que ent�o distribui as mensagens para Filas, onde os Subscribers as consomem.

*   **Exchange**:
    *   **Fun��o**: � como uma ag�ncia de correios para mensagens. Ele recebe mensagens do Publisher e as encaminha para uma ou mais filas. O tipo de exchange determina como as mensagens s�o roteadas.
    *   **No c�digo**: Utiliza-se um exchange do tipo **Fanout**.
        *   Declarado com `type: ExchangeType.Fanout` tanto no Publisher (`SagaPedidos.Infra\Messaging\Publisher.cs`) quanto na classe base Subscriber (`SagaPedidos.Infra\Messaging\Subscribers\Subscriber.cs`).
        *   O nome do exchange � configur�vel via `appsettings.json` (chave `RabbitMQ:ExchangeName`), com `"saga-pedidos"` como padr�o.
        *   **Comportamento do Fanout**: Envia uma c�pia de cada mensagem recebida para *todas* as filas vinculadas a ele, ignorando a `routingKey`. Ideal para broadcast.

*   **Fila (Queue)**:
    *   **Fun��o**: Armazena mensagens at� que um Subscriber as processe.
    *   **No c�digo**: Cada tipo de Subscriber possui sua fila dedicada:
        *   `PedidoSubscriber`: `"pedido_queue"`
        *   `PagamentoSubscriber`: `"pagamento_queue"`
        *   `EnvioSubscriber`: `"envio_queue"`
        *   As filas s�o declaradas como `durable: true` (persistem a reinicializa��es do broker) e `autoDelete: false` (n�o s�o deletadas quando o �ltimo consumidor se desconecta) no m�todo `SetupInfrastructure` da classe `Subscriber`.

*   **Binding**:
    *   **Fun��o**: Regra que conecta um exchange a uma fila.
    *   **No c�digo**: No m�todo `SetupInfrastructure` da classe `Subscriber`, cada fila � vinculada ao exchange Fanout:
        ```csharp
        _channel.QueueBind(
                queue: _queueName,      // Ex: "pedido_queue"
                exchange: _exchangeName, // Ex: "saga-pedidos"
                routingKey: "");        // Routing key � ignorada para Fanout
        ```
        Isso garante que mensagens no exchange `"saga-pedidos"` sejam roteadas para todas as filas vinculadas.

## 2. Publisher (`SagaPedidos.Infra\Messaging\Publisher.cs`)

*   **Fun��o**: Enviar mensagens para o RabbitMQ.
*   **Inicializa��o**:
    *   Recebe `RabbitMQConnection` e o nome do exchange.
    *   Cria um canal de comunica��o e chama `DeclareExchange()` para garantir a exist�ncia do exchange Fanout.
*   **Publica��o de Mensagens (`Publish` m�todo)**:
    1.  Recebe um `SagaMessage`.
    2.  Serializa para JSON e converte para bytes.
    3.  Cria propriedades b�sicas (`IBasicProperties`):
        *   `DeliveryMode = 2` (mensagem persistente).
        *   `MessageId`, `Timestamp`, `ContentType`.
        *   Adiciona `Headers` customizados.
    4.  Publica a mensagem no exchange (`_exchangeName`) com `routingKey: ""`.

## 3. Subscriber (Classe base `SagaPedidos.Infra\Messaging\Subscribers\Subscriber.cs` e suas implementa��es)

*   **Fun��o**: Receber e processar mensagens de uma fila espec�fica.
*   **Estrutura**:
    *   **`Subscriber` (Classe Abstrata)**:
        *   **Inicializa��o**:
            *   Recebe `RabbitMQConnection`, `IServiceProvider`, nome do exchange e da fila.
            *   Cria um canal e chama `SetupInfrastructure()`:
                *   Declara o exchange Fanout.
                *   Declara a fila espec�fica.
                *   Vincula a fila ao exchange.
                *   Configura `BasicQos` (prefetchCount: 1) para processar uma mensagem por vez.
        *   **`Subscribe()` M�todo**:
            *   Cria um `EventingBasicConsumer`.
            *   No evento `consumer.Received`:
                1.  Cria um novo escopo de inje��o de depend�ncia para cada mensagem.
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
            *   Obt�m servi�os via `serviceProvider`.
            *   Executam l�gica de neg�cios e interagem com `PedidoSagaOrchestrator`.

## 4. Comunica��o e Fluxo Geral

1.  **Publica��o**: Uma a��o no sistema leva � chamada do `Publish` do `Publisher`. A `SagaMessage` vai para o exchange Fanout.
2.  **Distribui��o**: O exchange Fanout envia a mensagem para todas as filas vinculadas.
3.  **Consumo**: Cada Subscriber escuta sua fila espec�fica e recebe a mensagem.
4.  **Processamento**: No `ProcessMessageAsync`, o subscriber decide como processar a mensagem com base no seu `Type`.

## 5. Intera��o com o `PedidoSagaOrchestrator`

O `PedidoSagaOrchestrator` coordena os passos da saga.

*   **In�cio da Saga**:
    *   Uma a��o inicial (ex: cria��o de pedido) leva � publica��o de um evento (ex: `PedidoCriadoEvent`).
    *   O `PedidoSagaOrchestrator.IniciarSaga` publica a primeira mensagem da saga (ex: `Type = "ProcessarPagamento"`).

*   **Passos da Saga e Respostas**:
    *   **Exemplo: Pagamento**:
        1.  `PagamentoSubscriber` recebe `"ProcessarPagamento"`.
        2.  Chama `IPagamentoService`.
        3.  **Sucesso**: Cria `PagamentoAprovadoEvent`, chama `_sagaOrchestrator.ContinuarSagaAposPagamento()`. Orquestrador publica pr�ximo passo (ex: `"ProcessarEnvio"`).
        4.  **Falha**: Cria `PagamentoRecusadoEvent`, chama `_sagaOrchestrator.TratarFalhaPagamento()`. Orquestrador inicia compensa��o (ex: `"CancelarPedido"`).
    *   **Exemplo: Envio**:
        1.  `EnvioSubscriber` recebe `"ProcessarEnvio"`.
        2.  Chama `IEnvioService`.
        3.  **Sucesso**: Cria `EnvioProcessadoEvent`, chama `_sagaOrchestrator.FinalizarSaga()`.
        4.  **Falha**: Cria `EnvioFalhadoEvent`, chama `_sagaOrchestrator.TratarFalhaEnvio()`. Orquestrador inicia compensa��o.

*   **Compensa��o**:
    *   Se um passo falha, o orquestrador publica mensagens de compensa��o (ex: `"EstornarPagamento"`, depois `"CancelarPedido"`), que s�o processadas pelos subscribers correspondentes.

Esta arquitetura promove baixo acoplamento e resili�ncia, onde o Publisher envia eventos/comandos, o Exchange os distribui, e os Subscribers os processam, notificando o SagaOrchestrator para conduzir o fluxo da transa��o distribu�da.
