using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GreatEscape
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //create TimeSpan
            TimeSpan ts = TimeSpan.FromSeconds(0.3);


            this.DataContextChanged += (e, ev) =>
            {
                //create observable around the click event of the slider 
                var ob = Observable.FromEventPattern<RoutedEventArgs>(slidero, "ValueChanged")
                    .Select(x => x.EventArgs)
                    .Throttle(ts)
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(e => this.VM.SliderDebounced(e));
            };
           


        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            VM.Start();
            VM.DebugProp = "start clicked";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            VM.Save();   
        }



        private MainWindowViewModel VM { get => DataContext as MainWindowViewModel; }

        private void ButtonLoops_Click(object sender, RoutedEventArgs e)
        {
            if (sender is null) return;
            Button? s = sender as Button;
            if (s is null) return;
            string? vs = s.Content as string;
            if (vs is null) return;
            VM.Loop( int.Parse(vs));
            
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            VM.PreviewKeyDown(e);
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            VM.PreviewKeyUp(e);
        }


        private void ToggleSize_Click(object sender, RoutedEventArgs e)
        {
            bord.Width = 256;
            bord.Height = 192;
        }

        private void StartExec_Click(object sender, RoutedEventArgs e) => VM.StartExecLog();

        private void StopExec_Click(object sender, RoutedEventArgs e) => VM.StopExecLog();

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) 
            => VM.SliderValueChanged(e, (sender as Slider).DataContext);


        private void Dis_Click(object sender, RoutedEventArgs e)
        {
            VM.Disassemble();
        }


        private void CreateNewFromSelection_Click(object sender, RoutedEventArgs e) => VM.CreateNewFromSelection();

        private void SetSelectionStart(object sender, RoutedEventArgs e) => VM.SetSelectionStart();
        private void SetSelectionEnd(object sender, RoutedEventArgs e) => VM.SetSelectionEnd();


        private void InNextButNotSelected(object sender, RoutedEventArgs e) => VM.LogicalFunctionForGhidra();

        private void ButtonStepTests_Click(object sender, RoutedEventArgs e) => VM.StepWithTests();

    }
}
