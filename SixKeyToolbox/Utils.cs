using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SixKeyToolbox;

public static class Utils
{
	[return: NotNull]
	public static T EnsureNotNull<T>(this T? value, string? message = null, [CallerArgumentExpression(nameof(value))] string expression = "<unknown>") where T : notnull
	{
		if (value is null)
		{
			throw new ArgumentNullException(expression, message ?? "Value cannot be null.");
		}
		return value;
	}
}
