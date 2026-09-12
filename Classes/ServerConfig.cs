using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sscs2023.Classes
{
    public class ServerConfig
    {
        public static object Bracket => new List<object>(); 
        public static string BaseURL = "https://reloxa.xyz";
        public static int GameVersion = 20230406;
        public static bool isDormPrivate = true; // doesn't work.
    }
}
