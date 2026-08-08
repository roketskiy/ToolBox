using System.Threading;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ToolBox;

public partial class App : Application
{
    Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(false, @"Local\ToolBox", out bool createdNew);
        // createdNew=false 且 WaitOne(0) 成功 = 旧实例已异常退出（mutex 被 abandoned），接管继续运行
        if (!createdNew && !_mutex.WaitOne(0))
        {
            MessageBox.Show("ToolBox 已在运行。", "ToolBox", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }
}
