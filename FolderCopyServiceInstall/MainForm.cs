using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FolderCopyServiceInstall
{
    public class MainForm : Form
    {
        // Campos padrão (serviço)
        private TextBox txtServiceName;
        private TextBox txtExePath;
        private Button btnBrowse;
        private Button btnInstall;
        private Button btnUninstall;
        private Button btnStart;
        private Button btnStop;
        private Button btnStatus;
        private Label lblStatus;

        // Campos de configuração (appsettings.json)
        private TextBox txtSourcePath;
        private TextBox txtTargetPath;
        private NumericUpDown numInterval;
        private Button btnLoadConfig;
        private Button btnSaveConfig;

        public MainForm()
        {
            InitializeComponents();
            InitializeOnLoad();
        }

        private void InitializeComponents()
        {
            Text = "Instalador / Gerenciador - FolderCopyService";
            Width = 720;
            Height = 480;
            StartPosition = FormStartPosition.CenterScreen;

            // ----------------------------
            // SEÇÃO DO SERVIÇO
            // ----------------------------

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
                Width = 520,
                Text = "FolderCopyService"
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
                Width = 430
            };

            btnBrowse = new Button
            {
                Text = "...",
                Left = 570,
                Top = 53,
                Width = 80
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
                Width = 150
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "Parar serviço",
                Left = 490,
                Top = 100,
                Width = 150
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

            // ----------------------------
            // SEÇÃO DE CONFIGURAÇÃO
            // ----------------------------

            var grpConfig = new GroupBox
            {
                Text = "Configurações (appsettings.json)",
                Left = 10,
                Top = 220,
                Width = 680,
                Height = 200
            };

            var lblSource = new Label
            {
                Text = "Pasta de Origem:",
                Left = 20,
                Top = 40,
                AutoSize = true
            };

            txtSourcePath = new TextBox
            {
                Left = 150,
                Top = 35,
                Width = 480
            };

            var lblTarget = new Label
            {
                Text = "Pasta de Destino:",
                Left = 20,
                Top = 80,
                AutoSize = true
            };

            txtTargetPath = new TextBox
            {
                Left = 150,
                Top = 75,
                Width = 480
            };

            var lblInterval = new Label
            {
                Text = "Intervalo (minutos):",
                Left = 20,
                Top = 120,
                AutoSize = true
            };

            numInterval = new NumericUpDown
            {
                Left = 150,
                Top = 115,
                Width = 100,
                Minimum = 1,
                Maximum = 1440,
                Value = 20
            };

            btnLoadConfig = new Button
            {
                Text = "Carregar Config",
                Left = 270,
                Top = 155,
                Width = 150
            };
            btnLoadConfig.Click += BtnLoadConfig_Click;

            btnSaveConfig = new Button
            {
                Text = "Salvar / Aplicar",
                Left = 430,
                Top = 155,
                Width = 150
            };
            btnSaveConfig.Click += BtnSaveConfig_Click;

            grpConfig.Controls.Add(lblSource);
            grpConfig.Controls.Add(txtSourcePath);
            grpConfig.Controls.Add(lblTarget);
            grpConfig.Controls.Add(txtTargetPath);
            grpConfig.Controls.Add(lblInterval);
            grpConfig.Controls.Add(numInterval);
            grpConfig.Controls.Add(btnLoadConfig);
            grpConfig.Controls.Add(btnSaveConfig);

            // Adiciona os controles ao form
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
            Controls.Add(grpConfig);
        }

        private void InitializeOnLoad()
        {
            EnsureInstallFolder();
            CopyInstallerToInstallFolder();

            string serviceName = txtServiceName.Text.Trim();

            if (ServiceExists(serviceName))
            {
                string? imagePath = TryGetServiceImagePath(serviceName);
                if (imagePath != null)
                {
                    string exe = ExtractExePathFromImagePath(imagePath);
                    if (File.Exists(exe))
                    {
                        txtExePath.Text = exe;
                    }
                }

                UpdateStatus();
                LoadConfig();
            }
            else
            {
                // Se não existe serviço, tenta sugerir o EXE na mesma pasta do installer
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string candidate = Path.Combine(baseDir, "FolderCopyService.exe");

                if (File.Exists(candidate))
                {
                    txtExePath.Text = candidate;
                }
            }
        }

        #region --- BOTÕES INSTALAÇÃO/CONTROLE DO SERVIÇO ---

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Executável (*.exe)|*.exe",
                Title = "Selecione FolderCopyService.exe"
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

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                MessageBox.Show("Informe o nome do serviço.");
                return;
            }

            if (!File.Exists(exePathOriginal))
            {
                MessageBox.Show("EXE não encontrado.");
                return;
            }

            if (ServiceExists(serviceName))
            {
                MessageBox.Show("O serviço já está instalado.");
                return;
            }

            string sourceDir = Path.GetDirectoryName(exePathOriginal)!;
            string installDir = GetInstallFolder();

            try
            {
                CopyDirectoryRecursive(sourceDir, installDir);

                string installedExe = Path.Combine(installDir, "FolderCopyService.exe");
                string args = $"create \"{serviceName}\" binPath= \"{installedExe}\" start= auto";

                RunSc(args, out string output, out string error);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    MessageBox.Show("Saída do SC:\n" + error);
                }

                MessageBox.Show("Serviço instalado com sucesso!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao instalar:\n" + ex.Message);
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

                // Mesmo assim tenta apagar a pasta de instalação
                DeleteInstallFolder();
                UpdateStatus();
                return;
            }

            // Para o serviço antes de deletar
            StopService(serviceName);

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

            // Depois de remover o serviço, apaga toda a pasta de instalação
            DeleteInstallFolder();

            UpdateStatus();
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            string name = txtServiceName.Text.Trim();
            StartService(name);
            UpdateStatus();
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            string name = txtServiceName.Text.Trim();
            StopService(name);
            UpdateStatus();
        }

        private void BtnStatus_Click(object? sender, EventArgs e)
        {
            UpdateStatus();
        }

        #endregion

        #region --- CONFIGURAÇÃO DO APPSETTINGS ---

        private string GetConfigPath()
        {
            return Path.Combine(GetInstallFolder(), "appsettings.json");
        }

        private void BtnLoadConfig_Click(object? sender, EventArgs e)
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            string file = GetConfigPath();

            if (!File.Exists(file))
            {
                // Se não existir, não reclama alto, só deixa em branco.
                return;
            }

            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("BackupOptions", out var backupOptions))
                    return;

                if (backupOptions.TryGetProperty("SourcePath", out var srcProp))
                    txtSourcePath.Text = srcProp.GetString() ?? string.Empty;

                if (backupOptions.TryGetProperty("TargetPath", out var tgtProp))
                    txtTargetPath.Text = tgtProp.GetString() ?? string.Empty;

                if (backupOptions.TryGetProperty("IntervalMinutes", out var intProp))
                {
                    int val = intProp.GetInt32();
                    if (val < numInterval.Minimum) val = (int)numInterval.Minimum;
                    if (val > numInterval.Maximum) val = (int)numInterval.Maximum;
                    numInterval.Value = val;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar config:\n" + ex,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnSaveConfig_Click(object? sender, EventArgs e)
        {
            SaveConfig();
        }

        private void SaveConfig()
        {
            string file = GetConfigPath();

            try
            {
                var json = new
                {
                    BackupOptions = new
                    {
                        SourcePath = txtSourcePath.Text,
                        TargetPath = txtTargetPath.Text,
                        IntervalMinutes = (int)numInterval.Value
                    }
                };

                string output = JsonSerializer.Serialize(json, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(file, output, Encoding.UTF8);

                MessageBox.Show("Configurações salvas!", "Sucesso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reinicia o serviço automaticamente pra aplicar as mudanças
                RestartService(txtServiceName.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar config:\n" + ex,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #endregion

        #region --- CONTROLE DE SERVIÇOS ---

        private void StartService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return;

            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Running &&
                    sc.Status != ServiceControllerStatus.StartPending)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar serviço:\n" + ex,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void StopService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return;

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
                // Ignora erro pra não travar a UX
            }
        }

        private void RestartService(string serviceName)
        {
            if (!ServiceExists(serviceName))
                return;

            StopService(serviceName);
            StartService(serviceName);
        }

        private void UpdateStatus()
        {
            string name = txtServiceName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                lblStatus.Text = "Status: (informe o nome do serviço)";
                return;
            }

            if (!ServiceExists(name))
            {
                lblStatus.Text = "Status: Não instalado";
                return;
            }

            try
            {
                using var sc = new ServiceController(name);
                lblStatus.Text = $"Status: {sc.Status}";
            }
            catch
            {
                lblStatus.Text = "Status: Erro ao consultar";
            }
        }

        private bool ServiceExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            try
            {
                foreach (var s in ServiceController.GetServices())
                    if (s.ServiceName.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            catch
            {
                // Se der erro, assume que não
            }

            return false;
        }

        #endregion

        #region --- SC (instalação) ---

        private bool RunSc(string arguments, out string output, out string error)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null)
            {
                output = "";
                error = "Não foi possível iniciar sc.exe.";
                return false;
            }

            output = p.StandardOutput.ReadToEnd();
            error = p.StandardError.ReadToEnd();

            p.WaitForExit();

            return p.ExitCode == 0;
        }

        #endregion

        #region --- ARQUIVOS & DIRETÓRIOS ---

        private string GetInstallFolder()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Path.Combine(programFiles, "FolderCopyService");
        }

        private void EnsureInstallFolder()
        {
            var dir = GetInstallFolder();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private void CopyInstallerToInstallFolder()
        {
            try
            {
                string installDir = GetInstallFolder();
                string current = Application.ExecutablePath;
                string dest = Path.Combine(installDir, "FolderCopyServiceInstall.exe");

                if (!File.Exists(dest))
                    File.Copy(current, dest, overwrite: false);
            }
            catch
            {
                // Se falhar, não é crítico
            }
        }

        private void CopyDirectoryRecursive(string from, string to)
        {
            Directory.CreateDirectory(to);

            foreach (string file in Directory.GetFiles(from))
            {
                string dest = Path.Combine(to, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
            }

            foreach (string dir in Directory.GetDirectories(from))
            {
                string dest = Path.Combine(to, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, dest);
            }
        }

        /// <summary>
        /// Apaga toda a pasta de instalação em Program Files.
        /// </summary>
        private void DeleteInstallFolder()
        {
            try
            {
                string installDir = GetInstallFolder();

                if (!Directory.Exists(installDir))
                    return;

                Directory.Delete(installDir, recursive: true);

                MessageBox.Show(
                    $"Pasta de instalação removida:\n{installDir}",
                    "Limpeza concluída",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível remover completamente a pasta de instalação.\n\n" +
                    "Você pode apagá-la manualmente em:\n" +
                    GetInstallFolder() + "\n\n" +
                    "Detalhes:\n" + ex.Message,
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        #endregion

        #region --- REGISTRO / SERVIÇO EXISTENTE ---

        private string? TryGetServiceImagePath(string serviceName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\{serviceName}");

                return key?.GetValue("ImagePath") as string;
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
                int index = imagePath.IndexOf('"', 1);
                if (index > 1)
                    return imagePath.Substring(1, index - 1);
            }

            int space = imagePath.IndexOf(' ');
            if (space > 0)
                return imagePath.Substring(0, space);

            return imagePath;
        }

        #endregion
    }
}
