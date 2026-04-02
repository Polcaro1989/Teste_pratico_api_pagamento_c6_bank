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

## Swagger / OpenAPI
- UI: `http://localhost:5201/swagger`
- Spec (json): `http://localhost:5201/swagger/v1/swagger.json`
- Implementação: `AddEndpointsApiExplorer`, `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI` em `Program.cs`.

## Requisitos
- .NET 10 SDK
- Credenciais e certificado mTLS fornecidos pelo C6 Bank (sandbox ou produção).

## Checkout Controller (implementado)
- Rota base: `api/v1/checkout`
- Endpoints: POST (criar, 201 + Location), POST autorizar (200), GET {id} (200/404), PUT {id}/cancelar (204/404).
- DTOs da API: `CreateCheckoutRequestDto`, `AuthorizeCheckoutRequestDto`, `CheckoutResponseDto` e aninhados (`PayerDto`, `AddressDto`, `PaymentDto`, `CardDto`, `PixPaymentDto`, `CardInfoDto`).
- Contratos da API separados da integracao C6: DTOs em `src/GatewayPagamentos.Api/Contracts` e mapeamento Anti-Corruption Layer em `src/GatewayPagamentos.Api/Mappers/C6CheckoutMapper.cs`.
- Validação: `id` obrigatório em GET/PUT; requisições inválidas do C6 retornam 400; 404 mapeado quando o C6 responde NotFound.
- Status codes: 201 Created (criação), 200 OK (consulta/autorizar), 204 NoContent (cancelar), 400/404 conforme erro.
- Tratamento de erros: captura `HttpRequestException` e mapeia para `ProblemDetails`, logando warnings (400) e errors (demais status).
- Logging: `ILogger<CheckoutController>` com logs em validação e falhas HTTP.
- Nomeação: versão no path (`v1`), verbo nos métodos, recurso singular no path (`checkout`).

## Testes
- Stack: xUnit + Moq + Microsoft.NET.Test.Sdk
- Projeto de testes: `tests/GatewayPagamentos.Api.Tests`
- Escopo atual: CheckoutController (happy e erros mapeados)

### Casos cobertos
- POST /api/v1/checkout -> 201 Created + Location
- POST /api/v1/checkout com erro de validação -> 400
- POST /api/v1/checkout com falha de auth (401/403) -> 503
- POST /api/v1/checkout com timeout -> 408
- POST /api/v1/checkout erro inesperado -> 500
- GET /api/v1/checkout/{id} com id vazio -> 400
- GET /api/v1/checkout/{id} quando C6 retorna 404 -> 404
- PUT /api/v1/checkout/{id}/cancelar sucesso -> 204

### Como rodar
```bash
dotnet test
```
(Requer .NET 10 SDK; nenhum serviço externo é chamado, tudo mockado.)
