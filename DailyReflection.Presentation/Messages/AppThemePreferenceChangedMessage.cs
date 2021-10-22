using DailyReflection.Presentation.Entities;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Presentation.Messages
{
    public class AppThemePreferenceChangedMessage : ValueChangedMessage<AppThemePreference>
    {
        public AppThemePreferenceChangedMessage(AppThemePreference value) : base(value)
        {
        }
    }
}
