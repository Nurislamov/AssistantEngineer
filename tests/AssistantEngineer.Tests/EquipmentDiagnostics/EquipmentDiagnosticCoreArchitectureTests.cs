using System.Reflection;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Diagnostics;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticCoreArchitectureTests
{
    [Fact]
    public void DiagnosticsCoreTypesHaveNoTelegramDependenciesOrTransportProperties()
    {
        var assembly = typeof(IEquipmentDiagnosticCore).Assembly;
        var coreTypes = assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(
                "AssistantEngineer.Modules.EquipmentDiagnostics.Application.Diagnostics",
                StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(coreTypes);
        Assert.DoesNotContain(coreTypes, type =>
            ReferencesTelegram(type.BaseType) ||
            type.GetInterfaces().Any(ReferencesTelegram) ||
            type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters())
                .Any(parameter => ReferencesTelegram(parameter.ParameterType)) ||
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(property => ReferencesTelegram(property.PropertyType)));

        var forbiddenPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "ChatId",
            "MessageId",
            "CallbackData",
            "ReplyMarkup",
            "InlineKeyboard",
            "FileId",
            "TelegramUserRole"
        };
        Assert.DoesNotContain(
            coreTypes.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)),
            property => forbiddenPropertyNames.Contains(property.Name));
    }

    private static bool ReferencesTelegram(Type? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type.IsArray)
        {
            return ReferencesTelegram(type.GetElementType());
        }

        if (type.IsGenericType &&
            type.GetGenericArguments().Any(ReferencesTelegram))
        {
            return true;
        }

        return type.Namespace?.Contains(".Application.Telegram", StringComparison.Ordinal) == true;
    }
}
