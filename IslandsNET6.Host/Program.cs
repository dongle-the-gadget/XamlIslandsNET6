using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandsNET6.Host
{
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            Components.App xamlApp = new();
            App app = new();
            app.InitializeComponent();
            app.Run();
        }
    }
}
