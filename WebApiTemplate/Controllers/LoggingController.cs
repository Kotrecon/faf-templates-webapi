using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApiTemplate.Contracts.Dto.Request.Logging;
using Faf.ResultPattern.Common;
using Faf.ResultPattern.Web;
using Serilog.Core;
using Serilog.Events;

namespace WebApiTemplate.Controllers;

/// <summary>
/// API для управления логированием Serilog.
/// Позволяет менять уровни логирования на лету через <see cref="LoggingLevelSwitch"/>.
/// Все изменения логируются с указанием пользователя (аудит).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class LoggingController : ControllerBase
{
    private readonly LoggingLevelSwitch _rootSwitch;
    private readonly IDictionary<string, LoggingLevelSwitch> _overrideSwitches;
    private readonly ILogger<LoggingController> _logger;

    /// <summary>
    /// Запрещённые уровни для безопасности.
    /// Fatal — отключит все логи (кроме фатальных), скроет следы атаки.
    /// Verbose — затопит логи, переполнит диск, затруднит анализ.
    /// </summary>
    private static readonly HashSet<LogEventLevel> ForbiddenLevels = new()
    {
        LogEventLevel.Fatal,
        LogEventLevel.Verbose
    };

    public LoggingController(
        LoggingLevelSwitch rootSwitch,
        IDictionary<string, LoggingLevelSwitch> overrideSwitches,
        ILogger<LoggingController> logger)
    {
        _rootSwitch = rootSwitch;
        _overrideSwitches = overrideSwitches;
        _logger = logger;
    }

    /// <summary>
    /// Получить текущие уровни логирования (root + overrides).
    /// </summary>
    /// <returns>Текущий root-уровень и overrides по категориям.</returns>
    /// <response code="200">Успешно. Возвращает JSON с полями Default и Overrides.</response>
    /// <response code="401">Неавторизован (нет или неверный JWT-токен).</response>
    /// <response code="403">Запрещено (нет роли Admin/Operator/Auditor).</response>
    /// <remarks>
    /// Доступно по политике <c>AuditViewer</c>: Admin, Operator, Auditor.
    /// </remarks>
    [HttpGet("level")]
    [Authorize(Policy = "AuditViewer")]
    public IActionResult GetLevel()
    {
        return Ok(new
        {
            Default = _rootSwitch.MinimumLevel.ToString(),
            Overrides = _overrideSwitches.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.MinimumLevel.ToString())
        });
    }

    /// <summary>
    /// Изменить уровень логирования для root или конкретной категории.
    /// </summary>
    /// <param name="request">Тело запроса с категорией (null = root) и новым уровнем.</param>
    /// <returns>200 при успехе, 400 при валидационной ошибке, 404 если категория не найдена.</returns>
    /// <response code="200">Уровень успешно изменён.</response>
    /// <response code="400">Невалидный запрос (неверный уровень, пустое поле).</response>
    /// <response code="401">Неавторизован.</response>
    /// <response code="403">Запрещено (нет роли Admin).</response>
    /// <response code="404">Категория не найдена в overrides.</response>
    /// <remarks>
    /// Доступно только по политике <c>AdminOnly</c>.
    /// Уровни <c>Fatal</c> и <c>Verbose</c> запрещены бизнес-правилом.
    /// Все изменения логируются для аудита.
    /// </remarks>
    [HttpPut("level")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult SetLevel([FromBody] SetLogLevelRequest request)
    {
        var validationResult = SetLogLevelValidator.Validate(request);
        if (!validationResult.IsSuccess)
            return validationResult.ToActionResult();

        var level = Enum.Parse<LogEventLevel>(request.Level, true);

        if (ForbiddenLevels.Contains(level))
            return Result.Failure(new BusinessRuleError($"Level {level} is forbidden"))
                .ToActionResult();

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            _rootSwitch.MinimumLevel = level;
            _logger.LogWarning("Root log level changed to {Level} by {User}",
                level, User.Identity?.Name ?? "Unknown");
        }
        else
        {
            if (!_overrideSwitches.TryGetValue(request.Category, out var sw))
                return Result.Failure(new NotFoundError("Category", request.Category))
                    .ToActionResult();

            sw.MinimumLevel = level;
            _logger.LogWarning("Log level for {Category} changed to {Level} by {User}",
                request.Category, level, User.Identity?.Name ?? "Unknown");
        }

        return Result.Success().ToActionResult();
    }

    /// <summary>
    /// Получить список категорий, для которых настроены overrides.
    /// </summary>
    /// <returns>Массив строк с именами категорий.</returns>
    /// <response code="200">Успешно.</response>
    /// <response code="401">Неавторизован.</response>
    /// <response code="403">Запрещено (нет роли Admin/Operator/Auditor).</response>
    /// <remarks>
    /// Доступно по политике <c>AuditViewer</c>: Admin, Operator, Auditor.
    /// </remarks>
    [HttpGet("categories")]
    [Authorize(Policy = "AuditViewer")]
    public IActionResult GetCategories()
    {
        return Ok(_overrideSwitches.Keys);
    }
}