using System.Threading;
using System.Windows.Forms;

namespace DropPost;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "DropPost_SingleInstance", out var isNew);
        if (!isNew) return;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }
}
