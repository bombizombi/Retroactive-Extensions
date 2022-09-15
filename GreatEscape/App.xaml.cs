using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GreatEscape
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        //override OnStartup
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //app init
            //ExecLogState.Initialize_Reg_ReadersWriters();


            MainWindow window = new MainWindow();

            window.DataContext = new MainWindowViewModel();
            window.Show();
        }

    }
}
