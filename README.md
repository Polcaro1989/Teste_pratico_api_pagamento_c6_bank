![C6 Bank](assets/c6bank.jpg)

# Gateway de Pagamentos C6 (sandbox)

Repositório .NET 10 para integração com os endpoints de Checkout/Pix do C6 Bank.

## Passos executados até aqui
- Criado repositório e solução: `dotnet new sln -n GatewayPagamentos`
- Projetos:
  - `GatewayPagamentos.Api` (Web API com Controllers) – `dotnet new webapi --use-controllers`
  - `GatewayPagamentos.IntegracoesC6` (SDK) – `dotnet new classlib`
- Removidos arquivos de template (`WeatherForecast.cs` e controller correspondente).
- Adicionadas dependências base (Serilog, Swagger, Polly) nas csproj.
- Configuração C6 adicionada (section em appsettings + HttpClient mTLS registrado via AddC6Clients).
- Swagger habilitado (UI em `/swagger`, spec em `/openapi/v1.json`).

## Como rodar local
```bash
cd src/GatewayPagamentos.Api
# use outra porta se 5163/5200 estiver ocupada
dotnet run --urls "http://localhost:5201"
```

## Credenciais e certificado
- Não versione secrets. Use *User Secrets* em dev:
  ```bash
  dotnet user-secrets init
  dotnet user-secrets set "C6:ClientId" "seu_id"
  dotnet user-secrets set "C6:ClientSecret" "seu_secret"
  dotnet user-secrets set "C6:ClientCertificatePath" "c:/certs/c6api.pfx"
  dotnet user-secrets set "C6:ClientCertificatePassword" "senha"
  ```
- Em produção, use variáveis de ambiente (`C6__ClientId`, `C6__ClientSecret`, etc.).
- O bypass inseguro de TLS em desenvolvimento agora é **opt-in explícito**:
  - `C6__AllowInsecureServerCertificateInDevelopment=true`
  - Use somente em ambiente local e temporariamente.

## Swagger / OpenAPI
- UI: `http://localhost:5201/swagger`
- Spec (json): `http://localhost:5201/swagger/v1/swagger.json`
- Implementação: `AddEndpointsApiExplorer`, `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI` em `Program.cs`.

## Requisitos
- .NET 10 SDK
- Credenciais e certificado mTLS fornecidos pelo C6 Bank (sandbox ou produção).

## Checkout Controller (implementado)
- Rota base: `v1/checkouts` (espelhando o C6)
- Endpoints: POST (criar, 201 + Location), POST `authorize` (200), GET `{id}` (200/404), PUT `{id}/cancel` (204/404).
- DTOs da API: `CreateCheckoutRequestDto`, `AuthorizeCheckoutRequestDto`, `CheckoutResponseDto` e aninhados (`PayerDto`, `AddressDto`, `PaymentDto`, `CardDto`, `PixPaymentDto`, `CardInfoDto`).
- Contratos da API separados da integracao C6: DTOs em `src/GatewayPagamentos.Api/Contracts` e mapeamento Anti-Corruption Layer em `src/GatewayPagamentos.Api/Mappers/C6CheckoutMapper.cs`.
- Serialização JSON na borda da API em `snake_case` para espelhar o payload do C6 (`external_reference_id`, `phone_number`, `card_info`, etc.).
- Validação: `id` obrigatório em GET/PUT; requisições inválidas do C6 retornam 400; 404 mapeado quando o C6 responde NotFound.
- Regra de pagamento: aceita `card`, `pix` ou ambos, conforme payload enviado no JSON do C6.
- Status codes: 201 Created (criação), 200 OK (consulta/autorizar), 204 NoContent (cancelar), 400/404 conforme erro.
- Tratamento de erros: captura `HttpRequestException` e mapeia para `ProblemDetails`, logando warnings (400) e errors (demais status).
- Logging: `ILogger<CheckoutController>` com logs em validação e falhas HTTP.
- Nomeação: versão no path (`v1`), verbo nos métodos, recurso singular no path (`checkout`).

## Testes
- Stack: xUnit + Moq + Microsoft.NET.Test.Sdk
- Projeto de testes: `tests/GatewayPagamentos.Api.Tests`
- Escopo atual: Controller, camada de aplicação, mapper, validações e integração de token/health check (com mocks/fakes)

### Casos cobertos
- `CheckoutControllerTests`
  - POST `/v1/checkouts` -> `201 Created`
  - POST `/v1/checkouts/authorize` -> `200 OK`
  - GET `/v1/checkouts/{id}` -> `200 OK`
  - PUT `/v1/checkouts/{id}/cancel` -> `204 NoContent`
  - propagação de exceção HTTP do serviço
- `CheckoutAppServiceTests`
  - mapeamento + orquestração de token/cliente C6 em criar/autorizar/consultar
  - validação de argumento (`id`) em consultar/cancelar
- `C6CheckoutMapperTests`
  - mapeamento de request API -> C6 e response C6 -> API
- `CheckoutValidatorsTests`
  - CPF/CNPJ válidos
  - rejeição de `TaxId`, `ZipCode` e `State` inválidos
  - aceitação quando `card` e `pix` são enviados juntos
  - aceitação de pagamento com cartão sem `card_info`
- `C6TokenClientTests`
  - reuso de token em cache
  - renovação automática após expiração segura
- `C6HealthCheckTests`
  - throttle de chamadas de autenticação em probes frequentes
  - retorno `Unhealthy` quando auth falha

### Como rodar
```bash
dotnet test
```
(Requer .NET 10 SDK; nenhum serviço externo é chamado, tudo mockado.)
