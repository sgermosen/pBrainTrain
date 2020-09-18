using Plugin.Settings;
using Plugin.Settings.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jmo.Common.Helpers
{
  public static  class Settings
    {
        private const string _questions = "question";
        private static readonly string _stringDefault = string.Empty;

        private static ISettings AppSettings => CrossSettings.Current;

        public static string Question //we cant save objets, but we serialize to save it as string and deserialize to recover it
        {
            get => AppSettings.GetValueOrDefault(_questions, _stringDefault);
            set => AppSettings.AddOrUpdateValue(_questions, value);
        }
        public static string Category { get; set; }
    }
}
