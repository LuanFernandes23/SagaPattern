# Planejamento de APIs REST

Este documento descreve as APIs REST que ser�o consumidas pelos Subscribers do sistema de pedidos.

## 1. API de Pedido

### 1.1 Criar Pedido

**Endpoint**: `POST /api/pedidos`

**Descri��o**: Cria um novo pedido no sistema.

**Payload (JSON)**:
```json
{
  "clienteId": "123",
  "itens": [
    {
      "produtoId": "456",
      "quantidade": 2,
      "preco": 50.00
    }
  ],
  "valorTotal": 100.00
}
```

**Resposta (JSON)**:
```json
{
  "pedidoId": "789",
  "status": "criado",
  "mensagem": "Pedido criado com sucesso"
}
```

### 1.2 Cancelar Pedido

**Endpoint**: `PUT /api/pedidos/{pedidoId}/cancelar`

**Descri��o**: Cancela um pedido existente.

**Payload (JSON)**:
```json
{
  "pedidoId": "789",
  "motivo": "Falha no processamento do pagamento"
}
```

**Resposta (JSON)**:
```json
{
  "pedidoId": "789",
  "status": "cancelado",
  "mensagem": "Pedido cancelado com sucesso"
}
```

### 1.3 Consultar Pedido

**Endpoint**: `GET /api/pedidos/{pedidoId}`

**Descri��o**: Obt�m informa��es sobre um pedido espec�fico.

**Resposta (JSON)**:
```json
{
  "pedidoId": "789",
  "clienteId": "123",
  "itens": [
    {
      "produtoId": "456",
      "quantidade": 2,
      "preco": 50.00
    }
  ],
  "valorTotal": 100.00,
  "status": "criado",
  "dataCriacao": "2025-05-10T14:30:00Z"
}
```

## 2. API de Pagamento

### 2.1 Processar Pagamento

**Endpoint**: `POST /api/pagamentos`

**Descri��o**: Processa o pagamento para um pedido.

**Payload (JSON)**:
```json
{
  "pedidoId": "789",
  "valor": 100.00,
  "metodoPagamento": "cartao"
}
```

**Resposta (JSON)**:
```json
{
  "transacaoId": "t123",
  "pedidoId": "789",
  "status": "aprovado",
  "mensagem": "Pagamento processado com sucesso"
}
```

### 2.2 Estornar Pagamento

**Endpoint**: `POST /api/pagamentos/{transacaoId}/estornar`

**Descri��o**: Estorna um pagamento realizado.

**Payload (JSON)**:
```json
{
  "trensacaoId": "t123",
  "pedidoId": "789",
  "motivo": "Falha no envio"
}
```

**Resposta (JSON)**:
```json
{
  "transacaoId": "t123",
  "pedidoId": "789",
  "status": "estornado",
  "mensagem": "Pagamento estornado com sucesso"
}
```

## 3. API de Envio

### 3.1 Processar Envio

**Endpoint**: `POST /api/envios`

**Descri��o**: Processa o envio de um pedido.

**Payload (JSON)**:
```json
{
  "pedidoId": "789",
  "endereco": {
    "rua": "Rua Exemplo",
    "numero": "123",
    "cidade": "S�o Paulo"
  }
}
```

**Resposta (JSON)**:
```json
{
  "envioId": "e456",
  "pedidoId": "789",
  "codigoRastreio": "BR12345678",
  "status": "processado",
  "mensagem": "Envio processado com sucesso"
}
```

### 3.2 Cancelar Envio

**Endpoint**: `PUT /api/envios/{envioId}/cancelar`

**Descri��o**: Cancela um envio que ainda n�o foi despachado.

**Payload (JSON)**:
```json
{
  "envioId": "e456",
  "pedidoId": "789",
  "motivo": "Pedido cancelado"
}
```

**Resposta (JSON)**:
```json
{
  "envioId": "e456",
  "pedidoId": "789",
  "status": "cancelado",
  "mensagem": "Envio cancelado com sucesso"
}
```