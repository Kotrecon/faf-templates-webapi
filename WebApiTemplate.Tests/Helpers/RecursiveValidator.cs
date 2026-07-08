using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace WebApiTemplate.Tests.Helpers;

/// <summary>
/// Рекурсивный валидатор для DataAnnotations.
/// Стандартный Validator.TryValidateObject НЕ валидирует вложенные объекты,
/// поэтому нужен этот helper для тестов.
/// </summary>
public static class RecursiveValidator
{
    public static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        ValidateRecursive(instance, results, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return results;
    }

    private static void ValidateRecursive(
        object instance,
        List<ValidationResult> results,
        HashSet<object> visited)
    {
        if (instance is null || !visited.Add(instance))
            return;

        var context = new ValidationContext(instance);
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);

        // Рекурсия по свойствам-объектам
        foreach (var prop in instance.GetType().GetProperties())
        {
            if (!prop.CanRead) continue;

            var value = prop.GetValue(instance);
            if (value is null) continue;

            // Пропускаем примитивы и строки
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is DateTime)
                continue;

            // Коллекции — валидируем каждый элемент
            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is not null)
                        ValidateRecursive(item, results, visited);
                }
            }
            else
            {
                // Обычный вложенный объект
                ValidateRecursive(value, results, visited);
            }
        }
    }
}