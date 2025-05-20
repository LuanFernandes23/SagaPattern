# Documentação de Arquitetura - Sistema de Pedidos com Saga Pattern

## 1. Visão Geral

Este documento descreve a arquitetura de um sistema distribuído que implementa o padrão Saga para gerenciar a consistência de transações em um processo de compra. O sistema utiliza RabbitMQ como sistema de mensageria para comunicação entre os serviços.

## 2. Componentes Principais

### 2.1 Publisher
- **Função**: Recebe solicitações do cliente e publica eventos no Exchange do RabbitMQ
- **Responsabilidades**:
  - Iniciar o fluxo da saga de pedido
  - Publicar eventos para os serviços
  - Monitorar o progresso da saga

### 2.2 Exchange RabbitMQ (Fanout)
- **Função**: Distribui mensagens para todas as filas vinculadas
- **Tipo**: Fanout
- **Comportamento**: Envia uma cópia de cada mensagem para todas as filas, independente de routing keys

### 2.3 Subscribers
- **Função**: Consomem mensagens das filas e processam ações específicas
- **Tipos**:
  - Subscriber de Pedido
  - Subscriber de Pagamento
  - Subscriber de Envio
- **Responsabilidades**:
  - Processar eventos relevantes
  - Chamar APIs REST para executar ações
  - Enviar respostas ao Publisher

### 2.4 APIs REST
- **Função**: Executar as operações de negócio solicitadas pelos Subscribers
- **Tipos**:
  - API de Pedido
  - API de Pagamento
  - API de Envio

## 3. Configuração do Exchange RabbitMQ

### 3.1 Tipo de Exchange
O sistema utiliza um Exchange do tipo **Fanout**, escolhido pelos seguintes motivos:
- **Simplicidade**: Configuração simples e direta
- **Broadcast**: Todas as mensagens são enviadas para todas as filas vinculadas
- **Desacoplamento**: Os subscribers não precisam conhecer detalhes de routing

### 3.2 Configuração do Exchange
```csharp
// Declaração do exchange
channel.ExchangeDeclare(
    exchange: "pedido_exchange",
    type: ExchangeType.Fanout,
    durable: true,
    autoDelete: false);
```

## 4. Configuração das Filas

### 4.1 Filas do Sistema
O sistema possui três filas principais:
- **Fila de Pedido**: Recebe eventos relacionados a pedidos
- **Fila de Pagamento**: Recebe eventos relacionados a pagamentos
- **Fila de Envio**: Recebe eventos relacionados a envios

### 4.2 Declaração das Filas
```csharp
// Exemplo de declaração de fila
channel.QueueDeclare(
    queue: "pedido_queue",
    durable: true,
    exclusive: false,
    autoDelete: false);
```

### 4.3 Vinculação das Filas ao Exchange
```csharp
// Vinculação da fila ao exchange fanout
channel.QueueBind(
    queue: "pedido_queue",
    exchange: "pedido_exchange",
    routingKey: ""); // Routing key vazia no caso de fanout
```

## 5. Consumo de Mensagens pelos Subscribers

### 5.1 Configuração do Consumer
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