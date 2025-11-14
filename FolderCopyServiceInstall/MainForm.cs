using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FolderCopyServiceInstall
{
    public class MainForm : Form
    {
        private TextBox txtServiceName;
        private TextBox txtExePath;
        private Button btnBrowse;
        private Button btnInstall;
        private Button btnUninstall;
        private Button btnStart;
        private Button btnStop;
        private Button btnStatus;
        private Label lblStatus;

        public MainForm()
        {
            InitializeComponents();
            InitializeOnLoad();
        }

        private void InitializeComponents()
        {
            Text = "Instalador / Gerenciador - FolderCopyService";
            Width = 650;
            Height = 260;
            StartPosition = FormStartPosition.CenterScreen;

            var lblServiceName = new Label
            {
                Text = "Nome do serviço:",
                Left = 10,
                Top = 20,
                AutoSize = true
            };

            txtServiceName = new TextBox
            {
                Left = 130,
                Top = 15,
                Width = 480,
                Text = "FolderCopyService" // mesmo nome do UseWindowsService
            };

            var lblExePath = new Label
            {
                Text = "Caminho do EXE:",
                Left = 10,
                Top = 60,
                AutoSize = true
            };

            txtExePath = new TextBox
            {
                Left = 130,
                Top = 55,
                Width = 400
            };

            btnBrowse = new Button
            {
                Text = "...",
                Left = 540,
                Top = 53,
                Width = 70
            };
            btnBrowse.Click += BtnBrowse_Click;

            btnInstall = new Button
            {
                Text = "Instalar serviço",
                Left = 10,
                Top = 100,
                Width = 150
            };
            btnInstall.Click += BtnInstall_Click;

            btnUninstall = new Button
            {
                Text = "Desinstalar serviço",
                Left = 170,
                Top = 100,
                Width = 150
            };
            btnUninstall.Click += BtnUninstall_Click;

            btnStart = new Button
            {
                Text = "Iniciar serviço",
                Left = 330,
                Top = 100,
                Width = 140
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "Parar serviço",
                Left = 480,
                Top = 100,
                Width = 130
            };
            btnStop.Click += BtnStop_Click;

            btnStatus = new Button
            {
                Text = "Atualizar status",
                Left = 10,
                Top = 140,
                Width = 150
            };
            btnStatus.Click += BtnStatus_Click;

            lblStatus = new Label
            {
                Text = "Status: (desconhecido)",
                Left = 10,
                Top = 180,
                AutoSize = true
            };

            Controls.Add(lblServiceName);
            Controls.Add(txtServiceName);
            Controls.Add(lblExePath);
            Controls.Add(txtExePath);
            Controls.Add(btnBrowse);
            Controls.Add(btnInstall);
            Controls.Add(btnUninstall);
            Controls.Add(btnStart);
            Controls.Add(btnStop);
            Controls.Add(btnStatus);
            Controls.Add(lblStatus);
        }

        #region Inicialização

        private void InitializeOnLoad()
        {
            // 1) Garante que a pasta de instalação existe
            EnsureInstallFolder();

            // 2) Copia o próprio installer para a pasta de instalação
            CopyInstallerToInstallFolder();

            string serviceName = txtServiceName.Text.Trim();

            if (ServiceExists(serviceName))
            {
                // Serviço já instalado: tenta ler o caminho do registro
                string? imagePath = TryGetServiceImagePath(serviceName);
                if (!string.IsNullOrWhiteSpace(imagePath))
                {
                    string exePath = ExtractExePathFromImagePath(imagePath);
                    if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
                    {
                        txtExePath.Text = exePath;
                    }
                }

                UpdateStatus();
            }
            else
            {
                // Serviço não existe ainda:
                // tenta sugerir o EXE na mesma pasta do instalador (pendrive)
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string candidate = Path.Combine(baseDir, "FolderCopyService.exe");

                if (File.Exists(candidate))
                {
                    txtExePath.Text = candidate;
                }
            }
        }

        #endregion

        #region Eventos dos botões

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Executável (*.exe)|*.exe|Todos os arquivos (*.*)|*.*",
                Title = "Selecione o executável do serviço (FolderCopyService.exe)"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtExePath.Text = ofd.FileName;
            }
        }

        private void BtnInstall_Click(object? sender, EventArgs e)
        {
            string serviceName = txtServiceName.Text.Trim();
            string exePathOriginal = txtExePath.Text.Trim();

            if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(exePathOriginal))
            {
                MessageBox.Show("Informe o nome do serviço e o caminho do EXE.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(exePathOriginal))
            {
                MessageBox.Show("Arquivo EXE não encontrado.", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (ServiceExists(serviceName))
            {
                MessageBox.Show("O serviço já existe.\n\nSe quiser atualizar os arquivos, desinstale e instale novamente.",
                    "Informação",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 1) Copiar todos os arquivos da pasta de origem para a pasta de instalação
            string sourceDir = Path.GetDirectoryName(exePathOriginal)!;
            string installDir = GetInstallFolder();

            try
            {
                CopyDirectoryRecursive(sourceDir, installDir);

                // Caminho que o serviço vai usar (no Program Files)
                string serviceExePath = Path.Combine(installDir, Path.GetFileName(exePathOriginal));

                // 2) Criar o serviço apontando para o EXE na pasta de instalação
                string args = $"create \"{serviceName}\" binPath= \"{serviceExePath}\" start= auto";

                if (RunSc(args, out string output, out string error))
                {
                    MessageBox.Show("Serviço instalado com sucesso.\n\n" + output, "Sucesso",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Falha ao instalar o serviço.\n\n" + error, "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao copiar arquivos para a pasta de instalação:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateStatus();
        }

        private void BtnUninstall_Click(object? sender, EventArgs e)
        {
            string serviceName = txtServiceName.Text.Trim();

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                MessageBox.Show("Informe o nome do serviço.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ServiceExists(serviceName))
            {
                MessageBox.Show("O serviço não existe.", "Informação",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Para o serviço antes de deletar
            StopServiceIfRunning(serviceName);

            string args = $"delete \"{serviceName}\"";

            if (RunSc(args, out string output, out string error))
            {
                MessageBox.Show("Serviço desinstalado com sucesso.\n\n" + output, "Sucesso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Falha ao desinstalar o serviço.\n\n" + error, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateStatus();
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            string serviceName = txtServiceName.Text.Trim();

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                MessageBox.Show("Informe o nome do serviço.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var sc = new ServiceController(serviceName);

                if (sc.Status == ServiceControllerStatus.Running)
                {
                    MessageBox.Show("O serviço já está em execução.", "Informação",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                MessageBox.Show("Serviço iniciado com sucesso.", "Sucesso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar o serviço:\n\n" + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateStatus();
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            string serviceName = txtServiceName.Text.Trim();

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                MessageBox.Show("Informe o nome do serviço.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var sc = new ServiceController(serviceName);

                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    MessageBox.Show("O serviço já está parado.", "Informação",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

                MessageBox.Show("Serviço parado com sucesso.", "Sucesso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao parar o serviço:\n\n" + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateStatus();
        }

        private void BtnStatus_Click(object? sender, EventArgs e)
        {
            UpdateStatus();
        }

        #endregion

        #region Métodos auxiliares (instalação / pasta / registro)

        private string GetInstallFolder()
        {
            // Instala em C:\Program Files\FolderCopyService
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Path.Combine(programFiles, "FolderCopyService");

            // Se preferir C:\FolderCopyService, troque por:
            // return @"C:\FolderCopyService";
        }

        private void EnsureInstallFolder()
        {
            string installDir = GetInstallFolder();
            try
            {
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Não foi possível criar a pasta de instalação '{installDir}'.\n\n{ex.Message}",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Copia o próprio instalador para a pasta de instalação,
        /// para que o usuário possa usá-lo depois sem pendrive.
        /// </summary>
        private void CopyInstallerToInstallFolder()
        {
            try
            {
                string installDir = GetInstallFolder();

                // Caminho do EXE atual (onde o instalador está rodando – pendrive, por exemplo)
                string currentExe = Application.ExecutablePath;

                // Nome fixo dentro da pasta de instalação
                string destExe = Path.Combine(installDir, "FolderCopyServiceInstall.exe");

                if (!File.Exists(destExe))
                {
                    File.Copy(currentExe, destExe, overwrite: false);
                }
                // Se quiser SEMPRE atualizar, use:
                // File.Copy(currentExe, destExe, overwrite: true);
            }
            catch
            {
                // Se der erro, não quebra o app – é só um "extra"
            }
        }

        private void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            sourceDir = Path.GetFullPath(sourceDir);
            destDir = Path.GetFullPath(destDir);

            if (string.Equals(sourceDir, destDir, StringComparison.OrdinalIgnoreCase))
            {
                // Já está no lugar, não precisa copiar
                return;
            }

            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"Diretório de origem não encontrado: {sourceDir}");
            }

            Directory.CreateDirectory(destDir);

            // Copia arquivos
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            // Copia subpastas
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectoryRecursive(dir, destSubDir);
            }
        }

        private string? TryGetServiceImagePath(string serviceName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\{serviceName}");

                if (key == null)
                    return null;

                return key.GetValue("ImagePath") as string;
            }
            catch
            {
                return null;
            }
        }

        private string ExtractExePathFromImagePath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return string.Empty;

            imagePath = imagePath.Trim();

            if (imagePath.StartsWith("\""))
            {
                int secondQuote = imagePath.IndexOf('"', 1);
                if (secondQuote > 1)
                {
                    return imagePath.Substring(1, secondQuote - 1);
                }
            }

            int firstSpace = imagePath.IndexOf(' ');
            if (firstSpace > 0)
            {
                return imagePath.Substring(0, firstSpace);
            }

            return imagePath;
        }

        #endregion

        #region Métodos auxiliares (status / sc / serviço)

        private void UpdateStatus()
        {
            string serviceName = txtServiceName.Text.Trim();

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                lblStatus.Text = "Status: (informe o nome do serviço)";
                return;
            }

            if (!ServiceExists(serviceName))
            {
                lblStatus.Text = "Status: serviço não encontrado";
                return;
            }

            try
            {
                using var sc = new ServiceController(serviceName);
                lblStatus.Text = $"Status: {sc.Status}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Status: erro ao consultar ({ex.Message})";
            }
        }

        private bool ServiceExists(string serviceName)
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                foreach (var s in services)
                {
                    if (string.Equals(s.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void StopServiceIfRunning(string serviceName)
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped &&
                    sc.Status != ServiceControllerStatus.StopPending)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }
            catch
            {
                // ignora, vamos tentar deletar mesmo assim
            }
        }

        private bool RunSc(string arguments, out string output, out string error)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            try
            {
                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    output = "";
                    error = "Não foi possível iniciar o processo 'sc.exe'.";
                    return false;
                }

                sbOut.Append(proc.StandardOutput.ReadToEnd());
                sbErr.Append(proc.StandardError.ReadToEnd());

                proc.WaitForExit();

                output = sbOut.ToString();
                error = sbErr.ToString();

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                output = sbOut.ToString();
                error = "Exceção ao executar 'sc.exe': " + ex.Message;
                return false;
            }
        }

        #endregion
    }
}
