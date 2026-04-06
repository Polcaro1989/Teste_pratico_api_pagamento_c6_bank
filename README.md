![C6 Bank](assets/c6bank.jpg)

# Gateway de Pagamentos C6

API .NET 10 para integração com C6 Checkout (`/v1/checkouts`) e PIX direto (`/v2/pix`).

## Status atual
- Ambiente validado: **Sandbox C6**.
- Stack: ASP.NET Core + FluentValidation + Serilog + Polly + Swagger.
- Cliente de integração: `HttpClient` próprio (não usa SDK oficial do C6).
- Segurança: mTLS com certificado cliente.

## O que foi implementado
- Contrato da borda em `snake_case`.
- Mapper anti-corruption API -> C6 e C6 -> API.
- Tratamento global de exceção com `ProblemDetails`.
- Cache/reuso de token C6 (evita autenticar a cada requisição).
- Health check com intervalo (evita carga excessiva no auth endpoint).
- Validações de entrada para checkout/pix.
- Suporte a PIX direto com retorno de `pixCopiaECola`.

## Endpoints implementados

### Checkout
- `POST /v1/checkouts`
- `POST /v1/checkouts/authorize`
- `GET /v1/checkouts/{id}`
- `PUT /v1/checkouts/{id}/cancel`

### PIX direto
- `POST /v2/pix/cob`
- `PUT /v2/pix/cob/{txid}`
- `GET /v2/pix/cob/{txid}`
- `GET /v2/pix/cob`
- `PUT /v2/pix/cobv/{txid}`
- `GET /v2/pix/cobv/{txid}`
- `GET /v2/pix/cobv`
- `PUT /v2/pix/webhook/{chave}`

## Regras de contrato importantes
- `external_reference_id` (checkout): **alfanumérico, 1 a 10 caracteres**.
- `tax_id`: formato de 11 ou 14 dígitos (sem checksum de CPF/CNPJ).
- `payment`: aceita `card`, `pix` ou ambos.
- Checkout hospedado retorna `url`; não retorna `pixCopiaECola` no payload.
- PIX direto retorna `pixCopiaECola` (e `location`/`redirect_url` quando disponível).

## Configuração segura
A API lê configuração em ordem de precedência:
1. Variáveis de ambiente do sistema/CI
2. Arquivo `.env` na raiz
3. `appsettings.json`

Variáveis necessárias:
- `C6__BaseUrl`
- `C6__TokenUrl`
- `C6__ClientId`
- `C6__ClientSecret`
- `C6__ClientCertificatePath`
- `C6__ClientCertificatePassword`
- `C6__AllowInsecureServerCertificateInDevelopment` (somente local)

Existe um arquivo de exemplo sem segredo: `.env.example`.

## Como rodar local
```bash
dotnet run --project src/GatewayPagamentos.Api/GatewayPagamentos.Api.csproj --urls "http://localhost:5203"
```

Swagger:
- UI: `http://localhost:5203/swagger`
- JSON: `http://localhost:5203/swagger/v1/swagger.json`

Health:
- `GET http://localhost:5203/health`

## Testes automatizados
Projeto: `tests/GatewayPagamentos.Api.Tests`

Executar:
```bash
dotnet test tests/GatewayPagamentos.Api.Tests/GatewayPagamentos.Api.Tests.csproj
```

Cobertura atual (alto nível):
- controllers checkout/pix
- app services
- mapper checkout
- validators checkout
- token cache
- health check
- client de checkout (cancel com fallback)

## Testes reais executados (sandbox)
Data da última bateria completa: **06/04/2026**.

Relatório local:
- `real-endpoint-test-report-final5.json`

Resultado da execução real:
- `GET /health` -> 200
- `POST /v1/checkouts` -> 201
- `POST /v1/checkouts/authorize` -> 200
- `GET /v1/checkouts/{id}` -> 200
- `PUT /v1/checkouts/{id}/cancel` -> 204
- `POST /v2/pix/cob` -> 201
- `PUT /v2/pix/cob/{txid}` -> 201
- `GET /v2/pix/cob/{txid}` -> 200
- `GET /v2/pix/cob` -> 200
- `PUT /v2/pix/cobv/{txid}` -> 201
- `GET /v2/pix/cobv/{txid}` -> 200
- `GET /v2/pix/cobv` -> 200
- `PUT /v2/pix/webhook/{chave}` -> 200

## Como reproduzir os testes reais
1. Configure `.env` com credenciais/certificado do sandbox (sem versionar).
2. Suba a API na porta 5203.
3. Use Swagger ou o arquivo de requests:
   - `src/GatewayPagamentos.Api/GatewayPagamentos.Api.http`
