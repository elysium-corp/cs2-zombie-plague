using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Results;

namespace Menu.Core.Validation;

internal static class MenuContractValidator
{
    private const int MaximumTextLength = 2_048;
    private const int MaximumPermissions = 64;
    private const int MaximumTranslations = 64;
    // Shared with the Flute payload validator. Keeping this exact prevents a
    // CMS-valid explicit audience from being rejected only after activation.
    private const int MaximumExplicitTargets = 256;

    public static void ValidateAccessPolicy(
        MenuAccessPolicyDefinition? policy,
        bool allowInherited,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (policy is null)
        {
            AddError(issues, "access.required", "Access policy is required.", path);
            return;
        }

        if (policy.Permissions is null)
        {
            AddError(issues, "access.permissions_required", "Permissions collection cannot be null.", $"{path}.permissions");
            return;
        }

        var permissions = policy.Permissions;
        if (permissions.Count > MaximumPermissions)
        {
            AddError(issues, "access.permission_limit_exceeded", "Access policy contains too many permissions.", $"{path}.permissions");
            return;
        }

        if (permissions.Any(static permission => !MenuIdentifier.IsPermission(permission)))
        {
            AddError(issues, "access.permission_invalid", "Permission has an invalid format.", $"{path}.permissions");
        }

        if (permissions.Distinct(StringComparer.Ordinal).Count() != permissions.Count)
        {
            AddError(issues, "access.permission_duplicate", "Access policy contains duplicate permissions.", $"{path}.permissions");
        }

        var countIsValid = policy.Kind switch
        {
            MenuAccessPolicyKind.Public => permissions.Count == 0,
            MenuAccessPolicyKind.Permission => permissions.Count == 1,
            MenuAccessPolicyKind.AnyOf => permissions.Count >= 1,
            MenuAccessPolicyKind.AllOf => permissions.Count >= 1,
            MenuAccessPolicyKind.Inherited => allowInherited && permissions.Count == 0,
            _ => false
        };

        if (!countIsValid)
        {
            AddError(
                issues,
                "access.policy_invalid",
                "Access policy kind and permissions are inconsistent.",
                path);
        }
    }

    public static void ValidateAudience(
        MenuAudienceDefinition? audience,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (audience is null)
        {
            AddError(issues, "audience.required", "Audience is required.", path);
            return;
        }

        if (!Enum.IsDefined(audience.Kind))
        {
            AddError(issues, "audience.kind_invalid", "Audience kind is unsupported.", $"{path}.kind");
        }

        if (audience.InvokePermission is not null && !MenuIdentifier.IsPermission(audience.InvokePermission))
        {
            AddError(
                issues,
                "audience.permission_invalid",
                "Audience invoke permission has an invalid format.",
                $"{path}.invokePermission");
        }

        var explicitSteamIds = audience.ExplicitSteamIds;
        if (explicitSteamIds is null)
        {
            AddError(
                issues,
                "audience.explicit_targets_required",
                "Explicit Steam IDs collection cannot be null.",
                $"{path}.explicitSteamIds");
            return;
        }

        if (explicitSteamIds.Count > MaximumExplicitTargets)
        {
            AddError(issues, "audience.explicit_targets_limit_exceeded", "Audience contains too many explicit targets.", $"{path}.explicitSteamIds");
            return;
        }

        if (audience.Kind != MenuAudienceKind.ExplicitTargets && explicitSteamIds.Count != 0)
        {
            AddError(
                issues,
                "audience.explicit_targets_invalid",
                "Explicit Steam IDs are allowed only for explicit_targets audience.",
                $"{path}.explicitSteamIds");
        }

        if (explicitSteamIds.Count != explicitSteamIds.Distinct().Count() ||
            explicitSteamIds.Any(static steamId => steamId == 0))
        {
            AddError(
                issues,
                "audience.explicit_targets_invalid",
                "Explicit Steam IDs must be non-zero and unique.",
                $"{path}.explicitSteamIds");
        }
    }

    public static void ValidateLocalizedText(
        LocalizedText? text,
        bool required,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (text is null)
        {
            AddError(issues, "text.required", "Localized text is required.", path);
            return;
        }

        ValidateTextValue(text.Default, required, $"{path}.default", issues);
        if (text.Translations is null)
        {
            AddError(issues, "text.translations_required", "Translations object cannot be null.", $"{path}.translations");
            return;
        }

        if (text.Translations.Count > MaximumTranslations)
        {
            AddError(issues, "text.translations_limit_exceeded", "Localized text contains too many translations.", $"{path}.translations");
            return;
        }

        foreach (var (locale, value) in text.Translations)
        {
            if (!IsLocale(locale))
            {
                AddError(issues, "text.locale_invalid", "Locale key has an invalid format.", $"{path}.translations");
            }

            ValidateTextValue(value, required: false, $"{path}.translations.{locale}", issues);
        }
    }

    private static void ValidateTextValue(
        string? value,
        bool required,
        string path,
        ICollection<MenuValidationIssue> issues)
    {
        if (value is null || (required && string.IsNullOrWhiteSpace(value)))
        {
            AddError(issues, "text.value_required", "Text value is required.", path);
            return;
        }

        if (value.Length > MaximumTextLength || value.Any(static character => character == '\0'))
        {
            AddError(issues, "text.value_invalid", "Text value is too long or contains a NUL character.", path);
        }
    }

    private static bool IsLocale(string value)
    {
        if (value.Length is < 2 or > 16)
        {
            return false;
        }

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (!(character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-'))
            {
                return false;
            }
        }

        return char.IsLetter(value[0]) && char.IsLetter(value[1]);
    }

    private static void AddError(
        ICollection<MenuValidationIssue> issues,
        string code,
        string message,
        string path)
    {
        issues.Add(new MenuValidationIssue
        {
            Severity = MenuValidationSeverity.Error,
            Code = code,
            Message = message,
            Path = path
        });
    }
}
