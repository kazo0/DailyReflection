using System;

namespace DailyReflection.Core.Entities;

/// <summary>The sober date selected in Settings; null when the user has never picked one.</summary>
public record SoberDateSelection(DateTime? Date);
