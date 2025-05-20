# Documenta��o de Arquitetura - Sistema de Pedidos com Saga Pattern

## 1. Vis�o Geral

Este documento descreve a arquitetura de um sistema distribu�do que implementa o padr�o Saga para gerenciar a consist�ncia de transa��es em um processo de compra. O sistema utiliza RabbitMQ como sistema de mensageria para comunica��o entre os servi�os.

## 2. Componentes Principais

### 2.1 Publisher
- **Fun��o**: Recebe solicita��es do cliente e publica eventos no Exchange do RabbitMQ
- **Responsabilidades**:
  - Iniciar o fluxo da saga de pedido
  - Publicar eventos para os servi�os
  - Monitorar o progresso da saga

### 2.2 Exchange RabbitMQ (Fanout)
- **Fun��o**: Distribui mensagens para todas as filas vinculadas
- **Tipo**: Fanout
- **Comportamento**: Envia uma c�pia de cada mensagem para todas as filas, independente de routing keys

### 2.3 Subscribers
- **Fun��o**: Consomem mensagens das filas e processam a��es espec�ficas
- **Tipos**:
  - Subscriber de Pedido
  - Subscriber de Pagamento
  - Subscriber de Envio
- **Responsabilidades**:
  - Processar eventos relevantes
  - Chamar APIs REST para executar a��es
  - Enviar respostas ao Publisher

### 2.4 APIs REST
- **Fun��o**: Executar as opera��es de neg�cio solicitadas pelos Subscribers
- **Tipos**:
  - API de Pedido
  - API de Pagamento
  - API de Envio

## 3. Configura��o do Exchange RabbitMQ

### 3.1 Tipo de Exchange
O sistema utiliza um Exchange do tipo **Fanout**, escolhido pelos seguintes motivos:
- **Simplicidade**: Configura��o simples e direta
- **Broadcast**: Todas as mensagens s�o enviadas para todas as filas vinculadas
- **Desacoplamento**: Os subscribers n�o precisam conhecer detalhes de routing

### 3.2 Configura��o do Exchange
```csharp
// Declara��o do exchange
channel.ExchangeDeclare(
    exchange: "pedido_exchange",
    type: ExchangeType.Fanout,
    durable: true,
    autoDelete: false);
```

## 4. Configura��o das Filas

### 4.1 Filas do Sistema
O sistema possui tr�s filas principais:
- **Fila de Pedido**: Recebe eventos relacionados a pedidos
- **Fila de Pagamento**: Recebe eventos relacionados a pagamentos
- **Fila de Envio**: Recebe eventos relacionados a envios

### 4.2 Declara��o das Filas
```csharp
// Exemplo de declara��o de fila
channel.QueueDeclare(
    queue: "pedido_queue",
    durable: true,
    exclusive: false,
    autoDelete: false);
```

### 4.3 Vincula��o das Filas ao Exchange
```csharp
// Vincula��o da fila ao exchange fanout
channel.QueueBind(
    queue: "pedido_queue",
    exchange: "pedido_exchange",
    routingKey: ""); // Routing key vazia no caso de fanout
```

## 5. Consumo de Mensagens pelos Subscribers

### 5.1 Configura��o do Consumer
```csharp
var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    
    // Processar a mensagem
    ProcessMessage(message);
    
    // Confirmar o processamento da mensagem
    channel.BasicAck(ea.DeliveryTag, multiple: false);
};
```