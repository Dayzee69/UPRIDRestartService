using System;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using Oracle.ManagedDataAccess.Client;
using System.Timers;

namespace UPRIDRestartService
{
    public partial class UPRIDRestartService : ServiceBase
    {
        public UPRIDRestartService()
        {
            InitializeComponent();
        }

        int count = 0;
        OracleConnect oracleConnect;

        protected override void OnStart(string[] args)
        {
            Logger.InitLogger();
            Logger.Log.Info("Начало работы службы");
            try
            {

                string strPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                INIManager manager = new INIManager(strPath + @"\settings.ini");

                oracleConnect = new OracleConnect(manager.GetPrivateString("Oracle", "host"), manager.GetPrivateString("Oracle", "port"),
                manager.GetPrivateString("Oracle", "database"), manager.GetPrivateString("Oracle", "user"), manager.GetPrivateString("Oracle", "password"),
                manager.GetPrivateString("Settings", "rowCount"), manager.GetPrivateString("Settings", "timeout"), 
                manager.GetPrivateString("Settings", "serviceName"), manager.GetPrivateString("Settings", "processName"));

                count = GetCount();

                Timer T2 = new Timer();
                T2.Interval = Double.Parse(manager.GetPrivateString("Settings", "interval"));
                T2.AutoReset = true;
                T2.Enabled = true;
                T2.Start();
                T2.Elapsed += new ElapsedEventHandler(T2_Elapsed);
                
            }
            catch (Exception ex) 
            {
                Logger.Log.Error(ex.ToString());
            }

        }

        private int GetCount() 
        {
            OracleConnection oracleConnection = oracleConnect.oracleConnection;
            try
            {
                oracleConnection.Open();

                OracleCommand oracleCommand = new OracleCommand("SELECT COUNT(*) FROM DUPRID_DBT");

                oracleCommand.Connection = oracleConnection;
                oracleCommand.CommandType = CommandType.Text;

                OracleDataReader reader = oracleCommand.ExecuteReader();

                int rowCount = 0;

                while (reader.Read())
                {
                    rowCount = int.Parse(reader.GetValue(0).ToString());
                }

                reader.Dispose();
                oracleCommand.Dispose();

                return rowCount;
            }
            catch (Exception ex)
            {
                oracleConnection.Close();

                Logger.Log.Error(ex.ToString());

                return 0;
            }
            finally
            {
                oracleConnection.Close();
            }
        }

        private bool CheckStatus() 
        {
            OracleConnection oracleConnection = oracleConnect.oracleConnection;
            string rowCount = oracleConnect.rowCount;
            try
            {
                oracleConnection.Open();

                OracleCommand oracleCommand = new OracleCommand($"SELECT T_STATUS FROM DUPRID_DBT ORDER BY T_OBJECTID DESC FETCH NEXT {rowCount} ROWS ONLY");

                oracleCommand.Connection = oracleConnection;
                oracleCommand.CommandType = CommandType.Text;

                OracleDataReader reader = oracleCommand.ExecuteReader();

                bool statusValue = false;

                while (reader.Read())
                {
                    if (reader.GetValue(0).ToString() == "VALID" || reader.GetValue(0).ToString() == "INVALID") 
                    {
                        statusValue = true;
                        break;
                    }

                }

                reader.Dispose();
                oracleCommand.Dispose();

                return statusValue;

            }
            catch (Exception ex)
            {
                oracleConnection.Close();

                Logger.Log.Error(ex.ToString());

                return false;
            }
            finally
            {
                oracleConnection.Close();
            }
        }

        private void RestartService()
        {
            try
            {
                string serviceName = oracleConnect.serviceName;
                string processName = oracleConnect.processName;
                string timeoutMilliseconds = oracleConnect.timeOut;

                foreach (Process proc in Process.GetProcessesByName(processName))
                {
                    proc.Kill();
                }

                TimeSpan timeout = TimeSpan.FromMilliseconds(int.Parse(timeoutMilliseconds));
                ServiceController service = new ServiceController(serviceName);

                System.Threading.Thread.Sleep(int.Parse(timeoutMilliseconds));

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.ToString());
            }
            finally
            {
                Logger.Log.Info("Сервис перезапущен");
            }
        }

        private void T2_Elapsed(object sender, EventArgs e)
        {
            try
            {
                if (GetCount() - count < int.Parse(oracleConnect.rowCount)) //|| !CheckStatus())
                {
                    RestartService();
                }

            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.ToString());
            }
            finally 
            {
                count = GetCount();
            }
        }

        

        protected override void OnStop()
        {
            oracleConnect.oracleConnection.Close();
            oracleConnect.oracleConnection.Dispose();
            Logger.Log.Info("Окончание работы");
        }
    }
}
