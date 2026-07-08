# Тестирование

## Запуск тестов

```bash
cd MyService.Tests
dotnet test
```

## Типы тестов

### Unit-тесты

- Configuration/Options — валидация Options pattern
- Contracts/Dto — валидация DTO
- Extensions — middleware и DI
- HealthChecks — проверки здоровья

### Integration-тесты

- AuthenticationTests — JWT auth
- AuthorizationTests — policy-based auth
- CorrelationIdE2ETests — сквозной correlation ID
- MetadataEndpointTests — endpoint /api/metadata
- DevTokenEndpointTests — генерация тестовых токенов

## Покрытие кода

```bash
dotnet test --collect:"XPlat Code Coverage" \
    --settings coverlet.runsettings
```

Отчёт в `TestResults/coverage.cobertura.xml`.

## TestWebApplicationFactory

Используется для integration-тестов:

```csharp
[Fact]
public async Task GetMetadata_ReturnsOk()
{
    await using var factory = new TestWebApplicationFactory();
    var client = factory.CreateClient();

    var response = await client.GetAsync("/api/metadata");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

---
