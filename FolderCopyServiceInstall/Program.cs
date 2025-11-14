using System;
using System.Security.Principal;
using System.Windows.Forms;

namespace FolderCopyServiceInstall
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            if (!IsRunningAsAdmin())
            {
                MessageBox.Show(
                    "Este instalador precisa ser executado como Administrador.\n\n" +
                    "Clique com o botão direito no executável e escolha 'Executar como administrador'.",
                    "Permissão insuficiente",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
