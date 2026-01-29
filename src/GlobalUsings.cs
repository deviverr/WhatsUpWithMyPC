// Global using statements to avoid namespace conflicts between WPF and WinForms
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.IO;
global using System.Diagnostics;

// Explicitly use WPF types when there are conflicts
global using Application = System.Windows.Application;
global using MessageBox = System.Windows.MessageBox;
global using MessageBoxButton = System.Windows.MessageBoxButton;
global using MessageBoxImage = System.Windows.MessageBoxImage;
global using MessageBoxResult = System.Windows.MessageBoxResult;
global using Window = System.Windows.Window;
global using WindowState = System.Windows.WindowState;
