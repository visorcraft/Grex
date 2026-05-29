using System;
using Grex.Models;
using Grex.Services;
using Microsoft.UI.Xaml.Data;

namespace Grex.Converters
{
    public sealed class SearchResultTooltipLinePrefixConverter : IValueConverter
    {
        private readonly ILocalizationService _localizationService = LocalizationService.Instance;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is SearchResult result)
            {
                return result.LineNumber > 0
                    ? _localizationService.GetLocalizedString("Controls.SearchTabContent.ResultsRowTooltip.LinePrefixFormat", result.LineNumber)
                    : string.Empty;
            }

            if (value is FileSearchResult fileResult)
            {
                return fileResult.FirstMatchLineNumber > 0
                    ? _localizationService.GetLocalizedString("Controls.SearchTabContent.ResultsRowTooltip.LinePrefixFormat", fileResult.FirstMatchLineNumber)
                    : string.Empty;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return string.Empty;
        }
    }
}
