# 📁 FolderCopyService  
### Serviço Windows para Sincronização de Pastas com Agendamento e Logging (Quartz + Serilog)

Este projeto contém **dois componentes**:

1. **FolderCopyService**  
   Serviço Windows desenvolvido em **.NET 8**, responsável por sincronizar automaticamente uma pasta de origem para uma pasta destino em intervalos configuráveis.  
   Ele utiliza **Quartz** para agendamento e **Serilog** para geração de logs diários (com retenção de 7 dias).

2. **FolderCopyServiceInstall**  
   Aplicação Windows (WinForms) em .NET 8 usada para:
   - Instalar o serviço
   - Desinstalar
   - Iniciar / Parar
   - Verificar status  
   - Copiar automaticamente os arquivos do serviço para `C:\\Program Files\\FolderCopyService`
   - Copiar o próprio instalador para a mesma pasta

---

## 🧩 Arquitetura do Sistema


O serviço é executado como **Windows Service**, com suporte nativo pelo Host do .NET:

- Estrutura modular  
- Injeção de dependência  
- Logging padronizado  
- Agendador robusto (Quartz)

---

## ✨ Funcionalidades do Serviço

- 🔄 **Cópia completa** de todos os arquivos e subpastas da origem para o destino  
- 🕒 **Agendamento automático** usando Quartz (padrão: a cada *20 minutos*)  
- ⚙️ Configurações via `appsettings.json`:

```json
{
  "BackupOptions": {
    "SourcePath": "C:\\Origem",
    "TargetPath": "C:\\Destino",
    "IntervalMinutes": 20
  }
}