4. Execute os endpoints na ordem:
   - checkout create -> checkout authorize -> checkout get -> checkout cancel
   - pix cob create -> pix cob put -> pix cob get -> pix cob list
   - pix cobv put -> pix cobv get -> pix cobv list -> pix webhook put

### Teste via Swagger
1. Abra `http://localhost:5203/swagger`.
2. Teste na ordem:
   - `POST /v1/checkouts`
   - `POST /v1/checkouts/authorize`
   - `GET /v1/checkouts/{id}`
   - `PUT /v1/checkouts/{id}/cancel`
   - `POST /v2/pix/cob`
   - `PUT /v2/pix/cob/{txid}`
   - `GET /v2/pix/cob/{txid}`
   - `GET /v2/pix/cob`
   - `PUT /v2/pix/cobv/{txid}`
   - `GET /v2/pix/cobv/{txid}`
   - `GET /v2/pix/cobv`
   - `PUT /v2/pix/webhook/{chave}`

### Execução rápida com curl (inline, sem arquivos externos)
```bash
# 1) Checkout - criar
curl -X POST "http://localhost:5203/v1/checkouts" \
  -H "Content-Type: application/json" \
  -d '{"amount":1.00,"description":"Teste checkout PIX","external_reference_id":"REFCRIA01","payer":{"name":"Jose da Silva","tax_id":"52998224725","email":"pagador@email.com.br","phone_number":"11993900134","address":{"street":"Av Nove de Julho","number":123,"complement":"Apto 10","city":"Rio de Janeiro","state":"RJ","zip_code":"05093000"}},"payment":{"pix":{"key":"AUTO"}},"redirect_url":"https://seusite.com/finaliza"}'

# 2) Checkout - autorizar
curl -X POST "http://localhost:5203/v1/checkouts/authorize" \
  -H "Content-Type: application/json" \
  -d '{"amount":1.00,"description":"Teste authorize PIX","external_reference_id":"REFAUTH01","payer":{"name":"Jose da Silva","tax_id":"52998224725","email":"pagador@email.com.br","phone_number":"11993900134","address":{"street":"Av Nove de Julho","number":123,"complement":"Apto 10","city":"Rio de Janeiro","state":"RJ","zip_code":"05093000"}},"payment":{"pix":{"key":"AUTO"}},"redirect_url":"https://seusite.com/finaliza"}'

# 3) Checkout - consultar (trocar CHECKOUT_ID)
curl -X GET "http://localhost:5203/v1/checkouts/CHECKOUT_ID"

# 4) Checkout - cancelar (trocar CHECKOUT_ID)
curl -X PUT "http://localhost:5203/v1/checkouts/CHECKOUT_ID/cancel"

# 5) PIX cob - criar sem txid
curl -X POST "http://localhost:5203/v2/pix/cob" \
  -H "Content-Type: application/json" \
  -d '{"calendario":{"expiracao":3600},"devedor":{"cpf":"52998224725","nome":"Jose da Silva"},"valor":{"original":"1.00","modalidadeAlteracao":1},"chave":"SUA_CHAVE_PIX","solicitacaoPagador":"Teste PIX direto","infoAdicionais":[{"nome":"pedido","valor":"PIX-1"}]}'

# 6) PIX cob - criar com txid (trocar TXID_COB)
curl -X PUT "http://localhost:5203/v2/pix/cob/TXID_COB" \
  -H "Content-Type: application/json" \
  -d '{"calendario":{"expiracao":3600},"devedor":{"cpf":"52998224725","nome":"Jose da Silva"},"valor":{"original":"1.00","modalidadeAlteracao":1},"chave":"SUA_CHAVE_PIX","solicitacaoPagador":"Teste PIX com txid","infoAdicionais":[{"nome":"pedido","valor":"PIX-2"}]}'

# 7) PIX cob - consultar (trocar TXID_COB)
curl -X GET "http://localhost:5203/v2/pix/cob/TXID_COB"

# 8) PIX cob - listar
curl -X GET "http://localhost:5203/v2/pix/cob?inicio=2026-04-06T00:00:00-03:00&fim=2026-04-06T23:59:59-03:00"

# 9) PIX cobv - criar com txid (trocar TXID_COBV)
curl -X PUT "http://localhost:5203/v2/pix/cobv/TXID_COBV" \
  -H "Content-Type: application/json" \
  -d '{"calendario":{"dataDeVencimento":"2026-12-31","validadeAposVencimento":30},"devedor":{"logradouro":"Rua A, 100","cidade":"Sao Paulo","uf":"SP","cep":"01001000","cpf":"52998224725","nome":"Jose da Silva"},"valor":{"original":"1.00"},"chave":"SUA_CHAVE_PIX","solicitacaoPagador":"Teste cobv"}'

# 10) PIX cobv - consultar (trocar TXID_COBV)
curl -X GET "http://localhost:5203/v2/pix/cobv/TXID_COBV"

# 11) PIX cobv - listar
curl -X GET "http://localhost:5203/v2/pix/cobv?inicio=2026-04-06T00:00:00-03:00&fim=2026-04-06T23:59:59-03:00"

# 12) PIX webhook - configurar (trocar SUA_CHAVE_PIX na URL)
curl -X PUT "http://localhost:5203/v2/pix/webhook/SUA_CHAVE_PIX" \
  -H "Content-Type: application/json" \
  -d '{"webhookUrl":"https://meusistema.com.br/webhooks/pix"}'
```

