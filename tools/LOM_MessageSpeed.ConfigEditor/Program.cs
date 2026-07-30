using System;
using System.Windows.Forms;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "設定エディタを開始できませんでした。\r\n\r\n" + ex.Message,
                    "LOM_MessageSpeed 設定エディタ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
