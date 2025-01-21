using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.Extensions
{
    [ContentProperty(nameof(RowDefinitionString))]
    public class GridRowDefinitionExtension : IMarkupExtension
    {
        public string RowDefinitionString { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(RowDefinitionString))
                return null;

            var rowDefinitions = new RowDefinitionCollection();
            var heights = RowDefinitionString.Split(',');

            foreach (var height in heights)
            {
                var trimmedHeight = height.Trim();
                if (double.TryParse(trimmedHeight.TrimEnd('*'), out var value))
                {
                    var isStar = trimmedHeight.EndsWith("*");
                    rowDefinitions.Add(new RowDefinition
                    {
                        Height = isStar ? new GridLength(value, GridUnitType.Star) : new GridLength(value, GridUnitType.Absolute)
                    });
                }
            }

            return rowDefinitions;
        }
    }
}