## Matriz de teste por endpoint (payload)

### Checkout
- `POST /v1/checkouts`
  - body JSON: **sim**
  - exemplo mínimo:
```json
{
  "amount": 1.00,
  "description": "Teste checkout",
  "external_reference_id": "REF1234567",
  "payer": {
    "name": "Jose da Silva",
    "tax_id": "52998224725",
    "email": "pagador@email.com.br",
    "phone_number": "11993900134",
    "address": {
      "street": "Av Nove de Julho",
      "number": 123,
      "city": "Rio de Janeiro",
      "state": "RJ",
      "zip_code": "05093000"
    }
  },
  "payment": {
    "pix": {
      "key": "AUTO"
    }
  },
  "redirect_url": "https://seusite.com/finaliza"
}
```

- `POST /v1/checkouts/authorize`
  - body JSON: **sim** (mesmo contrato do create)

- `GET /v1/checkouts/{id}`
  - body JSON: **não**

- `PUT /v1/checkouts/{id}/cancel`
  - body JSON: **não** (enviar vazio)

### PIX direto
- `POST /v2/pix/cob`
  - body JSON: **sim**
```json
{
  "calendario": { "expiracao": 3600 },
  "devedor": { "cpf": "52998224725", "nome": "Jose da Silva" },
  "valor": { "original": "1.00", "modalidadeAlteracao": 1 },
  "chave": "SUA_CHAVE_PIX",
  "solicitacaoPagador": "Teste PIX direto",
  "infoAdicionais": [{ "nome": "pedido", "valor": "PIX-1" }]
}
```

- `PUT /v2/pix/cob/{txid}`
  - body JSON: **sim** (mesmo contrato do `POST /cob`)

- `GET /v2/pix/cob/{txid}`
  - body JSON: **não**

- `GET /v2/pix/cob?inicio=...&fim=...`
  - body JSON: **não**

- `PUT /v2/pix/cobv/{txid}`
  - body JSON: **sim**
```json
{
  "calendario": {
    "dataDeVencimento": "2026-12-31",
    "validadeAposVencimento": 30
  },
  "devedor": {
    "logradouro": "Rua A, 100",
    "cidade": "Sao Paulo",
    "uf": "SP",
    "cep": "01001000",
    "cpf": "52998224725",
    "nome": "Jose da Silva"
  },
  "valor": { "original": "1.00" },
  "chave": "SUA_CHAVE_PIX",
  "solicitacaoPagador": "Teste cobv"
}
```

- `GET /v2/pix/cobv/{txid}`
  - body JSON: **não**

- `GET /v2/pix/cobv?inicio=...&fim=...`
  - body JSON: **não**

- `PUT /v2/pix/webhook/{chave}`
  - body JSON: **sim**
```json
{
  "webhookUrl": "https://meusistema.com.br/webhooks/pix"
}
```

## O que falta para produção real
1. Trocar sandbox por produção:
- `C6__BaseUrl=https://baas-api.c6bank.info`
- `C6__TokenUrl=https://baas-api.c6bank.info/v1/auth/`

2. Usar certificado mTLS de produção válido no host final.

3. Publicar com HTTPS e reverse proxy estável.

4. Proteger segredos em cofre (Key Vault/Secrets Manager), sem `.env` no servidor final.

5. Endurecer segurança da borda da API:
- autenticação/autorização
- rate limiting
- CORS
- auditoria e mascaramento de dados sensíveis em log

6. Observabilidade para operação:
- logs estruturados
- métricas de latência/erro
- alertas de indisponibilidade C6

7. Homologação final com C6 (roteiro de conformidade do parceiro).

## Fora do escopo atual
A API C6 completa de PIX tem endpoints adicionais não implementados aqui, por exemplo:
- `PATCH /v2/pix/cob/{txid}`
- `PATCH /v2/pix/cobv/{txid}`
- `GET/DELETE /v2/pix/webhook/{chave}`
- blocos `loc`, `lotecobv`, `pix recebidos/devolução`

## Segurança
- Este repositório não deve armazenar credenciais reais.
- `.env` e pasta de credenciais estão ignorados no `.gitignore`.
- Nunca exponha `client_secret`, senha do `.pfx` ou conteúdo de certificado em commit/log.

