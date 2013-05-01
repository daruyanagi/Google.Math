using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Google.Math.ViewModels
{
    public static class ApplicationCommands
    {
        public static readonly RoutedUICommand Detail = new RoutedUICommand(
            "詳細表示", "Detail", typeof(ApplicationCommands));
    }
}
